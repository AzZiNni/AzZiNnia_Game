using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;

namespace Environment
{
    /// <summary>
    /// Система температури з українськими сезонними змінами
    /// Враховує географічне положення, пору року, час доби та біоми
    /// </summary>
    public class TemperatureSystem : MonoBehaviour
    {
        [Header("Сезонні налаштування")]
        [SerializeField] private bool enableSeasonalChanges = true;
        [SerializeField] private float seasonLength = 300f; // секунд на сезон
        [SerializeField] private float currentDay = 0f;
        
        [Header("Температурні діапазони (°C)")]
        [SerializeField] private float winterMinTemp = -25f;  // Українська зима
        [SerializeField] private float winterMaxTemp = 5f;
        [SerializeField] private float springMinTemp = -5f;   // Українська весна
        [SerializeField] private float springMaxTemp = 20f;
        [SerializeField] private float summerMinTemp = 15f;   // Українське літо
        [SerializeField] private float summerMaxTemp = 35f;
        [SerializeField] private float autumnMinTemp = 0f;    // Українська осінь
        [SerializeField] private float autumnMaxTemp = 25f;
        
        [Header("Добові коливання")]
        [SerializeField] private bool enableDayNightCycle = true;
        [SerializeField] private float dayNightTempDifference = 10f; // Різниця між днем і ніччю
        
        [Header("Регіональні особливості")]
        [SerializeField] private float carpathianTempModifier = -8f;    // Карпати холодніші
        [SerializeField] private float coastalTempModifier = 3f;        // Узбережжя тепліше
        [SerializeField] private float steppeTempModifier = 2f;         // Степи жаркіші влітку
        [SerializeField] private float forestTempModifier = -2f;        // Ліси прохолодніші
        
        [Header("Погодні впливи")]
        [SerializeField] private float windTempEffect = -5f;            // Вітер охолоджує
        [SerializeField] private float rainTempEffect = -3f;            // Дощ охолоджує
        [SerializeField] private float snowTempEffect = -8f;            // Сніг сильно охолоджує
        [SerializeField] private float cloudinessTempEffect = -2f;      // Хмарність зменшує температуру
        
        [Header("Налаштування швидкості")]
        [SerializeField] private float timeScale = 1f;                 // Прискорення часу
        [SerializeField] private bool useRealTime = false;              // Використовувати реальний час
        
        // Компоненти системи
        private DayNightCycle dayNightCycle;
        private WeatherSystem weatherSystem;
        private Voxel.UkrainianTerrainGenerator terrainGenerator;
        
        // Поточні дані
        private Season currentSeason = Season.Spring;
        private float currentTemperature = 15f;
        private Dictionary<Vector2Int, float> temperatureMap;
        private Dictionary<Vector2Int, float> temperatureHistory;
        
        // Події
        public System.Action<Season> OnSeasonChanged;
        public System.Action<float> OnTemperatureChanged;
        public System.Action<TemperatureZone> OnTemperatureZoneChanged;
        
        public enum Season
        {
            Spring,  // Весна (березень-травень)
            Summer,  // Літо (червень-серпень)
            Autumn,  // Осінь (вересень-листопад)
            Winter   // Зима (грудень-лютий)
        }
        
        public enum TemperatureZone
        {
            Freezing,    // < -10°C
            Cold,        // -10°C до 0°C
            Cool,        // 0°C до 10°C
            Mild,        // 10°C до 20°C
            Warm,        // 20°C до 30°C
            Hot          // > 30°C
        }
        
        public static TemperatureSystem Instance { get; private set; }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeTemperatureSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            // Знаходимо компоненти
            dayNightCycle = FindFirstObjectByType<DayNightCycle>();
            weatherSystem = FindFirstObjectByType<WeatherSystem>();
            terrainGenerator = FindFirstObjectByType<Voxel.UkrainianTerrainGenerator>();
            
            // Встановлюємо початкову температуру
            SetInitialTemperature();
            
            Debug.Log("🌡️ Система температури ініціалізована");
        }
        
        void Update()
        {
            if (enableSeasonalChanges)
            {
                UpdateSeasonalCycle();
            }
            
            UpdateTemperature();
        }
        
        void InitializeTemperatureSystem()
        {
            temperatureMap = new Dictionary<Vector2Int, float>();
            temperatureHistory = new Dictionary<Vector2Int, float>();
            
            // Встановлюємо початковий сезон на основі дня
            currentSeason = GetSeasonFromDay(currentDay);
        }
        
        void SetInitialTemperature()
        {
            // Встановлюємо температуру на основі поточного сезону
            currentTemperature = GetBaseTemperatureForSeason(currentSeason);
            
            // Додаємо випадковість
            currentTemperature += UnityEngine.Random.Range(-5f, 5f);
            
            OnTemperatureChanged?.Invoke(currentTemperature);
        }
        
