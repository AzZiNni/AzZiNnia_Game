using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Inventory
{
    /// <summary>
    /// Представляє один елемент в інвентарі, з посиланням на його дані та кількість.
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        public ItemData data;
        public int stackSize;

        public InventoryItem(ItemData source, int count = 1)
        {
            data = source;
            stackSize = count;
        }

        public float GetTotalWeight()
        {
            return data.weight * stackSize;
        }
    }

    /// <summary>
    /// Основна система інвентарю, що базується на вазі. Керує предметами гравця.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        [Header("Налаштування інвентарю")]
        [SerializeField] private float maxWeight = 15.0f; // Максимальна вага, яку може нести гравець (в кг)
        
        // Події для оновлення UI та інших систем
        public System.Action<InventoryItem> OnItemAdded;
        public System.Action<InventoryItem> OnItemRemoved;
        public System.Action OnInventoryChanged;

        private List<InventoryItem> items;
        private float currentWeight = 0.0f;

        public float MaxWeight => maxWeight;
        public float CurrentWeight => currentWeight;
        public List<InventoryItem> Items => items;

        void Awake()
        {
            items = new List<InventoryItem>();
        }

        /// <summary>
        /// Спроба додати предмет до інвентарю.
        /// </summary>
        /// <param name="itemData">Дані предмета, що додається.</param>
        /// <param name="count">Кількість.</param>
        /// <returns>True, якщо предмет успішно додано.</returns>
        public bool AddItem(ItemData itemData, int count = 1)
        {
            float weightToAdd = itemData.weight * count;

            if (currentWeight + weightToAdd > maxWeight)
            {
                Debug.LogWarning($"Недостатньо місця в інвентарі! Вага: {currentWeight}/{maxWeight}, потрібно: {weightToAdd}");
                // Тут можна додати сповіщення для гравця
                return false;
            }

            // Якщо предмет стакається, шукаємо існуючий стак
            if (itemData.isStackable)
            {
                InventoryItem existingItem = items.FirstOrDefault(i => i.data == itemData);
                if (existingItem != null)
                {
                    existingItem.stackSize += count;
                }
                else
                {
                    items.Add(new InventoryItem(itemData, count));
                }
            }
            else // Інакше додаємо як новий окремий предмет
            {
                for (int i = 0; i < count; i++)
                {
                    items.Add(new InventoryItem(itemData, 1));
                }
            }
            
            UpdateWeight();
            OnInventoryChanged?.Invoke();
            OnItemAdded?.Invoke(new InventoryItem(itemData, count)); // Повідомляємо про доданий предмет
            Debug.Log($"Додано '{itemData.itemName}' x{count} до інвентарю. Нова вага: {currentWeight:F2}/{maxWeight} кг.");
            return true;
        }

        /// <summary>
        /// Видаляє предмет з інвентарю.
        /// </summary>
        /// <param name="itemData">Дані предмета, що видаляється.</param>
        /// <param name="count">Кількість.</param>
        public void RemoveItem(ItemData itemData, int count = 1)
        {
            InventoryItem itemToRemove = items.FirstOrDefault(i => i.data == itemData);
            if (itemToRemove == null)
            {
                Debug.LogWarning($"Спроба видалити предмет '{itemData.itemName}', якого немає в інвентарі.");
                return;
            }

            if (itemData.isStackable)
            {
                itemToRemove.stackSize -= count;
                if (itemToRemove.stackSize <= 0)
                {
                    items.Remove(itemToRemove);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    InventoryItem singleItem = items.FirstOrDefault(it => it.data == itemData);
                    if (singleItem != null)
                    {
                        items.Remove(singleItem);
                    }
                }
            }

            UpdateWeight();
            OnInventoryChanged?.Invoke();
            OnItemRemoved?.Invoke(new InventoryItem(itemData, count)); // Повідомляємо про видалений предмет
            Debug.Log($"Видалено '{itemData.itemName}' x{count} з інвентарю. Нова вага: {currentWeight:F2}/{maxWeight} кг.");
        }

        /// <summary>
        /// Перевіряє, чи є в інвентарі певний предмет.
        /// </summary>
        /// <param name="itemData">Предмет для перевірки.</param>
        /// <param name="count">Необхідна кількість.</param>
        /// <returns>True, якщо предмет є в достатній кількості.</returns>
        public bool HasItem(ItemData itemData, int count = 1)
        {
            if (itemData.isStackable)
            {
                InventoryItem item = items.FirstOrDefault(i => i.data == itemData);
                return item != null && item.stackSize >= count;
            }
            else
            {
                return items.Count(i => i.data == itemData) >= count;
            }
        }
        
        /// <summary>
        /// Оновлює загальну вагу інвентарю.
        /// </summary>
        private void UpdateWeight()
        {
            currentWeight = 0;
            foreach (var item in items)
            {
                currentWeight += item.GetTotalWeight();
            }
        }

        /// <summary>
        /// Змінює максимальну вагу (наприклад, при одяганні рюкзака).
        /// </summary>
        /// <param name="newMaxWeight">Нове значення максимальної ваги.</param>
        public void SetMaxWeight(float newMaxWeight)
        {
            this.maxWeight = newMaxWeight;
            OnInventoryChanged?.Invoke(); // Оновити UI
            Debug.Log($"Максимальну вагу інвентарю змінено на {maxWeight} кг.");
        }
    }
} 