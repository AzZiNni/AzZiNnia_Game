using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Voxel.Jobs
{
    /// <summary>
    /// Паралельна генерація щільності вокселів для чанка
    /// Використовує Unity Job System для максимальної продуктивності
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct ChunkDensityJob : IJobParallelFor
    {
        // Вхідні дані
        [ReadOnly] public float3 chunkWorldPos;
        [ReadOnly] public int chunkSize;
        [ReadOnly] public float voxelSize;
        [ReadOnly] public float noiseScale;
        [ReadOnly] public float heightScale;
        [ReadOnly] public float groundLevel;
        [ReadOnly] public int seed;
        
        // Вихідні дані
        [NativeDisableParallelForRestriction]
        public NativeArray<float> densityArray;
        
        // Розмір даних (chunkSize + 1)
        [ReadOnly] public int dataSize;
        
        public void Execute(int index)
        {
            // Конвертуємо лінійний індекс в 3D координати
            int x = index % dataSize;
            int y = (index / dataSize) % dataSize;
            int z = index / (dataSize * dataSize);
            
            // Обчислюємо світову позицію вокселя
            float3 worldPos = chunkWorldPos + new float3(x, y, z) * voxelSize;
            
            // Генеруємо щільність використовуючи шум
            float density = GenerateDensity(worldPos);
            
            // Зберігаємо результат
            densityArray[index] = density;
        }
        
        float GenerateDensity(float3 worldPos)
        {
            // Простий Simplex шум для швидкості (Burst-оптимізований)
            float noise = noise.snoise(worldPos * noiseScale);
            
            // Вертикальний градієнт
            float heightGradient = (groundLevel + noise * heightScale) - worldPos.y;
            
            // Додаємо 3D шум для печер
            float caveNoise = noise.snoise(worldPos * 0.03f);
            if (caveNoise > 0.7f && worldPos.y < groundLevel - 10f)
            {
                heightGradient -= (caveNoise - 0.7f) * 10f;
            }
            
            return heightGradient;
        }
    }
    
    /// <summary>
    /// Паралельна генерація типів вокселів
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct ChunkVoxelTypeJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> densityArray;
        [ReadOnly] public float3 chunkWorldPos;
        [ReadOnly] public int dataSize;
        [ReadOnly] public float voxelSize;
        
        [NativeDisableParallelForRestriction]
        public NativeArray<byte> voxelTypes; // Використовуємо byte для економії пам'яті
        
        public void Execute(int index)
        {
            float density = densityArray[index];
            
            if (density <= 0)
            {
                voxelTypes[index] = (byte)VoxelType.Air;
                return;
            }
            
            // Визначаємо позицію вокселя
            int x = index % dataSize;
            int y = (index / dataSize) % dataSize;
            int z = index / (dataSize * dataSize);
            
            float3 worldPos = chunkWorldPos + new float3(x, y, z) * voxelSize;
            
            // Визначаємо тип на основі глибини
            if (density < 2f)
                voxelTypes[index] = (byte)VoxelType.Grass;
            else if (density < 7f)
                voxelTypes[index] = (byte)VoxelType.Dirt;
            else
                voxelTypes[index] = (byte)VoxelType.Stone;
        }
    }
    
    /// <summary>
    /// Паралельний Marching Cubes (спрощена версія)
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct MarchingCubesJob : IJob
    {
        [ReadOnly] public NativeArray<float> densityArray;
        [ReadOnly] public int dataSize;
        [ReadOnly] public float surface;
        
        // Вихідні дані для мешу
        public NativeList<float3> vertices;
        public NativeList<int> triangles;
        
        public void Execute()
        {
            // Тут буде спрощена реалізація Marching Cubes
            // Для повної реалізації потрібно портувати таблиці lookup
            
            for (int x = 0; x < dataSize - 1; x++)
            {
                for (int y = 0; y < dataSize - 1; y++)
                {
                    for (int z = 0; z < dataSize - 1; z++)
                    {
                        ProcessCube(x, y, z);
                    }
                }
            }
        }
        
        void ProcessCube(int x, int y, int z)
        {
            // Отримуємо 8 кутів куба
            float[] cube = new float[8];
            cube[0] = GetDensity(x, y, z);
            cube[1] = GetDensity(x + 1, y, z);
            cube[2] = GetDensity(x + 1, y, z + 1);
            cube[3] = GetDensity(x, y, z + 1);
            cube[4] = GetDensity(x, y + 1, z);
            cube[5] = GetDensity(x + 1, y + 1, z);
            cube[6] = GetDensity(x + 1, y + 1, z + 1);
            cube[7] = GetDensity(x, y + 1, z + 1);
            
            // Визначаємо конфігурацію куба
            int cubeIndex = 0;
            for (int i = 0; i < 8; i++)
            {
                if (cube[i] > surface)
                    cubeIndex |= (1 << i);
            }
            
            // Якщо куб повністю всередині або зовні - пропускаємо
            if (cubeIndex == 0 || cubeIndex == 255)
                return;
                
            // Тут має бути генерація трикутників на основі таблиць
            // Для демонстрації - простий квад на поверхні
            if (cube[0] > surface && cube[1] <= surface)
            {
                float3 v0 = new float3(x, y, z);
                float3 v1 = new float3(x + 1, y, z);
                float3 v2 = new float3(x + 1, y + 1, z);
                float3 v3 = new float3(x, y + 1, z);
                
                int startIndex = vertices.Length;
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);
                
                // Два трикутники для квада
                triangles.Add(startIndex);
                triangles.Add(startIndex + 1);
                triangles.Add(startIndex + 2);
                
                triangles.Add(startIndex);
                triangles.Add(startIndex + 2);
                triangles.Add(startIndex + 3);
            }
        }
        
        float GetDensity(int x, int y, int z)
        {
            int index = x + y * dataSize + z * dataSize * dataSize;
            return densityArray[index];
        }
    }
    
    /// <summary>
    /// Допоміжна структура для передачі результатів
    /// </summary>
    public struct ChunkJobResult
    {
        public NativeArray<float> densityArray;
        public NativeArray<byte> voxelTypes;
        public NativeList<float3> vertices;
        public NativeList<int> triangles;
        public JobHandle jobHandle;
        
        public void Dispose()
        {
            if (densityArray.IsCreated) densityArray.Dispose();
            if (voxelTypes.IsCreated) voxelTypes.Dispose();
            if (vertices.IsCreated) vertices.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
        }
    }
}
