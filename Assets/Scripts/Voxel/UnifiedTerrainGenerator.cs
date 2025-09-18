using UnityEngine;
using Unity.Mathematics;
using ProceduralNoiseProject;

namespace Voxel
{
    /// <summary>
    /// Єдиний універсальний генератор терену з різними режимами
    /// Об'єднує функціональність Ukrainian, Hybrid та простих генераторів
    /// </summary>
    public class UnifiedTerrainGenerator : MonoBehaviour
    {
        public enum GeneratorMode
        {
            Simple,          // Простий Perlin/Simplex шум
            Ukrainian,       // Український ландшафт з біомами
            Hybrid,          // Гібридний режим з комбінацією методів
            Realistic,       // Реалістичний терен з ерозією
            Custom           // Користувацький режим
        }
        
        [Header("⚙️ Основні налаштування")]
        [SerializeField] private GeneratorMode mode = GeneratorMode.Ukrainian;
        [SerializeField] private int seed = 12345;
        [SerializeField] private float noiseScale = 0.05f;
        [SerializeField] private float heightScale = 20f;
        [SerializeField] private float groundLevel = 0f;
        
        [Header("🏔️ Український режим")]
        [SerializeField] private bool enableBiomes = true;
        [SerializeField] private float carpathianHeight = 50f;
        [SerializeField] private float steppeFlat = 0.2f;
        [SerializeField] private float riverDepth = 5f;
        
        [Header("🌊 Гібридний режим")]
        [SerializeField] private float erosionStrength = 0.3f;
        [SerializeField] private int octaves = 4;
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private float lacunarity = 2f;
        
        [Header("🎨 Налаштування шуму")]
        [SerializeField] private NoiseType noiseType = NoiseType.Simplex;
        [SerializeField] private AnimationCurve heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        // Кешовані компоненти
        private INoise baseNoise;
        private FractalNoise fractalNoise;
        private VoxelTerrain terrain;
        
        // Біоми для українського режиму
        private BiomeType[,] biomeMap;
        private int biomeMapSize = 256;
        
        void Awake()
        {
            terrain = GetComponent<VoxelTerrain>();
            InitializeNoiseGenerators();
        }
        
        void InitializeNoiseGenerators()
        {
            // Базовий генератор шуму
            switch (noiseType)
            {
                case NoiseType.Perlin:
                    baseNoise = new PerlinNoise(seed, 1.0f);
                    break;
                case NoiseType.Simplex:
                default:
                    baseNoise = new SimplexNoise(seed, 1.0f);
                    break;
            }
            
            // Фрактальний шум для деталізації
            fractalNoise = new FractalNoise(baseNoise, octaves, persistence);
            
            // Генеруємо карту біомів для українського режиму
            if (mode == GeneratorMode.Ukrainian && enableBiomes)
            {
                GenerateBiomeMap();
            }
        }
        
        /// <summary>
        /// Головна функція генерації висоти терену
        /// </summary>
        public float GenerateHeight(float3 worldPos)
        {
            switch (mode)
            {
                case GeneratorMode.Simple:
                    return GenerateSimpleHeight(worldPos);
                    
                case GeneratorMode.Ukrainian:
                    return GenerateUkrainianHeight(worldPos);
                    
                case GeneratorMode.Hybrid:
                    return GenerateHybridHeight(worldPos);
                    
                case GeneratorMode.Realistic:
                    return GenerateRealisticHeight(worldPos);
                    
                case GeneratorMode.Custom:
                    return GenerateCustomHeight(worldPos);
                    
                default:
                    return GenerateSimpleHeight(worldPos);
            }
        }
        
