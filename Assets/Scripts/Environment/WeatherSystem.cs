using UnityEngine;
using System.Collections.Generic;
using System;

namespace Environment
{
    /// <summary>
    /// Система погоди з реалістичним українським кліматом
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        [Header("Типи погоди")]
        [SerializeField] private WeatherType currentWeather = WeatherType.Clear;
        [SerializeField] private float weatherTransitionDuration = 30f;
        
        [Header("Ефекти погоди")]
        [SerializeField] private ParticleSystem rainEffect;
        [SerializeField] private ParticleSystem snowEffect;
        [SerializeField] private ParticleSystem fogEffect;
        
        [Header("Налаштування вітру")]
        [SerializeField] private WindZone windZone;
        [SerializeField] private float baseWindStrength = 1f;
        [SerializeField] private float windVariation = 0.5f;
        
        [Header("Хмари")]
        [SerializeField] private Material cloudMaterial;
        [SerializeField] private float cloudDensity = 0.5f;
        [SerializeField] private float cloudSpeed = 0.1f;
        
        [Header("Сезонні налаштування")]
        [SerializeField] private SeasonSettings[] seasonSettings;
        
        [Header("Налаштування погоди")]
        public WeatherPreset[] weatherPresets;
        public float weatherChangeSpeed = 0.5f;
        
        [Header("Компоненти")]
        public ParticleSystem rainParticles;
        public ParticleSystem snowParticles;
        
        private Light mainLight;
        private DayNightCycle dayNightCycle;
        private float weatherTimer;
        private WeatherType targetWeather;
        private float transitionProgress;
        
        // Події
        public event Action<WeatherType> OnWeatherChanged;
        public event Action<Season> OnSeasonChanged;
        
        private Season currentSeason;
        
        public enum WeatherType
        {
            Clear,      // Ясно
            Cloudy,     // Хмарно
            Overcast,   // Похмуро
            LightRain,  // Легкий дощ
            Rain,       // Дощ
            Storm,      // Гроза
            LightSnow,  // Легкий сніг
            Snow,       // Сніг
            Blizzard,   // Хуртовина
            Fog,        // Туман
            Windy       // Вітряно
        }
        
        public enum Season
        {
            Spring,     // Весна
            Summer,     // Літо
            Autumn,     // Осінь
            Winter      // Зима
        }
        
        [System.Serializable]
        public class SeasonSettings
        {
            public Season season;
            public WeatherProbability[] weatherProbabilities;
            public float temperature;
            public float humidity;
            public Color vegetationColor;
        }
        
        [System.Serializable]
        public class WeatherProbability
        {
            public WeatherType weather;
            [Range(0, 100)]
            public float probability;
        }
        
        [System.Serializable]
        public class WeatherPreset
        {
            public string name;
            public WeatherType weatherType;
            [Range(0, 1)] public float intensity = 1f;
            public Color skyColor = Color.white;
            public Color lightColor = Color.white;
            public float lightIntensity = 1f;
            public float windSpeed = 10f;
        }
        
        void Start()
        {
            mainLight = GetComponent<Light>();
            dayNightCycle = FindFirstObjectByType<DayNightCycle>();
            
            if (windZone == null)
            {
                GameObject wind = new GameObject("Wind Zone");
                windZone = wind.AddComponent<WindZone>();
                windZone.mode = WindZoneMode.Directional;
                windZone.windMain = baseWindStrength;
            }
            
            // Ініціалізуємо сезонні налаштування
            InitializeSeasons();
            
            // Створюємо ефекти погоди якщо не призначені
            CreateWeatherEffects();
            
            // Визначаємо поточний сезон
            UpdateSeason();
            
            // Починаємо з випадкової погоди
            SetRandomWeatherForSeason();
        }
        
