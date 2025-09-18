using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

namespace Audio
{
    /// <summary>
    /// Менеджер аудіо системи з українською тематикою
    /// Управляє фоновою музикою, звуками природи та ефектами
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Аудіо джерела")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource sfxSource;
        
        [Header("Українська народна музика")]
        [SerializeField] private AudioClip[] folkSongs;
        [SerializeField] private AudioClip[] battleSongs;
        [SerializeField] private AudioClip[] peacefulSongs;
        [SerializeField] private AudioClip[] nightSongs;
        
        [Header("Звуки природи")]
        [SerializeField] private AudioClip[] forestSounds;
        [SerializeField] private AudioClip[] riverSounds;
        [SerializeField] private AudioClip[] windSounds;
        [SerializeField] private AudioClip[] birdSounds;
        [SerializeField] private AudioClip[] rainSounds;
        
        [Header("Звукові ефекти")]
        [SerializeField] private AudioClip[] digSounds;
        [SerializeField] private AudioClip[] buildSounds;
        [SerializeField] private AudioClip[] footstepSounds;
        [SerializeField] private AudioClip[] toolSounds;
        
        [Header("Налаштування")]
        [SerializeField] private AudioMixer mainMixer;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float ambientVolume = 0.5f;
        [SerializeField] private float sfxVolume = 0.8f;
        [SerializeField] private float musicFadeTime = 2f;
        [SerializeField] private float ambientFadeTime = 1f;
        
        [Header("Динамічна музика")]
        [SerializeField] private bool enableDynamicMusic = true;
        [SerializeField] private float musicChangeInterval = 300f; // 5 хвилин
        [SerializeField] private bool adaptToTimeOfDay = true;
        [SerializeField] private bool adaptToWeather = true;
        [SerializeField] private bool adaptToBiome = true;
        
        // Приватні змінні
        private AudioClip currentMusicClip;
        private AudioClip currentAmbientClip;
        private MusicMood currentMood = MusicMood.Peaceful;
        private float nextMusicChangeTime;
        private bool isMusicFading = false;
        private bool isAmbientFading = false;
        
        // Компоненти системи
        private Environment.DayNightCycle dayNightCycle;
        private Environment.WeatherSystem weatherSystem;
        private Voxel.UkrainianTerrainGenerator terrainGenerator;
        
        public enum MusicMood
        {
            Peaceful,    // Мирна музика
            Battle,      // Бойова музика
            Night,       // Нічна музика
            Folk,        // Народна музика
            Atmospheric  // Атмосферна музика
        }
        
        public enum BiomeType
        {
            Forest,      // Ліс
            River,       // Річка
            Plains,      // Рівнина
            Mountains,   // Гори
            Village      // Село
        }
        
        public static AudioManager Instance { get; private set; }
        
        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioSources();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            // Знаходимо компоненти системи
            dayNightCycle = FindFirstObjectByType<Environment.DayNightCycle>();
            weatherSystem = FindFirstObjectByType<Environment.WeatherSystem>();
            terrainGenerator = FindFirstObjectByType<Voxel.UkrainianTerrainGenerator>();
            
            // Встановлюємо початкові налаштування
            SetMasterVolume();
            
            // Запускаємо початкову музику
            StartCoroutine(PlayInitialMusic());
            
