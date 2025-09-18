using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace Voxel
{
    /// <summary>
    /// Генератор заготовок українських сіл та міст
    /// Створює основні локації з інфраструктурою для майбутнього розвитку
    /// </summary>
    public class UkrainianSettlementGenerator : MonoBehaviour
    {
        [Header("Налаштування генерації")]
        [SerializeField] private bool generateSettlements = true;
        [SerializeField] private int maxVillages = 15;
        [SerializeField] private int maxTowns = 5;
        [SerializeField] private int maxCities = 3;
        
        [Header("Відстані між поселеннями")]
        [SerializeField] private float minVillageDistance = 300f;
        [SerializeField] private float minTownDistance = 600f;
        [SerializeField] private float minCityDistance = 1000f;
        
        [Header("Розміри поселень")]
        [SerializeField] private float villageRadius = 100f;
        [SerializeField] private float townRadius = 200f;
        [SerializeField] private float cityRadius = 400f;
        
        [Header("Історичні міста України")]
        [SerializeField] private bool generateHistoricalCities = true;
        
        [Header("Префаби та маркери")]
        [SerializeField] private GameObject villageMarkerPrefab;
        [SerializeField] private GameObject townMarkerPrefab;
        [SerializeField] private GameObject cityMarkerPrefab;
        [SerializeField] private GameObject roadMarkerPrefab;
        [SerializeField] private GameObject fieldMarkerPrefab;
        
        [Header("Інфраструктура")]
        [SerializeField] private bool generateRoads = true;
        [SerializeField] private bool generateFields = true;
        [SerializeField] private bool generateMarketplaces = true;
        [SerializeField] private float roadWidth = 8f;
        
        // Приватні змінні
        private UkrainianTerrainGenerator terrainGenerator;
        private UkrainianRiverSystem riverSystem;
        private List<Settlement> allSettlements;
        private List<Road> allRoads;
        private List<GameObject> settlementMarkers;
        private HistoricalCity[] historicalCities;
        
        [System.Serializable]
        public struct HistoricalCity
        {
            public string name;
            public string nameUkrainian;
            public Vector2 mapPosition;
            public SettlementType type;
            public string historicalPeriod;
            public string description;
            public bool isCapital;
        }
        
        public struct Settlement
        {
            public string name;
            public string nameUkrainian;
            public Vector3 position;
            public SettlementType type;
            public float radius;
            public int population;
            public List<Building> buildings;
            public List<Vector3> roads;
            public List<Vector3> fields;
            public bool hasMarketplace;
            public bool hasChurch;
            public bool hasFortification;
            public Voxel.BiomeType biome;
            public string foundingPeriod;
        }
        
        public struct Building
        {
            public Vector3 position;
            public BuildingType type;
            public float size;
            public string name;
        }
        
        public struct Road
        {
            public Vector3 start;
            public Vector3 end;
            public float width;
            public RoadType type;
            public List<Vector3> waypoints;
        }
        
        public enum SettlementType
        {
            Village,        // Село
            Town,           // Містечко
            City,           // Місто
            RegionalCenter, // Обласний центр
            Capital         // Столиця
        }
        
        public enum BuildingType
        {
            House,          // Хата
            Church,         // Церква
            Marketplace,    // Ринок
            Windmill,       // Вітряк
            Blacksmith,     // Кузня
            Well,           // Колодязь
            Barn,           // Комора
            Fortress        // Фортеця
        }
        
        public enum RoadType
        {
            Path,           // Стежка
            Village,        // Сільська дорога
            Regional,       // Регіональна дорога
            Main            // Головна дорога
        }
        
        void Start()
        {
            terrainGenerator = GetComponent<UkrainianTerrainGenerator>();
            riverSystem = GetComponent<UkrainianRiverSystem>();
            
            InitializeSettlementSystem();
            
            if (generateSettlements)
            {
                GenerateUkrainianSettlements();
            }
            
            Debug.Log("🏘️ Система українських поселень ініціалізована");
        }
        
        void InitializeSettlementSystem()
        {
            allSettlements = new List<Settlement>();
            allRoads = new List<Road>();
            settlementMarkers = new List<GameObject>();
            
            // Ініціалізуємо історичні міста якщо не задані
            if (historicalCities == null || historicalCities.Length == 0)
            {
                InitializeHistoricalCities();
            }
        }
        
        void InitializeHistoricalCities()
        {
            historicalCities = new HistoricalCity[]
            {
                new HistoricalCity
                {
                    name = "Kyiv",
                    nameUkrainian = "Київ",
                    mapPosition = new Vector2(0, 0), // Центр карти
                    type = SettlementType.Capital,
                    historicalPeriod = "482 р.",
                    description = "Мати міст руських, столиця України",
                    isCapital = true
                },
                new HistoricalCity
                {
                    name = "Lviv",
                    nameUkrainian = "Львів",
                    mapPosition = new Vector2(-800, 200),
                    type = SettlementType.RegionalCenter,
                    historicalPeriod = "1256 р.",
                    description = "Культурна столиця України",
                    isCapital = false
                },
                new HistoricalCity
                {
                    name = "Kharkiv",
                    nameUkrainian = "Харків",
                    mapPosition = new Vector2(600, 300),
                    type = SettlementType.RegionalCenter,
                    historicalPeriod = "1654 р.",
                    description = "Перша столиця Радянської України",
                    isCapital = false
                },
                new HistoricalCity
                {
                    name = "Odesa",
                    nameUkrainian = "Одеса",
                    mapPosition = new Vector2(-200, -800),
                    type = SettlementType.RegionalCenter,
                    historicalPeriod = "1794 р.",
                    description = "Перлина біля моря",
                    isCapital = false
                },
                new HistoricalCity
                {
                    name = "Dnipro",
                    nameUkrainian = "Дніпро",
                    mapPosition = new Vector2(200, -200),
                    type = SettlementType.RegionalCenter,
                    historicalPeriod = "1776 р.",
                    description = "Промисловий центр України",
                    isCapital = false
                },
                new HistoricalCity
                {
                    name = "Chernihiv",
                    nameUkrainian = "Чернігів",
                    mapPosition = new Vector2(-100, 400),
                    type = SettlementType.City,
                    historicalPeriod = "907 р.",
                    description = "Древній центр Чернігівського князівства",
                    isCapital = false
                },
                new HistoricalCity
                {
                    name = "Poltava",
                    nameUkrainian = "Полтава",
                    mapPosition = new Vector2(300, 100),
                    type = SettlementType.City,
                    historicalPeriod = "1174 р.",
                    description = "Місце Полтавської битви",
                    isCapital = false
                },
                new HistoricalCity
                {
                    name = "Cherkasy",
                    nameUkrainian = "Черкаси",
                    mapPosition = new Vector2(100, -100),
                    type = SettlementType.City,
                    historicalPeriod = "1286 р.",
                    description = "Козацький центр",
                    isCapital = false
                }
            };
        }
        
        void GenerateUkrainianSettlements()
        {
            // Спочатку генеруємо історичні міста
            if (generateHistoricalCities)
            {
                GenerateHistoricalCities();
            }
            
            // Потім генеруємо випадкові поселення
            GenerateRandomVillages();
            GenerateRandomTowns();
            
            // Генеруємо дороги між поселеннями
            if (generateRoads)
            {
                GenerateRoadNetwork();
            }
            
            // Генеруємо поля навколо поселень
            if (generateFields)
            {
                GenerateAgricultureFields();
            }
            
            Debug.Log($"🏘️ Згенеровано {allSettlements.Count} поселень");
        }
        
        void GenerateHistoricalCities()
        {
            foreach (var historicalCity in historicalCities)
            {
                Vector3 worldPosition = new Vector3(historicalCity.mapPosition.x, 0, historicalCity.mapPosition.y);
                
                // Коригуємо висоту на основі терену
                if (terrainGenerator != null)
                {
                    float terrainHeight = terrainGenerator.GetUkrainianElevation(worldPosition.x, worldPosition.z);
                    worldPosition.y = terrainHeight + 2f;
                }
                
                Settlement settlement = new Settlement
                {
                    name = historicalCity.name,
                    nameUkrainian = historicalCity.nameUkrainian,
                    position = worldPosition,
                    type = historicalCity.type,
                    radius = GetRadiusForSettlementType(historicalCity.type),
                    population = GetPopulationForSettlementType(historicalCity.type),
                    buildings = new List<Building>(),
                    roads = new List<Vector3>(),
                    fields = new List<Vector3>(),
                    hasMarketplace = true,
                    hasChurch = true,
                    hasFortification = historicalCity.type >= SettlementType.City,
                    biome = terrainGenerator?.GetRegionalBiome(worldPosition.x, worldPosition.z) ?? Voxel.BiomeType.Plains,
                    foundingPeriod = historicalCity.historicalPeriod
                };
                
                // Генеруємо будівлі для міста
                GenerateBuildingsForSettlement(ref settlement);
                
                allSettlements.Add(settlement);
                CreateSettlementMarker(settlement);
                
                Debug.Log($"🏛️ Створено історичне місто: {settlement.nameUkrainian}");
            }
        }
        
        void GenerateRandomVillages()
        {
            int villagesGenerated = 0;
            int attempts = 0;
            int maxAttempts = maxVillages * 5;
            
            while (villagesGenerated < maxVillages && attempts < maxAttempts)
            {
                attempts++;
                
                Vector3 position = GenerateRandomPosition();
                
                if (IsValidSettlementPosition(position, SettlementType.Village))
                {
                    Settlement village = CreateVillage(position);
                    allSettlements.Add(village);
                    CreateSettlementMarker(village);
                    villagesGenerated++;
                }
            }
            
            Debug.Log($"🏘️ Згенеровано {villagesGenerated} сіл");
        }
        
        void GenerateRandomTowns()
        {
            int townsGenerated = 0;
            int attempts = 0;
            int maxAttempts = maxTowns * 5;
            
            while (townsGenerated < maxTowns && attempts < maxAttempts)
            {
                attempts++;
                
                Vector3 position = GenerateRandomPosition();
                
                if (IsValidSettlementPosition(position, SettlementType.Town))
                {
                    Settlement town = CreateTown(position);
                    allSettlements.Add(town);
                    CreateSettlementMarker(town);
                    townsGenerated++;
                }
            }
            
            Debug.Log($"🏘️ Згенеровано {townsGenerated} містечок");
        }
        
        Vector3 GenerateRandomPosition()
        {
            float x = UnityEngine.Random.Range(-800f, 800f);
            float z = UnityEngine.Random.Range(-800f, 800f);
            float y = 0f;
            
            if (terrainGenerator != null)
            {
                y = terrainGenerator.GetUkrainianElevation(x, z) + 2f;
            }
            
            return new Vector3(x, y, z);
        }
        
        bool IsValidSettlementPosition(Vector3 position, SettlementType type)
        {
            float minDistance = GetMinDistanceForSettlementType(type);
            
            foreach (var settlement in allSettlements)
            {
                float distance = Vector3.Distance(position, settlement.position);
                if (distance < minDistance)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        Settlement CreateVillage(Vector3 position)
        {
            Settlement village = new Settlement
            {
                name = GenerateVillageName(),
                nameUkrainian = GenerateVillageNameUkrainian(),
                position = position,
                type = SettlementType.Village,
                radius = villageRadius,
                population = UnityEngine.Random.Range(50, 300),
                buildings = new List<Building>(),
                roads = new List<Vector3>(),
                fields = new List<Vector3>(),
                hasMarketplace = UnityEngine.Random.value < 0.3f,
                hasChurch = UnityEngine.Random.value < 0.8f,
                hasFortification = false,
                biome = terrainGenerator?.GetRegionalBiome(position.x, position.z) ?? Voxel.BiomeType.Plains,
                foundingPeriod = GenerateFoundingPeriod()
            };
            
            GenerateBuildingsForSettlement(ref village);
            return village;
        }
        
        Settlement CreateTown(Vector3 position)
        {
            Settlement town = new Settlement
            {
                name = GenerateTownName(),
                nameUkrainian = GenerateTownNameUkrainian(),
                position = position,
                type = SettlementType.Town,
                radius = townRadius,
                population = UnityEngine.Random.Range(500, 2000),
                buildings = new List<Building>(),
                roads = new List<Vector3>(),
                fields = new List<Vector3>(),
                hasMarketplace = true,
                hasChurch = true,
                hasFortification = UnityEngine.Random.value < 0.5f,
                biome = terrainGenerator?.GetRegionalBiome(position.x, position.z) ?? Voxel.BiomeType.Plains,
                foundingPeriod = GenerateFoundingPeriod()
            };
            
            GenerateBuildingsForSettlement(ref town);
            return town;
        }
        
        void GenerateBuildingsForSettlement(ref Settlement settlement)
        {
            int buildingCount = GetBuildingCountForSettlement(settlement.type);
            
            for (int i = 0; i < buildingCount; i++)
            {
                Vector3 buildingPos = GenerateBuildingPosition(settlement);
                BuildingType buildingType = GetRandomBuildingType(settlement.type);
                
                Building building = new Building
                {
                    position = buildingPos,
                    type = buildingType,
                    size = GetBuildingSizeForType(buildingType),
                    name = GenerateBuildingName(buildingType)
                };
                
                settlement.buildings.Add(building);
            }
            
            // Додаємо обов'язкові будівлі
            if (settlement.hasChurch)
            {
                AddSpecialBuilding(ref settlement, BuildingType.Church);
            }
            
            if (settlement.hasMarketplace)
            {
                AddSpecialBuilding(ref settlement, BuildingType.Marketplace);
            }
        }
        
        Vector3 GenerateBuildingPosition(Settlement settlement)
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(10f, settlement.radius * 0.8f);
            
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );
            
            Vector3 buildingPos = settlement.position + offset;
            
            if (terrainGenerator != null)
            {
                buildingPos.y = terrainGenerator.GetUkrainianElevation(buildingPos.x, buildingPos.z) + 1f;
            }
            
            return buildingPos;
        }
        
        void AddSpecialBuilding(ref Settlement settlement, BuildingType type)
        {
            Vector3 centralPos = settlement.position;
            
            if (type == BuildingType.Marketplace)
            {
                centralPos += new Vector3(20f, 0f, 0f);
            }
            
            Building specialBuilding = new Building
            {
                position = centralPos,
                type = type,
                size = GetBuildingSizeForType(type),
                name = GenerateBuildingName(type)
            };
            
            settlement.buildings.Add(specialBuilding);
        }
        
        void GenerateRoadNetwork()
        {
            var sortedSettlements = allSettlements.OrderByDescending(s => (int)s.type).ToList();
            
            foreach (var settlement in sortedSettlements)
            {
                var nearbySettlements = FindNearbySettlements(settlement, 3);
                
                foreach (var nearby in nearbySettlements)
                {
                    if (!RoadExists(settlement.position, nearby.position))
                    {
                        Road road = CreateRoad(settlement, nearby);
                        allRoads.Add(road);
                        CreateRoadMarkers(road);
                    }
                }
            }
            
            Debug.Log($"🛤️ Згенеровано {allRoads.Count} доріг");
        }
        
        List<Settlement> FindNearbySettlements(Settlement settlement, int maxCount)
        {
            return allSettlements
                .Where(s => s.position != settlement.position)
                .OrderBy(s => Vector3.Distance(settlement.position, s.position))
                .Take(maxCount)
                .ToList();
        }
        
        bool RoadExists(Vector3 start, Vector3 end)
        {
            foreach (var road in allRoads)
            {
                if ((Vector3.Distance(road.start, start) < 10f && Vector3.Distance(road.end, end) < 10f) ||
                    (Vector3.Distance(road.start, end) < 10f && Vector3.Distance(road.end, start) < 10f))
                {
                    return true;
                }
            }
            return false;
        }
        
        Road CreateRoad(Settlement from, Settlement to)
        {
            RoadType roadType = GetRoadType(from.type, to.type);
            
            Road road = new Road
            {
                start = from.position,
                end = to.position,
                width = roadWidth,
                type = roadType,
                waypoints = GenerateRoadWaypoints(from.position, to.position)
            };
            
            return road;
        }
        
        List<Vector3> GenerateRoadWaypoints(Vector3 start, Vector3 end)
        {
            List<Vector3> waypoints = new List<Vector3>();
            waypoints.Add(start);
            
            int waypointCount = Mathf.RoundToInt(Vector3.Distance(start, end) / 200f);
            
            for (int i = 1; i < waypointCount; i++)
            {
                float t = (float)i / waypointCount;
                Vector3 lerped = Vector3.Lerp(start, end, t);
                
                lerped += new Vector3(
                    UnityEngine.Random.Range(-50f, 50f),
                    0f,
                    UnityEngine.Random.Range(-50f, 50f)
                );
                
                if (terrainGenerator != null)
                {
                    lerped.y = terrainGenerator.GetUkrainianElevation(lerped.x, lerped.z) + 1f;
                }
                
                waypoints.Add(lerped);
            }
            
            waypoints.Add(end);
            return waypoints;
        }
        
        void GenerateAgricultureFields()
        {
            foreach (var settlement in allSettlements)
            {
                int fieldCount = GetFieldCountForSettlement(settlement.type);
                
                for (int i = 0; i < fieldCount; i++)
                {
                    Vector3 fieldPosition = GenerateFieldPosition(settlement);
                    CreateFieldMarker(fieldPosition);
                }
            }
        }
        
        Vector3 GenerateFieldPosition(Settlement settlement)
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(settlement.radius, settlement.radius * 2f);
            
            Vector3 fieldPos = settlement.position + new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );
            
            if (terrainGenerator != null)
            {
                fieldPos.y = terrainGenerator.GetUkrainianElevation(fieldPos.x, fieldPos.z);
            }
            
            return fieldPos;
        }
        
        string GenerateVillageName()
        {
            string[] prefixes = { "Nova", "Stara", "Velyka", "Mala" };
            string[] suffixes = { "ka", "vka", "tsi", "yne" };
            
            return prefixes[UnityEngine.Random.Range(0, prefixes.Length)] + 
                   suffixes[UnityEngine.Random.Range(0, suffixes.Length)];
        }
        
        string GenerateVillageNameUkrainian()
        {
            string[] prefixes = { "Нова", "Стара", "Велика", "Мала" };
            string[] suffixes = { "ка", "вка", "ці", "ине" };
            
            return prefixes[UnityEngine.Random.Range(0, prefixes.Length)] + 
                   suffixes[UnityEngine.Random.Range(0, suffixes.Length)];
        }
        
        string GenerateTownName()
        {
            string[] names = { "Kozatsk", "Stepove", "Richne", "Polske" };
            return names[UnityEngine.Random.Range(0, names.Length)];
        }
        
        string GenerateTownNameUkrainian()
        {
            string[] names = { "Козацьк", "Степове", "Річне", "Польське" };
            return names[UnityEngine.Random.Range(0, names.Length)];
        }
        
        string GenerateFoundingPeriod()
        {
            int year = UnityEngine.Random.Range(1200, 1800);
            return $"{year} р.";
        }
        
        string GenerateBuildingName(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Church: return "Церква Св. Миколая";
                case BuildingType.Marketplace: return "Торговий майдан";
                case BuildingType.Windmill: return "Вітряк";
                case BuildingType.Blacksmith: return "Кузня";
                case BuildingType.Well: return "Криниця";
                default: return "Хата";
            }
        }
        
        float GetRadiusForSettlementType(SettlementType type)
        {
            switch (type)
            {
                case SettlementType.Village: return villageRadius;
                case SettlementType.Town: return townRadius;
                case SettlementType.City: return cityRadius;
                case SettlementType.RegionalCenter: return cityRadius * 1.5f;
                case SettlementType.Capital: return cityRadius * 2f;
                default: return villageRadius;
            }
        }
        
        int GetPopulationForSettlementType(SettlementType type)
        {
            switch (type)
            {
                case SettlementType.Village: return UnityEngine.Random.Range(50, 300);
                case SettlementType.Town: return UnityEngine.Random.Range(500, 2000);
                case SettlementType.City: return UnityEngine.Random.Range(2000, 10000);
                case SettlementType.RegionalCenter: return UnityEngine.Random.Range(10000, 50000);
                case SettlementType.Capital: return UnityEngine.Random.Range(50000, 200000);
                default: return 100;
            }
        }
        
        float GetMinDistanceForSettlementType(SettlementType type)
        {
            switch (type)
            {
                case SettlementType.Village: return minVillageDistance;
                case SettlementType.Town: return minTownDistance;
                default: return minCityDistance;
            }
        }
        
        int GetBuildingCountForSettlement(SettlementType type)
        {
            switch (type)
            {
                case SettlementType.Village: return UnityEngine.Random.Range(5, 15);
                case SettlementType.Town: return UnityEngine.Random.Range(15, 40);
                case SettlementType.City: return UnityEngine.Random.Range(40, 100);
                default: return UnityEngine.Random.Range(100, 300);
            }
        }
        
        int GetFieldCountForSettlement(SettlementType type)
        {
            switch (type)
            {
                case SettlementType.Village: return UnityEngine.Random.Range(3, 8);
                case SettlementType.Town: return UnityEngine.Random.Range(8, 15);
                default: return UnityEngine.Random.Range(15, 30);
            }
        }
        
        BuildingType GetRandomBuildingType(SettlementType settlementType)
        {
            if (UnityEngine.Random.value < 0.7f)
                return BuildingType.House;
            
            BuildingType[] otherTypes = { BuildingType.Well, BuildingType.Barn, BuildingType.Windmill, BuildingType.Blacksmith };
            return otherTypes[UnityEngine.Random.Range(0, otherTypes.Length)];
        }
        
        float GetBuildingSizeForType(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.House: return UnityEngine.Random.Range(8f, 12f);
                case BuildingType.Church: return UnityEngine.Random.Range(15f, 25f);
                case BuildingType.Marketplace: return UnityEngine.Random.Range(20f, 30f);
                case BuildingType.Windmill: return UnityEngine.Random.Range(10f, 15f);
                case BuildingType.Fortress: return UnityEngine.Random.Range(30f, 50f);
                default: return 10f;
            }
        }
        
        RoadType GetRoadType(SettlementType from, SettlementType to)
        {
            if (from >= SettlementType.City || to >= SettlementType.City)
                return RoadType.Main;
            if (from >= SettlementType.Town || to >= SettlementType.Town)
                return RoadType.Regional;
            return RoadType.Village;
        }
        
        void CreateSettlementMarker(Settlement settlement)
        {
            GameObject prefab = GetMarkerPrefabForSettlement(settlement.type);
            if (prefab != null)
            {
                GameObject marker = Instantiate(prefab, settlement.position, Quaternion.identity);
                marker.name = $"Settlement_{settlement.nameUkrainian}";
                marker.transform.SetParent(transform);
                settlementMarkers.Add(marker);
                
                var info = marker.AddComponent<SettlementInfo>();
                info.settlement = settlement;
            }
        }
        
        GameObject GetMarkerPrefabForSettlement(SettlementType type)
        {
            switch (type)
            {
                case SettlementType.Village: return villageMarkerPrefab;
                case SettlementType.Town: return townMarkerPrefab;
                default: return cityMarkerPrefab;
            }
        }
        
        void CreateRoadMarkers(Road road)
        {
            if (roadMarkerPrefab == null) return;
            
            for (int i = 0; i < road.waypoints.Count - 1; i++)
            {
                Vector3 start = road.waypoints[i];
                Vector3 end = road.waypoints[i + 1];
                Vector3 direction = (end - start).normalized;
                
                float distance = Vector3.Distance(start, end);
                int markerCount = Mathf.RoundToInt(distance / 50f);
                
                for (int j = 0; j <= markerCount; j++)
                {
                    float t = (float)j / markerCount;
                    Vector3 markerPos = Vector3.Lerp(start, end, t);
                    
                    GameObject roadMarker = Instantiate(roadMarkerPrefab, markerPos, Quaternion.LookRotation(direction));
                    roadMarker.transform.SetParent(transform);
                }
            }
        }
        
        void CreateFieldMarker(Vector3 position)
        {
            if (fieldMarkerPrefab != null)
            {
                GameObject fieldMarker = Instantiate(fieldMarkerPrefab, position, Quaternion.identity);
                fieldMarker.transform.SetParent(transform);
            }
        }
        
        public List<Settlement> GetAllSettlements()
        {
            return new List<Settlement>(allSettlements);
        }
        
        public Settlement GetNearestSettlement(Vector3 position)
        {
            if (allSettlements.Count == 0) return default(Settlement);
            
            Settlement nearest = allSettlements[0];
            float minDistance = Vector3.Distance(position, nearest.position);
            
            foreach (var settlement in allSettlements)
            {
                float distance = Vector3.Distance(position, settlement.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = settlement;
                }
            }
            
            return nearest;
        }
        
        public bool IsPositionInSettlement(Vector3 position, out Settlement settlement)
        {
            settlement = default(Settlement);
            
            foreach (var s in allSettlements)
            {
                if (Vector3.Distance(position, s.position) < s.radius)
                {
                    settlement = s;
                    return true;
                }
            }
            
            return false;
        }
        
        public class SettlementInfo : MonoBehaviour
        {
            public Settlement settlement;
            
            void OnDrawGizmosSelected()
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(settlement.position, settlement.radius);
            }
        }
        
        void OnDrawGizmosSelected()
        {
            if (allSettlements == null) return;
            
            foreach (var settlement in allSettlements)
            {
                switch (settlement.type)
                {
                    case SettlementType.Village: Gizmos.color = Color.green; break;
                    case SettlementType.Town: Gizmos.color = Color.blue; break;
                    case SettlementType.City: Gizmos.color = Color.red; break;
                    case SettlementType.RegionalCenter: Gizmos.color = Color.magenta; break;
                    case SettlementType.Capital: Gizmos.color = Color.yellow; break;
                }
                
                Gizmos.DrawSphere(settlement.position, 10f);
                Gizmos.DrawWireSphere(settlement.position, settlement.radius);
            }
            
            Gizmos.color = Color.brown;
            foreach (var road in allRoads)
            {
                for (int i = 0; i < road.waypoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(road.waypoints[i], road.waypoints[i + 1]);
                }
            }
        }
    }
} 