        void InitializeSeasons()
        {
            if (seasonSettings == null || seasonSettings.Length == 0)
            {
                seasonSettings = new SeasonSettings[]
                {
                    // Весна
                    new SeasonSettings
                    {
                        season = Season.Spring,
                        temperature = 15f,
                        humidity = 60f,
                        vegetationColor = new Color(0.5f, 0.8f, 0.3f),
                        weatherProbabilities = new WeatherProbability[]
                        {
                            new WeatherProbability { weather = WeatherType.Clear, probability = 30f },
                            new WeatherProbability { weather = WeatherType.Cloudy, probability = 25f },
                            new WeatherProbability { weather = WeatherType.LightRain, probability = 20f },
                            new WeatherProbability { weather = WeatherType.Rain, probability = 15f },
                            new WeatherProbability { weather = WeatherType.Fog, probability = 10f }
                        }
                    },
                    // Літо
                    new SeasonSettings
                    {
                        season = Season.Summer,
                        temperature = 25f,
                        humidity = 50f,
                        vegetationColor = new Color(0.3f, 0.7f, 0.2f),
                        weatherProbabilities = new WeatherProbability[]
                        {
                            new WeatherProbability { weather = WeatherType.Clear, probability = 50f },
                            new WeatherProbability { weather = WeatherType.Cloudy, probability = 20f },
                            new WeatherProbability { weather = WeatherType.LightRain, probability = 15f },
                            new WeatherProbability { weather = WeatherType.Storm, probability = 10f },
                            new WeatherProbability { weather = WeatherType.Windy, probability = 5f }
                        }
                    },
                    // Осінь
                    new SeasonSettings
                    {
                        season = Season.Autumn,
                        temperature = 10f,
                        humidity = 70f,
                        vegetationColor = new Color(0.8f, 0.6f, 0.2f),
                        weatherProbabilities = new WeatherProbability[]
                        {
                            new WeatherProbability { weather = WeatherType.Cloudy, probability = 30f },
                            new WeatherProbability { weather = WeatherType.Overcast, probability = 20f },
                            new WeatherProbability { weather = WeatherType.Rain, probability = 20f },
                            new WeatherProbability { weather = WeatherType.Fog, probability = 15f },
                            new WeatherProbability { weather = WeatherType.Windy, probability = 15f }
                        }
                    },
                    // Зима
                    new SeasonSettings
                    {
                        season = Season.Winter,
                        temperature = -5f,
                        humidity = 80f,
                        vegetationColor = new Color(0.8f, 0.8f, 0.9f),
                        weatherProbabilities = new WeatherProbability[]
                        {
                            new WeatherProbability { weather = WeatherType.Overcast, probability = 25f },
                            new WeatherProbability { weather = WeatherType.LightSnow, probability = 20f },
                            new WeatherProbability { weather = WeatherType.Snow, probability = 20f },
                            new WeatherProbability { weather = WeatherType.Cloudy, probability = 15f },
                            new WeatherProbability { weather = WeatherType.Blizzard, probability = 10f },
                            new WeatherProbability { weather = WeatherType.Clear, probability = 10f }
                        }
                    }
                };
            }
        }
        
        void CreateWeatherEffects()
        {
            // Дощ
            if (rainEffect == null)
            {
                GameObject rain = new GameObject("Rain Effect");
                rain.transform.SetParent(transform);
                rainEffect = rain.AddComponent<ParticleSystem>();
                
                var main = rainEffect.main;
                main.maxParticles = 5000;
                main.startLifetime = 2f;
                main.startSpeed = 10f;
                main.startSize = 0.1f;
                main.startColor = new Color(0.5f, 0.5f, 0.7f, 0.5f);
                
                var shape = rainEffect.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(50, 1, 50);
                shape.position = new Vector3(0, 20, 0);
                
                var emission = rainEffect.emission;
                emission.rateOverTime = 1000;
                
                rainEffect.gameObject.SetActive(false);
            }
            
            // Сніг
            if (snowEffect == null)
            {
                GameObject snow = new GameObject("Snow Effect");
                snow.transform.SetParent(transform);
                snowEffect = snow.AddComponent<ParticleSystem>();
                
                var main = snowEffect.main;
                main.maxParticles = 3000;
                main.startLifetime = 5f;
                main.startSpeed = 2f;
                main.startSize = 0.3f;
                main.gravityModifier = 0.1f;
                
                var shape = snowEffect.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(50, 1, 50);
                shape.position = new Vector3(0, 20, 0);
                
                var emission = snowEffect.emission;
                emission.rateOverTime = 500;
                
                snowEffect.gameObject.SetActive(false);
            }
        }
        
