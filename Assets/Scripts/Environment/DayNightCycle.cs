using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

namespace Environment
{
    /// <summary>
    /// Система циклу дня і ночі
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Налаштування часу")]
        [SerializeField] private float dayDurationInMinutes = 120f; // 2 години реального часу
        [SerializeField] private float startHour = 6f; // Початок о 6:00 ранку
        [SerializeField] private bool pauseTime = false;
        
        [Header("Освітлення")]
        [SerializeField] private Light sunLight;
        [SerializeField] private Light moonLight;
        [SerializeField] private Gradient sunColor;
        [SerializeField] private Gradient fogColor;
        [SerializeField] private AnimationCurve sunIntensity;
        [SerializeField] private AnimationCurve moonIntensity;
        
        [Header("Skybox")]
        [SerializeField] private Material daySkybox;
        [SerializeField] private Material nightSkybox;
        [SerializeField] private Material currentSkybox;
        
        [Header("Скайбокси")]
        [SerializeField] private Material[] daySkyboxes; // Денні скайбокси з папки 8K Skybox Pack
        [SerializeField] private Material[] nightSkyboxes; // Нічні скайбокси
        [SerializeField] private Material sunriseSkybox; // Схід сонця
        [SerializeField] private Material sunsetSkybox; // Захід сонця
        [SerializeField] private bool useMultipleSkyboxes = true; // Використовувати множинні скайбокси
        
        [Header("Атмосфера")]
        [SerializeField] private AnimationCurve fogDensity;
        [SerializeField] private Volume postProcessVolume;
        
        // Поточний час
        private float currentTimeInHours;
        private int currentDay = 1;
        private int currentMonth = 6; // Червень
        private int currentYear = 1648; // Історичний рік
        
        // Події
        public event Action<float> OnHourChanged;
        public event Action<int> OnDayChanged;
        public event Action<TimeOfDay> OnTimeOfDayChanged;
        
        private TimeOfDay currentTimeOfDay = TimeOfDay.Morning;
        
        public enum TimeOfDay
        {
            Night,      // 0:00 - 5:00
            Dawn,       // 5:00 - 7:00
            Morning,    // 7:00 - 12:00
            Afternoon,  // 12:00 - 17:00
            Evening,    // 17:00 - 19:00
            Dusk,       // 19:00 - 21:00
            Night2      // 21:00 - 24:00
        }
        
        public float CurrentHour => currentTimeInHours;
        public int CurrentDay => currentDay;
        public int CurrentMonth => currentMonth;
        public int CurrentYear => currentYear;
        public TimeOfDay CurrentTimeOfDay => currentTimeOfDay;
        