        void UpdateSeasonalCycle()
        {
            if (useRealTime)
            {
                // Використовуємо реальний час (приблизно)
                System.DateTime now = System.DateTime.Now;
                currentDay = now.DayOfYear;
            }
            else
            {
                // Використовуємо ігровий час
                currentDay += Time.deltaTime * timeScale / seasonLength;
                
                // Обмежуємо до 365 днів
                if (currentDay > 365f)
                {
                    currentDay = 0f;
                }
            }
            
            // Перевіряємо зміну сезону
            Season newSeason = GetSeasonFromDay(currentDay);
            if (newSeason != currentSeason)
            {
                currentSeason = newSeason;
                OnSeasonChanged?.Invoke(currentSeason);
                Debug.Log($"🍂 Змінився сезон на: {GetSeasonNameUkrainian(currentSeason)}");
            }
        }
        
        Season GetSeasonFromDay(float day)
        {
            // Українські сезони (приблизно):
            // Весна: 1 березня - 31 травня (день 60-151)
            // Літо: 1 червня - 31 серпня (день 152-243)
            // Осінь: 1 вересня - 30 листопада (день 244-334)
            // Зима: 1 грудня - 28 лютого (день 335-59)
            
            float dayOfYear = day % 365f;
            
            if (dayOfYear >= 60f && dayOfYear < 152f)
                return Season.Spring;
            else if (dayOfYear >= 152f && dayOfYear < 244f)
                return Season.Summer;
            else if (dayOfYear >= 244f && dayOfYear < 335f)
                return Season.Autumn;
            else
                return Season.Winter;
        }
        
        void UpdateTemperature()
        {
            // Базова температура для сезону
            float baseTemp = GetBaseTemperatureForSeason(currentSeason);
            
            // Добові коливання
            float timeOfDayModifier = 0f;
            if (enableDayNightCycle && dayNightCycle != null)
            {
                float timeOfDay = dayNightCycle.GetTimeOfDay();
                // Найхолодніше о 6 ранку, найтепліше о 14:00
                float timeNormalized = (timeOfDay - 6f) / 8f; // Нормалізуємо до 0-1
                timeNormalized = Mathf.Clamp01(timeNormalized);
                
                // Використовуємо синусоїдальну криву для плавних змін
                float timeFactor = Mathf.Sin(timeNormalized * Mathf.PI);
                timeOfDayModifier = timeFactor * dayNightTempDifference;
            }
            
            // Погодні впливи
            float weatherModifier = 0f;
            if (weatherSystem != null)
            {
                // TODO: Отримати дані про погоду з WeatherSystem
                // weatherModifier = CalculateWeatherTemperatureEffect();
            }
            
            // Підсумкова температура
            currentTemperature = baseTemp + timeOfDayModifier + weatherModifier;
            
            // Додаємо невеликі випадкові коливання
            currentTemperature += Mathf.PerlinNoise(Time.time * 0.1f, 0f) * 2f - 1f;
            
            OnTemperatureChanged?.Invoke(currentTemperature);
        }
        
        float GetBaseTemperatureForSeason(Season season)
        {
            switch (season)
            {
                case Season.Winter:
                    return UnityEngine.Random.Range(winterMinTemp, winterMaxTemp);
                case Season.Spring:
                    return UnityEngine.Random.Range(springMinTemp, springMaxTemp);
                case Season.Summer:
                    return UnityEngine.Random.Range(summerMinTemp, summerMaxTemp);
                case Season.Autumn:
                    return UnityEngine.Random.Range(autumnMinTemp, autumnMaxTemp);
                default:
                    return 15f;
            }
        }
        
