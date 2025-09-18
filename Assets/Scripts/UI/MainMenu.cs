#pragma warning disable 0414

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace UI
{
    /// <summary>
    /// Головне меню гри AzZiNnia з українською тематикою
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("UI Елементи")]
        [SerializeField] private Canvas mainMenuCanvas;
        [SerializeField] private Canvas loadingCanvas;
        [SerializeField] private Canvas settingsCanvas;
        
        [Header("Кнопки головного меню")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button exitButton;
        
        [Header("Налаштування")]
        [SerializeField] private Button backFromSettingsButton;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider qualitySlider;
        [SerializeField] private Toggle fullscreenToggle;
        
        [Header("Завантаження")]
        [SerializeField] private Slider loadingProgressBar;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI loadingTipText;
        
        [Header("Фон")]
        [SerializeField] private RawImage backgroundImage;
        [SerializeField] private Texture2D[] backgroundTextures;
        [SerializeField] private float backgroundChangeInterval = 10f;
        
        [Header("Музика")]
        [SerializeField] private AudioSource menuMusic;
        [SerializeField] private AudioClip[] ukrainianMusic;
        
        [Header("Налаштування гри")]
        [SerializeField] private string gameSceneName = "SampleScene";
        
        [Header("Панелі UI")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject characterStatusPanel;
        
        [Header("Елементи статусу персонажа")]
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI staminaText;
        [SerializeField] private TextMeshProUGUI hungerText;
        [SerializeField] private TextMeshProUGUI thirstText;
        
        // Приватні змінні
        private bool isLoading = false;
        private float backgroundTimer = 0f;
        private int currentBackgroundIndex = 0;
        
        // Посилання на компоненти гравця
        private Player.CharacterStatus playerStatus;
        
        // Українські тексти для завантаження
        private string[] loadingTexts = {
            "Генерується світ Козацької України...",
            "Створюються українські ландшафти...",
            "Завантажуються історичні локації...",
            "Підготовка до пригод..."
        };
        
        private string[] loadingTips = {
            "💡 Натисніть T щоб увійти в режим модифікації терену",
            "💡 Використовуйте C для кастомізації персонажа",
            "💡 F3 відкриває панель налагодження",
            "💡 Досліджуйте різні біоми України",
            "💡 Шукайте історичні локації для квестів",
            "💡 Копайте землю щоб знайти корисні ресурси",
            "💡 Будуйте козацькі фортеці та поселення"
        };
        
        void Start()
        {
            InitializeMenu();
            SetupButtons();
            CheckSaveFile();
            StartBackgroundMusic();
            loadingPanel.SetActive(false);
            settingsPanel.SetActive(false);
            if (characterStatusPanel != null) characterStatusPanel.SetActive(false);
        }
        
        void Update()
        {
            UpdateBackground();
            
            // ESC для повернення з налаштувань
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (settingsCanvas.gameObject.activeInHierarchy)
                {
                    CloseSettings();
                }
            }
            
            // Тимчасово для тестування - відкриття меню статусу
            if (Input.GetKeyDown(KeyCode.C))
            {
                ToggleCharacterStatusPanel();
            }
        }
        
        void InitializeMenu()
        {
            // Показуємо головне меню
            if (mainMenuCanvas != null)
                mainMenuCanvas.gameObject.SetActive(true);
            
            // Ховаємо інші канваси
            if (loadingCanvas != null)
                loadingCanvas.gameObject.SetActive(false);
            if (settingsCanvas != null)
                settingsCanvas.gameObject.SetActive(false);
            
            // Налаштовуємо курсор
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
            
            // Завантажуємо налаштування
            LoadSettings();
        }
        
        void SetupButtons()
        {
            if (startGameButton != null)
                startGameButton.onClick.AddListener(StartNewGame);
            if (continueButton != null)
                continueButton.onClick.AddListener(ContinueGame);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            if (creditsButton != null)
                creditsButton.onClick.AddListener(ShowCredits);
            if (exitButton != null)
                exitButton.onClick.AddListener(ExitGame);
            if (backFromSettingsButton != null)
                backFromSettingsButton.onClick.AddListener(CloseSettings);
                
            // Налаштування слайдерів
            if (volumeSlider != null)
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (qualitySlider != null)
                qualitySlider.onValueChanged.AddListener(OnQualityChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
        
        void CheckSaveFile()
        {
            // Перевіряємо чи є збережена гра
            bool hasSaveFile = PlayerPrefs.HasKey("GameTime");
            
            if (continueButton != null)
                continueButton.interactable = hasSaveFile;
        }
        
        void StartBackgroundMusic()
        {
            if (menuMusic != null && ukrainianMusic != null && ukrainianMusic.Length > 0)
            {
                int randomIndex = Random.Range(0, ukrainianMusic.Length);
                menuMusic.clip = ukrainianMusic[randomIndex];
                menuMusic.Play();
            }
        }
        
        void UpdateBackground()
        {
            if (backgroundTextures == null || backgroundTextures.Length <= 1) return;
            
            backgroundTimer += Time.deltaTime;
            if (backgroundTimer >= backgroundChangeInterval)
            {
                backgroundTimer = 0f;
                currentBackgroundIndex = (currentBackgroundIndex + 1) % backgroundTextures.Length;
                
                if (backgroundImage != null)
                {
                    backgroundImage.texture = backgroundTextures[currentBackgroundIndex];
                }
            }
        }
        
        public void StartNewGame()
        {
            if (isLoading) return;
            
            Debug.Log("🎮 Початок нової гри...");
            
            // Видаляємо старі збереження
            PlayerPrefs.DeleteAll();
            
            StartCoroutine(LoadGameScene());
        }
        
        public void ContinueGame()
        {
            if (isLoading) return;
            
            Debug.Log("📁 Продовження гри...");
            StartCoroutine(LoadGameScene());
        }
        
        public void OpenSettings()
        {
            mainMenuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }
        
        public void CloseSettings()
        {
            SaveSettings();
            
            settingsPanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
        
        public void ShowCredits()
        {
            Debug.Log("📜 Показ титрів...");
            // TODO: Додати екран з титрами
        }
        
        public void ExitGame()
        {
            Debug.Log("👋 Вихід з гри...");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        IEnumerator LoadGameScene()
        {
            isLoading = true;
            
            // Показуємо екран завантаження
            if (mainMenuCanvas != null)
                mainMenuCanvas.gameObject.SetActive(false);
            if (loadingCanvas != null)
                loadingCanvas.gameObject.SetActive(true);
            
            // Симулюємо завантаження з українськими текстами
            for (int i = 0; i < loadingTexts.Length; i++)
            {
                if (loadingText != null)
                    loadingText.text = loadingTexts[i];
                
                if (loadingTipText != null && i < loadingTips.Length)
                    loadingTipText.text = loadingTips[i];
                
                float progress = (float)i / loadingTexts.Length;
                if (loadingProgressBar != null)
                    loadingProgressBar.value = progress;
                
                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
            }
            
            // Завершуємо завантаження
            if (loadingText != null)
                loadingText.text = "Готово! Ласкаво просимо до AzZiNnia!";
            if (loadingProgressBar != null)
                loadingProgressBar.value = 1f;
            
            yield return new WaitForSeconds(1f);
            
            // Завантажуємо сцену гри
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
            
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            asyncLoad.allowSceneActivation = true;
        }
        
        void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
        }
        
        void OnQualityChanged(float value)
        {
            int qualityLevel = Mathf.RoundToInt(value);
            QualitySettings.SetQualityLevel(qualityLevel);
        }
        
        void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }
        
        void SaveSettings()
        {
            if (volumeSlider != null)
                PlayerPrefs.SetFloat("MasterVolume", volumeSlider.value);
            if (qualitySlider != null)
                PlayerPrefs.SetInt("QualityLevel", Mathf.RoundToInt(qualitySlider.value));
            if (fullscreenToggle != null)
                PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
            
            PlayerPrefs.Save();
        }
        
        void LoadSettings()
        {
            // Завантажуємо гучність
            if (volumeSlider != null)
            {
                float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
                volumeSlider.value = volume;
                AudioListener.volume = volume;
            }
            
            // Завантажуємо якість
            if (qualitySlider != null)
            {
                int quality = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
                qualitySlider.value = quality;
                QualitySettings.SetQualityLevel(quality);
            }
            
            // Завантажуємо повноекранний режим
            if (fullscreenToggle != null)
            {
                bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
                fullscreenToggle.isOn = fullscreen;
                Screen.fullScreen = fullscreen;
            }
        }
        
        // Публічні методи для UI
        public void SetLoadingProgress(float progress)
        {
            if (loadingProgressBar != null)
                loadingProgressBar.value = progress;
        }
        
        public void SetLoadingText(string text)
        {
            if (loadingText != null)
                loadingText.text = text;
        }
        
        public void SetLoadingTip(string tip)
        {
            if (loadingTipText != null)
                loadingTipText.text = tip;
        }
        
        // --- Нові методи для меню статусу персонажа ---
        
        public void ToggleCharacterStatusPanel()
        {
            if (characterStatusPanel == null) return;

            bool isActive = !characterStatusPanel.activeSelf;
            characterStatusPanel.SetActive(isActive);

            if (isActive)
            {
                UpdateCharacterStatusUI();
            }
        }
        
        public void UpdateCharacterStatusUI()
        {
            if (playerStatus == null)
            {
                // Знаходимо статус, якщо ще не знайшли
                playerStatus = FindFirstObjectByType<Player.CharacterStatus>();
                if (playerStatus == null) return; // Якщо гравця ще немає
            }

            // Оновлюємо текстові поля
            if (healthText != null) healthText.text = $"Здоров'я: {playerStatus.CurrentHealth}/{playerStatus.MaxHealth}";
            if (staminaText != null) staminaText.text = $"Витривалість: {playerStatus.CurrentStamina:F0}/{playerStatus.MaxStamina}";
            if (hungerText != null) hungerText.text = $"Голод: {playerStatus.CurrentHunger:F0}/{playerStatus.MaxHunger}";
            if (thirstText != null) thirstText.text = $"Спрага: {playerStatus.CurrentThirst:F0}/{playerStatus.MaxThirst}";
        }
    }
} 