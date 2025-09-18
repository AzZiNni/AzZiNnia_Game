using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Azurin.Core;

namespace Voxel
{
    /// <summary>
    /// Новий менеджер вокселів - простіший і надійніший
    /// </summary>
    public class VoxelTerrain : MonoBehaviour
{
    [Header("Налаштування світу")]
    public int chunkSize = 16;
    public float voxelSize = 1f;
    public int viewDistance = 3;
    
    [Header("Генерація терену")]
    public float noiseScale = 0.05f;
    public float heightScale = 20f;
    public float groundLevel = 0f;
    public int seed = 12345;
    public AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Header("Marching Cubes")]
    public bool useTextureAtlas = true; // Опція для текстурного атласу
    public bool smoothNormals = true;
    public float textureScale = 0.1f; // Масштаб текстур (менше = більша текстура)
    
    [Header("Вибір генератора шуму")]
    public VoxelNoiseType noiseType = VoxelNoiseType.Simple3D;
    public NoiseType trnNoiseType = NoiseType.Perlin;
    
    [Header("Генератори терену")]
    public bool useHybridGenerator = true;
    public HybridUkrainianTerrainGenerator hybridGenerator;
    
    [Header("Український генератор")]
    public bool useUkrainianGenerator = true;
    public UkrainianTerrainGenerator ukrainianGenerator;
    
    [Header("Розширені налаштування шуму")]
    public NoiseSettings noiseSettings = NoiseSettings.Default;
    
    [Header("Матеріали та Текстури")]
    public Material terrainMaterial;
    public VoxelTextureMapping textureMapping; // Посилання на нашу карту текстур
    public Texture2DArray textureArray; // Посилання на наш масив текстур
        public bool assignTextureArrayToMaterialOnStart = true;
    
    [Header("Префаби")]
    public GameObject chunkPrefab;
    
    // Активні чанки
    private Dictionary<int3, TerrainChunkV2> chunks = new Dictionary<int3, TerrainChunkV2>();
    public Dictionary<int3, TerrainChunkV2> Chunks => chunks; // Публічна властивість для доступу
    private Transform player;
    private int3 lastPlayerChunk;
    
    // Пул неактивних чанків для переіспользування
    private Queue<TerrainChunkV2> chunkPool = new Queue<TerrainChunkV2>();
    
    private WorldSaveSystem saveSystem;

    void Start()
    {
        // Знаходимо гравця
        player = GameObject.FindWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("Гравця не знайдено! Переконайтеся, що у гравця є тег 'Player'");
            return;
        }
        
        // Переміщуємо гравця на безпечну висоту для початку
        // Спочатку генеруємо висоту терену в точці гравця
        if (ukrainianGenerator != null)
        {
            float terrainHeight = ukrainianGenerator.GenerateHeight(new float3(player.position.x, 0, player.position.z));
            float safeHeight = terrainHeight + 5f; // 5 метрів над поверхнею
            
            if (player.position.y < safeHeight)
            {
                player.position = new Vector3(player.position.x, safeHeight, player.position.z);
                Debug.Log($"Переміщено гравця на висоту {safeHeight} (терен: {terrainHeight}) для безпечного спавну");
            }
        }
        else if (player.position.y < 10f)
        {
            player.position = new Vector3(player.position.x, 10f, player.position.z);
            Debug.Log($"Переміщено гравця на висоту {player.position.y} для безпечного спавну");
        }
        
        // Ініціалізуємо генератори
        if (ukrainianGenerator == null)
        {
            ukrainianGenerator = GetComponent<UkrainianTerrainGenerator>();
            if (ukrainianGenerator == null)
            {
                ukrainianGenerator = gameObject.AddComponent<UkrainianTerrainGenerator>();
            }
        }
        
        // Ініціалізуємо систему ґрунту
        var soilSystem = GetComponent<SoilLayerSystem>();
        if (soilSystem == null)
        {
            soilSystem = gameObject.AddComponent<SoilLayerSystem>();
        }
        
        // Підключаємо систему ґрунту до українського генератора
        if (ukrainianGenerator.soilSystem == null)
        {
            ukrainianGenerator.soilSystem = soilSystem;
        }
        
        // Ініціалізуємо гібридний генератор якщо потрібен
        if (useHybridGenerator)
        {
            if (hybridGenerator == null)
            {
                hybridGenerator = GetComponent<HybridUkrainianTerrainGenerator>();
                if (hybridGenerator == null)
                {
                    hybridGenerator = gameObject.AddComponent<HybridUkrainianTerrainGenerator>();
                }
            }
        }
        
