#pragma warning disable 0414

using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Voxel
{
    /// <summary>
    /// Реалістичний генератор терену в стилі мода Far from Home
    /// </summary>
    public class RealisticTerrainGenerator : MonoBehaviour
    {
        [Header("Біоми")]
        [SerializeField] private BiomeSettings[] biomes;
        [SerializeField] private float biomeScale = 0.001f;
        [SerializeField] private float temperatureScale = 0.0005f;
        [SerializeField] private float humidityScale = 0.0005f;
        
        [Header("Рельєф")]
        [SerializeField] private float continentalScale = 0.0001f;
        [SerializeField] private float erosionScale = 0.0003f;
        [SerializeField] private float peaksScale = 0.0002f;
        
        [Header("Річки")]
        [SerializeField] private bool generateRivers = true;
        [SerializeField] private float riverFrequency = 0.01f;
        [SerializeField] private float riverDepth = 5f;
        
        [Header("Деталі")]
        [SerializeField] private int terrainOctaves = 6;
        [SerializeField] private float terrainPersistence = 0.5f;
        [SerializeField] private float terrainLacunarity = 2f;
        
        private VoxelTerrain terrain;
        private int seed;
        
        private void Awake()
        {
            terrain = GetComponent<VoxelTerrain>();
            seed = terrain.seed;
            
            // Ініціалізуємо стандартні біоми якщо не задані
            if (biomes == null || biomes.Length == 0)
            {
                InitializeDefaultBiomes();
            }
        }
        
        private void InitializeDefaultBiomes()
        {
            biomes = new BiomeSettings[]
            {
                // Океан
                new BiomeSettings
                {
                    name = "Ocean",
                    minHeight = -50,
                    maxHeight = -10,
                    surfaceBlock = VoxelType.Sand,
                    subsurfaceBlock = VoxelType.Clay,
                    stoneBlock = VoxelType.Stone,
                    temperature = 0.5f,
                    humidity = 1f,
                    treeFrequency = 0f
                },
                
                // Пляж
                new BiomeSettings
                {
                    name = "Beach",
                    minHeight = -10,
                    maxHeight = 5,
                    surfaceBlock = VoxelType.Sand,
                    subsurfaceBlock = VoxelType.Sand,
                    stoneBlock = VoxelType.Stone,
                    temperature = 0.8f,
                    humidity = 0.4f,
                    treeFrequency = 0.01f
                },
                
                // Рівнини
                new BiomeSettings
                {
                    name = "Plains",
                    minHeight = 5,
                    maxHeight = 30,
                    surfaceBlock = VoxelType.Grass,
                    subsurfaceBlock = VoxelType.Dirt,
                    stoneBlock = VoxelType.Stone,
                    temperature = 0.7f,
                    humidity = 0.4f,
                    treeFrequency = 0.02f
                },
                
                // Ліс
                new BiomeSettings
                {
                    name = "Forest",
                    minHeight = 10,
                    maxHeight = 40,
                    surfaceBlock = VoxelType.Grass,
                    subsurfaceBlock = VoxelType.Dirt,
                    stoneBlock = VoxelType.Stone,
                    temperature = 0.6f,
                    humidity = 0.7f,
                    treeFrequency = 0.1f
                },
                
                // Гори
                new BiomeSettings
                {
                    name = "Mountains",
                    minHeight = 40,
                    maxHeight = 100,
                    surfaceBlock = VoxelType.Stone,
                    subsurfaceBlock = VoxelType.Stone,
                    stoneBlock = VoxelType.Stone,
                    temperature = 0.2f,
                    humidity = 0.3f,
                    treeFrequency = 0.01f
                },
                
                // Сніжні вершини
                new BiomeSettings
                {
                    name = "Snow Peaks",
                    minHeight = 70,
                    maxHeight = 150,
                    surfaceBlock = VoxelType.Snow,
                    subsurfaceBlock = VoxelType.Stone,
                    stoneBlock = VoxelType.Stone,
                    temperature = 0f,
                    humidity = 0.8f,
                    treeFrequency = 0f
                },
                
                // Пустеля
                new BiomeSettings
                {
                    name = "Desert",
                    minHeight = 5,
                    maxHeight = 30,
                    surfaceBlock = VoxelType.Sand,
                    subsurfaceBlock = VoxelType.Sand,
                    stoneBlock = VoxelType.Stone,
                    temperature = 1f,
                    humidity = 0f,
                    treeFrequency = 0.001f
                }
            };
        }
        
        /// <summary>
        /// Генерує висоту терену для заданої позиції
        /// </summary>
        public float GenerateTerrainHeight(float x, float z)
        {
            // Континентальність (великі форми рельєфу)
            float continentalness = NoiseGenerator.PerlinNoise3D(
                new float3(x * continentalScale, 0, z * continentalScale),
                1f,
                4,
                0.5f,
                2f
            );
            
            // Ерозія (згладжування)
            float erosion = NoiseGenerator.PerlinNoise3D(
                new float3(x * erosionScale + 1000, 0, z * erosionScale + 1000),
                1f,
                3,
                0.6f,
                2f
            );
            
            // Піки і долини
            float peaksValleys = NoiseGenerator.RidgeNoise(
                new float3(x * peaksScale, 0, z * peaksScale),
                1f,
                4
            );
            
            // Комбінуємо фактори
            float baseHeight = Mathf.Lerp(-30f, 80f, continentalness);
            
            // Ерозія зменшує висоту
            baseHeight *= Mathf.Lerp(0.5f, 1f, erosion);
            
            // Додаємо піки в гірських районах
            if (continentalness > 0.6f)
            {
                float peakStrength = (continentalness - 0.6f) / 0.4f;
                baseHeight += peaksValleys * 50f * peakStrength;
            }
            
            // Детальний шум
            float detail = NoiseGenerator.PerlinNoise3D(
                new float3(x * 0.01f, 0, z * 0.01f),
                1f,
                terrainOctaves,
                terrainPersistence,
                terrainLacunarity
            );
            
            baseHeight += detail * 10f;
            
            // Річки
            if (generateRivers && baseHeight > 0 && baseHeight < 40)
            {
                float riverNoise = GenerateRiverNoise(x, z);
                if (riverNoise > 0.7f)
                {
                    float riverStrength = (riverNoise - 0.7f) / 0.3f;
                    baseHeight -= riverDepth * riverStrength;
                }
            }
            
            return baseHeight;
        }
        
        /// <summary>
        /// Генерує шум для річок
        /// </summary>
        private float GenerateRiverNoise(float x, float z)
        {
            // Використовуємо воронойський шум для річкової мережі
            float voronoi = NoiseGenerator.VoronoiNoise(
                new float3(x * riverFrequency, 0, z * riverFrequency),
                1f
            );
            
            // Інвертуємо і робимо гострішим
            voronoi = 1f - Mathf.Abs(voronoi);
            voronoi = Mathf.Pow(voronoi, 3f);
            
            return voronoi;
        }
        
        /// <summary>
        /// Визначає біом для заданої позиції
        /// </summary>
        public BiomeSettings GetBiome(float x, float z, float height)
        {
            // Температура
            float temperature = NoiseGenerator.PerlinNoise3D(
                new float3(x * temperatureScale, 0, z * temperatureScale),
                1f,
                2,
                0.5f,
                2f
            );
            
            // Вологість
            float humidity = NoiseGenerator.PerlinNoise3D(
                new float3(x * humidityScale + 5000, 0, z * humidityScale + 5000),
                1f,
                2,
                0.5f,
                2f
            );
            
            // Знаходимо найкращий біом
            BiomeSettings bestBiome = biomes[0];
            float bestScore = float.MaxValue;
            
            foreach (var biome in biomes)
            {
                // Перевіряємо висоту
                if (height < biome.minHeight || height > biome.maxHeight)
                    continue;
                
                // Рахуємо відстань в кліматичному просторі
                float tempDiff = Mathf.Abs(temperature - biome.temperature);
                float humDiff = Mathf.Abs(humidity - biome.humidity);
                float score = tempDiff + humDiff;
                
                if (score < bestScore)
                {
                    bestScore = score;
                    bestBiome = biome;
                }
            }
            
            return bestBiome;
        }
        
        /// <summary>
        /// Генерує тип блоку для заданої позиції
        /// </summary>
        public VoxelType GenerateVoxelType(float3 worldPos)
        {
            float height = GenerateTerrainHeight(worldPos.x, worldPos.z);
            BiomeSettings biome = GetBiome(worldPos.x, worldPos.z, height);
            
            float depth = height - worldPos.y;
            
            // Під водою
            if (worldPos.y < 0)
            {
                if (depth < 0) return VoxelType.Water;
                if (depth < 3) return biome.surfaceBlock;
                if (depth < 8) return biome.subsurfaceBlock;
                return biome.stoneBlock;
            }
            
            // Над водою
            if (depth < 0) return VoxelType.Air;
            if (depth < 1) return biome.surfaceBlock;
            if (depth < 5) return biome.subsurfaceBlock;
            
            // Глибокі шари з рудами
            if (depth > 20)
            {
                float oreNoise = NoiseGenerator.PerlinNoise3D(worldPos * 0.1f, 1f, 1, 0.5f, 2f);
                
                if (depth > 40 && oreNoise > 0.9f) return VoxelType.DiamondOre;
                if (depth > 30 && oreNoise > 0.85f) return VoxelType.GoldOre;
                if (depth > 20 && oreNoise > 0.8f) return VoxelType.IronOre;
                if (oreNoise > 0.75f) return VoxelType.CoalOre;
            }
            
            return biome.stoneBlock;
        }
    }
    
    [System.Serializable]
    public struct BiomeSettings
    {
        public string name;
        public float minHeight;
        public float maxHeight;
        public VoxelType surfaceBlock;
        public VoxelType subsurfaceBlock;
        public VoxelType stoneBlock;
        public float temperature;
        public float humidity;
        public float treeFrequency;
    }
} 