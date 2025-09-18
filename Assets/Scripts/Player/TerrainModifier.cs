using UnityEngine;
using Voxel;

namespace Player
{
    /// <summary>
    /// Компонент для модифікації терену гравцем
    /// </summary>
    public class TerrainModifier : MonoBehaviour
    {
        [Header("Терен")]
        public VoxelTerrain terrain;
        
        [Header("Керування")]
        public KeyCode modifyKey = KeyCode.T; // Одна кнопка для входу в режим
        public bool holdToModify = false;
        
        [Header("Налаштування променю")]
        public float rayDistance = 10f;
        public float currentRadius = 2f;
        public float minRadius = 0.5f;
        public float maxRadius = 5f;
        public float radiusChangeSpeed = 0.5f;
        
        [Header("Сила модифікації")]
        public float digStrength = -2f;
        public float buildStrength = 2f;
        
        [Header("Візуалізація")]
        public GameObject cursorPrefab;
        public Color digColor = Color.red;
        public Color buildColor = Color.green;
        
        private Camera playerCamera;
        private GameObject cursor;
        private MeshRenderer cursorRenderer;
        private bool isModifying = false;
        
        void Start()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
            
            // Створюємо курсор
            if (cursorPrefab != null)
            {
                cursor = Instantiate(cursorPrefab);
                cursorRenderer = cursor.GetComponent<MeshRenderer>();
            }
            else
            {
                // Створюємо простий курсор якщо префаб не заданий
                cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cursor.name = "TerrainCursor";
                cursorRenderer = cursor.GetComponent<MeshRenderer>();
                Destroy(cursor.GetComponent<SphereCollider>());
            }
            
            cursor.SetActive(false);
        }
        
        void Update()
        {
            // Якщо камера не знайдена, пробуємо знайти її знову
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null) return; // Виходимо, якщо камери все ще немає
            }

            // Перемикаємо режим модифікації
            if (holdToModify)
            {
                isModifying = Input.GetKey(modifyKey);
            }
            else
            {
                if (Input.GetKeyDown(modifyKey))
                {
                    isModifying = !isModifying;
                    Debug.Log($"Режим модифікації терену: {(isModifying ? "Увімкнено" : "Вимкнено")}");
                }
            }
            
            // Показуємо/ховаємо курсор
            cursor.SetActive(isModifying);
            
            if (!isModifying) return;
            
            // Кастуємо промінь з камери
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                // Позиціонуємо курсор
                cursor.transform.position = hit.point;
                cursor.transform.localScale = Vector3.one * currentRadius * 2f;
                
                // Міняємо радіус колесом миші
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll != 0)
                {
                    currentRadius = Mathf.Clamp(currentRadius + scroll * radiusChangeSpeed, minRadius, maxRadius);
                }

                if (terrain != null)
                {
                    // Копання (ЛКМ)
                    if (Input.GetMouseButton(0))
                    {
                        terrain.ModifyTerrain(hit.point, currentRadius, digStrength);
                        
                        // Оновлюємо колір курсора
                        if (cursorRenderer != null)
                        {
                            cursorRenderer.material.color = digColor;
                        }
                    }
                    // Будування (ПКМ)
                    else if (Input.GetMouseButton(1))
                    {
                        terrain.ModifyTerrain(hit.point, currentRadius, buildStrength);
                        
                        // Оновлюємо колір курсора
                        if (cursorRenderer != null)
                        {
                            cursorRenderer.material.color = buildColor;
                        }
                    }
                    else
                    {
                        // Нейтральний колір коли не модифікуємо
                        if (cursorRenderer != null)
                        {
                            cursorRenderer.material.color = Color.white;
                        }
                    }
                }
            }
        }
        
        void OnGUI()
        {
            if (!isModifying) return;
            
            // Показуємо інформацію про поточний режим
            GUI.Box(new Rect(10, Screen.height - 80, 250, 70), "");
            GUI.Label(new Rect(15, Screen.height - 75, 240, 20), $"Режим модифікації терену");
            GUI.Label(new Rect(15, Screen.height - 55, 240, 20), $"Радіус: {currentRadius:F1}");
            GUI.Label(new Rect(15, Screen.height - 35, 240, 20), "ЛКМ - Копати | ПКМ - Будувати");
            
            // Підказки
            int y = 10;
            GUI.Label(new Rect(10, y, 300, 20), $"{modifyKey} - {(holdToModify ? "Утримувати" : "Перемкнути")} режим модифікації");
            y += 20;
            GUI.Label(new Rect(10, y, 300, 20), "Колесо миші - Змінити радіус");
        }
    }
} 