        /// <summary>
        /// Отримати щільність для вокселя
        /// </summary>
        public float GetDensity(float3 worldPos)
        {
            float height = GenerateHeight(new float3(worldPos.x, 0, worldPos.z));
            float density = height - worldPos.y + groundLevel;
            
            // Додаємо 3D шум для печер і деталей
            if (mode == GeneratorMode.Hybrid || mode == GeneratorMode.Realistic)
            {
                float caveNoise = fractalNoise.Sample3D(
                    worldPos.x * 0.03f,
                    worldPos.y * 0.03f,
                    worldPos.z * 0.03f
                );
                
                if (caveNoise > 0.7f && worldPos.y < height - 10f)
                {
                    density -= (caveNoise - 0.7f) * 10f; // Створюємо печери
                }
            }
            
            return density;
        }
        
        /// <summary>
        /// Отримати тип вокселя на основі позиції
        /// </summary>
        public VoxelType GetVoxelType(float3 worldPos)
        {
            float density = GetDensity(worldPos);
            
            if (density <= 0) return VoxelType.Air;
            
            // Визначаємо тип на основі глибини та біому
            float height = GenerateHeight(new float3(worldPos.x, 0, worldPos.z));
            float depth = height - worldPos.y;
            
            if (mode == GeneratorMode.Ukrainian && enableBiomes)
            {
                BiomeType biome = GetBiomeAt(worldPos.x, worldPos.z);
                return GetVoxelTypeForBiome(biome, depth, worldPos.y);
            }
            
            // Стандартне визначення типів
            if (depth < 1f) return VoxelType.Grass;
            if (depth < 5f) return VoxelType.Dirt;
            if (depth < 20f) return VoxelType.Stone;
            return VoxelType.Bedrock;
        }
        
        // === Режими генерації ===
        
        float GenerateSimpleHeight(float3 pos)
        {
            float noise = fractalNoise.Sample2D(pos.x * noiseScale, pos.z * noiseScale);
            return heightCurve.Evaluate(noise) * heightScale;
        }
        
        float GenerateUkrainianHeight(float3 pos)
        {
            BiomeType biome = GetBiomeAt(pos.x, pos.z);
            float baseHeight = GenerateSimpleHeight(pos);
            
            switch (biome)
            {
                case BiomeType.Carpathians:
                    // Карпатські гори
                    float mountainNoise = fractalNoise.Sample2D(pos.x * 0.02f, pos.z * 0.02f);
                    baseHeight += mountainNoise * carpathianHeight;
                    break;
                    
                case BiomeType.Steppes:
                    // Степи - плоскі території
                    baseHeight *= steppeFlat;
                    break;
                    
                case BiomeType.Dnipro:
                    // Річка Дніпро
                    baseHeight -= riverDepth;
                    break;
                    
                case BiomeType.Forests:
                    // Ліси - помірні пагорби
                    baseHeight *= 0.7f;
                    break;
            }
            
            return baseHeight;
        }
        
        float GenerateHybridHeight(float3 pos)
        {
            // Комбінуємо різні методи
            float simpleHeight = GenerateSimpleHeight(pos);
            float ukrainianHeight = GenerateUkrainianHeight(pos);
            
            // Змішуємо з вагами
            float height = simpleHeight * 0.3f + ukrainianHeight * 0.7f;
            
            // Додаємо ерозію
            if (erosionStrength > 0)
            {
                float erosion = fractalNoise.Sample2D(pos.x * 0.1f, pos.z * 0.1f);
                height -= erosion * erosionStrength * heightScale;
            }
            
            return height;
        }
        
        float GenerateRealisticHeight(float3 pos)
        {
            // Реалістична генерація з тектонічними плитами та ерозією
            float tectonicNoise = baseNoise.Sample2D(pos.x * 0.001f, pos.z * 0.001f);
            float continentalHeight = tectonicNoise * heightScale * 2f;
            
            // Додаємо деталі
            float detailNoise = fractalNoise.Sample2D(pos.x * noiseScale, pos.z * noiseScale);
            continentalHeight += detailNoise * heightScale * 0.5f;
            
            // Симуляція ерозії
            float erosion = Mathf.PerlinNoise(pos.x * 0.05f, pos.z * 0.05f);
            continentalHeight *= (1f - erosion * erosionStrength);
            
            return continentalHeight;
        }
        
