using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;
using MarchingCubesProject; // Потрібно для роботи з готовою реалізацією

namespace Voxel
{
    public enum MARCHING_MODE { CUBES, TETRAHEDRON };
    
    /// <summary>
    /// Інтеграція готової реалізації Marching Cubes для нашої системи вокселів
    /// </summary>
    public class MarchingCubesIntegration : MonoBehaviour
    {
        [Header("Налаштування Marching Cubes")]
        [SerializeField] private MARCHING_MODE mode = MARCHING_MODE.CUBES;
        [SerializeField] private float surface = 0.0f;
        [SerializeField] private bool smoothNormals = false;
        
        private Marching marching;
        
        void Awake()
        {
            // Ініціалізуємо алгоритм
                    if (mode == MARCHING_MODE.TETRAHEDRON)
            marching = new MarchingTertrahedron();
            else
                marching = new MarchingCubes();
                
            marching.Surface = surface;
        }
        
        /// <summary>
        /// Генерує меш з даних вокселів використовуючи готову реалізацію Marching Cubes
        /// </summary>
        public void GenerateMesh(float[,,] voxelData, List<Vector3> vertices, List<int> triangles, List<Vector3> normals = null)
        {
            vertices.Clear();
            triangles.Clear();
            if (normals != null) normals.Clear();
            
            // Використовуємо готову реалізацію
            marching.Generate(voxelData, vertices, triangles);
            
            // Генеруємо нормалі якщо потрібно
            if (smoothNormals && normals != null)
            {
                int width = voxelData.GetLength(0);
                int height = voxelData.GetLength(1);
                int depth = voxelData.GetLength(2);
                
                var voxelArray = new VoxelArray(width, height, depth);
                // Копіюємо дані вокселів у VoxelArray для розрахунку нормалей
                for (int ix = 0; ix < width; ix++)
                {
                    for (int iy = 0; iy < height; iy++)
                    {
                        for (int iz = 0; iz < depth; iz++)
                        {
                            voxelArray[ix, iy, iz] = voxelData[ix, iy, iz];
                        }
                    }
                }
                
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vector3 p = vertices[i];
                    float u = p.x / (width - 1.0f);
                    float v = p.y / (height - 1.0f);
                    float w = p.z / (depth - 1.0f);
                    
                    Vector3 n = voxelArray.GetNormal(u, v, w);
                    normals.Add(n);
                }
            }
        }
        
        /// <summary>
        /// Генерує меш для чанка
        /// </summary>
        public void GenerateChunkMesh(NativeArray<float> voxelData, int chunkSize, float voxelSize, 
            out List<Vector3> vertices, out List<int> triangles)
        {
            vertices = new List<Vector3>();
            triangles = new List<int>();
            
            // Конвертуємо NativeArray в 3D масив
            int dataSize = chunkSize + 1;
            float[,,] voxels = new float[dataSize, dataSize, dataSize];
            
            for (int x = 0; x < dataSize; x++)
            {
                for (int y = 0; y < dataSize; y++)
                {
                    for (int z = 0; z < dataSize; z++)
                    {
                        int index = x + y * dataSize + z * dataSize * dataSize;
                        voxels[x, y, z] = voxelData[index];
                    }
                }
            }
            
            // Генеруємо меш
            marching.Generate(voxels, vertices, triangles);
            
            // Масштабуємо вершини
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = vertices[i] * voxelSize;
            }
        }
    }
} 