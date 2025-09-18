using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Azurin.Player;
using Voxel.Jobs;

namespace Voxel
{
    /// <summary>
    /// Система оптимізації генерації чанків з мультипроцесністю та кешуванням
    /// </summary>
    public class ChunkOptimizer : MonoBehaviour
    {
        [Header("Оптимізація")]
        [SerializeField] private bool enableMultithreading = true;
        [SerializeField] private bool enableCaching = true;
        [SerializeField] private int maxConcurrentJobs = 4;
        [SerializeField] private int cacheSize = 100;
        
        [Header("Продуктивність")]
        [SerializeField] private int chunksPerFrame = 2;
        [SerializeField] private float targetFrameTime = 16.67f;
        
        [Header("Налаштування LOD")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float[] lodDistances = { 50f, 100f, 150f };
        [SerializeField] private int[] lodResolutions = { 16, 8, 4 };
        
        [Header("Налаштування продуктивності")]
        [SerializeField] private float unloadDistance = 200f;
        [SerializeField] private bool adaptiveGeneration = true;
        
        // Кеш чанків
        private Dictionary<Vector3Int, TerrainChunkV2> chunkCache;
        private Dictionary<Vector3Int, ChunkData> serializedChunkCache;
        private Queue<Vector3Int> chunkLRU;
        
        // Черга генерації
        private Queue<ChunkGenerationJob> generationQueue;
        private List<Task> activeTasks;
        private CancellationTokenSource cancellationTokenSource;
        
        // Статистика
        private float lastFrameTime;
        private int chunksGeneratedThisFrame;
        private int totalChunksGenerated;
        private int cacheHits;
        private int cacheMisses;
        
        // Компоненти
        private VoxelTerrain voxelTerrain;
        private Transform playerTransform;
        
        [System.Serializable]
        public struct ChunkData
        {
            public Vector3Int position;
            public byte[] voxelData;
            public float timestamp;
            public int lodLevel;
        }
        
        public struct ChunkGenerationJob
        {
            public Vector3Int position;
            public float priority;
        }
        
        void Awake()
        {
            InitializeOptimizer();
        }
        
        void Start()
        {
            chunkCache = new Dictionary<Vector3Int, TerrainChunkV2>();
            generationQueue = new Queue<ChunkGenerationJob>();
            voxelTerrain = GetComponent<VoxelTerrain>();
            
            // Знаходимо гравця
            var player = FindFirstObjectByType<CossackPlayer>();
            if (player != null)
                playerTransform = player.transform;
            
            Debug.Log($"🚀 ChunkOptimizer ініціалізовано: {maxConcurrentJobs} потоків");
        }
        
        void Update()
        {
            UpdateFrameTime();
            ProcessGenerationQueue();
            UpdateLOD();
            CleanupDistantChunks();
            UpdateStatistics();
        }
        
        void OnDestroy()
        {
            Cleanup();
        }
        
        void InitializeOptimizer()
        {
            serializedChunkCache = new Dictionary<Vector3Int, ChunkData>();
            chunkLRU = new Queue<Vector3Int>();
            activeTasks = new List<Task>();
            cancellationTokenSource = new CancellationTokenSource();
            
            // Налаштовуємо максимальну кількість потоків
            if (maxConcurrentJobs <= 0)
                maxConcurrentJobs = Mathf.Max(1, System.Environment.ProcessorCount - 1);
                
            Debug.Log($"🚀 ChunkOptimizer ініціалізовано: {maxConcurrentJobs} потоків, кеш {cacheSize} чанків");
        }
        
        void UpdateFrameTime()
        {
            lastFrameTime = Time.unscaledDeltaTime * 1000f; // в мілісекундах
            chunksGeneratedThisFrame = 0;
        }
        
        void ProcessGenerationQueue()
        {
            int processed = 0;
            while (generationQueue.Count > 0 && processed < chunksPerFrame)
            {
                var job = generationQueue.Dequeue();
                
                if (enableMultithreading)
                {
                    ProcessChunkJobParallel(job);
                }
                else
                {
                    ProcessChunkJob(job);
                }
                
                processed++;
            }
        }
        
        void ProcessChunkJobParallel(ChunkGenerationJob job)
        {
            if (voxelTerrain == null) return;
            
            int dataSize = voxelTerrain.chunkSize + 1;
            int totalVoxels = dataSize * dataSize * dataSize;
            
            // Алокуємо native масиви
            NativeArray<float> densityArray = new NativeArray<float>(totalVoxels, Allocator.TempJob);
            NativeArray<byte> voxelTypes = new NativeArray<byte>(totalVoxels, Allocator.TempJob);
            
            // Створюємо job для генерації щільності
            var densityJob = new ChunkDensityJob
            {
                chunkWorldPos = new float3(
                    job.position.x * voxelTerrain.chunkSize * voxelTerrain.voxelSize,
                    job.position.y * voxelTerrain.chunkSize * voxelTerrain.voxelSize,
                    job.position.z * voxelTerrain.chunkSize * voxelTerrain.voxelSize
                ),
                chunkSize = voxelTerrain.chunkSize,
                voxelSize = voxelTerrain.voxelSize,
                noiseScale = voxelTerrain.noiseScale,
                heightScale = voxelTerrain.heightScale,
                groundLevel = voxelTerrain.groundLevel,
                seed = voxelTerrain.seed,
                dataSize = dataSize,
                densityArray = densityArray
            };
            
            // Запускаємо job
            JobHandle densityHandle = densityJob.Schedule(totalVoxels, 64);
            
            // Job для типів вокселів
            var typeJob = new ChunkVoxelTypeJob
            {
                densityArray = densityArray,
                chunkWorldPos = densityJob.chunkWorldPos,
                dataSize = dataSize,
                voxelSize = voxelTerrain.voxelSize,
                voxelTypes = voxelTypes
            };
            
            JobHandle typeHandle = typeJob.Schedule(totalVoxels, 64, densityHandle);
            
            // Чекаємо завершення
            typeHandle.Complete();
            
            // Конвертуємо результати та створюємо чанк
            var chunk = CreateChunkFromJobData(job.position, densityArray, voxelTypes);
            if (chunk != null)
            {
                AddToCache(job.position, chunk);
            }
            
            // Очищаємо native масиви
            densityArray.Dispose();
            voxelTypes.Dispose();
        }
        
        TerrainChunkV2 CreateChunkFromJobData(Vector3Int position, NativeArray<float> densityArray, NativeArray<byte> voxelTypes)
        {
            if (voxelTerrain == null) return null;
            
            var chunkGO = new GameObject($"Chunk_{position.x}_{position.y}_{position.z}");
            var chunk = chunkGO.AddComponent<TerrainChunkV2>();
            
            chunk.Initialize(voxelTerrain, new int3(position.x, position.y, position.z), voxelTerrain.chunkSize, voxelTerrain.voxelSize);
            
            // TODO: Передати дані з job в chunk
            // Поки що генеруємо стандартним методом
            chunk.GenerateTerrain();
            
            totalChunksGenerated++;
            return chunk;
        }
        
        void ProcessChunkJob(ChunkGenerationJob job)
        {
            if (chunkCache.ContainsKey(job.position))
                return;
                
            var chunk = GenerateChunk(job.position);
            if (chunk != null)
            {
                AddToCache(job.position, chunk);
            }
        }
        
        TerrainChunkV2 GenerateChunk(Vector3Int position)
        {
            if (voxelTerrain == null) return null;
            
            // Створюємо новий чанк
            var chunkGO = new GameObject($"Chunk_{position.x}_{position.y}_{position.z}");
            var chunk = chunkGO.AddComponent<TerrainChunkV2>();
            
            // Налаштовуємо чанк
            chunk.Initialize(voxelTerrain, new int3(position.x, position.y, position.z), 16, voxelTerrain.voxelSize);
            
            // Генеруємо терен
            chunk.GenerateTerrain();
            
            totalChunksGenerated++;
            return chunk;
        }
        
        void UpdateLOD()
        {
            if (!enableLOD || playerTransform == null) return;
            
            Vector3 playerPos = playerTransform.position;
            
            foreach (var kvp in chunkCache)
            {
                Vector3Int chunkPos = kvp.Key;
                TerrainChunkV2 chunk = kvp.Value;
                
                if (chunk == null) continue;
                
                float distance = Vector3.Distance(playerPos, chunkPos);
                int newLOD = GetLODLevel(distance);
                
                // TODO: Додати LOD підтримку в TerrainChunkV2
                // Поки що просто запитуємо чанк якщо він далеко
                if (distance > lodDistances[0])
                {
                    RequestChunk(chunkPos, 1f / (distance + 1f));
                }
            }
        }
        
        int GetLODLevel(float distance)
        {
            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (distance < lodDistances[i])
                    return i;
            }
            return lodDistances.Length;
        }
        
