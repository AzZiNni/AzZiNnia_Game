using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Voxel
{
    /// <summary>
    /// Гібридний генератор терену: RealisticTerrainGenerator + UkrainianTerrainGenerator
    /// Використовує реалістичну генерацію біомів як базу і адаптує під українську географію
    /// </summary>
    public class HybridUkrainianTerrainGenerator : MonoBehaviour
    {
        [Header("Компоненти")]
        [SerializeField] private RealisticTerrainGenerator realisticGenerator;
        [SerializeField] private UkrainianTerrainGenerator ukrainianGenerator;
        
        [Header("Гібридні налаштування")]
        [SerializeField] private float ukrainianInfluence = 0.7f; // Наскільки сильно впливає українська географія
        [SerializeField] private float realisticInfluence = 0.3f; // Наскільки сильно впливає реалістична генерація
        [SerializeField] private bool adaptBiomesToRegions = true; // Адаптувати біоми під українські регіони
        [SerializeField] private bool useUkrainianElevations = true; // Використовувати українські висоти
        
        [Header("Регіональна адаптація")]
        [SerializeField] private RegionalBiomeMapping[] regionalMappings;
        
        private VoxelTerrain terrain;
        private int seed;
        
        private void Awake()
        {
            terrain = GetComponent<VoxelTerrain>();
            seed = terrain.seed;
            
            // Отримуємо або створюємо компоненти
            if (realisticGenerator == null)
            {
                realisticGenerator = GetComponent<RealisticTerrainGenerator>();
                if (realisticGenerator == null)
                    realisticGenerator = gameObject.AddComponent<RealisticTerrainGenerator>();
            }
            if (ukrainianGenerator == null)
            {
                ukrainianGenerator = GetComponent<UkrainianTerrainGenerator>();
                if (ukrainianGenerator == null)
                    ukrainianGenerator = gameObject.AddComponent<UkrainianTerrainGenerator>();
            }
                
            InitializeRegionalMappings();
        }
        
        private void InitializeRegionalMappings()
        {
            regionalMappings = new RegionalBiomeMapping[]
            {
                // Карпати -> Гірські біоми
                new RegionalBiomeMapping
                {
                    region = UkrainianRegion.Carpathians,
                    preferredBiomes = new BiomeType[] { BiomeType.Mountains, BiomeType.Forest, BiomeType.Snow_Peaks },
                    elevationModifier = 1.2f,
                    temperatureModifier = -0.3f,
                    humidityModifier = 0.2f
                },
                
                // Поліська низовина -> Ліси і болота
                new RegionalBiomeMapping
                {
                    region = UkrainianRegion.PolissiaLowland,
                    preferredBiomes = new BiomeType[] { BiomeType.Forest, BiomeType.Swamp, BiomeType.Plains },
                    elevationModifier = 0.3f,
                    temperatureModifier = -0.1f,
                    humidityModifier = 0.4f
                },
                
                // Придніпровська низовина -> Степи і рівнини
                new RegionalBiomeMapping
                {
                    region = UkrainianRegion.DniproLowland,
                    preferredBiomes = new BiomeType[] { BiomeType.Plains, BiomeType.River },
                    elevationModifier = 0.2f,
                    temperatureModifier = 0.1f,
                    humidityModifier = 0.1f
                },
                
                // Причорноморська низовина -> Степи і пустелі
                new RegionalBiomeMapping
                {
                    region = UkrainianRegion.BlackSeaLowland,
                    preferredBiomes = new BiomeType[] { BiomeType.Plains, BiomeType.Desert, BiomeType.Beach },
                    elevationModifier = 0.1f,
                    temperatureModifier = 0.3f,
                    humidityModifier = -0.2f
                }
            };
        }
        
        /// <summary>
        /// Головний метод генерації висоти терену (гібридний)
        /// </summary>
        public float GenerateHybridTerrainHeight(float x, float z)
        {
            // 1. Отримуємо базову висоту від реалістичного генератора
            float realisticHeight = realisticGenerator.GenerateTerrainHeight(x, z);
            
            // 2. Отримуємо українську висоту
            float ukrainianHeight = ukrainianGenerator.GetUkrainianElevation(x, z);
            
            // 3. Визначаємо український регіон
            UkrainianRegion region = GetRegionAtPosition(x, z);
            RegionalBiomeMapping mapping = GetRegionalMapping(region);
            
            // 4. Комбінуємо висоти з урахуванням регіональних модифікаторів
            float hybridHeight;
            
            if (useUkrainianElevations)
            {
                // Українська географія як основа, реалістична як деталі
                hybridHeight = ukrainianHeight * ukrainianInfluence + 
                              (realisticHeight * mapping.elevationModifier) * realisticInfluence;
            }
            else
            {
                // Реалістична генерація як основа, українська як адаптація
                hybridHeight = realisticHeight * realisticInfluence + 
                              ukrainianHeight * ukrainianInfluence;
            }
            
            // 5. Застосовуємо регіональні модифікатори
            hybridHeight *= mapping.elevationModifier;
            
            return hybridHeight;
        }
        
        /// <summary>
        /// Отримує біом з урахуванням українських регіонів
        /// </summary>
        public BiomeSettings GetHybridBiome(float x, float z, float height)
        {
            // 1. Отримуємо базовий біом від реалістичного генератора
            BiomeSettings baseBiome = realisticGenerator.GetBiome(x, z, height);
            
            if (!adaptBiomesToRegions)
                return baseBiome;
            
            // 2. Визначаємо український регіон
            UkrainianRegion region = GetRegionAtPosition(x, z);
            RegionalBiomeMapping mapping = GetRegionalMapping(region);
            
            // 3. Перевіряємо чи підходить поточний біом для регіону
            BiomeType currentBiomeType = ConvertToEnumBiome(baseBiome.name);
            
            if (System.Array.Exists(mapping.preferredBiomes, biome => biome == currentBiomeType))
            {
                // Біом підходить - адаптуємо його під регіон
                return AdaptBiomeToRegion(baseBiome, mapping);
            }
            else
            {
                // Біом не підходить - замінюємо на найкращий для регіону
                BiomeType preferredBiome = GetBestBiomeForRegion(mapping, height);
                return CreateRegionalBiome(preferredBiome, mapping, height);
            }
        }
        
        /// <summary>
        /// Генерує тип воксела з урахуванням гібридної системи
        /// </summary>
        public VoxelType GenerateHybridVoxelType(float3 worldPos, float depth)
        {
            // Використовуємо покращену логіку з українського генератора
            return ukrainianGenerator.GetTerrainVoxelType(worldPos, depth);
        }
        
        /// <summary>
        /// Генерує чанк терену використовуючи гібридну систему
        /// </summary>
        public void GenerateHybridTerrain(TerrainChunkV2 chunk, float3 chunkPosition)
        {
            for (int x = 0; x < chunk.ChunkSize; x++)
            {
                for (int z = 0; z < chunk.ChunkSize; z++)
                {
                    float3 worldPos = chunkPosition + new float3(x, 0, z);
                    float surfaceHeight = GenerateHybridTerrainHeight(worldPos.x, worldPos.z);
                    
                    for (int y = 0; y < chunk.ChunkSize; y++)
                    {
                        float worldY = chunkPosition.y + y;
                        
                        if (worldY <= surfaceHeight)
                        {
                            float depth = surfaceHeight - worldY;
                            VoxelType voxelType = GenerateHybridVoxelType(worldPos, depth);
                            chunk.SetVoxel(x, y, z, voxelType);
                        }
                        else if (worldY <= ukrainianGenerator.GetSeaLevel())
                        {
                            chunk.SetVoxel(x, y, z, VoxelType.Water);
                        }
                        else
                        {
                            chunk.SetVoxel(x, y, z, VoxelType.Air);
                        }
                    }
                }
            }
        }
        
        // Допоміжні методи
        private UkrainianRegion GetRegionAtPosition(float x, float z)
        {
            // Делегуємо українському генератору
            return ukrainianGenerator.DetermineRegion(x, z);
        }
        
        private RegionalBiomeMapping GetRegionalMapping(UkrainianRegion region)
        {
            foreach (var mapping in regionalMappings)
            {
                if (mapping.region == region)
                    return mapping;
            }
            return regionalMappings[0]; // Fallback
        }
        
        private BiomeType ConvertToEnumBiome(string biomeName)
        {
            switch (biomeName.ToLower())
            {
                case "ocean": return BiomeType.Ocean;
                case "beach": return BiomeType.Beach;
                case "plains": return BiomeType.Plains;
                case "forest": return BiomeType.Forest;
                case "mountains": return BiomeType.Mountains;
                case "snow peaks": return BiomeType.Snow_Peaks;
                case "desert": return BiomeType.Desert;
                default: return BiomeType.Plains;
            }
        }
        
        private BiomeSettings AdaptBiomeToRegion(BiomeSettings baseBiome, RegionalBiomeMapping mapping)
        {
            BiomeSettings adaptedBiome = baseBiome;
            
            // Адаптуємо температуру і вологість
            adaptedBiome.temperature = Mathf.Clamp01(baseBiome.temperature + mapping.temperatureModifier);
            adaptedBiome.humidity = Mathf.Clamp01(baseBiome.humidity + mapping.humidityModifier);
            
            // Адаптуємо висоти
            adaptedBiome.minHeight *= mapping.elevationModifier;
            adaptedBiome.maxHeight *= mapping.elevationModifier;
            
            return adaptedBiome;
        }
        
        private BiomeType GetBestBiomeForRegion(RegionalBiomeMapping mapping, float height)
        {
            // Вибираємо найкращий біом для висоти
            foreach (var biome in mapping.preferredBiomes)
            {
                if (IsBiomeSuitableForHeight(biome, height))
                    return biome;
            }
            return mapping.preferredBiomes[0]; // Fallback
        }
        
        private bool IsBiomeSuitableForHeight(BiomeType biome, float height)
        {
            switch (biome)
            {
                case BiomeType.Ocean: return height < 0;
                case BiomeType.Beach: return height >= 0 && height < 10;
                case BiomeType.Plains: return height >= 5 && height < 50;
                case BiomeType.Forest: return height >= 10 && height < 80;
                case BiomeType.Mountains: return height >= 50 && height < 150;
                case BiomeType.Snow_Peaks: return height >= 100;
                case BiomeType.Desert: return height >= 0 && height < 40;
                case BiomeType.Swamp: return height >= 0 && height < 20;
                default: return true;
            }
        }
        
        private BiomeSettings CreateRegionalBiome(BiomeType biomeType, RegionalBiomeMapping mapping, float height)
        {
            // Створюємо новий біом спеціально для українського регіону
            BiomeSettings regionalBiome = new BiomeSettings();
            
            switch (biomeType)
            {
                case BiomeType.Plains:
                    regionalBiome.name = "Ukrainian Plains";
                    regionalBiome.surfaceBlock = VoxelType.Grass;
                    regionalBiome.subsurfaceBlock = VoxelType.Rich_Soil; // Чорнозем!
                    regionalBiome.stoneBlock = VoxelType.Stone;
                    break;
                    
                case BiomeType.Forest:
                    regionalBiome.name = "Ukrainian Forest";
                    regionalBiome.surfaceBlock = VoxelType.Grass;
                    regionalBiome.subsurfaceBlock = VoxelType.Dirt;
                    regionalBiome.stoneBlock = VoxelType.Stone;
                    break;
                    
                case BiomeType.Mountains:
                    regionalBiome.name = "Carpathian Mountains";
                    regionalBiome.surfaceBlock = VoxelType.Stone;
                    regionalBiome.subsurfaceBlock = VoxelType.Stone;
                    regionalBiome.stoneBlock = VoxelType.Stone;
                    break;
                    
                default:
                    regionalBiome.name = "Ukrainian Region";
                    regionalBiome.surfaceBlock = VoxelType.Grass;
                    regionalBiome.subsurfaceBlock = VoxelType.Dirt;
                    regionalBiome.stoneBlock = VoxelType.Stone;
                    break;
            }
            
            // Застосовуємо регіональні модифікатори
            regionalBiome.temperature = Mathf.Clamp01(0.5f + mapping.temperatureModifier);
            regionalBiome.humidity = Mathf.Clamp01(0.5f + mapping.humidityModifier);
            regionalBiome.minHeight = height - 10f;
            regionalBiome.maxHeight = height + 20f;
            regionalBiome.treeFrequency = GetTreeFrequencyForBiome(biomeType);
            
            return regionalBiome;
        }
        
        private float GetTreeFrequencyForBiome(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Forest: return 0.15f;
                case BiomeType.Plains: return 0.03f;
                case BiomeType.Mountains: return 0.02f;
                case BiomeType.Swamp: return 0.08f;
                default: return 0.01f;
            }
        }
    }
    
    /// <summary>
    /// Мапінг українських регіонів на біоми
    /// </summary>
    [System.Serializable]
    public struct RegionalBiomeMapping
    {
        public UkrainianRegion region;
        public BiomeType[] preferredBiomes;
        public float elevationModifier;
        public float temperatureModifier;
        public float humidityModifier;
    }
} 