        void Start()
        {
            // Ініціалізуємо час
            currentTimeInHours = startHour;
            
            // Знаходимо світло якщо не призначено
            if (sunLight == null)
            {
                GameObject sun = new GameObject("Sun Light");
                sunLight = sun.AddComponent<Light>();
                sunLight.type = LightType.Directional;
                sunLight.intensity = 1f;
                sun.transform.rotation = Quaternion.Euler(45f, -30f, 0);
            }
            
            if (moonLight == null)
            {
                GameObject moon = new GameObject("Moon Light");
                moonLight = moon.AddComponent<Light>();
                moonLight.type = LightType.Directional;
                moonLight.intensity = 0.2f;
                moonLight.color = new Color(0.7f, 0.7f, 1f);
                moon.transform.rotation = Quaternion.Euler(45f, 150f, 0);
            }
            
            // Налаштовуємо градієнти якщо не призначені
            if (sunColor == null || sunColor.colorKeys.Length == 0)
            {
                sunColor = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[5];
                colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.3f), 0f);    // Ніч
                colorKeys[1] = new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.25f);    // Схід
                colorKeys[2] = new GradientColorKey(new Color(1f, 1f, 0.9f), 0.5f);       // День
                colorKeys[3] = new GradientColorKey(new Color(1f, 0.7f, 0.5f), 0.75f);    // Захід
                colorKeys[4] = new GradientColorKey(new Color(0.2f, 0.2f, 0.3f), 1f);     // Ніч
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                
                sunColor.SetKeys(colorKeys, alphaKeys);
            }
            
            // Створюємо skybox матеріал
            if (currentSkybox == null)
            {
                currentSkybox = new Material(Shader.Find("Skybox/Procedural"));
            }
            
            UpdateTimeOfDay();
        }
        
        void Update()
        {
            if (!pauseTime)
            {
                // Оновлюємо час
                float timeMultiplier = 24f / (dayDurationInMinutes * 60f);
                currentTimeInHours += Time.deltaTime * timeMultiplier;
                
                // Перехід на новий день
                if (currentTimeInHours >= 24f)
                {
                    currentTimeInHours -= 24f;
                    currentDay++;
                    
                    // Перехід на новий місяць
                    if (currentDay > GetDaysInMonth(currentMonth))
                    {
                        currentDay = 1;
                        currentMonth++;
                        
                        // Перехід на новий рік
                        if (currentMonth > 12)
                        {
                            currentMonth = 1;
                            currentYear++;
                        }
                    }
                    
                    OnDayChanged?.Invoke(currentDay);
                }
                
                UpdateTimeOfDay();
                UpdateLighting();
                UpdateSkybox();
                
                OnHourChanged?.Invoke(currentTimeInHours);
            }
        }
        
        void UpdateTimeOfDay()
        {
            TimeOfDay newTimeOfDay = GetTimeOfDay(currentTimeInHours);
            if (newTimeOfDay != currentTimeOfDay)
            {
                currentTimeOfDay = newTimeOfDay;
                OnTimeOfDayChanged?.Invoke(currentTimeOfDay);
            }
        }
        
        public TimeOfDay GetTimeOfDay(float hour)
        {
            if (hour < 5f || hour >= 21f) return TimeOfDay.Night;
            if (hour < 7f) return TimeOfDay.Dawn;
            if (hour < 12f) return TimeOfDay.Morning;
            if (hour < 17f) return TimeOfDay.Afternoon;
            if (hour < 19f) return TimeOfDay.Evening;
            return TimeOfDay.Dusk;
        }
        
        public float GetTimeOfDay()
        {
            return currentTimeInHours;
        }
        
        void UpdateLighting()
        {
            // Нормалізований час (0-1)
            float normalizedTime = currentTimeInHours / 24f;
            
            // Оновлюємо колір сонця
            if (sunLight != null)
            {
                sunLight.color = sunColor.Evaluate(normalizedTime);
                sunLight.intensity = sunIntensity != null && sunIntensity.length > 0 
                    ? sunIntensity.Evaluate(normalizedTime) 
                    : Mathf.Clamp01((Mathf.Cos((normalizedTime - 0.5f) * 2f * Mathf.PI) + 1f) / 2f);
            }
            
            // Оновлюємо місяць
            if (moonLight != null)
            {
                moonLight.intensity = moonIntensity != null && moonIntensity.length > 0
                    ? moonIntensity.Evaluate(normalizedTime)
                    : Mathf.Clamp01(1f - sunLight.intensity);
            }
            
            // Обертаємо світло
            float sunAngle = (normalizedTime - 0.25f) * 360f;
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle - 90f, 30f, 0f);
            }
            
            if (moonLight != null)
            {
                moonLight.transform.rotation = Quaternion.Euler(sunAngle + 90f, 30f, 0f);
            }
            
            // Оновлюємо туман
            if (fogColor != null)
            {
                RenderSettings.fogColor = fogColor.Evaluate(normalizedTime);
            }
            
            if (fogDensity != null && fogDensity.length > 0)
            {
                RenderSettings.fogDensity = fogDensity.Evaluate(normalizedTime);
            }
        }
        
        void UpdateSkybox()
        {
            if (useMultipleSkyboxes)
            {
                // Використовуємо множинні скайбокси
                Material newSkybox = null;
                
                // Вибираємо скайбокс залежно від часу
                if (currentTimeInHours >= 5f && currentTimeInHours < 7f && sunriseSkybox != null)
                {
                    newSkybox = sunriseSkybox;
                }
                else if (currentTimeInHours >= 17f && currentTimeInHours < 19f && sunsetSkybox != null)
                {
                    newSkybox = sunsetSkybox;
                }
                else if (currentTimeInHours >= 6f && currentTimeInHours < 18f && daySkyboxes != null && daySkyboxes.Length > 0)
                {
                    // Вибираємо випадковий денний скайбокс на основі часу
                    int index = Mathf.FloorToInt((currentTimeInHours - 6f) / 12f * daySkyboxes.Length) % daySkyboxes.Length;
                    newSkybox = daySkyboxes[index];
                }
                else if (nightSkyboxes != null && nightSkyboxes.Length > 0)
                {
                    // Вибираємо випадковий нічний скайбокс
                    float nightHour = currentTimeInHours < 6f ? currentTimeInHours + 24f : currentTimeInHours;
                    int index = Mathf.FloorToInt((nightHour - 18f) / 12f * nightSkyboxes.Length) % nightSkyboxes.Length;
                    newSkybox = nightSkyboxes[index];
                }
                
                // Застосовуємо скайбокс
                if (newSkybox != null && newSkybox != currentSkybox)
                {
                    RenderSettings.skybox = newSkybox;
                    currentSkybox = newSkybox;
                    DynamicGI.UpdateEnvironment();
                }
            }
            else
            {
                // Використовуємо процедурний скайбокс
                if (currentSkybox != null)
                {
                    float normalizedTime = currentTimeInHours / 24f;
                    
                    // Процедурний skybox
                    currentSkybox.SetFloat("_SunSize", 0.04f);
                    currentSkybox.SetFloat("_SunSizeConvergence", 5f);
                    currentSkybox.SetFloat("_AtmosphereThickness", 1f);
                    currentSkybox.SetColor("_SkyTint", sunColor.Evaluate(normalizedTime));
                    currentSkybox.SetColor("_GroundColor", fogColor != null ? fogColor.Evaluate(normalizedTime) : Color.gray);
                    currentSkybox.SetFloat("_Exposure", Mathf.Lerp(0.5f, 1.5f, sunLight.intensity));
                    
                    RenderSettings.skybox = currentSkybox;
                }
            }
        }
        
        int GetDaysInMonth(int month)
        {
            switch (month)
            {
                case 2: return 28; // Лютий
                case 4:
                case 6:
                case 9:
                case 11: return 30;
                default: return 31;
            }
        }
        
        public void SetTime(float hour)
        {
            currentTimeInHours = Mathf.Clamp(hour, 0f, 23.99f);
            UpdateTimeOfDay();
            UpdateLighting();
            UpdateSkybox();
        }
        
        public void SetDate(int day, int month, int year)
        {
            currentDay = Mathf.Clamp(day, 1, GetDaysInMonth(month));
            currentMonth = Mathf.Clamp(month, 1, 12);
            currentYear = year;
        }
        
        public void PauseTime(bool pause)
        {
            pauseTime = pause;
        }
        
        public string GetTimeString()
        {
            int hours = Mathf.FloorToInt(currentTimeInHours);
            int minutes = Mathf.FloorToInt((currentTimeInHours - hours) * 60f);
            return $"{hours:00}:{minutes:00}";
        }
        
        public string GetDateString()
        {
            string[] monthNames = { "Січень", "Лютий", "Березень", "Квітень", "Травень", "Червень",
                                   "Липень", "Серпень", "Вересень", "Жовтень", "Листопад", "Грудень" };
            return $"{currentDay} {monthNames[currentMonth - 1]} {currentYear}";
        }
        
        void OnGUI()
        {
            // Показуємо час та дату
            GUI.Box(new Rect(Screen.width - 210, 10, 200, 60), "");
            GUI.Label(new Rect(Screen.width - 205, 15, 190, 20), GetTimeString(), new GUIStyle { fontSize = 16, alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(Screen.width - 205, 35, 190, 20), GetDateString(), new GUIStyle { fontSize = 12, alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(Screen.width - 205, 50, 190, 20), currentTimeOfDay.ToString(), new GUIStyle { fontSize = 10, alignment = TextAnchor.MiddleCenter });
        }
    }
} 