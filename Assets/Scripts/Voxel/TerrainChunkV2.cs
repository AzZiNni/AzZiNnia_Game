#pragma warning disable 0414

using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using MarchingCubesProject;
using ProceduralNoiseProject;

namespace Voxel
{
    /// <summary>
    /// Універсальний TerrainChunk з підтримкою текстурного атласу
    /// Об'єднує функціональність V2 (базовий Marching Cubes) і V3 (з атласом)
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class TerrainChunkV2 : MonoBehaviour
    {
        private VoxelTerrain terrain;
        private int3 chunkCoord;
        private int chunkSize;
        private float voxelSize;
        
        // LOD система
        private int currentLOD = 0;
        private float[] lodDistances = { 50f, 100f, 200f };
        private int[] lodResolutions = { 1, 2, 4 }; // Skip every N voxels
        
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        public MeshRenderer MeshRenderer => meshRenderer; // Публічна властивість для доступу
        private MeshCollider meshCollider;
        
        // Дані вокселів
        private VoxelArray densityArray;
        private VoxelType[,,] voxelTypes; // Додано для атласу
        private bool isDirty = false;
        
        // Marching Cubes
        private Marching marching;
        
        // Для генерації меша
        private Mesh mesh;
        // Reusable buffers to reduce GC pressure
        private readonly List<Vector3> verticesBuffer = new List<Vector3>(4096);
        private readonly List<int> trianglesBuffer = new List<int>(8192);
        private readonly List<Vector2> uvBuffer = new List<Vector2>(4096);
        private readonly List<Vector2> uv3Buffer = new List<Vector2>(4096);
        
        // Noise для кращої генерації
        private FractalNoise fractalNoise;
        
        void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.mesh = mesh;
            
            // Ініціалізуємо Marching Cubes
            marching = new MarchingCubes();
            marching.Surface = 0.0f; // Set to 0.0 to align with our new density function (negative is solid)
        }
        
        public void Initialize(VoxelTerrain terrain, int3 coord, int size, float voxelSize)
        {
            this.terrain = terrain;
            this.chunkCoord = coord;
            this.chunkSize = size;
            this.voxelSize = voxelSize;
            
            // Створюємо масиви
            int dataSize = size + 1;
            densityArray = new VoxelArray(dataSize, dataSize, dataSize);
            densityArray.FlipNormals = true;
            voxelTypes = new VoxelType[dataSize, dataSize, dataSize];
            
            // Встановлюємо матеріал
            if (terrain.terrainMaterial != null)
            {
                meshRenderer.material = terrain.terrainMaterial;
            }
            
            // Ініціалізуємо шум
            INoise perlin = new PerlinNoise(terrain.seed, 1.0f);
            fractalNoise = new FractalNoise(perlin, 3, 1.0f);
        }
        
        public void GenerateTerrain()
        {
            int dataSize = chunkSize + 1;
            
            // Заповнюємо дані вокселів
            for (int x = 0; x < dataSize; x++)
            {
                for (int y = 0; y < dataSize; y++)
                {
                    for (int z = 0; z < dataSize; z++)
                    {
                        float3 worldPos = new float3(transform.position) + new float3(x, y, z) * voxelSize;
                        
                        // Генеруємо щільність
                        densityArray[x, y, z] = GetTerrainDensity(worldPos);
                        
                        // Генеруємо типи вокселів
                        voxelTypes[x, y, z] = terrain.GetVoxelType(worldPos);
                    }
                }
            }
            
            // Генеруємо меш
            GenerateMesh();
        }
        
        private float GetTerrainDensity(float3 worldPos)
        {
            // Використовуємо готовий noise або терен генератор
            if (terrain != null)
            {
                return terrain.GetDensity(worldPos);
            }
            
            // Fallback - використовуємо fractal noise
            float u = worldPos.x * terrain.noiseScale;
            float v = worldPos.y * terrain.noiseScale;
            float w = worldPos.z * terrain.noiseScale;
            
            float noise = fractalNoise.Sample3D(u, v, w);
            
            // Додаємо gradient для земної поверхні
            float surfaceHeight = terrain.groundLevel + noise * terrain.heightScale;
            float density = surfaceHeight - worldPos.y;
            
            return density;
        }
        