        void CleanupDistantChunks()
        {
            if (playerTransform == null) return;
            
            Vector3 playerPos = playerTransform.position;
            var chunksToRemove = new List<Vector3Int>();
            
            foreach (var kvp in chunkCache)
            {
                Vector3Int chunkPos = kvp.Key;
                float distance = Vector3.Distance(playerPos, chunkPos);
                
                if (distance > unloadDistance)
                {
                    chunksToRemove.Add(chunkPos);
                }
            }
            
            foreach (var chunkPos in chunksToRemove)
            {
                UnloadChunk(chunkPos);
            }
        }
        
        void AddToCache(Vector3Int position, TerrainChunkV2 chunk)
        {
            if (!enableCaching) return;
            
            // Перевіряємо розмір кешу
            if (chunkCache.Count >= cacheSize)
            {
                RemoveOldestFromCache();
            }
            
            chunkCache[position] = chunk;
        }
        
        void RemoveOldestFromCache()
        {
            if (chunkLRU.Count > 0)
            {
                var oldestPos = chunkLRU.Dequeue();
                
                if (chunkCache.TryGetValue(oldestPos, out TerrainChunkV2 oldChunk))
                {
                    // Серіалізуємо перед видаленням
                    var data = SerializeChunk(oldChunk);
                    serializedChunkCache[oldestPos] = data;
                    
                    // Видаляємо з активного кешу
                    chunkCache.Remove(oldestPos);
                    
                    if (oldChunk != null)
                        Destroy(oldChunk.gameObject);
                }
            }
        }
        
