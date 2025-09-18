using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;

namespace Voxel
{
    public enum UkrainianRegion
    {
        Carpathians,        // Карпати (2061м)
        VolhynianUpland,    // Волинська височина (400м)
        PodolianUpland,     // Подільська височина (515м)
        DniproUpland,       // Придніпровська височина (323м)
        DonetskRidge,       // Донецький кряж (367м)
        CrimeanMountains,   // Кримські гори (1545м)
        PolissiaLowland,    // Поліська низовина (150м)
        DniproLowland,      // Придніпровська низовина (120м)
        BlackSeaLowland,    // Причорноморська низовина (156м)
        DanubeDelta,        // Дельта Дунаю (0м)
        AzovUpland          // Приазовська височина (324м)
    }

    [System.Serializable]
    public struct UkrainianRegionData
    {
        public UkrainianRegion region;
        public float baseElevation;
        public float maxElevation;
        public float roughness;
        public BiomeType dominantBiome;
        public VoxelType dominantRock;
        public float riverDensity;
        public string description;
    }

    /// <summary>
    /// Генератор українського ландшафту з реалістичними біомами
    /// </summary>
    public class UkrainianTerrainGenerator : MonoBehaviour
    {
        [Header("Карта України")]
        public int2 mapSize = new int2(1000, 800); // Розмір карти в чанках
        public float2 mapCenter = new float2(500, 400); // Центр карти
        
        [Header("Біоми України")]
        public BiomeSettings carpathians = new BiomeSettings { heightScale = 50f, noiseScale = 0.02f };
        public BiomeSettings steppes = new BiomeSettings { heightScale = 5f, noiseScale = 0.05f };
        public BiomeSettings forests = new BiomeSettings { heightScale = 15f, noiseScale = 0.03f };
        public BiomeSettings riverValleys = new BiomeSettings { heightScale = -5f, noiseScale = 0.1f };
        
        [Header("Головні річки")]
        public RiverSettings dnipro = new RiverSettings { startPoint = new float2(700, 800), endPoint = new float2(400, 0), width = 20f };
        public RiverSettings dnister = new RiverSettings { startPoint = new float2(200, 600), endPoint = new float2(100, 0), width = 10f };
        public RiverSettings pivdennyyBuh = new RiverSettings { startPoint = new float2(300, 500), endPoint = new float2(350, 0), width = 8f };
        
        [Header("Регіони")]
        public RegionSettings[] regions;
        
        private VoxelTerrain terrain;
        private Dictionary<int2, BiomeType> biomeMap = new Dictionary<int2, BiomeType>();
        
        [Header("Додаткові налаштування висоти")]
        public float terrainScale = 0.001f;
        public bool useRealisticElevations = false;
        public float elevationMultiplier = 1f;

        [Header("Вплив річок")]
        [Range(0.1f, 10f)]
        public float riverInfluence = 2f;
        
        [Header("Розмір біомів")]
        [Range(100f, 10000f)]
        public float biomeSize = 1000f; // Розмір біомів для тестування

        // Масив регіональних даних (може бути заповнений у редакторі або в коді)
        public UkrainianRegionData[] ukrainianRegions = new UkrainianRegionData[0];

        // Посилання на допоміжні системи (можуть бути відсутні)
        public SoilLayerSystem soilSystem;
        public BiomeObjectGenerator biomeGenerator;
        
        [System.Serializable]
        public struct BiomeSettings
        {
            public float heightScale;
            public float noiseScale;
            public float temperature;
            public float humidity;
        }
        
        [System.Serializable]
        public struct RiverSettings
        {
            public float2 startPoint;
            public float2 endPoint;
            public float width;
            public AnimationCurve widthCurve;
        }
        
        [System.Serializable]
        public struct RegionSettings
        {
            public string name;
            public float2 center;
            public float radius;
            public BiomeType biome;
            public bool hasCity;
        }
        
        // Використовуємо глобальний Voxel.BiomeType

        void Awake()
        {
            terrain = GetComponent<VoxelTerrain>();
            InitializeRegions();
            InitializeUkrainianRegions();
        }
        
