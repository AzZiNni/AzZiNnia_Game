using UnityEngine;
using Voxel;

namespace Core
{
    /// <summary>
    /// Скрипт для автоматичного налаштування гібридної системи генерації терену
    /// </summary>
    public class HybridTerrainSetup : MonoBehaviour
    {
        [Header("Налаштування")]
        [SerializeField] private bool autoSetup = true;
        [SerializeField] private bool debugLog = true;
        
        [Header("Гібридні параметри")]
        [Range(0f, 1f)]
        [SerializeField] private float ukrainianInfluence = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float realisticInfluence = 0.3f;
        [SerializeField] private bool adaptBiomesToRegions = true;
        [SerializeField] private bool useUkrainianElevations = true;
        
        private VoxelTerrain voxelTerrain;
        private HybridUkrainianTerrainGenerator hybridGenerator;
        private RealisticTerrainGenerator realisticGenerator;
        private UkrainianTerrainGenerator ukrainianGenerator;
        
        void Awake()
        {
            if (autoSetup)
            {
                SetupHybridSystem();
            }
        }
        
        void Start()
        {
            if (hybridGenerator != null)
            {
                ApplySettings();
            }
        }
        
        [ContextMenu("Setup Hybrid System")]
        public void SetupHybridSystem()
        {
            voxelTerrain = GetComponent<VoxelTerrain>();
            if (voxelTerrain == null)
            {
                LogError("VoxelTerrain компонент не знайдено!");
                return;
            }
            
            // Створюємо або знаходимо RealisticTerrainGenerator
            realisticGenerator = GetComponent<RealisticTerrainGenerator>();
            if (realisticGenerator == null)
            {
                realisticGenerator = gameObject.AddComponent<RealisticTerrainGenerator>();
                LogDebug("Створено RealisticTerrainGenerator");
            }
            
            // Створюємо або знаходимо UkrainianTerrainGenerator
            ukrainianGenerator = GetComponent<UkrainianTerrainGenerator>();
            if (ukrainianGenerator == null)
            {
                ukrainianGenerator = gameObject.AddComponent<UkrainianTerrainGenerator>();
                LogDebug("Створено UkrainianTerrainGenerator");
            }
            
            // Створюємо або знаходимо HybridUkrainianTerrainGenerator
            hybridGenerator = GetComponent<HybridUkrainianTerrainGenerator>();
            if (hybridGenerator == null)
            {
                hybridGenerator = gameObject.AddComponent<HybridUkrainianTerrainGenerator>();
                LogDebug("Створено HybridUkrainianTerrainGenerator");
            }
            
            // Налаштовуємо VoxelTerrain для використання гібридного генератора
            voxelTerrain.useHybridGenerator = true;
            voxelTerrain.hybridGenerator = hybridGenerator;
            
            LogDebug("Гібридна система налаштована успішно!");
        }
        
        [ContextMenu("Apply Settings")]
        public void ApplySettings()
        {
            if (hybridGenerator == null)
            {
                LogError("HybridUkrainianTerrainGenerator не знайдено!");
                return;
            }
            
            // Застосовуємо налаштування через рефлексію (оскільки поля приватні)
            var hybridType = typeof(HybridUkrainianTerrainGenerator);
            
            var ukrainianInfluenceField = hybridType.GetField("ukrainianInfluence", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ukrainianInfluenceField?.SetValue(hybridGenerator, ukrainianInfluence);
            
            var realisticInfluenceField = hybridType.GetField("realisticInfluence", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            realisticInfluenceField?.SetValue(hybridGenerator, realisticInfluence);
            
            var adaptBiomesField = hybridType.GetField("adaptBiomesToRegions", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            adaptBiomesField?.SetValue(hybridGenerator, adaptBiomesToRegions);
            
            var useUkrainianElevationsField = hybridType.GetField("useUkrainianElevations", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            useUkrainianElevationsField?.SetValue(hybridGenerator, useUkrainianElevations);
            
            LogDebug($"Налаштування застосовано: Ukrainian={ukrainianInfluence}, Realistic={realisticInfluence}");
        }
        
        [ContextMenu("Test Terrain Generation")]
        public void TestTerrainGeneration()
        {
            if (hybridGenerator == null)
            {
                LogError("Гібридний генератор не налаштований!");
                return;
            }
            
            // Тестуємо генерацію в різних точках
            Vector2[] testPoints = {
                new Vector2(0, 0),      // Центр
                new Vector2(1000, 1000), // Карпати?
                new Vector2(-1000, 500), // Полісся?
                new Vector2(2000, -1000) // Причорномор'я?
            };
            
            LogDebug("=== Тест генерації терену ===");
            
            foreach (var point in testPoints)
            {
                float height = hybridGenerator.GenerateHybridTerrainHeight(point.x, point.y);
                var region = ukrainianGenerator.DetermineRegion(point.x, point.y);
                var biome = hybridGenerator.GetHybridBiome(point.x, point.y, height);
                
                LogDebug($"Точка ({point.x}, {point.y}): Висота={height:F1}м, Регіон={region}, Біом={biome.name}");
            }
        }
        
        [ContextMenu("Show Regional Info")]
        public void ShowRegionalInfo()
        {
            if (ukrainianGenerator == null)
            {
                LogError("UkrainianTerrainGenerator не знайдений!");
                return;
            }
            
            LogDebug("=== Інформація про українські регіони ===");
            
            // Показуємо інформацію про кожен регіон
            Vector2[] regionCenters = {
                new Vector2(500, 1500),   // Карпати
                new Vector2(-500, 1000),  // Полісся
                new Vector2(0, 0),        // Центральна Україна
                new Vector2(1000, -500),  // Донбас
                new Vector2(0, -1000)     // Причорномор'я
            };
            
            foreach (var center in regionCenters)
            {
                var region = ukrainianGenerator.DetermineRegion(center.x, center.y);
                var regionData = ukrainianGenerator.GetRegionData(region);
                var info = ukrainianGenerator.GetRegionInfo(center.x, center.y);
                
                LogDebug($"Регіон {region}: {regionData.description}");
                LogDebug($"  Висота: {regionData.baseElevation}-{regionData.maxElevation}м");
                LogDebug($"  Біом: {regionData.dominantBiome}");
                LogDebug($"  Інфо: {info}");
            }
        }
        
        private void LogDebug(string message)
        {
            if (debugLog)
            {
                Debug.Log($"[HybridTerrainSetup] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[HybridTerrainSetup] {message}");
        }
        
        void OnValidate()
        {
            // Переконуємося що сума впливів не перевищує 1
            float total = ukrainianInfluence + realisticInfluence;
            if (total > 1f)
            {
                float ratio = 1f / total;
                ukrainianInfluence *= ratio;
                realisticInfluence *= ratio;
            }
        }
    }
} 