        public void ModifyTerrain(Vector3 worldPos, float radius, float strength)
        {
            int dataSize = chunkSize + 1;
            float3 chunkWorldPos = new float3(transform.position);
            float3 modifyPos = new float3(worldPos);
            
            // Модифікуємо дані вокселів
            for (int x = 0; x < dataSize; x++)
            {
                for (int y = 0; y < dataSize; y++)
                {
                    for (int z = 0; z < dataSize; z++)
                    {
                        float3 voxelWorldPos = chunkWorldPos + new float3(x, y, z) * voxelSize;
                        float distance = math.length(voxelWorldPos - modifyPos);
                        
                        if (distance < radius)
                        {
                            float factor = 1f - (distance / radius);
                            densityArray[x, y, z] += strength * factor;
                            
                            // Оновлюємо тип воксела якщо копаємо
                            if (terrain.useTextureAtlas && strength < 0 && densityArray[x, y, z] > 0)
                            {
                                voxelTypes[x, y, z] = VoxelType.Air;
                            }
                        }
                    }
                }
            }
            
            // Перегенеруємо меш
            GenerateMesh();
        }
        
        public void GenerateMesh()
        {
            using (Azurin.Core.ProfilerMarkers.Chunk_GenerateMesh.Auto())
            {
                GenerateMeshWithLOD(currentLOD);
            }
        }
        
        /// <summary>
        /// Генерація мешу з урахуванням LOD
        /// </summary>
        public void GenerateMeshWithLOD(int lodLevel)
        {
            if (terrain == null) return;
            
            currentLOD = Mathf.Clamp(lodLevel, 0, lodResolutions.Length - 1);
            int skipVoxels = lodResolutions[currentLOD];
            
            // Prepare buffers
            verticesBuffer.Clear();
            trianglesBuffer.Clear();
            uvBuffer.Clear();
            uv3Buffer.Clear();
            
            // Для LOD створюємо спрощений масив щільності
            if (skipVoxels > 1)
            {
                int lodSize = (chunkSize / skipVoxels) + 1;
                VoxelArray lodDensityArray = new VoxelArray(lodSize, lodSize, lodSize);
                
                // Заповнюємо спрощений масив
                for (int x = 0; x < lodSize; x++)
                {
                    for (int y = 0; y < lodSize; y++)
                    {
                        for (int z = 0; z < lodSize; z++)
                        {
                            int srcX = Mathf.Min(x * skipVoxels, chunkSize);
                            int srcY = Mathf.Min(y * skipVoxels, chunkSize);
                            int srcZ = Mathf.Min(z * skipVoxels, chunkSize);
                            lodDensityArray[x, y, z] = densityArray[srcX, srcY, srcZ];
                        }
                    }
                }
                
                marching.Generate(lodDensityArray.Voxels, verticesBuffer, trianglesBuffer);
            }
            else
            {
                marching.Generate(densityArray.Voxels, verticesBuffer, trianglesBuffer);
            }
            
            if (terrain.textureMapping == null)
            {
                Debug.LogError("VoxelTextureMapping is not set on VoxelTerrain!");
                return;
            }

            for (int i = 0; i < verticesBuffer.Count; i++)
            {
                Vector3 gridPos = verticesBuffer[i]; 
                
                // --- NEW TEXTURE ARRAY LOGIC ---
                Vector3 normal = GetVertexNormal(gridPos);
                int x = Mathf.Clamp(Mathf.RoundToInt(gridPos.x), 0, chunkSize);
                int y = Mathf.Clamp(Mathf.RoundToInt(gridPos.y), 0, chunkSize);
                int z = Mathf.Clamp(Mathf.RoundToInt(gridPos.z), 0, chunkSize);
                VoxelType voxelType = voxelTypes[x, y, z];
                
                // Add null check for textureMapping
                VoxelFaceTextures faceTextures;
                if (terrain.textureMapping != null)
                {
                    faceTextures = terrain.textureMapping.GetFaceTextures(voxelType);
                }
                else
                {
                    // Fallback to default texture index 0
                    faceTextures = new VoxelFaceTextures(0);
                }
                float textureIndex = faceTextures.sideFaceTextureIndex; // Default to side

                if (Vector3.Dot(normal, Vector3.up) > 0.5f) // Top face
                {
                    textureIndex = faceTextures.topFaceTextureIndex;
                }
                else if (Vector3.Dot(normal, Vector3.down) > 0.5f) // Bottom face
                {
                    textureIndex = faceTextures.bottomFaceTextureIndex;
                }
                
                // Triplanar mapping UVs
                Vector2 uv = new Vector2(gridPos.x, gridPos.z); // Top-down projection
                if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
                {
                    uv = new Vector2(gridPos.y, gridPos.z); // Side projection
                }
                if (Mathf.Abs(normal.z) > Mathf.Abs(normal.y) && Mathf.Abs(normal.z) > Mathf.Abs(normal.x))
                {
                    uv = new Vector2(gridPos.x, gridPos.y); // Other side projection
                }
                
                uvBuffer.Add(uv);
                uv3Buffer.Add(new Vector2(textureIndex, 0)); // Pass texture index via uv3.x
                
                verticesBuffer[i] *= voxelSize;
            }

            mesh.Clear();
            mesh.vertices = verticesBuffer.ToArray();
            mesh.triangles = trianglesBuffer.ToArray();
            mesh.uv = uvBuffer.ToArray();
            mesh.uv3 = uv3Buffer.ToArray(); // Set texture indices
            mesh.RecalculateNormals();

            // Assign the mesh to the collider ONLY if it has vertices
            if (verticesBuffer.Count > 0)
            {
                meshCollider.sharedMesh = mesh;
            }
            else
            {
                meshCollider.sharedMesh = null; // Ensure collider is cleared for empty chunks
            }
            // Suppressed per-chunk logs in production path
        }
        