        ChunkData SerializeChunk(TerrainChunkV2 chunk)
        {
            // Простий спосіб серіалізації - зберігаємо тільки основні дані
            return new ChunkData
            {
                position = new Vector3Int(
                    Mathf.RoundToInt(chunk.transform.position.x), 
                    Mathf.RoundToInt(chunk.transform.position.y), 
                    Mathf.RoundToInt(chunk.transform.position.z)
                ),
                timestamp = Time.time,
                lodLevel = 0, // TODO: Додати LOD підтримку в TerrainChunkV2
                voxelData = new byte[0] // TODO: Серіалізувати воксельні дані
            };
        }
        
        TerrainChunkV2 DeserializeChunk(ChunkData data)
        {
            // TODO: Відновити чанк з серіалізованих даних
            return null;
        }
        
        void UnloadChunk(Vector3Int position)
        {
            if (chunkCache.TryGetValue(position, out TerrainChunkV2 chunk))
            {
                chunkCache.Remove(position);
                
                if (chunk != null)
                    Destroy(chunk.gameObject);
            }
            
            serializedChunkCache.Remove(position);
        }
        
        void UpdateStatistics()
        {
            // Оновлюємо статистику кожну секунду
            if (Time.time % 1f < Time.deltaTime)
            {
                float hitRate = cacheHits + cacheMisses > 0 ? (float)cacheHits / (cacheHits + cacheMisses) * 100f : 0f;
                
                Debug.Log($"📊 Чанки: {totalChunksGenerated} згенеровано, кеш: {chunkCache.Count}/{cacheSize}, " +
                         $"попадання: {hitRate:F1}%, FPS: {1000f / lastFrameTime:F1}");
            }
        }
        
        void Cleanup()
        {
            cancellationTokenSource?.Cancel();
            
            // Чекаємо завершення всіх задач
            if (activeTasks != null)
            {
                Task.WaitAll(activeTasks.ToArray(), 1000); // Максимум 1 секунда
            }
            
            cancellationTokenSource?.Dispose();
        }
        
        // Публічні методи
        public void RequestChunk(Vector3Int position, float priority = 1f)
        {
            var job = new ChunkGenerationJob
            {
                position = position,
                priority = priority
            };
            
            generationQueue.Enqueue(job);
        }
        
        public bool IsChunkLoaded(Vector3Int position)
        {
            return chunkCache.ContainsKey(position);
        }
        
        public TerrainChunkV2 GetChunk(Vector3Int position)
        {
            chunkCache.TryGetValue(position, out TerrainChunkV2 chunk);
            return chunk;
        }
        
        public void ClearCache()
        {
            foreach (var chunk in chunkCache.Values)
            {
                if (chunk != null)
                    Destroy(chunk.gameObject);
            }
            
            chunkCache.Clear();
            serializedChunkCache.Clear();
            chunkLRU.Clear();
            
            Debug.Log("🧹 Кеш чанків очищено");
        }
        
        // Налаштування оптимізації
        public void SetMaxConcurrentJobs(int count)
        {
            maxConcurrentJobs = Mathf.Max(1, count);
        }
        
        public void SetCacheSize(int size)
        {
            cacheSize = Mathf.Max(10, size);
        }
        
        public void SetTargetFrameTime(float ms)
        {
            targetFrameTime = Mathf.Max(8.33f, ms); // Мінімум 120 FPS
        }
        
        // Статистика для UI
        public string GetPerformanceStats()
        {
            float hitRate = cacheHits + cacheMisses > 0 ? (float)cacheHits / (cacheHits + cacheMisses) * 100f : 0f;
            
            return $"Чанки: {totalChunksGenerated} згенеровано\n" +
                   $"Кеш: {chunkCache.Count}/{cacheSize} ({hitRate:F1}% попадань)\n" +
                   $"Активні задачі: {activeTasks.Count}/{maxConcurrentJobs}\n" +
                   $"FPS: {1000f / lastFrameTime:F1}\n" +
                   $"Час кадру: {lastFrameTime:F1}мс";
        }
    }
} 