        void Update()
        {
            // Оновлюємо погоду
            weatherTimer += Time.deltaTime;
            
            // Змінюємо погоду кожні 30-60 хвилин
            if (weatherTimer > UnityEngine.Random.Range(1800f, 3600f))
            {
                weatherTimer = 0f;
                SetRandomWeatherForSeason();
            }
            
            // Плавний перехід між погодами
            if (currentWeather != targetWeather)
            {
                transitionProgress += Time.deltaTime / weatherTransitionDuration;
                if (transitionProgress >= 1f)
                {
                    currentWeather = targetWeather;
                    transitionProgress = 0f;
                    OnWeatherChanged?.Invoke(currentWeather);
                }
                
                UpdateWeatherEffects(transitionProgress);
            }
            
            // Оновлюємо вітер
            UpdateWind();
            
            // Оновлюємо сезон
            if (dayNightCycle != null && dayNightCycle.CurrentDay == 1)
            {
                UpdateSeason();
            }
        }
        
        void UpdateSeason()
        {
            if (dayNightCycle == null) return;
            
            int month = dayNightCycle.CurrentMonth;
            Season newSeason = GetSeasonByMonth(month);
            
            if (newSeason != currentSeason)
            {
                currentSeason = newSeason;
                OnSeasonChanged?.Invoke(currentSeason);
            }
        }
        
        Season GetSeasonByMonth(int month)
        {
            if (month >= 3 && month <= 5) return Season.Spring;
            if (month >= 6 && month <= 8) return Season.Summer;
            if (month >= 9 && month <= 11) return Season.Autumn;
            return Season.Winter;
        }
        
        void SetRandomWeatherForSeason()
        {
            var settings = GetSeasonSettings(currentSeason);
            if (settings == null || settings.weatherProbabilities == null) return;
            
            // Вибираємо погоду на основі ймовірностей
            float totalProbability = 0f;
            foreach (var wp in settings.weatherProbabilities)
            {
                totalProbability += wp.probability;
            }
            
            float random = UnityEngine.Random.Range(0f, totalProbability);
            float currentProb = 0f;
            
            foreach (var wp in settings.weatherProbabilities)
            {
                currentProb += wp.probability;
                if (random <= currentProb)
                {
                    targetWeather = wp.weather;
                    break;
                }
            }
        }
        
        SeasonSettings GetSeasonSettings(Season season)
        {
            foreach (var settings in seasonSettings)
            {
                if (settings.season == season)
                    return settings;
            }
            return null;
        }
        