        float GenerateCustomHeight(float3 pos)
        {
            // Користувацька логіка - можна розширити
            return GenerateSimpleHeight(pos);
        }
        
        // === Система біомів ===
        
        void GenerateBiomeMap()
        {
            biomeMap = new BiomeType[biomeMapSize, biomeMapSize];
            
            for (int x = 0; x < biomeMapSize; x++)
            {
                for (int z = 0; z < biomeMapSize; z++)
                {
                    float temperature = Mathf.PerlinNoise(x * 0.01f, z * 0.01f);
                    float humidity = Mathf.PerlinNoise(x * 0.01f + 1000f, z * 0.01f + 1000f);
                    
                    biomeMap[x, z] = DetermineBiome(temperature, humidity, x, z);
                }
            }
        }
        
        BiomeType DetermineBiome(float temperature, float humidity, int x, int z)
        {
            // Карпати на заході
            if (x < biomeMapSize * 0.2f)
                return BiomeType.Carpathians;
                
            // Дніпро по центру
            if (Mathf.Abs(x - biomeMapSize * 0.5f) < 10)
                return BiomeType.Dnipro;
                
            // Степи на півдні та сході
            if (z > biomeMapSize * 0.7f || x > biomeMapSize * 0.7f)
                return BiomeType.Steppes;
                
            // Решта - ліси
            return BiomeType.Forests;
        }
        
        BiomeType GetBiomeAt(float worldX, float worldZ)
        {
            if (biomeMap == null) return BiomeType.Plains;
            
            int x = Mathf.Clamp((int)(worldX / 10f) % biomeMapSize, 0, biomeMapSize - 1);
            int z = Mathf.Clamp((int)(worldZ / 10f) % biomeMapSize, 0, biomeMapSize - 1);
            
            return biomeMap[x, z];
        }
        
        VoxelType GetVoxelTypeForBiome(BiomeType biome, float depth, float worldY)
        {
            switch (biome)
            {
                case BiomeType.Carpathians:
                    if (worldY > carpathianHeight * 0.8f) return VoxelType.Snow;
                    if (depth < 1f) return VoxelType.Grass;
                    if (depth < 3f) return VoxelType.Dirt;
                    return VoxelType.Stone;
                    
                case BiomeType.Steppes:
                    if (depth < 0.5f) return VoxelType.TallGrass;
                    if (depth < 2f) return VoxelType.Dirt;
                    if (depth < 5f) return VoxelType.Clay;
                    return VoxelType.Stone;
                    
                case BiomeType.Dnipro:
                    if (worldY < 0) return VoxelType.Water;
                    if (depth < 2f) return VoxelType.Sand;
                    if (depth < 4f) return VoxelType.Gravel;
                    return VoxelType.Stone;
                    
                case BiomeType.Forests:
                    if (depth < 1f) return VoxelType.Grass;
                    if (depth < 4f) return VoxelType.Dirt;
                    return VoxelType.Stone;
                    
                default:
                    if (depth < 1f) return VoxelType.Grass;
                    if (depth < 5f) return VoxelType.Dirt;
                    return VoxelType.Stone;
            }
        }
        
        // === Публічні методи для налаштування ===
        
        public void SetMode(GeneratorMode newMode)
        {
            mode = newMode;
            InitializeNoiseGenerators();
        }
        
        public void SetSeed(int newSeed)
        {
            seed = newSeed;
            InitializeNoiseGenerators();
        }
        
        public void RegenerateBiomes()
        {
            if (mode == GeneratorMode.Ukrainian && enableBiomes)
            {
                GenerateBiomeMap();
            }
        }
        
        // === Допоміжні структури ===
        
        public enum NoiseType
        {
            Perlin,
            Simplex,
            Value,
            Worley
        }
    }
}
