using UnityEngine;
using Azurin.Player;

namespace Azurin.CameraSystem
{
    public class IsometricCamera : MonoBehaviour
    {
        [Header("🎥 Ціль слідування")]
        public Transform target;
        public Vector3 offset = new Vector3(5, 8, -5);
        
        [Header("📐 Ізометричні налаштування")]
        public bool useOrthographic = true;
        public float orthographicSize = 4f;
        public float fieldOfView = 60f;
        
        [Header("🔄 Обертання")]
        public float rotationSpeed = 2f;
        public bool allowRotation = true;
        public float[] presetAngles = { 45f, 135f, 225f, 315f };
        private int currentAngleIndex = 0;
        
        [Header("🔍 Масштабування")]
        public float zoomSpeed = 2f;
        public float minZoom = 3f;
        public float maxZoom = 20f;
        public bool allowZoom = true;
        
        [Header("📱 Рух камери")]
        public float followSpeed = 5f;
        public float rotationSmoothness = 5f;
        public bool smoothFollow = true;
        
        [Header("🎮 Керування")]
        public KeyCode rotateLeftKey = KeyCode.Q;
        public KeyCode rotateRightKey = KeyCode.E;
        public KeyCode resetCameraKey = KeyCode.F2;
        public KeyCode toggleAngleKey = KeyCode.F1;
        
        // Приватні змінні
        private Camera cam;
        private Vector3 currentVelocity;
        private float currentRotationY = 45f;
        private float targetRotationY = 45f;
        private Vector3 lastTargetPosition;
        
        void Start()
        {
            Debug.Log("🎥 Ініціалізація ізометричної камери...");
            
            SetupCamera();
            FindTarget();
            SetInitialPosition();
            
            Debug.Log("✅ Камера готова!");
        }
        
        void SetupCamera()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }
            
            // Налаштовуємо тип проекції
            cam.orthographic = useOrthographic;
            if (useOrthographic)
            {
                cam.orthographicSize = orthographicSize;
            }
            else
            {
                cam.fieldOfView = fieldOfView;
            }
            