        void UpdateWeatherEffects(float transition)
        {
            // Вимикаємо всі ефекти
            if (rainEffect != null) rainEffect.gameObject.SetActive(false);
            if (snowEffect != null) snowEffect.gameObject.SetActive(false);
            
            // Вмикаємо потрібні ефекти
            switch (targetWeather)
            {
                case WeatherType.LightRain:
                    if (rainEffect != null)
                    {
                        rainEffect.gameObject.SetActive(true);
                        var emission = rainEffect.emission;
                        emission.rateOverTime = 500 * transition;
                    }
                    break;
                    
                case WeatherType.Rain:
                    if (rainEffect != null)
                    {
                        rainEffect.gameObject.SetActive(true);
                        var emission = rainEffect.emission;
                        emission.rateOverTime = 1000 * transition;
                    }
                    break;
                    
                case WeatherType.Storm:
                    if (rainEffect != null)
                    {
                        rainEffect.gameObject.SetActive(true);
                        var emission = rainEffect.emission;
                        emission.rateOverTime = 2000 * transition;
                    }
                    // TODO: Додати блискавки
                    break;
                    
                case WeatherType.LightSnow:
                    if (snowEffect != null)
                    {
                        snowEffect.gameObject.SetActive(true);
                        var emission = snowEffect.emission;
                        emission.rateOverTime = 200 * transition;
                    }
                    break;
                    
                case WeatherType.Snow:
                    if (snowEffect != null)
                    {
                        snowEffect.gameObject.SetActive(true);
                        var emission = snowEffect.emission;
                        emission.rateOverTime = 500 * transition;
                    }
                    break;
                    
                case WeatherType.Blizzard:
                    if (snowEffect != null)
                    {
                        snowEffect.gameObject.SetActive(true);
                        var emission = snowEffect.emission;
                        emission.rateOverTime = 1000 * transition;
                    }
                    break;
                    
                case WeatherType.Fog:
                    RenderSettings.fogDensity = 0.1f * transition;
                    break;
            }
            
            // Оновлюємо хмарність
            UpdateClouds(transition);
        }
        
        void UpdateClouds(float density)
        {
            switch (targetWeather)
            {
                case WeatherType.Clear:
                    cloudDensity = 0.1f * density;
                    break;
                case WeatherType.Cloudy:
                    cloudDensity = 0.5f * density;
                    break;
                case WeatherType.Overcast:
                case WeatherType.Rain:
                case WeatherType.Storm:
                case WeatherType.Snow:
                case WeatherType.Blizzard:
                    cloudDensity = 0.9f * density;
                    break;
            }
            
            // TODO: Оновити матеріал хмар
        }
        
        void UpdateWind()
        {
            if (windZone == null) return;
            
            // Базова сила вітру залежить від погоди
            float targetWindStrength = baseWindStrength;
            
            switch (currentWeather)
            {
                case WeatherType.Windy:
                    targetWindStrength = baseWindStrength * 3f;
                    break;
                case WeatherType.Storm:
                case WeatherType.Blizzard:
                    targetWindStrength = baseWindStrength * 4f;
                    break;
                case WeatherType.Rain:
                case WeatherType.Snow:
                    targetWindStrength = baseWindStrength * 2f;
                    break;
            }
            
            // Додаємо варіації
            float variation = Mathf.PerlinNoise(Time.time * 0.1f, 0f) * windVariation;
            windZone.windMain = targetWindStrength + variation;
            
            // Змінюємо напрямок вітру
            float windDirection = Mathf.PerlinNoise(Time.time * 0.05f, 100f) * 360f;
            windZone.transform.rotation = Quaternion.Euler(0, windDirection, 0);
        }
        
        public WeatherType GetCurrentWeather() => currentWeather;
        public Season GetCurrentSeason() => currentSeason;
        public float GetTemperature() => GetSeasonSettings(currentSeason)?.temperature ?? 20f;
        public float GetHumidity() => GetSeasonSettings(currentSeason)?.humidity ?? 50f;
        
        void OnGUI()
        {
            // Показуємо інформацію про погоду
            GUI.Box(new Rect(Screen.width - 210, 80, 200, 80), "");
            GUI.Label(new Rect(Screen.width - 205, 85, 190, 20), $"Сезон: {currentSeason}", new GUIStyle { fontSize = 12, alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(Screen.width - 205, 105, 190, 20), $"Погода: {currentWeather}", new GUIStyle { fontSize = 12, alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(Screen.width - 205, 125, 190, 20), $"Температура: {GetTemperature():F0}°C", new GUIStyle { fontSize = 12, alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(Screen.width - 205, 145, 190, 20), $"Вологість: {GetHumidity():F0}%", new GUIStyle { fontSize = 12, alignment = TextAnchor.MiddleCenter });
        }
    }
} 