        // Ініціалізуємо префаб чанка якщо не призначено
        if (chunkPrefab == null)
        {
            chunkPrefab = new GameObject("ChunkPrefab");
            chunkPrefab.AddComponent<MeshFilter>();
            chunkPrefab.AddComponent<MeshRenderer>();
            chunkPrefab.AddComponent<MeshCollider>();
            
            // Завжди використовуємо TerrainChunkV2 (універсальний)
            chunkPrefab.AddComponent<TerrainChunkV2>();
            
            chunkPrefab.SetActive(false);
        }
        
        // Створюємо матеріал з нашим воксельним шейдером якщо не призначено
        if (terrainMaterial == null)
        {
            // Спробуємо знайти наш шейдер
            Shader voxelShader = Shader.Find("Custom/SimpleVoxelShader");
            if (voxelShader == null)
            {
                voxelShader = Shader.Find("Custom/VoxelArrayShader");
            }
            if (voxelShader == null)
            {
                // Спробуємо альтернативну назву
                voxelShader = Shader.Find("Custom/VoxelTerrainArrayMaterial");
            }
            
            if (voxelShader != null)
            {
                terrainMaterial = new Material(voxelShader);
                // створено матеріал
            }
            else
            {
                Shader fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
                if (fallbackShader != null)
                {
                    terrainMaterial = new Material(fallbackShader);
                    terrainMaterial.color = new Color(0.5f, 0.4f, 0.3f); // Коричневий колір землі
                }
            }
        }

        // Передаємо масив текстур в матеріал
        if (assignTextureArrayToMaterialOnStart && terrainMaterial != null && textureArray != null)
        {
            terrainMaterial.SetTexture("_MainTex", textureArray);
            // Оновлюємо всі активні чанки щоб вони використовували новий матеріал
            foreach (var chunk in Chunks.Values)
            {
                if (chunk != null && chunk.MeshRenderer != null)
                {
                    chunk.MeshRenderer.material = terrainMaterial;
                }
            }
        }
        
        
        // Кешуємо save system, якщо є
        saveSystem = GetComponent<WorldSaveSystem>();

        // Генеруємо початкові чанки
        UpdateChunks();
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Перевіряємо чи гравець перейшов в новий чанк
        int3 currentChunk = GetChunkCoord(player.position);
        if (!currentChunk.Equals(lastPlayerChunk))
        {
            lastPlayerChunk = currentChunk;
            UpdateChunks();
        }

