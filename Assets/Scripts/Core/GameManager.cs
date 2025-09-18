using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Voxel;
using Player;
using UI;
using System;
using Azurin.Player;
using Azurin.CameraSystem;

/// <summary>
/// This singleton class manages game-wide settings and debug information.
/// It displays FPS, player coordinates and current magic type.
/// It also provides hotkeys to switch between different debug views or settings.
/// </summary>
namespace Azurin.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState { Running, Paused }
        public static event Action<GameState> OnGameStateChanged;
        public GameState CurrentState { get; private set; }

        [Header("🎮 Основні компоненти")]
        public VoxelTerrain voxelTerrain;
        public CossackPlayer player;
        public IsometricCamera gameCamera;
        public PauseUI pauseUI;

        [Header("📊 Статистика гри")]
        public float gameTime = 0f;
        public int blocksDestroyed = 0;
        public int blocksPlaced = 0;
        public float distanceTraveled = 0f;
        
        [Header("🎯 Налаштування геймплею")]
        public bool showDebugInfo = false;
        public bool pauseOnFocusLoss = true;
        public float autoSaveInterval = 300f; // 5 хвилин
        
        // Приватні змінні
        private float lastAutoSave = 0f;
        private Vector3 lastPlayerPosition;
        private bool gameInitialized = false;
        
        // Services (resolved via ServiceLocator if available)
        private GameStateController gameStateController;
        private SaveLoadService saveLoadService;
        private GameplayStatsService statsService;
        private DebugHUD debugHUD;
        
        // UI елементи (опціонально)
        private Canvas debugCanvas;
        private TextMeshProUGUI debugText;
        private TextMeshProUGUI buildModeText;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                CossackPlayer.OnBuildModeChanged += UpdateBuildModeText;
            }
        }

        void Start()
        {
            Debug.Log("🎮 Ініціалізація GameManager AzZiNnia...");
            
            InitializeGame();
            SetupGameplayUI();

            // Resolve services
            ServiceLocator.TryGet(out gameStateController);
            ServiceLocator.TryGet(out saveLoadService);
            ServiceLocator.TryGet(out statsService);
            ServiceLocator.TryGet(out debugHUD);

            if (gameStateController != null)
            {
                gameStateController.OnStateChanged += HandleStateChangedForward;
            }
            
            // Оновлюємо початковий стан тексту ПІСЛЯ створення UI
            if (player != null)
            {
                UpdateBuildModeText(player.IsInBuildMode);
            }
            
            // Підписуємось на події з InputHandler (через ServiceLocator або fallback)
            InputHandler inputHandler = null;
            if (!ServiceLocator.TryGet(out inputHandler))
            {
                inputHandler = FindFirstObjectByType<InputHandler>();
            }
            if (inputHandler != null)
            {
                inputHandler.OnPause += TogglePause;
                inputHandler.OnToggleDebug += ToggleDebugInfo;
                inputHandler.OnQuickSave += SaveGame;
                inputHandler.OnQuickLoad += LoadGame;
            }
            else
            {
                Debug.LogError("InputHandler не знайдено! Пауза не працюватиме.");
            }
            
            Debug.Log("✅ GameManager готовий!");
        }
        
        void InitializeGame()
        {
            // Знаходимо компоненти якщо не призначені
            if (voxelTerrain == null)
            {
                voxelTerrain = FindFirstObjectByType<VoxelTerrain>();
                if (voxelTerrain != null)
                {
                    Debug.Log("✅ VoxelTerrain знайдено");
                }
                else
                {
                    Debug.LogError("❌ VoxelTerrain не знайдено!");
                }
            }
            
            if (player == null)
            {
                player = FindFirstObjectByType<CossackPlayer>();
                if (player != null)
                {
                    Debug.Log("✅ CossackPlayer знайдено");
                    lastPlayerPosition = player.GetPosition();
                }
                else
                {
                    Debug.LogError("❌ CossackPlayer не знайдено!");
                }
            }

            // Знаходимо або створюємо PauseUI
            if (pauseUI == null)
            {
                pauseUI = FindFirstObjectByType<PauseUI>();
                if (pauseUI == null)
                {
                    GameObject pauseUIGameObject = new GameObject("PauseUI");
                    pauseUI = pauseUIGameObject.AddComponent<PauseUI>();
                    Debug.LogWarning("PauseUI не знайдено на сцені, створено новий. Не забудьте налаштувати його в інспекторі!");
                }
            }

            if (pauseUI != null)
            {
                 Debug.Log("✅ PauseUI знайдено");
                // Налаштовуємо кнопки, лише якщо вони існують, щоб уникнути помилок
                if (pauseUI.resumeButton != null)
                {
                    pauseUI.resumeButton.onClick.AddListener(ResumeGame);
                }
                if (pauseUI.mainMenuButton != null)
                {
                    pauseUI.mainMenuButton.onClick.AddListener(ReturnToMainMenu);
                }
            }
            
            // IsometricCamera is optional now; do not log an error if it is missing
            if (gameCamera == null)
            {
                gameCamera = FindFirstObjectByType<IsometricCamera>();
                if (gameCamera != null && player != null)
                {
                    gameCamera.SetTarget(player.transform);
                }
            }
            
            // Налаштовуємо початковий стан
            Time.timeScale = 1f;
            gameTime = 0f;
            
            gameInitialized = true;
            SetState(GameState.Running);
        }
        
        void SetupGameplayUI()
        {
            // Створюємо Canvas, якщо його ще немає
            if (debugCanvas == null)
            {
                GameObject canvasGO = new GameObject("GameplayCanvas");
                debugCanvas = canvasGO.AddComponent<Canvas>();
                debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                debugCanvas.sortingOrder = 100;
                
                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (showDebugInfo)
            {
                // Якщо є префабний HUD — не створюємо текст динамічно
                if (debugHUD == null)
                {
                    SetupDebugText();
                    SetupBuildModeText();
                }
            }
        }

        void SetupDebugText()
        {
             if (debugText != null) return;

            // Створюємо текст для debug інформації
            GameObject textGO = new GameObject("DebugText");
            textGO.transform.SetParent(debugCanvas.transform, false);
            
            debugText = textGO.AddComponent<TextMeshProUGUI>();
            
            TMP_FontAsset font = GetAvailableFont();
            if (font != null) debugText.font = font;

            debugText.fontSize = 16;
            debugText.color = Color.white;
            debugText.alignment = TextAlignmentOptions.TopLeft;
            
            RectTransform rectTransform = debugText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(10, -250);
            rectTransform.sizeDelta = new Vector2(400, 200);
            
            CreateTextShadow(textGO, debugText);
        }

        void SetupBuildModeText()
        {
            if (buildModeText != null) return;

            GameObject textGO = new GameObject("BuildModeText");
            textGO.transform.SetParent(debugCanvas.transform, false);

            buildModeText = textGO.AddComponent<TextMeshProUGUI>();

            TMP_FontAsset font = GetAvailableFont();
            if (font != null) buildModeText.font = font;
            
            buildModeText.fontSize = 24;
            buildModeText.color = new Color(1f, 0.8f, 0.4f); // Помаранчевий
            buildModeText.alignment = TextAlignmentOptions.Top;

            RectTransform rectTransform = buildModeText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1);
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.anchoredPosition = new Vector2(0, -20);
            rectTransform.sizeDelta = new Vector2(400, 50);

            CreateTextShadow(textGO, buildModeText);
        }

        TMP_FontAsset GetAvailableFont()
        {
            TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (allFonts != null && allFonts.Length > 0)
            {
                return allFonts[0];
            }
            Debug.LogWarning("Не знайдено жодного TMP шрифту!");
            return null;
        }

        void CreateTextShadow(GameObject parent, TextMeshProUGUI originalText)
        {
            GameObject shadowGO = new GameObject("Shadow");
            shadowGO.transform.SetParent(parent.transform, false);
            
            TextMeshProUGUI shadowText = shadowGO.AddComponent<TextMeshProUGUI>();
            shadowText.font = originalText.font;
            shadowText.fontSize = originalText.fontSize;
            shadowText.color = Color.black;
            shadowText.alignment = originalText.alignment;
            
            RectTransform shadowRect = shadowText.GetComponent<RectTransform>();
            shadowRect.anchorMin = Vector2.zero;
            shadowRect.anchorMax = Vector2.one;
            shadowRect.offsetMin = new Vector2(2, -2);
            shadowRect.offsetMax = new Vector2(2, -2);
        }
        
        void Update()
        {
            if (!gameInitialized) return;
            
            // Пауза та інші хоткеї тепер обробляються через Input System (InputHandler)
            
            if (CurrentState == GameState.Paused) return;

            UpdateGameTime();
            UpdateStatistics();
            UpdateDebugInfo();
            HandleAutoSave();
        }
        
        // Видалено дублююче опитування клавіатури; залишена інтеграція з InputHandler
        
        void UpdateGameTime()
        {
            gameTime += Time.deltaTime;
            if (statsService != null && player != null)
            {
                statsService.Tick(Time.deltaTime, player.GetPosition());
            }
        }
        
        void UpdateStatistics()
        {
            if (player == null) return;
            
            // Відстань подорожі
            Vector3 currentPosition = player.GetPosition();
            float distance = Vector3.Distance(lastPlayerPosition, currentPosition);
            if (distance > 0.1f) // Мінімальна відстань для підрахунку
            {
                distanceTraveled += distance;
                lastPlayerPosition = currentPosition;
            }
        }
        
        void UpdateDebugInfo()
        {
            if (!showDebugInfo) return;
            string debugInfo = GetDebugInfoString();

            if (debugHUD != null)
            {
                debugHUD.SetDebug(debugInfo);
                return;
            }
            if (debugText == null) return;
            
            debugText.text = debugInfo;
            
            // Оновлюємо тінь
            Transform shadowTransform = debugText.transform.Find("Shadow");
            if (shadowTransform != null)
            {
                TextMeshProUGUI shadowText = shadowTransform.GetComponent<TextMeshProUGUI>();
                if (shadowText != null)
                {
                    shadowText.text = debugInfo;
                }
            }
        }
        
        string GetDebugInfoString()
        {
            string info = "AzZiNnia Debug Panel\n";
            info += "=======================\n";
            
            // Час гри
            int hours = Mathf.FloorToInt(gameTime / 3600);
            int minutes = Mathf.FloorToInt((gameTime % 3600) / 60);
            int seconds = Mathf.FloorToInt(gameTime % 60);
            info += $"Час гри: {hours:00}:{minutes:00}:{seconds:00}\n";
            
            // FPS
            float fps = 1f / Time.unscaledDeltaTime;
            info += $"FPS: {fps:F1}\n";
            
            // Статистика гравця
            if (player != null)
            {
                Vector3 pos = player.GetPosition();
                info += $"Позиція: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})\n";
                info += $"Здоров'я: {player.GetHealthPercent() * 100:F0}%\n";
                info += $"Стаміна: {player.GetStaminaPercent() * 100:F0}%\n";
                
                string state = "Стоїть";
                if (player.IsProne()) state = "Лежить";
                else if (player.IsCrouching()) state = "Присів";
                else if (player.IsMoving()) state = "Рухається";
                
                info += $"🚶 Стан: {state}\n";
            }
            
            // Статистика світу
            if (voxelTerrain != null)
            {
                // TODO: Add biome info from VoxelTerrain
                info += $"Знищено блоків: {blocksDestroyed}\n";
                info += $"Поставлено блоків: {blocksPlaced}\n";
            }
            
            // Загальна статистика
            info += $"Пройдено: {distanceTraveled:F1}м\n";
            
            // Керування
            info += "\nКерування:\n";
            info += "F3 - Debug панель\n";
            info += "F5 - Зберегти\n";
            info += "ESC - Пауза\n";
            
            return info;
        }
        
        void HandleAutoSave()
        {
            if (autoSaveInterval > 0 && gameTime - lastAutoSave >= autoSaveInterval)
            {
                SaveGame();
                lastAutoSave = gameTime;
                Debug.Log("💾 Автозбереження виконано");
            }
        }
        
        public void TogglePause()
        {
            if (gameStateController != null)
            {
                gameStateController.TogglePause();
            }
            else
            {
                SetState(CurrentState == GameState.Running ? GameState.Paused : GameState.Running);
            }
        }

        private void PauseGame()
        {
            Time.timeScale = 0f;
            if (pauseUI != null) pauseUI.Show();
            CursorManager.Instance.UnlockCursor();
            CursorManager.Instance.ShowCursor();
            Debug.Log("⏸️ Гра призупинена");
        }

        private void ResumeGame()
        {
            Time.timeScale = 1f;
            if (pauseUI != null) pauseUI.Hide();
            CursorManager.Instance.LockCursor();
            CursorManager.Instance.HideCursor();
            Debug.Log("▶️ Гра відновлена");
        }

        private void SetState(GameState newState)
        {
            // Delegate to GameStateController if available
            if (gameStateController != null)
            {
                var mapped = newState == GameState.Paused ? GameStateController.State.Paused : GameStateController.State.Running;
                gameStateController.SetState(mapped);
                return;
            }

            if (CurrentState == newState) return;

            CurrentState = newState;

            switch (newState)
            {
                case GameState.Running:
                    ResumeGame();
                    break;
                case GameState.Paused:
                    PauseGame();
                    break;
            }

            OnGameStateChanged?.Invoke(newState);
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(Scenes.MainMenu);
        }
        
        public void ToggleDebugInfo()
        {
            showDebugInfo = !showDebugInfo;
            
            // Lazy create/destroy debug UI for cleanliness
            if (showDebugInfo)
            {
                if (debugCanvas == null)
                {
                    SetupGameplayUI();
                }
                if (debugCanvas != null)
                {
                    debugCanvas.gameObject.SetActive(true);
                    if (debugText == null) SetupDebugText();
                }
            }
            else
            {
                if (debugCanvas != null)
                {
                    debugCanvas.gameObject.SetActive(false);
                }
            }
            
            Debug.Log(showDebugInfo ? "📊 Debug інформація увімкнена" : "📊 Debug інформація вимкнена");
        }
        
        public void SaveGame()
        {
            Debug.Log("💾 Збереження гри...");

            if (saveLoadService != null && player != null)
            {
                var pos = player.GetPosition();
                saveLoadService.SavePlayerState(
                    gameTime,
                    blocksDestroyed,
                    blocksPlaced,
                    distanceTraveled,
                    pos,
                    player.GetHealthPercent(),
                    player.GetStaminaPercent());
                Debug.Log("✅ Гру збережено (SaveLoadService)!");
                return;
            }

            // Fallback to PlayerPrefs
            PlayerPrefs.SetFloat("GameTime", gameTime);
            PlayerPrefs.SetInt("BlocksDestroyed", blocksDestroyed);
            PlayerPrefs.SetInt("BlocksPlaced", blocksPlaced);
            PlayerPrefs.SetFloat("DistanceTraveled", distanceTraveled);
            if (player != null)
            {
                Vector3 pos = player.GetPosition();
                PlayerPrefs.SetFloat("PlayerPosX", pos.x);
                PlayerPrefs.SetFloat("PlayerPosY", pos.y);
                PlayerPrefs.SetFloat("PlayerPosZ", pos.z);
                PlayerPrefs.SetFloat("PlayerHealth", player.GetHealthPercent());
                PlayerPrefs.SetFloat("PlayerStamina", player.GetStaminaPercent());
            }
            PlayerPrefs.Save();
            Debug.Log("✅ Гру збережено (PlayerPrefs)!");
        }
        
        public void LoadGame()
        {
            Debug.Log("📁 Завантаження гри...");

            if (saveLoadService != null)
            {
                if (saveLoadService.TryLoadPlayerState(out var gt, out var bd, out var bp, out var dist, out var pos))
                {
                    gameTime = gt; blocksDestroyed = bd; blocksPlaced = bp; distanceTraveled = dist;
                    if (player != null)
                    {
                        player.transform.position = pos;
                        lastPlayerPosition = pos;
                    }
                    Debug.Log("✅ Гру завантажено (SaveLoadService)!");
                    return;
                }
                else
                {
                    Debug.LogWarning("⚠️ Збереження не знайдено (SaveLoadService)");
                }
            }

            // Fallback to PlayerPrefs
            if (PlayerPrefs.HasKey("GameTime"))
            {
                gameTime = PlayerPrefs.GetFloat("GameTime");
                blocksDestroyed = PlayerPrefs.GetInt("BlocksDestroyed");
                blocksPlaced = PlayerPrefs.GetInt("BlocksPlaced");
                distanceTraveled = PlayerPrefs.GetFloat("DistanceTraveled");
                if (player != null && PlayerPrefs.HasKey("PlayerPosX"))
                {
                    Vector3 savedPos = new Vector3(
                        PlayerPrefs.GetFloat("PlayerPosX"),
                        PlayerPrefs.GetFloat("PlayerPosY"),
                        PlayerPrefs.GetFloat("PlayerPosZ")
                    );
                    player.transform.position = savedPos;
                    lastPlayerPosition = savedPos;
                }
                Debug.Log("✅ Гру завантажено (PlayerPrefs)!");
            }
            else
            {
                Debug.LogWarning("⚠️ Файл збереження не знайдено!");
            }
        }
        
        public void OnBlockDestroyed()
        {
            blocksDestroyed++;
        }
        
        public void OnBlockPlaced()
        {
            blocksPlaced++;
        }
        
        // Методи для зовнішнього доступу
        public bool IsPaused()
        {
            return CurrentState == GameState.Paused;
        }
        
        public float GetGameTime()
        {
            return gameTime;
        }
        
        public Vector3 GetPlayerPosition()
        {
            if (player == null) return Vector3.zero;
            return player.GetPosition();
        }
        
        // Unity события
        void OnApplicationFocus(bool hasFocus)
        {
            if (pauseOnFocusLoss && !hasFocus && CurrentState == GameState.Running)
            {
                SetState(GameState.Paused);
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseOnFocusLoss && pauseStatus && CurrentState == GameState.Running)
            {
                SetState(GameState.Paused);
            }
        }
        
        void OnDestroy()
        {
            if (Instance == this)
            {
                CossackPlayer.OnBuildModeChanged -= UpdateBuildModeText;
                if (gameStateController != null)
                {
                    gameStateController.OnStateChanged -= HandleStateChangedForward;
                }

                InputHandler inputHandler = null;
                if (!ServiceLocator.TryGet(out inputHandler))
                {
                    inputHandler = FindFirstObjectByType<InputHandler>();
                }
                if (inputHandler != null)
                {
                    inputHandler.OnPause -= TogglePause;
                    inputHandler.OnToggleDebug -= ToggleDebugInfo;
                    inputHandler.OnQuickSave -= SaveGame;
                    inputHandler.OnQuickLoad -= LoadGame;
                }
            }
        }

        private void UpdateBuildModeText(bool isActive)
        {
            if (debugHUD != null)
            {
                debugHUD.SetBuildMode(isActive);
                return;
            }
            if (buildModeText == null)
            {
                Debug.LogError("BuildModeText is null!");
                return;
            }

            Debug.Log($"GameManager: Updating build mode text. IsActive: {isActive}");
            
            if (isActive)
            {
                buildModeText.text = "Режим: Будівництво";
                buildModeText.gameObject.SetActive(true);
            }
            else
            {
                // Можна або ховати текст, або показувати інший стан
                buildModeText.text = "Режим: Копання";
                buildModeText.gameObject.SetActive(true); // Показуємо завжди
            }
        }

        private void HandleStateChangedForward(GameStateController.State newState)
        {
            var mapped = newState == GameStateController.State.Paused ? GameState.Paused : GameState.Running;
            CurrentState = mapped;
            if (mapped == GameState.Paused) PauseGame(); else ResumeGame();
            OnGameStateChanged?.Invoke(mapped);
        }
    }
}