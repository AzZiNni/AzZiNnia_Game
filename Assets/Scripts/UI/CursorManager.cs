using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI
{
    /// <summary>
    /// Менеджер курсорів для гри
    /// </summary>
    public class CursorManager : MonoBehaviour
    {
        [Header("Курсори")]
        [SerializeField] private Texture2D defaultCursor;
        [SerializeField] private Texture2D digCursor;
        [SerializeField] private Texture2D buildCursor;
        [SerializeField] private Texture2D combatCursor;
        
        [Header("Налаштування")]
        [SerializeField] private Vector2 hotspot = Vector2.zero;
        [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
        
        private static CursorManager instance;
        
        public static CursorManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<CursorManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("CursorManager");
                        instance = go.AddComponent<CursorManager>();
                    }
                }
                return instance;
            }
        }
        
        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Завантажуємо курсори з асетів якщо не призначені
            if (defaultCursor == null)
            {
                defaultCursor = Resources.Load<Texture2D>("Cursors/Cursors 256/G_Cursor_Move2");
                if (defaultCursor == null)
                {
                    // Спробуємо завантажити безпосередньо з AssetDatabase лише в редакторі
                    #if UNITY_EDITOR
                    if (defaultCursor == null)
                    {
                        defaultCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Cursors/Cursors 256/G_Cursor_Move2.png");
                    }
                    #endif
                }
            }
            
            // Встановлюємо стандартний курсор
            SetDefaultCursor();
        }
        
        public void SetDefaultCursor()
        {
            if (defaultCursor != null)
            {
                Cursor.SetCursor(defaultCursor, hotspot, cursorMode);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
        
        public void SetDigCursor()
        {
            if (digCursor != null)
            {
                Cursor.SetCursor(digCursor, hotspot, cursorMode);
            }
        }
        
        public void SetBuildCursor()
        {
            if (buildCursor != null)
            {
                Cursor.SetCursor(buildCursor, hotspot, cursorMode);
            }
        }
        
        public void SetCombatCursor()
        {
            if (combatCursor != null)
            {
                Cursor.SetCursor(combatCursor, hotspot, cursorMode);
            }
        }
        
        public void ShowCursor()
        {
            Cursor.visible = true;
        }
        
        public void HideCursor()
        {
            Cursor.visible = false;
        }
        
        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
        }

        // React to build mode changes
        private void OnEnable()
        {
            Azurin.Player.CossackPlayer.OnBuildModeChanged += HandleBuildModeChanged;
        }

        private void OnDisable()
        {
            Azurin.Player.CossackPlayer.OnBuildModeChanged -= HandleBuildModeChanged;
        }

        private void HandleBuildModeChanged(bool isActive)
        {
            if (isActive) SetBuildCursor(); else SetDefaultCursor();
        }
    }
} 