using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Azurin.Core;

namespace Azurin.Player
{
    /// <summary>
    /// Handles player input using the new Input System.
    /// It reads raw input data and makes it available for other components.
    /// It also handles switching between Player and UI action maps.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        public static InputHandler Instance { get; private set; }

        private PlayerControls _playerControls;
        private CossackPlayer cossackPlayer;

        // --- Player Input Properties ---
        public Vector2 MoveInput => _playerControls.Player.Move.ReadValue<Vector2>();
        public Vector2 LookInput => _playerControls.Player.Look.ReadValue<Vector2>();
        public bool IsSprinting => _playerControls.Player.Sprint.IsPressed();
        
        // --- Player Input Events ---
        public event Action OnJump;
        public event Action OnCrouch;
        public event Action OnProne;
        public event Action OnInteract;
        public event Action OnPrimaryAction;
        public event Action OnSecondaryAction;
        public event Action OnInventory;
        public event Action OnPause;
        public event Action OnToggleBuildMode;
        
        // --- Magic selection events (migration from legacy Input.GetKeyDown) ---
        public event Action OnMagicSlot5; // Lightning
        public event Action OnMagicSlot6; // Healing
        public event Action OnMagicSlot7; // Building
        public event Action OnMagicSlot8; // Protection
        public event Action OnMagicSlot9; // Detection
        public event Action OnToggleDebug;
        public event Action OnQuickSave;
        public event Action OnQuickLoad;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Register in the ServiceLocator for decoupled access
            Azurin.Core.ServiceLocator.Register(this);

            cossackPlayer = GetComponent<CossackPlayer>();
        }

        private void OnEnable()
        {
            if (_playerControls == null)
            {
                _playerControls = new PlayerControls();
            }
            
            _playerControls.Player.Enable();
            
            _playerControls.Player.Jump.performed += OnJumpPerformed;
            _playerControls.Player.Crouch.performed += OnCrouchPerformed;
            _playerControls.Player.Prone.performed += OnPronePerformed;
            _playerControls.Player.Interact.performed += OnInteractPerformed;
            _playerControls.Player.PrimaryAction.performed += OnPrimaryActionPerformed;
            _playerControls.Player.SecondaryAction.performed += OnSecondaryActionPerformed;
            _playerControls.Player.Inventory.performed += OnInventoryPerformed;
            _playerControls.Player.Pause.performed += OnPausePerformed;
            _playerControls.Player.ToggleBuildMode.performed += OnToggleBuildModePerformed;
        }

        private void OnDisable()
        {
            if (_playerControls != null)
            {
                _playerControls.Player.Disable();

                _playerControls.Player.Jump.performed -= OnJumpPerformed;
                _playerControls.Player.Crouch.performed -= OnCrouchPerformed;
                _playerControls.Player.Prone.performed -= OnPronePerformed;
                _playerControls.Player.Interact.performed -= OnInteractPerformed;
                _playerControls.Player.PrimaryAction.performed -= OnPrimaryActionPerformed;
                _playerControls.Player.SecondaryAction.performed -= OnSecondaryActionPerformed;
                _playerControls.Player.Inventory.performed -= OnInventoryPerformed;
                _playerControls.Player.Pause.performed -= OnPausePerformed;
                _playerControls.Player.ToggleBuildMode.performed -= OnToggleBuildModePerformed;
            }
        }
        
        private void Start()
        {
            GameManager.OnGameStateChanged += HandleGameStateChange;
        }

        private void OnDestroy()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChange;
            if (_playerControls != null)
            {
                _playerControls.Dispose();
                _playerControls = null;
            }
            Azurin.Core.ServiceLocator.Unregister<InputHandler>();
        }

        private void OnJumpPerformed(InputAction.CallbackContext context) => OnJump?.Invoke();
        private void OnCrouchPerformed(InputAction.CallbackContext context) => OnCrouch?.Invoke();
        private void OnPronePerformed(InputAction.CallbackContext context) => OnProne?.Invoke();
        private void OnInteractPerformed(InputAction.CallbackContext context) => OnInteract?.Invoke();
        private void OnPrimaryActionPerformed(InputAction.CallbackContext context) => OnPrimaryAction?.Invoke();
        private void OnSecondaryActionPerformed(InputAction.CallbackContext context) => OnSecondaryAction?.Invoke();
        private void OnInventoryPerformed(InputAction.CallbackContext context) => OnInventory?.Invoke();
        private void OnPausePerformed(InputAction.CallbackContext context) => OnPause?.Invoke();
        private void OnToggleBuildModePerformed(InputAction.CallbackContext context) => OnToggleBuildMode?.Invoke();

        public void SwitchToPlayerActions()
        {
            _playerControls.UI.Disable();
            _playerControls.Player.Enable();
        }

        public void SwitchToUIActions()
        {
            _playerControls.Player.Disable();
            _playerControls.UI.Enable();
        }

        private void HandleGameStateChange(GameManager.GameState newState)
        {
            if (newState == GameManager.GameState.Paused)
            {
                SwitchToUIActions();
            }
            else
            {
                SwitchToPlayerActions();
            }
        }

        private void Update()
        {
            // Temporary hotkeys routed through the new Input System keyboard
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f3Key.wasPressedThisFrame) OnToggleDebug?.Invoke();
                if (Keyboard.current.f5Key.wasPressedThisFrame) OnQuickSave?.Invoke();
                if (Keyboard.current.f9Key.wasPressedThisFrame) OnQuickLoad?.Invoke();
                
                // Magic slots
                if (Keyboard.current.digit5Key.wasPressedThisFrame) OnMagicSlot5?.Invoke();
                if (Keyboard.current.digit6Key.wasPressedThisFrame) OnMagicSlot6?.Invoke();
                if (Keyboard.current.digit7Key.wasPressedThisFrame) OnMagicSlot7?.Invoke();
                if (Keyboard.current.digit8Key.wasPressedThisFrame) OnMagicSlot8?.Invoke();
                if (Keyboard.current.digit9Key.wasPressedThisFrame) OnMagicSlot9?.Invoke();
            }
        }

        private void SetupCallbacks()
        {
            // Цей метод більше не потрібен у такому вигляді
        }
    }
} 