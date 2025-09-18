#pragma warning disable 0414

using UnityEngine;
using Azurin.Core;
using Voxel;

namespace Azurin.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(InputHandler))]
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(CharacterVisuals))]
    [RequireComponent(typeof(MagicSystem))]
    public class CossackPlayer : MonoBehaviour
    {
        [Header("👤 Персонаж")]
        public float height = 1.8f;
        public float width = 0.6f;
        public Color clothingColor = Color.blue;
        public Color skinColor = new Color(1f, 0.8f, 0.6f);

        [Header("🏃 Рух")]
        public float walkSpeed = 3f;
        public float runSpeed = 6f;
        public float jumpHeight = 1.5f;
        public float gravity = 20f;
        public float crouchSpeedMultiplier = 0.5f;
        public float lieDownSpeedMultiplier = 0.2f;

        [Header("🔨 Взаємодія")]
        public float reachDistance = 3f;
        public float digSpeed = 2f;
        public LayerMask voxelLayer = 1;

        [Header("💪 Стани")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public float maxStamina = 100f;
        public float currentStamina = 100f;

        [Header("⚔️ Характеристики козака")]
        [SerializeField] private string cossackName = "Іван Характерник";
        
        [Header("🎮 Керування")]
        [SerializeField] private KeyCode magicKey = KeyCode.Q;
        [Header("Mouse Settings")] 
        // Це значення можна налаштувати в інспекторі для комфортної гри
        public float mouseSensitivity = 0.1f;

        // Події
        public static event System.Action<bool> OnBuildModeChanged;

        // Публічні властивості
        public bool IsInBuildMode { get; private set; }

        // Компоненти
        private InputHandler inputHandler;
        private CharacterController _controller;
        private CharacterStats stats;
        private CharacterVisuals visuals;
        private MagicSystem magicSystem;
        private Camera playerCamera;
        private VoxelTerrain _voxelTerrain;
        

        // Рух
        private Vector3 velocity;
        private bool isGrounded;
        private bool isCrouching;
        private bool isProne;
        private bool _jumpPressed;

        // Стани
        private float originalHeight;
        private float crouchHeight;
        private float proneHeight;
        
        private Vector3 desiredMove;
        private float _rotationX = 0f;

        // Візуалізація
        private GameObject digPreview;

        // Components

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            inputHandler = GetComponent<InputHandler>();
            stats = GetComponent<CharacterStats>();
            visuals = GetComponent<CharacterVisuals>();
            magicSystem = GetComponent<MagicSystem>();
        }

        void Start()
        {
            UI.CursorManager.Instance.LockCursor();
            UI.CursorManager.Instance.HideCursor();
            
            SetupComponents();
            visuals.CreateCharacterModel(transform, height, skinColor, clothingColor);
            SetupHeights();
            SetupDigPreview();
            
            _voxelTerrain = FindFirstObjectByType<VoxelTerrain>();
            if (_voxelTerrain == null)
            {
                Debug.LogError("No VoxelTerrain found in scene!");
            }
        }
        
        private void OnEnable()
        {
            Debug.Log("CossackPlayer: OnEnable - Subscribing to events");
            inputHandler.OnJump += HandleJump;
            inputHandler.OnCrouch += HandleCrouchToggle;
            inputHandler.OnProne += HandleProneToggle;
            inputHandler.OnPrimaryAction += HandlePrimaryAction;
            inputHandler.OnSecondaryAction += HandleSecondaryAction;
            inputHandler.OnPause += HandlePause;
            inputHandler.OnToggleBuildMode += HandleToggleBuildMode;
            
            // Magic slots via InputHandler
            inputHandler.OnMagicSlot5 += () => magicSystem.SetMagicType(MagicType.Lightning);
            inputHandler.OnMagicSlot6 += () => magicSystem.SetMagicType(MagicType.Healing);
            inputHandler.OnMagicSlot7 += () => magicSystem.SetMagicType(MagicType.Building);
            inputHandler.OnMagicSlot8 += () => magicSystem.SetMagicType(MagicType.Protection);
            inputHandler.OnMagicSlot9 += () => magicSystem.SetMagicType(MagicType.Detection);
        }

        private void OnDisable()
        {
            Debug.Log("CossackPlayer: OnDisable - Unsubscribing from events");
            inputHandler.OnJump -= HandleJump;
            inputHandler.OnCrouch -= HandleCrouchToggle;
            inputHandler.OnProne -= HandleProneToggle;
            inputHandler.OnPrimaryAction -= HandlePrimaryAction;
            inputHandler.OnSecondaryAction -= HandleSecondaryAction;
            inputHandler.OnPause -= HandlePause;
            inputHandler.OnToggleBuildMode -= HandleToggleBuildMode;
            
            inputHandler.OnMagicSlot5 -= () => magicSystem.SetMagicType(MagicType.Lightning);
            inputHandler.OnMagicSlot6 -= () => magicSystem.SetMagicType(MagicType.Healing);
            inputHandler.OnMagicSlot7 -= () => magicSystem.SetMagicType(MagicType.Building);
            inputHandler.OnMagicSlot8 -= () => magicSystem.SetMagicType(MagicType.Protection);
            inputHandler.OnMagicSlot9 -= () => magicSystem.SetMagicType(MagicType.Detection);
        }
        
        void Update()
        {
            HandleMovement();
            HandleMouseLook();
            HandleMagicInput();
            UpdateStats();
            UpdateDigPreview();
        }

        void SetupComponents()
        {
            CharacterController cc = GetComponent<CharacterController>();
            cc.height = height;
            cc.radius = width * 0.5f;
            cc.center = new Vector3(0, height * 0.5f, 0);
            cc.skinWidth = cc.radius * 0.1f;

            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindFirstObjectByType<Camera>();
            }

            if (playerCamera != null && playerCamera.transform.parent != transform)
            {
                playerCamera.transform.SetParent(transform);
                playerCamera.transform.localPosition = new Vector3(0, height * 0.9f, 0);
                playerCamera.transform.localRotation = Quaternion.identity;
            }
        }

        void SetupHeights()
        {
            originalHeight = height;
            crouchHeight = height * 0.7f;
            proneHeight = height * 0.3f;
        }

        void SetupDigPreview()
        {
            digPreview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            digPreview.name = "DigPreview";
            
            var collider = digPreview.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            
            var renderer = digPreview.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Створюємо напівпрозорий матеріал
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetFloat("_Surface", 1); // Opaque to Transparent
                material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.3f));
                renderer.material = material;
            }
            
            digPreview.transform.localScale = Vector3.one * 2f * 2; // Радіус * 2
            digPreview.SetActive(false);
        }

        void UpdateDigPreview()
        {
            if (IsInBuildMode)
            {
                if (Physics.Raycast(GetInteractionRay(), out RaycastHit hit, reachDistance, voxelLayer))
                {
                    digPreview.SetActive(true);
                    digPreview.transform.position = hit.point;
                }
                else
                {
                    digPreview.SetActive(false);
                }
            }
            else
            {
                if (Physics.Raycast(GetInteractionRay(), out RaycastHit hit, reachDistance, voxelLayer))
                {
                    digPreview.SetActive(true);
                    digPreview.transform.position = hit.point;
                }
                else
                {
                    digPreview.SetActive(false);
                }
            }
        }

        void HandleMovement()
        {
            isGrounded = _controller.isGrounded;
            
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            float currentSpeed = GetCurrentSpeed();
            Vector2 moveInput = inputHandler.MoveInput;
            desiredMove = (forward * moveInput.y + right * moveInput.x).normalized * currentSpeed;
            
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; 
            }
            
            if (_jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
                _jumpPressed = false;
            }
            
            velocity.y -= gravity * Time.deltaTime;
            
            Vector3 move = desiredMove + Vector3.up * velocity.y;
            _controller.Move(move * Time.deltaTime);
        }

        void HandleMouseLook()
        {
            if (playerCamera == null || (GameManager.Instance != null && GameManager.Instance.IsPaused())) return;
            
            Vector2 lookInput = inputHandler.LookInput;
            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity;

            _rotationX -= mouseY;
            _rotationX = Mathf.Clamp(_rotationX, -90f, 90f);

            playerCamera.transform.localRotation = Quaternion.Euler(_rotationX, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        float GetCurrentSpeed()
        {
            if (isProne) return walkSpeed * lieDownSpeedMultiplier;
            if (isCrouching) return walkSpeed * crouchSpeedMultiplier;
            if (inputHandler.IsSprinting)
            {
                if (stats.TryUseStamina(10f * Time.deltaTime))
                {
                    return runSpeed;
                }
            }
            return walkSpeed;
        }

        void ChangeHeight(float newHeight)
        {
            _controller.height = newHeight;
            _controller.center = new Vector3(0, newHeight * 0.5f, 0);
            visuals.UpdateHeightVisuals(newHeight, originalHeight);
            
            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = new Vector3(0, newHeight * 0.9f, 0);
            }
        }
        
        void DestroyVoxel()
        {
            if (_voxelTerrain == null)
            {
                _voxelTerrain = FindFirstObjectByType<VoxelTerrain>();
                if (_voxelTerrain == null)
                {
                    Debug.LogWarning("VoxelTerrain not available for DestroyVoxel()");
                    return;
                }
            }
            
            if (Physics.Raycast(GetInteractionRay(), out RaycastHit hit, reachDistance, voxelLayer))
            {
                _voxelTerrain.ModifyTerrain(hit.point, 2f, -1f); 
            }
        }
        
        void PlaceVoxel()
        {
            if (_voxelTerrain == null)
            {
                _voxelTerrain = FindFirstObjectByType<VoxelTerrain>();
                if (_voxelTerrain == null)
                {
                    Debug.LogWarning("VoxelTerrain not available for PlaceVoxel()");
                    return;
                }
            }
            
            if (Physics.Raycast(GetInteractionRay(), out RaycastHit hit, reachDistance, voxelLayer))
            {
                _voxelTerrain.ModifyTerrain(hit.point, 2f, 1f);
            }
        }
        
        Ray GetInteractionRay()
        {
            return playerCamera != null 
                ? playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0)) 
                : new Ray(transform.position + Vector3.up * 1.6f, transform.forward);
        }
        
        void HandleMagicInput()
        {
            // Casting remains on the legacy key until an input action is added
            if (Input.GetKeyDown(magicKey))
                magicSystem.UseCurrentMagic(playerCamera, reachDistance);
        }
        
        void UpdateStats()
        {
            if (!inputHandler.IsSprinting && isGrounded)
            {
                stats.RestoreStamina(25f * Time.deltaTime);
            }
            stats.Heal(2f * Time.deltaTime); // Пасивна регенерація
        }

        // --- Event Handlers ---

        private void HandleJump()
        {
            if (isGrounded && !isCrouching && !isProne)
            {
                _jumpPressed = true;
            }
        }

        private void HandleCrouchToggle()
        {
            if (isProne) return;
            isCrouching = !isCrouching;
            ChangeHeight(isCrouching ? crouchHeight : originalHeight);
        }
        
        private void HandleProneToggle()
        {
            if (isCrouching) return;
            isProne = !isProne;
            ChangeHeight(isProne ? proneHeight : originalHeight);
        }

        private void HandlePrimaryAction()
        {
            if (IsInBuildMode)
            {
                PlaceVoxel();
            }
            else
            {
                DestroyVoxel();
            }
        }
        
        private void HandleSecondaryAction()
        {
            // Наразі не використовується, але можна додати логіку
            // наприклад, для сильного удару або іншої дії
        }
        
        private void HandlePause()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }
        
        private void HandleToggleBuildMode()
        {
            IsInBuildMode = !IsInBuildMode;
            Debug.Log($"Режим будівництва: {(IsInBuildMode ? "Увімкнено" : "Вимкнено")}");
            OnBuildModeChanged?.Invoke(IsInBuildMode);
        }
        
        // --- Public Getters ---

        public bool IsMoving() => _controller != null && _controller.velocity.magnitude > 0.1f;
        public Vector3 GetPosition() => transform.position;
        public bool IsCrouching() => isCrouching;
        public bool IsProne() => isProne;
        public float GetHealthPercent() => stats != null ? stats.GetHealthPercent() : 0;
        public float GetStaminaPercent() => stats != null ? stats.GetStaminaPercent() : 0;

        public void ToggleBuildMode()
        {
            HandleToggleBuildMode();
        }

        public void PauseGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }
    }
}