        void InitializeRegions()
        {
            // Ініціалізуємо основні регіони України
            regions = new RegionSettings[]
            {
                // Західна Україна
                new RegionSettings { name = "Закарпаття", center = new float2(100, 400), radius = 80, biome = BiomeType.Carpathians },
                new RegionSettings { name = "Львів", center = new float2(150, 450), radius = 50, biome = BiomeType.Forests, hasCity = true },
                new RegionSettings { name = "Івано-Франківськ", center = new float2(180, 380), radius = 40, biome = BiomeType.Carpathians },
                
                // Центральна Україна
                new RegionSettings { name = "Київ", center = new float2(500, 500), radius = 60, biome = BiomeType.Forests, hasCity = true },
                new RegionSettings { name = "Черкаси", center = new float2(480, 420), radius = 40, biome = BiomeType.Forests },
                new RegionSettings { name = "Полтава", center = new float2(550, 450), radius = 50, biome = BiomeType.Steppes },
                
                // Східна Україна
                new RegionSettings { name = "Харків", center = new float2(700, 500), radius = 50, biome = BiomeType.Steppes, hasCity = true },
                new RegionSettings { name = "Донецьк", center = new float2(750, 400), radius = 60, biome = BiomeType.Donbass },
                new RegionSettings { name = "Луганськ", center = new float2(800, 420), radius = 50, biome = BiomeType.Donbass },
                
                // Південна Україна
                new RegionSettings { name = "Одеса", center = new float2(400, 200), radius = 50, biome = BiomeType.BlackSeaCoast, hasCity = true },
                new RegionSettings { name = "Миколаїв", center = new float2(450, 250), radius = 40, biome = BiomeType.Steppes },
                new RegionSettings { name = "Херсон", center = new float2(500, 200), radius = 40, biome = BiomeType.Steppes },
                new RegionSettings { name = "Крим", center = new float2(550, 150), radius = 80, biome = BiomeType.BlackSeaCoast },
                
                // Північна Україна
                new RegionSettings { name = "Чернігів", center = new float2(480, 600), radius = 40, biome = BiomeType.Polissya },
                new RegionSettings { name = "Житомир", center = new float2(400, 550), radius = 40, biome = BiomeType.Polissya },
                new RegionSettings { name = "Рівне", center = new float2(300, 550), radius = 40, biome = BiomeType.Polissya }
            };
        }
        