        // Оновлюємо LOD для активних чанків
        foreach (var chunk in Chunks.Values)
        {
            if (chunk != null)
            {
                chunk.UpdateLOD(player.position);
            }
        }
    }
    
    void UpdateChunks()
    {
        // Визначаємо які чанки мають бути активними
        using (Azurin.Core.ProfilerMarkers.Voxel_UpdateChunks.Auto())
        {
        HashSet<int3> activeCoords = new HashSet<int3>();
        
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                for (int y = -2; y <= 2; y++) // Обмежуємо висоту
                {
                    int3 coord = lastPlayerChunk + new int3(x, y, z);
                    activeCoords.Add(coord);
                }
            }
        }
        
        // Видаляємо чанки які занадто далеко
        List<int3> toRemove = new List<int3>();
        foreach (var kvp in Chunks)
        {
            if (!activeCoords.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
            }
        }
        
        foreach (var coord in toRemove)
        {
            RemoveChunk(coord);
        }
        
        // Створюємо нові чанки
        foreach (var coord in activeCoords)
        {
            if (!Chunks.ContainsKey(coord))
            {
                CreateChunk(coord);
            }
        }
        }
    }
    
    void CreateChunk(int3 coord)
    {
        TerrainChunkV2 chunk;
        
        // Використовуємо чанк з пулу або створюємо новий
        if (chunkPool.Count > 0)
        {
            chunk = chunkPool.Dequeue();
            chunk.gameObject.SetActive(true);
        }
        else
        {
            GameObject go = Instantiate(chunkPrefab, transform);
            go.SetActive(true);
            chunk = go.GetComponent<TerrainChunkV2>();
        }
        
        // Позиціонуємо чанк
        chunk.transform.position = new Vector3(
            coord.x * chunkSize * voxelSize,
            coord.y * chunkSize * voxelSize,
            coord.z * chunkSize * voxelSize
        );
        
        chunk.gameObject.name = $"Chunk_{coord.x}_{coord.y}_{coord.z}";
        
        // Ініціалізуємо та генеруємо чанк
        chunk.Initialize(this, coord, chunkSize, voxelSize);
        Chunks[coord] = chunk;
        chunk.GenerateTerrain();

        // Застосовуємо модифікації з сейва, якщо є
        if (saveSystem != null && saveSystem.TryGetChunkModifications(coord, out var modifications))
        {
            int dataSize = chunkSize + 1;
            foreach (var entry in modifications.modifications)
            {
                int index = entry.Key;
                float value = entry.Value;
                int x = index % dataSize;
                int y = (index / dataSize) % dataSize;
                int z = index / (dataSize * dataSize);
                chunk.SetVoxel(x, y, z, value > 0 ? VoxelType.Stone : VoxelType.Air);
            }
            chunk.GenerateMesh();
        }
    }
    
    void RemoveChunk(int3 coord)
    {
        if (Chunks.TryGetValue(coord, out var chunk))
        {
            chunk.gameObject.SetActive(false);
            chunkPool.Enqueue(chunk);
            Chunks.Remove(coord);
        }
    }
    
    int3 GetChunkCoord(Vector3 worldPos)
    {
        return new int3(
            Mathf.FloorToInt(worldPos.x / (chunkSize * voxelSize)),
            Mathf.FloorToInt(worldPos.y / (chunkSize * voxelSize)),
            Mathf.FloorToInt(worldPos.z / (chunkSize * voxelSize))
        );
    }
    
    /// <summary>
    /// Функція щільності для генерації терену
    /// </summary>
    public float GetDensity(float3 worldPos)
    {
        // --- NEW SMOOTH TERRAIN LOGIC V5 (Multi-layered) ---
        // Combining different noise frequencies (octaves) to create more detailed and natural terrain.
        
        // Low-frequency noise for large, continental shapes
        float baseNoise = NoiseGenerator.SimplexNoise(worldPos, noiseScale * 0.25f);
        
        // Mid-frequency noise for hills and general terrain features
        float hillsNoise = NoiseGenerator.SimplexNoise(worldPos, noiseScale * 1.0f);

        // High-frequency noise for smaller details and roughness
        float detailNoise = NoiseGenerator.SimplexNoise(worldPos, noiseScale * 4.0f);
        
        // Blend the noise layers together. The base noise has the most influence.
        float combinedNoise = baseNoise * 0.65f + hillsNoise * 0.25f + detailNoise * 0.1f;

        // The final density calculation remains the same. Positive values are solid.
        float density = (combinedNoise * heightScale) - (worldPos.y - groundLevel);
        
        return density;
        
        /*
        // --- OLD V4 SMOOTH TERRAIN LOGIC (Corrected Normals, but flat) ---
        // This version correctly calculates density for the Marching Cubes library,
        // which expects positive values for solid terrain and negative for air.
        float effectiveNoiseScale = 0.015f; 
        float noise = NoiseGenerator.SimplexNoise(worldPos, effectiveNoiseScale);
        
        // Vertical gradient where density increases with depth.
        float density_v4 = worldPos.y - groundLevel;

        // Subtract noise to carve out terrain.
        density_v4 -= noise * heightScale;
        
        return density_v4;
        */

        /*
        // --- OLD V3 SMOOTH TERRAIN LOGIC (Inverted Normals) ---
        // Base noise for the main landmass shape
        float baseNoise_v3 = NoiseGenerator.SimplexNoise(worldPos, noiseScale); // Use the inspector value

        // Detail noise for smaller features on the surface
        float detailNoise_v3 = NoiseGenerator.SimplexNoise(worldPos, noiseScale * 3.0f);
        
        // The final noise value is a weighted combination of these layers.
        float combinedNoise_v3 = baseNoise_v3 * 0.8f + detailNoise_v3 * 0.2f;
        
        // Vertical gradient. Lower values are solid.
        float density_v3 = worldPos.y - groundLevel;

        // Subtract the combined noise from the density.
        density_v3 -= combinedNoise_v3 * heightScale;
        
        return density_v3;
        */
    }
    
    /// <summary>
    /// Модифікація терену (копання/будування) з фізичними ефектами
    /// </summary>
    public void ModifyTerrain(Vector3 worldPos, float radius, float strength)
    {
        // Визначаємо тип вокселя перед модифікацією для фізики
        VoxelType originalType = GetVoxelType(new float3(worldPos));
        bool isDestroying = strength < 0;
        
        // Знаходимо всі чанки які можуть бути зачеплені
        int range = Mathf.CeilToInt(radius / (chunkSize * voxelSize)) + 1;
        int3 centerChunk = GetChunkCoord(worldPos);
        
        List<int3> modifiedChunks = new List<int3>();
        
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                for (int z = -range; z <= range; z++)
                {
                    int3 coord = centerChunk + new int3(x, y, z);
                    if (Chunks.TryGetValue(coord, out var chunk))
                    {
                        chunk.ModifyTerrain(worldPos, radius, strength);
                        modifiedChunks.Add(coord);
                    }
                }
            }
        }
        
        // Викликаємо фізичні ефекти якщо руйнуємо блоки
        if (isDestroying && originalType != VoxelType.Air)
        {
            // Повідомляємо систему фізики
            VoxelPhysics physics = GetComponent<VoxelPhysics>();
            if (physics != null)
            {
                Vector3Int voxelPos = Vector3Int.FloorToInt(worldPos / voxelSize);
                physics.OnVoxelDestroyed(voxelPos, originalType);
            }
            
            // Повідомляємо систему рідин для води/лави
            if (originalType == VoxelType.Water || originalType == VoxelType.Lava)
            {
                LiquidSimulation liquid = GetComponent<LiquidSimulation>();
                if (liquid != null)
                {
                    Vector3Int voxelPos = Vector3Int.FloorToInt(worldPos / voxelSize);
                    liquid.OnLiquidVoxelDestroyed(voxelPos, originalType);
                }
            }
        }
        
        // Записуємо модифікацію для збереження
        WorldSaveSystem saveSystem = GetComponent<WorldSaveSystem>();
        if (saveSystem != null)
        {
            foreach (var coord in modifiedChunks)
            {
                Vector3Int localPos = Vector3Int.FloorToInt((worldPos - new Vector3(
                    coord.x * chunkSize * voxelSize,
                    coord.y * chunkSize * voxelSize,
                    coord.z * chunkSize * voxelSize
                )) / voxelSize);
                
                saveSystem.RecordModification(coord, new int3(localPos.x, localPos.y, localPos.z), strength);
            }
        }
        
        // Викликаємо подію модифікації
        OnTerrainModified?.Invoke(worldPos, radius, strength);
        GameEvents.RaiseTerrainModified(new float3(worldPos), radius, strength);
    }
    
    /// <summary>
    /// Подія модифікації терену
    /// </summary>
    public static event System.Action<Vector3, float, float> OnTerrainModified;
    
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        // Показуємо межі активної зони
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(
            lastPlayerChunk.x * chunkSize * voxelSize,
            lastPlayerChunk.y * chunkSize * voxelSize,
            lastPlayerChunk.z * chunkSize * voxelSize
        );
        
        Vector3 size = new Vector3(
            viewDistance * 2 * chunkSize * voxelSize,
            4 * chunkSize * voxelSize,
            viewDistance * 2 * chunkSize * voxelSize
        );
        
        Gizmos.DrawWireCube(center, size);
    }
    
    // Додаткові методи для роботи з вокселями
    public float GetVoxelValue(int x, int y, int z)
    {
        Vector3 worldPos = new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
        int3 chunkCoord = GetChunkCoord(worldPos);
        
        if (Chunks.TryGetValue(chunkCoord, out var chunk))
        {
            Vector3 localPos = worldPos - chunk.transform.position;
            int localX = Mathf.RoundToInt(localPos.x / voxelSize);
            int localY = Mathf.RoundToInt(localPos.y / voxelSize);
            int localZ = Mathf.RoundToInt(localPos.z / voxelSize);
            
            return chunk.GetVoxelValue(localX, localY, localZ);
        }
        
        return GetDensity(new float3(worldPos));
    }
    
    public VoxelType GetVoxelType(float3 worldPos)
    {
        float density = GetDensity(worldPos);

        if (density > 0)
        {
            // Solid - determine type based on depth from the surface
            if (density < 2f)
                return VoxelType.Grass;
            if (density < 7f)
                return VoxelType.Dirt;
            
            return VoxelType.Stone;
        }
        else
        {
            return VoxelType.Air;
        }
    }
    
    public void SetVoxel(int x, int y, int z, VoxelType type)
    {
        Vector3 worldPos = new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
        float strength = (type == VoxelType.Air) ? 10f : -10f;
        ModifyTerrain(worldPos, voxelSize * 0.5f, strength);
        
        // Повідомляємо системи фізики та рідин
        if (type == VoxelType.Air)
        {
            VoxelPhysics physics = GetComponent<VoxelPhysics>();
            if (physics != null)
            {
                physics.OnVoxelDestroyed(new Vector3Int(x, y, z), GetVoxelType(new float3(worldPos)));
            }
        }
        else if (type == VoxelType.Water || type == VoxelType.Lava)
        {
            LiquidSimulation liquid = GetComponent<LiquidSimulation>();
            if (liquid != null)
            {
                liquid.AddLiquidSource(new Vector3Int(x, y, z), type);
            }
        }
    }

    public enum VoxelNoiseType
    {
        Simple3D,
        TRN_Gen,
        Unity_Perlin
    }
}
} 