        private Vector3 GetVertexNormal(Vector3 localPos)
        {
            float h = 0.5f;
            
            float dx = densityArray.GetInterpolatedValue(localPos.x + h, localPos.y, localPos.z) -
                       densityArray.GetInterpolatedValue(localPos.x - h, localPos.y, localPos.z);
            float dy = densityArray.GetInterpolatedValue(localPos.x, localPos.y + h, localPos.z) -
                       densityArray.GetInterpolatedValue(localPos.x, localPos.y - h, localPos.z);
            float dz = densityArray.GetInterpolatedValue(localPos.x, localPos.y, localPos.z + h) -
                       densityArray.GetInterpolatedValue(localPos.x, localPos.y, localPos.z - h);
            
            return new Vector3(-dx, -dy, -dz).normalized;
        }
        
        public float GetVoxelValue(int x, int y, int z)
        {
            if (x < 0 || x > chunkSize || y < 0 || y > chunkSize || z < 0 || z > chunkSize)
                return 0f;
                
            return densityArray.GetVoxel(x, y, z);
        }
        
        public VoxelType GetVoxelType(int x, int y, int z)
        {
            if (x < 0 || x > chunkSize || y < 0 || y > chunkSize || z < 0 || z > chunkSize)
                return VoxelType.Air;
                
            return voxelTypes[x, y, z];
        }
        
        public void SetVoxel(int x, int y, int z, VoxelType type)
        {
            if (x < 0 || x >= chunkSize || y < 0 || y >= chunkSize || z < 0 || z >= chunkSize)
                return;
                
            // Встановлюємо значення щільності на основі типу вокселя
            densityArray[x, y, z] = (type == VoxelType.Air) ? 1.0f : -1.0f;
            voxelTypes[x, y, z] = type;
            isDirty = true;
        }
        
        public int ChunkSize => chunkSize;
        
        /// <summary>
        /// Оновити LOD на основі відстані до гравця
        /// </summary>
        public void UpdateLOD(Vector3 playerPos)
        {
            float distance = Vector3.Distance(transform.position + Vector3.one * chunkSize * voxelSize * 0.5f, playerPos);
            
            int newLOD = 0;
            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (distance > lodDistances[i])
                    newLOD = i + 1;
            }
            
            newLOD = Mathf.Clamp(newLOD, 0, lodResolutions.Length - 1);
            
            if (newLOD != currentLOD)
            {
                GenerateMeshWithLOD(newLOD);
            }
        }
        
        public int GetCurrentLOD() => currentLOD;
        
        void OnDestroy()
        {
            // VoxelArray не використовує NativeArray, тому не потребує Dispose
        }
        
        // Для дебагу - показуємо інформацію про чанк
        void OnDrawGizmosSelected()
        {
            if (densityArray != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(
                    transform.position + Vector3.one * chunkSize * voxelSize * 0.5f,
                    Vector3.one * chunkSize * voxelSize
                );
            }
        }
    }
} 