        void InitializeUkrainianRegions()
        {
            // Ініціалізуємо дані українських регіонів якщо вони не задані
            if (ukrainianRegions == null || ukrainianRegions.Length == 0)
            {
                ukrainianRegions = new UkrainianRegionData[]
                {
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.Carpathians,
                        baseElevation = 500f,
                        maxElevation = 2061f,
                        roughness = 0.8f,
                        dominantBiome = BiomeType.Mountains,
                        dominantRock = VoxelType.Stone,
                        riverDensity = 0.3f,
                        description = "Карпатські гори - найвища точка України"
                    },
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.PolissiaLowland,
                        baseElevation = 150f,
                        maxElevation = 250f,
                        roughness = 0.3f,
                        dominantBiome = BiomeType.Forest,
                        dominantRock = VoxelType.Sand,
                        riverDensity = 0.6f,
                        description = "Поліська низовина - ліси і болота"
                    },
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.DniproLowland,
                        baseElevation = 120f,
                        maxElevation = 200f,
                        roughness = 0.2f,
                        dominantBiome = BiomeType.Plains,
                        dominantRock = VoxelType.Clay,
                        riverDensity = 0.4f,
                        description = "Придніпровська низовина - родючі чорноземи"
                    },
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.BlackSeaLowland,
                        baseElevation = 50f,
                        maxElevation = 156f,
                        roughness = 0.1f,
                        dominantBiome = BiomeType.Plains,
                        dominantRock = VoxelType.Stone,
                        riverDensity = 0.2f,
                        description = "Причорноморська низовина - степи"
                    },
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.VolhynianUpland,
                        baseElevation = 200f,
                        maxElevation = 400f,
                        roughness = 0.4f,
                        dominantBiome = BiomeType.Forest,
                        dominantRock = VoxelType.Stone,
                        riverDensity = 0.5f,
                        description = "Волинська височина - ліси та озера"
                    },
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.PodolianUpland,
                        baseElevation = 250f,
                        maxElevation = 515f,
                        roughness = 0.5f,
                        dominantBiome = BiomeType.Plains,
                        dominantRock = VoxelType.Stone,
                        riverDensity = 0.4f,
                        description = "Подільська височина - родючі землі"
                    },
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.DniproUpland,
                        baseElevation = 170f,
                        maxElevation = 323f,
                        roughness = 0.3f,
                        dominantBiome = BiomeType.Plains,
                        dominantRock = VoxelType.Clay,
                        riverDensity = 0.3f,
                        description = "Придніпровська височина"
                    },
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.DonetskRidge,
                        baseElevation = 200f,
                        maxElevation = 367f,
                        roughness = 0.4f,
                        dominantBiome = BiomeType.Plains,
                        dominantRock = VoxelType.CoalOre,
                        riverDensity = 0.2f,
                        description = "Донецький кряж - вугільний басейн"
                    },
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.CrimeanMountains,
                        baseElevation = 300f,
                        maxElevation = 1545f,
                        roughness = 0.7f,
                        dominantBiome = BiomeType.Mountains,
                        dominantRock = VoxelType.Stone,
                        riverDensity = 0.2f,
                        description = "Кримські гори"
                    },
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.AzovUpland,
                        baseElevation = 150f,
                        maxElevation = 324f,
                        roughness = 0.3f,
                        dominantBiome = BiomeType.Plains,
                        dominantRock = VoxelType.Stone,
                        riverDensity = 0.2f,
                        description = "Приазовська височина"
                    },
                    new UkrainianRegionData
                    {
                        region = UkrainianRegion.DanubeDelta,
                        baseElevation = 0f,
                        maxElevation = 50f,
                        roughness = 0.1f,
                        dominantBiome = BiomeType.Swamp,
                        dominantRock = VoxelType.Clay,
                        riverDensity = 0.9f,
                        description = "Дельта Дунаю - водно-болотні угіддя"
                    }
                };
            }
        }
        
        /// <summary>
        /// Генерує висоту для заданої позиції з урахуванням біомів України
        /// </summary>
        public float GenerateHeight(float3 worldPos)
        {
            float2 pos2D = new float2(worldPos.x, worldPos.z);
            
            // Визначаємо біом для позиції
            BiomeType biome = GetBiomeAt(pos2D);
            BiomeSettings settings = GetBiomeSettings(biome);
            
            // Базова висота з шумом
            float baseHeight = 0f;
            
            // Використовуємо різні типи шуму для різних біомів
            switch (biome)
            {
                case BiomeType.Carpathians:
                    // Гори - використовуємо ridge noise для гострих піків
                    float ridgeNoise = Mathf.PerlinNoise(pos2D.x * 0.01f, pos2D.y * 0.01f);
                    ridgeNoise = 1f - Mathf.Abs(ridgeNoise * 2f - 1f); // Ridge effect
                    baseHeight = ridgeNoise * settings.heightScale;
                    
                    // Додаємо деталі
                    baseHeight += Mathf.PerlinNoise(pos2D.x * 0.05f, pos2D.y * 0.05f) * 10f;
                    break;
                    
                case BiomeType.Steppes:
                    // Степи - плавні хвилі
                    baseHeight = Mathf.PerlinNoise(pos2D.x * settings.noiseScale, pos2D.y * settings.noiseScale) * settings.heightScale;
                    break;
                    
                case BiomeType.Forests:
                    // Ліси - помірні пагорби
                    baseHeight = Mathf.PerlinNoise(pos2D.x * settings.noiseScale, pos2D.y * settings.noiseScale) * settings.heightScale;
                    baseHeight += Mathf.PerlinNoise(pos2D.x * 0.1f, pos2D.y * 0.1f) * 3f;
                    break;
                    
                case BiomeType.Donbass:
                    // Донбас - індустріальний ландшафт з шахтами
                    baseHeight = Mathf.PerlinNoise(pos2D.x * settings.noiseScale, pos2D.y * settings.noiseScale) * settings.heightScale;
                    // Додаємо "шахтні провали"
                    float mineNoise = Mathf.PerlinNoise(pos2D.x * 0.2f, pos2D.y * 0.2f);
                    if (mineNoise > 0.7f)
                    {
                        baseHeight -= 10f; // Провал від шахти
                    }
                    break;
                    
                case BiomeType.BlackSeaCoast:
                    // Узбережжя - плавний спуск до моря
                    float distToCoast = Mathf.Min(pos2D.y / 100f, 1f);
                    baseHeight = Mathf.Lerp(-5f, 5f, distToCoast);
                    baseHeight += Mathf.PerlinNoise(pos2D.x * 0.1f, pos2D.y * 0.1f) * 2f;
                    break;
            }
            
            // Додаємо річки
            float riverDepth = CalculateRiverDepth(pos2D);
            baseHeight -= riverDepth;
            
            // Плавні переходи між біомами
            baseHeight = SmoothBiomeTransitions(pos2D, baseHeight);
            
            return baseHeight;
        }
        
        /// <summary>
        /// Визначає біом для заданої позиції
        /// </summary>
        BiomeType GetBiomeAt(float2 position)
        {
            // Перевіряємо чи позиція в межах певного регіону
            foreach (var region in regions)
            {
                float dist = math.distance(position, region.center);
                if (dist < region.radius)
                {
                    return region.biome;
                }
            }
            
            // За замовчуванням - степи (найпоширеніший біом України)
            return BiomeType.Steppes;
        }
        
        /// <summary>
        /// Отримує налаштування для біому
        /// </summary>
        BiomeSettings GetBiomeSettings(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Carpathians: return carpathians;
                case BiomeType.Forests: return forests;
                case BiomeType.RiverValley: return riverValleys;
                default: return steppes;
            }
        }
        
        /// <summary>
        /// Розраховує глибину річки в заданій точці
        /// </summary>
        float CalculateRiverDepth(float2 position)
        {
            float depth = 0f;
            
            // Дніпро
            depth = Mathf.Max(depth, CalculateRiverDepthForRiver(position, dnipro));
            
            // Дністер
            depth = Mathf.Max(depth, CalculateRiverDepthForRiver(position, dnister));
            
            // Південний Буг
            depth = Mathf.Max(depth, CalculateRiverDepthForRiver(position, pivdennyyBuh));
            
            return depth;
        }
        
        float CalculateRiverDepthForRiver(float2 position, RiverSettings river)
        {
            // Відстань до лінії річки
            float2 riverDir = math.normalize(river.endPoint - river.startPoint);
            float2 toPoint = position - river.startPoint;
            float progress = math.dot(toPoint, riverDir);
            float2 closestPoint = river.startPoint + riverDir * math.clamp(progress, 0, math.distance(river.startPoint, river.endPoint));
            
            float distToRiver = math.distance(position, closestPoint);
            
            // Ширина річки змінюється вздовж течії
            float riverProgress = progress / math.distance(river.startPoint, river.endPoint);
            float currentWidth = river.width;
            if (river.widthCurve != null && river.widthCurve.length > 0)
            {
                currentWidth *= river.widthCurve.Evaluate(riverProgress);
            }
            
            if (distToRiver < currentWidth)
            {
                // Глибина річки
                float depth = (1f - distToRiver / currentWidth) * 5f;
                
                // Додаємо шум для природності
                depth += Mathf.PerlinNoise(position.x * 0.1f, position.y * 0.1f) * 1f;
                
                return depth;
            }
            
            return 0f;
        }
        
        /// <summary>
        /// Згладжує переходи між біомами
        /// </summary>
        float SmoothBiomeTransitions(float2 position, float currentHeight)
        {
            // Знаходимо найближчі біоми
            float totalWeight = 0f;
            float weightedHeight = 0f;
            
            foreach (var region in regions)
            {
                float dist = math.distance(position, region.center);
                float weight = math.max(0, 1f - dist / (region.radius * 2f));
                
                if (weight > 0)
                {
                    BiomeSettings settings = GetBiomeSettings(region.biome);
                    float biomeHeight = Mathf.PerlinNoise(position.x * settings.noiseScale, position.y * settings.noiseScale) * settings.heightScale;
                    
                    weightedHeight += biomeHeight * weight;
                    totalWeight += weight;
                }
            }
            
            if (totalWeight > 0)
            {
                return math.lerp(currentHeight, weightedHeight / totalWeight, 0.5f);
            }
            
            return currentHeight;
        }
        
        /// <summary>
        /// Визначає тип вокселя для української місцевості з урахуванням ґрунтових шарів
        /// </summary>
        public VoxelType GetVoxelType(float3 worldPos, float depth)
        {
            BiomeType biome = GetBiomeAt(new float2(worldPos.x, worldPos.z));
            
            // Якщо під водою (річка)
            if (depth < -3f)
            {
                return VoxelType.Water;
            }
            
            // Поверхня (depth <= 1 блок)
            if (depth <= 1f)
            {
                return GetSurfaceVoxelType(biome, worldPos);
            }
            
            // Використовуємо систему ґрунтових шарів для підповерхневих блоків
            if (soilSystem != null && depth <= 15f)
            {
                var soilLayer = soilSystem.GetSoilLayerAtDepth(worldPos, depth);
                return soilLayer.voxelType;
            }
            
            // Глибокі шари - скельна основа або спеціальні породи
            return GetDeepLayerVoxelType(biome, worldPos, depth);
        }

        private VoxelType GetSurfaceVoxelType(BiomeType biome, float3 worldPos)
        {
            switch (biome)
            {
                case BiomeType.Carpathians:
                    if (worldPos.y > 1800f) return VoxelType.Snow; // Сніг на високих вершинах
                    if (worldPos.y > 1200f) return VoxelType.Stone; // Скелясті схили
                    return VoxelType.Grass; // Альпійські луки
                    
                case BiomeType.BlackSeaCoast:
                    if (worldPos.y < 0f) return VoxelType.Water; // Море
                    if (worldPos.y < 5f) return VoxelType.Sand; // Пляж
                    return VoxelType.Grass;
                    
                default:
                    return VoxelType.Grass; // Стандартна трава для степів, лісів тощо
            }
        }

        private VoxelType GetDeepLayerVoxelType(BiomeType biome, float3 worldPos, float depth)
        {
            switch (biome)
            {
                case BiomeType.Donbass:
                    if (depth > 20f && depth < 40f) return VoxelType.CoalOre; // Вугільні пласти
                    break;
                    
                case BiomeType.Carpathians:
                    if (depth > 50f) return VoxelType.Stone; // Гранітна основа
                    break;
            }
            
            // Стандартна скельна основа
            if (depth > 30f) return VoxelType.Stone;
            return VoxelType.Weathered_Rock; // Звітрена порода як перехідний шар
        }

        public VoxelType GetTerrainVoxelType(float3 worldPosition, float depth)
        {
            UkrainianRegion region = DetermineRegion(worldPosition.x, worldPosition.z);
            UkrainianRegionData regionData = GetRegionData(region);
            
            // Поверхня
            if (depth <= 1f)
            {
                return GetSurfaceVoxelType(regionData.dominantBiome, worldPosition);
            }
            
            // Ґрунтові шари (1-15 блоків вглиб)
            if (soilSystem != null && depth <= 15f)
            {
                // Налаштовуємо систему ґрунту для конкретного біому
                soilSystem.currentBiome = regionData.dominantBiome;
                var soilLayer = soilSystem.GetSoilLayerAtDepth(worldPosition, depth);
                
                // Для степів України застосовуємо чорнозем
                if (regionData.dominantBiome == BiomeType.Plains && depth > 1f && depth <= 4f)
                {
                    return VoxelType.Rich_Soil; // Чорнозем
                }
                
                return soilLayer.voxelType;
            }
            
            // Глибокі шари
            if (depth > 15f)
            {
                return regionData.dominantRock;
            }
            
            return VoxelType.Stone;
        }

        public float GetUkrainianElevation(float worldX, float worldZ)
        {
            UkrainianRegion region = DetermineRegion(worldX, worldZ);
            UkrainianRegionData regionData = GetRegionData(region);

            float baseHeight = regionData.baseElevation;
            
            float primaryNoise = NoiseGenerator.PerlinNoise3D(new float3(worldX * terrainScale, 0, worldZ * terrainScale), 1, 4, 0.5f, 2f);
            float secondaryNoise = NoiseGenerator.PerlinNoise3D(new float3(worldX * terrainScale * 4f, 0, worldZ * terrainScale * 4f), 1, 2, 0.5f, 2f) * 0.3f;
            float detailNoise = NoiseGenerator.PerlinNoise3D(new float3(worldX * terrainScale * 16f, 0, worldZ * terrainScale * 16f), 1, 2, 0.5f, 2f) * 0.1f;
            
            float combinedNoise = (primaryNoise + secondaryNoise + detailNoise) * regionData.roughness;
            
            float finalHeight = baseHeight + combinedNoise * (regionData.maxElevation - regionData.baseElevation);
            
            finalHeight = ApplyRiverInfluence(worldX, worldZ, finalHeight, regionData);
            
            if (useRealisticElevations)
            {
                finalHeight *= elevationMultiplier;
            }
            
            return Mathf.Max(finalHeight, GetSeaLevel());
        }

        public UkrainianRegion DetermineRegion(float worldX, float worldZ)
        {
            // Використовуємо налаштовуваний розмір біомів
            float scaledX = (worldX / biomeSize);
            float scaledZ = (worldZ / biomeSize);
            
            // Використовуємо шум для більш природного розподілу регіонів
            float regionNoise = Mathf.PerlinNoise(scaledX * 0.5f, scaledZ * 0.5f);
            
            // Розподіляємо регіони на основі шуму та позиції
            if (regionNoise < 0.1f)
            {
                return UkrainianRegion.Carpathians; // 10% - Карпати
            }
            else if (regionNoise < 0.2f)
            {
                return UkrainianRegion.PolissiaLowland; // 10% - Полісся
            }
            else if (regionNoise < 0.25f)
            {
                return UkrainianRegion.VolhynianUpland; // 5% - Волинська височина
            }
            else if (regionNoise < 0.3f)
            {
                return UkrainianRegion.PodolianUpland; // 5% - Подільська височина
            }
            else if (regionNoise < 0.6f)
            {
                return UkrainianRegion.DniproLowland; // 30% - Придніпровська низовина (найбільша)
            }
            else if (regionNoise < 0.75f)
            {
                return UkrainianRegion.BlackSeaLowland; // 15% - Причорноморська низовина
            }
            else if (regionNoise < 0.8f)
            {
                return UkrainianRegion.DonetskRidge; // 5% - Донецький кряж
            }
            else if (regionNoise < 0.85f)
            {
                return UkrainianRegion.CrimeanMountains; // 5% - Кримські гори
            }
            else if (regionNoise < 0.9f)
            {
                return UkrainianRegion.AzovUpland; // 5% - Приазовська височина
            }
            else if (regionNoise < 0.95f)
            {
                return UkrainianRegion.DniproUpland; // 5% - Придніпровська височина
            }
            else
            {
                return UkrainianRegion.DanubeDelta; // 5% - Дельта Дунаю
            }
        }

        public UkrainianRegionData GetRegionData(UkrainianRegion region)
        {
            foreach (var regionData in ukrainianRegions)
            {
                if (regionData.region == region)
                    return regionData;
            }
            return ukrainianRegions[0];
        }

        private float ApplyRiverInfluence(float worldX, float worldZ, float currentHeight, UkrainianRegionData regionData)
        {
            if (regionData.riverDensity <= 0f) return currentHeight;
            
            float riverNoise = NoiseGenerator.SimplexNoise(new float3(worldX * 0.0005f, 0, worldZ * 0.0005f), 1f);
            float riverMask = Mathf.Abs(riverNoise);
            
            float riverThreshold = 0.1f * regionData.riverDensity;
            
            if (riverMask < riverThreshold)
            {
                float riverDepth = (riverThreshold - riverMask) / riverThreshold;
                float depthReduction = riverDepth * riverInfluence;
                return currentHeight - depthReduction;
            }
            
            return currentHeight;
        }

        public BiomeType GetRegionalBiome(float worldX, float worldZ)
        {
            UkrainianRegion region = DetermineRegion(worldX, worldZ);
            UkrainianRegionData regionData = GetRegionData(region);
            return regionData.dominantBiome;
        }

        public void GenerateUkrainianTerrain(TerrainChunkV2 chunk, float3 chunkPosition)
        {
            for (int x = 0; x < chunk.ChunkSize; x++)
            {
                for (int z = 0; z < chunk.ChunkSize; z++)
                {
                    float3 worldPos = chunkPosition + new float3(x, 0, z);
                    float surfaceHeight = GetUkrainianElevation(worldPos.x, worldPos.z);
                    
                    for (int y = 0; y < chunk.ChunkSize; y++)
                    {
                        float worldY = chunkPosition.y + y;
                        
                        if (worldY <= surfaceHeight)
                        {
                            float depth = surfaceHeight - worldY;
                            VoxelType voxelType = GetVoxelType(worldPos, depth);
                            chunk.SetVoxel(x, y, z, voxelType);
                        }
                        else
                        {
                            if (worldY <= GetSeaLevel())
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
            
            BiomeType biome = GetRegionalBiome(chunkPosition.x, chunkPosition.z);
            biomeGenerator.GenerateBiomeObjects(chunk, biome, chunkPosition);
        }

        public float GetSeaLevel()
        {
            return 62f;
        }

        public string GetRegionInfo(float worldX, float worldZ)
        {
            UkrainianRegion region = DetermineRegion(worldX, worldZ);
            UkrainianRegionData regionData = GetRegionData(region);
            
            return $"Регіон: {region}\n" +
                   $"Висота: {regionData.baseElevation:F0}-{regionData.maxElevation:F0}м\n" +
                   $"Біом: {regionData.dominantBiome}\n" +
                   $"Опис: {regionData.description}";
        }

        // Система кліматичних зон України
        public float GetTemperature(float worldX, float worldZ, float elevation)
        {
            // Базова температура залежно від широти (умовно)
            float baseTemp = 8f + (worldZ % 1000f) / 1000f * 4f; // 8-12°C
            
            // Зменшення температури з висотою (6.5°C на 1000м)
            float altitudeEffect = -(elevation / 1000f) * 6.5f;
            
            return baseTemp + altitudeEffect;
        }

        public float GetPrecipitation(float worldX, float worldZ)
        {
            UkrainianRegion region = DetermineRegion(worldX, worldZ);
            
            return region switch
            {
                UkrainianRegion.Carpathians => 1200f,       // Найбільше опадів
                UkrainianRegion.PolissiaLowland => 600f,    // Помірні опади
                UkrainianRegion.CrimeanMountains => 400f,   // Мало опадів
                UkrainianRegion.BlackSeaLowland => 350f,    // Найменше опадів
                _ => 500f                                   // Середні опади
            };
        }

        // Історичні локації для квестів
        public bool IsHistoricalLocation(float worldX, float worldZ)
        {
            // Перевіряємо, чи знаходимося поблизу історичної локації
            float historicalNoise = NoiseGenerator.SimplexNoise(new float3(worldX * 0.0001f, 0, worldZ * 0.0001f), 1f);
            return historicalNoise > 0.8f; // Рідкісні історичні місця
        }

        public string GetHistoricalLocationName(float worldX, float worldZ)
        {
            string[] historicalSites = {
                "Стародавнє городище",
                "Козацький зимівник", 
                "Скіфський курган",
                "Княжий град",
                "Монастирські руїни",
                "Козацька переправа",
                "Торговий шлях",
                "Святе джерело"
            };
            
            int index = Mathf.Abs((int)(worldX + worldZ)) % historicalSites.Length;
            return historicalSites[index];
        }

        // Інтеграція з системою збереження
        public void SaveRegionalData(string fileName)
        {
            // Зберігаємо дані про регіони для подальшого використання
            string jsonData = JsonUtility.ToJson(ukrainianRegions);
            System.IO.File.WriteAllText(fileName, jsonData);
        }

        public void LoadRegionalData(string fileName)
        {
            if (System.IO.File.Exists(fileName))
            {
                string jsonData = System.IO.File.ReadAllText(fileName);
                ukrainianRegions = JsonUtility.FromJson<UkrainianRegionData[]>(jsonData);
            }
        }
    }
} 