        // Публічні методи
        public float GetTemperatureAtPosition(Vector3 worldPosition)
        {
            Vector2Int gridPos = new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / 100f),
                Mathf.FloorToInt(worldPosition.z / 100f)
            );
            
            // Перевіряємо кеш
            if (temperatureMap.TryGetValue(gridPos, out float cachedTemp))
            {
                return cachedTemp;
            }
            
            // Розраховуємо температуру для позиції
            float temperature = currentTemperature;
            
            // Регіональні модифікатори
            if (terrainGenerator != null)
            {
                var biome = terrainGenerator.GetRegionalBiome(worldPosition.x, worldPosition.z);
                temperature += GetBiomeTemperatureModifier(biome);
                
                // Висотний ефект (температура падає з висотою)
                float elevation = terrainGenerator.GetUkrainianElevation(worldPosition.x, worldPosition.z);
                temperature -= elevation * 0.006f; // 6°C на 1000м висоти
            }
            
            // Кешуємо результат
            temperatureMap[gridPos] = temperature;
            
            return temperature;
        }
        
        float GetBiomeTemperatureModifier(Voxel.BiomeType biome)
        {
            switch (biome)
            {
                case Voxel.BiomeType.Carpathians:
                    return carpathianTempModifier;
                case Voxel.BiomeType.BlackSeaCoast:
                    return coastalTempModifier;
                case Voxel.BiomeType.Steppes:
                    return steppeTempModifier;
                case Voxel.BiomeType.Forests:
                    return forestTempModifier;
                default:
                    return 0f;
            }
        }
        
        public TemperatureZone GetTemperatureZone(float temperature)
        {
            if (temperature < -10f) return TemperatureZone.Freezing;
            if (temperature < 0f) return TemperatureZone.Cold;
            if (temperature < 10f) return TemperatureZone.Cool;
            if (temperature < 20f) return TemperatureZone.Mild;
            if (temperature < 30f) return TemperatureZone.Warm;
            return TemperatureZone.Hot;
        }
        
        public bool IsFreezingTemperature(Vector3 position)
        {
            return GetTemperatureAtPosition(position) < 0f;
        }
        
        public bool CanSnow(Vector3 position)
        {
            return GetTemperatureAtPosition(position) < 2f;
        }
        
        public bool CanRain(Vector3 position)
        {
            float temp = GetTemperatureAtPosition(position);
            return temp > 0f && temp < 35f;
        }
        
        public Season GetCurrentSeason()
        {
            return currentSeason;
        }
        
        public float GetCurrentTemperature()
        {
            return currentTemperature;
        }
        
        public float GetSeasonProgress()
        {
            float seasonDay = currentDay % (365f / 4f);
            return seasonDay / (365f / 4f);
        }
        
        public string GetSeasonNameUkrainian(Season season)
        {
            switch (season)
            {
                case Season.Spring: return "Весна";
                case Season.Summer: return "Літо";
                case Season.Autumn: return "Осінь";
                case Season.Winter: return "Зима";
                default: return "Невідомо";
            }
        }
        
        public string GetTemperatureDescription(float temperature)
        {
            TemperatureZone zone = GetTemperatureZone(temperature);
            
            switch (zone)
            {
                case TemperatureZone.Freezing: return "Морозно";
                case TemperatureZone.Cold: return "Холодно";
                case TemperatureZone.Cool: return "Прохолодно";
                case TemperatureZone.Mild: return "Тепло";
                case TemperatureZone.Warm: return "Жарко";
                case TemperatureZone.Hot: return "Спекотно";
                default: return "Комфортно";
            }
        }
        
        // Методи для налаштувань
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Max(0.1f, scale);
        }
        
        public void SetSeason(Season season)
        {
            if (currentSeason != season)
            {
                currentSeason = season;
                OnSeasonChanged?.Invoke(currentSeason);
                
                // Оновлюємо день відповідно до сезону
                switch (season)
                {
                    case Season.Spring: currentDay = 100f; break;
                    case Season.Summer: currentDay = 200f; break;
                    case Season.Autumn: currentDay = 300f; break;
                    case Season.Winter: currentDay = 50f; break;
                }
            }
        }
        
        public void SetCurrentDay(float day)
        {
            currentDay = Mathf.Clamp(day, 0f, 365f);
            Season newSeason = GetSeasonFromDay(currentDay);
            if (newSeason != currentSeason)
            {
                currentSeason = newSeason;
                OnSeasonChanged?.Invoke(currentSeason);
            }
        }
        
        // Дебаг та статистика
        public string GetTemperatureStats()
        {
            return $"Сезон: {GetSeasonNameUkrainian(currentSeason)}\n" +
                   $"День року: {currentDay:F0}\n" +
                   $"Температура: {currentTemperature:F1}°C ({GetTemperatureDescription(currentTemperature)})\n" +
                   $"Зона: {GetTemperatureZone(currentTemperature)}";
        }
        
        public void ClearTemperatureCache()
        {
            temperatureMap.Clear();
            temperatureHistory.Clear();
        }
        
        void OnDrawGizmosSelected()
        {
            // Показуємо температурні зони в редакторі
            if (temperatureMap != null)
            {
                foreach (var kvp in temperatureMap)
                {
                    Vector3 worldPos = new Vector3(kvp.Key.x * 100f, 0, kvp.Key.y * 100f);
                    
                    // Колір залежно від температури
                    Color tempColor = GetTemperatureColor(kvp.Value);
                    Gizmos.color = tempColor;
                    Gizmos.DrawCube(worldPos, Vector3.one * 50f);
                }
            }
        }
        
        Color GetTemperatureColor(float temperature)
        {
            if (temperature < -10f) return Color.blue;          // Морозно
            if (temperature < 0f) return Color.cyan;            // Холодно
            if (temperature < 10f) return Color.green;          // Прохолодно
            if (temperature < 20f) return Color.yellow;         // Тепло
            if (temperature < 30f) return new Color(1f, 0.5f, 0f); // Orange approximation
            return Color.red;                                   // Спекотно
        }
        
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
} 