            Debug.Log("🎵 AudioManager ініціалізовано з українською тематикою");
        }
        
        void Update()
        {
            if (enableDynamicMusic && Time.time >= nextMusicChangeTime && !isMusicFading)
            {
                UpdateDynamicMusic();
                nextMusicChangeTime = Time.time + musicChangeInterval;
            }
        }
        
        void InitializeAudioSources()
        {
            // Створюємо аудіо джерела якщо не призначені
            if (musicSource == null)
            {
                GameObject musicGO = new GameObject("MusicSource");
                musicGO.transform.SetParent(transform);
                musicSource = musicGO.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.volume = musicVolume;
            }
            
            if (ambientSource == null)
            {
                GameObject ambientGO = new GameObject("AmbientSource");
                ambientGO.transform.SetParent(transform);
                ambientSource = ambientGO.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
                ambientSource.volume = ambientVolume;
            }
            
            if (sfxSource == null)
            {
                GameObject sfxGO = new GameObject("SFXSource");
                sfxGO.transform.SetParent(transform);
                sfxSource = sfxGO.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = sfxVolume;
            }
        }
        
        IEnumerator PlayInitialMusic()
        {
            yield return new WaitForSeconds(1f); // Чекаємо ініціалізації інших систем
            
            // Визначаємо початковий настрій
            MusicMood initialMood = DetermineMoodFromEnvironment();
            PlayMusicForMood(initialMood);
            
            // Запускаємо амбієнт
            PlayAmbientForCurrentBiome();
        }
        
        void UpdateDynamicMusic()
        {
            MusicMood newMood = DetermineMoodFromEnvironment();
            
            if (newMood != currentMood)
            {
                Debug.Log($"🎵 Зміна настрою музики: {currentMood} → {newMood}");
                CrossfadeToMood(newMood);
            }
        }
        
        MusicMood DetermineMoodFromEnvironment()
        {
            // Адаптуємося до часу доби
            if (adaptToTimeOfDay && dayNightCycle != null)
            {
                float timeOfDay = dayNightCycle.GetTimeOfDay();
                
                // Нічна музика (22:00 - 06:00)
                if (timeOfDay > 22f || timeOfDay < 6f)
                {
                    return MusicMood.Night;
                }
                
                // Ранкова/вечірня атмосферна музика
                if ((timeOfDay >= 6f && timeOfDay <= 8f) || (timeOfDay >= 18f && timeOfDay <= 20f))
                {
                    return MusicMood.Atmospheric;
                }
            }
            
            // Адаптуємося до погоди
            if (adaptToWeather && weatherSystem != null)
            {
                // TODO: Отримати поточну погоду з WeatherSystem
                // Поки що повертаємо мирну музику
            }
            
            // За замовчуванням - мирна музика
            return MusicMood.Peaceful;
        }
        
        void PlayMusicForMood(MusicMood mood)
        {
            AudioClip[] songsForMood = GetSongsForMood(mood);
            
            if (songsForMood != null && songsForMood.Length > 0)
            {
                AudioClip selectedSong = songsForMood[Random.Range(0, songsForMood.Length)];
                PlayMusic(selectedSong);
                currentMood = mood;
            }
        }
        
        AudioClip[] GetSongsForMood(MusicMood mood)
        {
            switch (mood)
            {
                case MusicMood.Peaceful:
                    return peacefulSongs;
                case MusicMood.Battle:
                    return battleSongs;
                case MusicMood.Night:
                    return nightSongs;
                case MusicMood.Folk:
                    return folkSongs;
                case MusicMood.Atmospheric:
                    return peacefulSongs; // Fallback
                default:
                    return folkSongs;
            }
        }
        
        void PlayAmbientForCurrentBiome()
        {
            // Визначаємо біом на основі позиції гравця
            BiomeType biome = DetermineCurrentBiome();
            AudioClip[] ambientSounds = GetAmbientSoundsForBiome(biome);
            
            if (ambientSounds != null && ambientSounds.Length > 0)
            {
                AudioClip selectedAmbient = ambientSounds[Random.Range(0, ambientSounds.Length)];
                PlayAmbient(selectedAmbient);
            }
        }
        
        BiomeType DetermineCurrentBiome()
        {
            // TODO: Інтеграція з системою біомів
            // Поки що повертаємо ліс як найпоширеніший біом України
            return BiomeType.Forest;
        }
        
        AudioClip[] GetAmbientSoundsForBiome(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Forest:
                    return forestSounds;
                case BiomeType.River:
                    return riverSounds;
                case BiomeType.Plains:
                    return windSounds;
                case BiomeType.Mountains:
                    return windSounds;
                case BiomeType.Village:
                    return birdSounds;
                default:
                    return forestSounds;
            }
        }
        
        void CrossfadeToMood(MusicMood newMood)
        {
            if (isMusicFading) return;
            
            StartCoroutine(CrossfadeMusicCoroutine(newMood));
        }
        
        IEnumerator CrossfadeMusicCoroutine(MusicMood newMood)
        {
            isMusicFading = true;
            
            // Поступово зменшуємо гучність поточної музики
            float startVolume = musicSource.volume;
            float elapsedTime = 0f;
            
            while (elapsedTime < musicFadeTime / 2f)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (musicFadeTime / 2f);
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }
            
            // Змінюємо музику
            PlayMusicForMood(newMood);
            
            // Поступово збільшуємо гучність нової музики
            elapsedTime = 0f;
            while (elapsedTime < musicFadeTime / 2f)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (musicFadeTime / 2f);
                musicSource.volume = Mathf.Lerp(0f, musicVolume, t);
                yield return null;
            }
            
            musicSource.volume = musicVolume;
            isMusicFading = false;
        }
        
        // Публічні методи
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            
            musicSource.clip = clip;
            musicSource.Play();
            currentMusicClip = clip;
            
            Debug.Log($"🎵 Відтворюється музика: {clip.name}");
        }
        
        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null) return;
            
            ambientSource.clip = clip;
            ambientSource.Play();
            currentAmbientClip = clip;
            
            Debug.Log($"🌲 Відтворюється амбієнт: {clip.name}");
        }
        
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            
            sfxSource.PlayOneShot(clip);
        }
        
        public void PlayRandomSFX(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;
            
            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            PlaySFX(randomClip);
        }
        
        // Методи для ігрових подій
        public void PlayDigSound()
        {
            PlayRandomSFX(digSounds);
        }
        
        public void PlayBuildSound()
        {
            PlayRandomSFX(buildSounds);
        }
        
        public void PlayFootstepSound()
        {
            PlayRandomSFX(footstepSounds);
        }
        
        public void PlayToolSound()
        {
            PlayRandomSFX(toolSounds);
        }
        
        // Налаштування гучності
        public void SetMasterVolume()
        {
            if (mainMixer != null)
            {
                mainMixer.SetFloat("MasterVolume", Mathf.Log10(musicVolume) * 20f);
                mainMixer.SetFloat("MusicVolume", Mathf.Log10(musicVolume) * 20f);
                mainMixer.SetFloat("AmbientVolume", Mathf.Log10(ambientVolume) * 20f);
                mainMixer.SetFloat("SFXVolume", Mathf.Log10(sfxVolume) * 20f);
            }
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = musicVolume;
            
            if (mainMixer != null)
            {
                mainMixer.SetFloat("MusicVolume", Mathf.Log10(musicVolume) * 20f);
            }
        }
        
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            ambientSource.volume = ambientVolume;
            
            if (mainMixer != null)
            {
                mainMixer.SetFloat("AmbientVolume", Mathf.Log10(ambientVolume) * 20f);
            }
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            sfxSource.volume = sfxVolume;
            
            if (mainMixer != null)
            {
                mainMixer.SetFloat("SFXVolume", Mathf.Log10(sfxVolume) * 20f);
            }
        }
        
        // Контроль відтворення
        public void PauseMusic()
        {
            musicSource.Pause();
        }
        
        public void ResumeMusic()
        {
            musicSource.UnPause();
        }
        
        public void StopMusic()
        {
            musicSource.Stop();
        }
        
        public void PauseAmbient()
        {
            ambientSource.Pause();
        }
        
        public void ResumeAmbient()
        {
            ambientSource.UnPause();
        }
        
        public void StopAmbient()
        {
            ambientSource.Stop();
        }
        
        // Методи для налаштувань
        public void ToggleMusic()
        {
            if (musicSource.isPlaying)
            {
                PauseMusic();
            }
            else
            {
                ResumeMusic();
            }
        }
        
        public void ToggleAmbient()
        {
            if (ambientSource.isPlaying)
            {
                PauseAmbient();
            }
            else
            {
                ResumeAmbient();
            }
        }
        
        // Інформаційні методи
        public bool IsMusicPlaying()
        {
            return musicSource.isPlaying;
        }
        
        public bool IsAmbientPlaying()
        {
            return ambientSource.isPlaying;
        }
        
        public string GetCurrentMusicName()
        {
            return currentMusicClip != null ? currentMusicClip.name : "Немає";
        }
        
        public string GetCurrentAmbientName()
        {
            return currentAmbientClip != null ? currentAmbientClip.name : "Немає";
        }
        
        public MusicMood GetCurrentMood()
        {
            return currentMood;
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