using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Azurin.Player;

namespace UI
{
    /// <summary>
    /// Керує візуальним відображенням інвентарю, що базується на вазі.
    /// Показує предмети, вагу, та дозволяє взаємодію.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("Компоненти UI")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private Slider weightSlider;

        [Header("Клавіша активації")]
        [SerializeField] private KeyCode toggleKey = KeyCode.I;

        private Inventory.InventorySystem inventorySystem;
        private List<GameObject> itemSlots = new List<GameObject>();
        private bool isVisible = false;

        void Start()
        {
            // Знаходимо систему інвентарю на гравці
            inventorySystem = FindFirstObjectByType<CossackPlayer>().GetComponent<Inventory.InventorySystem>();
            if (inventorySystem == null)
            {
                Debug.LogError("InventorySystem не знайдено на гравці!");
                this.enabled = false;
                return;
            }

            // Підписуємось на події
            inventorySystem.OnInventoryChanged += UpdateUI;

            // Ховаємо панель на старті
            inventoryPanel.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleInventory();
            }
        }

        /// <summary>
        /// Перемикає видимість інвентарю.
        /// </summary>
        public void ToggleInventory()
        {
            isVisible = !isVisible;
            inventoryPanel.SetActive(isVisible);
            
            // Керування курсором через CursorManager
            if (isVisible)
            {
                UI.CursorManager.Instance.UnlockCursor();
                UI.CursorManager.Instance.ShowCursor();
                UpdateUI();
            }
            else
            {
                UI.CursorManager.Instance.LockCursor();
                UI.CursorManager.Instance.HideCursor();
            }
        }

        /// <summary>
        /// Оновлює всі елементи UI інвентарю.
        /// </summary>
        private void UpdateUI()
        {
            if (!inventoryPanel.activeSelf && !isVisible) return;

            // Очищуємо старі слоти
            foreach (GameObject slot in itemSlots)
            {
                Destroy(slot);
            }
            itemSlots.Clear();

            // Створюємо нові слоти для кожного предмета
            if (inventorySystem != null && inventorySystem.Items != null)
            {
                 foreach (var item in inventorySystem.Items)
                {
                    if (itemSlotPrefab != null && itemsContainer != null)
                    {
                        GameObject slotGO = Instantiate(itemSlotPrefab, itemsContainer);
                        var slotUI = slotGO.GetComponent<ItemSlotUI>();
                        if (slotUI != null)
                        {
                            slotUI.SetItem(item);
                        }
                        itemSlots.Add(slotGO);
                    }
                }
            }
           
            // Оновлюємо показники ваги
            if (inventorySystem != null && weightText != null && weightSlider != null)
            {
                float currentWeight = inventorySystem.CurrentWeight;
                float maxWeight = inventorySystem.MaxWeight;
                
                weightText.text = $"Вага: {currentWeight:F2} / {maxWeight:F1} кг";
                weightSlider.maxValue = maxWeight;
                weightSlider.value = currentWeight;
            }
        }

        void OnDestroy()
        {
            // Відписуємось від подій
            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged -= UpdateUI;
            }
        }
    }
} 