            // Налаштовуємо відстані відсікання
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            
            // Очищення фону
            cam.clearFlags = CameraClearFlags.Skybox;
        }
        
        void FindTarget()
        {
            if (target == null)
            {
                // Шукаємо гравця
                CossackPlayer player = FindFirstObjectByType<CossackPlayer>();
                if (player != null)
                {
                    target = player.transform;
                    Debug.Log("🎯 Знайдено ціль: " + target.name);
                }
                else
                {
                    Debug.LogWarning("⚠️ Ціль для камери не знайдена!");
                }
            }
        }
        
        void SetInitialPosition()
        {
            if (target != null)
            {
                // Встановлюємо початкову позицію
                Vector3 desiredPosition = CalculateCameraPosition();
                transform.position = desiredPosition;
                
                // Дивимося на ціль
                LookAtTarget();
                
                lastTargetPosition = target.position;
            }
        }
        
        void Update()
        {
            if (target == null)
            {
                FindTarget();
                return;
            }
            
            HandleInput();
            UpdateCameraPosition();
            UpdateCameraRotation();
            HandleZoom();
        }
        
        void HandleInput()
        {
            // Обертання камери
            if (allowRotation)
            {
                if (Input.GetKeyDown(rotateLeftKey))
                {
                    RotateCamera(-90f);
                }
                else if (Input.GetKeyDown(rotateRightKey))
                {
                    RotateCamera(90f);
                }
                
                // Перемикання між заданими кутами
                if (Input.GetKeyDown(toggleAngleKey))
                {
                    SwitchToNextPresetAngle();
                }
            }
            
            // Скидання камери
            if (Input.GetKeyDown(resetCameraKey))
            {
                ResetCamera();
            }
            
            // Ручне обертання мишею (опціонально)
            if (Input.GetMouseButton(2)) // Середня кнопка миші
            {
                float mouseX = Input.GetAxis("Mouse X");
                targetRotationY += mouseX * rotationSpeed;
            }
        }
        
        void UpdateCameraPosition()
        {
            Vector3 desiredPosition = CalculateCameraPosition();
            
            if (smoothFollow)
            {
                // Плавне слідування
                transform.position = Vector3.SmoothDamp(
                    transform.position, 
                    desiredPosition, 
                    ref currentVelocity, 
                    1f / followSpeed
                );
            }
            else
            {
                // Миттєве слідування
                transform.position = desiredPosition;
            }
        }
        
        void UpdateCameraRotation()
        {
            // Плавне обертання до цільового кута
            currentRotationY = Mathf.LerpAngle(currentRotationY, targetRotationY, rotationSmoothness * Time.deltaTime);
            
            // Оновлюємо обертання камери
            LookAtTarget();
        }
        
        Vector3 CalculateCameraPosition()
        {
            if (target == null) return transform.position;
            
            // Обчислюємо позицію камери відносно цілі
            float radianAngle = currentRotationY * Mathf.Deg2Rad;
            
            Vector3 rotatedOffset = new Vector3(
                offset.x * Mathf.Cos(radianAngle) - offset.z * Mathf.Sin(radianAngle),
                offset.y,
                offset.x * Mathf.Sin(radianAngle) + offset.z * Mathf.Cos(radianAngle)
            );
            
            return target.position + rotatedOffset;
        }
        
        void LookAtTarget()
        {
            if (target == null) return;
            
            // Дивимося на ціль з правильним нахилом для ізометрії
            Vector3 targetPosition = target.position + Vector3.up * 1f; // Трохи вище центру персонажа
            transform.LookAt(targetPosition);
        }
        
        void HandleZoom()
        {
            if (!allowZoom) return;
            
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                if (useOrthographic)
                {
                    orthographicSize -= scroll * zoomSpeed;
                    orthographicSize = Mathf.Clamp(orthographicSize, minZoom, maxZoom);
                    cam.orthographicSize = orthographicSize;
                }
                else
                {
                    fieldOfView -= scroll * zoomSpeed * 10f;
                    fieldOfView = Mathf.Clamp(fieldOfView, 30f, 90f);
                    cam.fieldOfView = fieldOfView;
                }
            }
        }
        
        void RotateCamera(float angle)
        {
            targetRotationY += angle;
            
            // Нормалізуємо кут
            while (targetRotationY >= 360f) targetRotationY -= 360f;
            while (targetRotationY < 0f) targetRotationY += 360f;
            
            Debug.Log($"🔄 Камера повертається на {targetRotationY}°");
        }
        
        void SwitchToNextPresetAngle()
        {
            currentAngleIndex = (currentAngleIndex + 1) % presetAngles.Length;
            targetRotationY = presetAngles[currentAngleIndex];
            
            Debug.Log($"📐 Переключено на кут {targetRotationY}°");
        }
        
        void ResetCamera()
        {
            targetRotationY = 45f;
            currentAngleIndex = 0;
            orthographicSize = 4f;
            
            if (cam != null)
            {
                cam.orthographicSize = orthographicSize;
            }
            
            Debug.Log("🔄 Камера скинута");
        }
        
        // Публічні методи для зовнішнього керування
        public void SetTarget(Transform target)
        {
            this.target = target;
            if (this.target == null)
            {
                // Try to find Player if target is cleared
                var player = FindFirstObjectByType<CossackPlayer>();
                if (player != null)
                {
                    this.target = player.transform;
                }
            }
            if (this.target != null)
            {
                lastTargetPosition = this.target.position;
                Debug.Log($"🎯 Нова ціль камери: {this.target.name}");
            }
        }
        
        public void SetZoom(float zoom)
        {
            orthographicSize = Mathf.Clamp(zoom, minZoom, maxZoom);
            if (cam != null && useOrthographic)
            {
                cam.orthographicSize = orthographicSize;
            }
        }
        
        public void SetRotation(float angle)
        {
            targetRotationY = angle;
            while (targetRotationY >= 360f) targetRotationY -= 360f;
            while (targetRotationY < 0f) targetRotationY += 360f;
        }
        
        public void FocusOnTarget()
        {
            if (target != null)
            {
                Vector3 desiredPosition = CalculateCameraPosition();
                transform.position = desiredPosition;
                LookAtTarget();
            }
        }
        
        public Vector3 GetCameraDirection()
        {
            return transform.forward;
        }
        
        public bool IsTargetVisible()
        {
            if (target == null) return false;
            
            Vector3 screenPoint = cam.WorldToViewportPoint(target.position);
            return screenPoint.x > 0 && screenPoint.x < 1 && 
                   screenPoint.y > 0 && screenPoint.y < 1 && 
                   screenPoint.z > 0;
        }
        
        // Debug методи
        void OnDrawGizmosSelected()
        {
            if (target == null) return;
            
            // Показуємо зв'язок з ціллю
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position);
            
            // Показуємо напрямок камери
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 5f);
            
            // Показуємо бажану позицію
            Vector3 desiredPos = CalculateCameraPosition();
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(desiredPos, 0.5f);
        }
    }
} 