using UnityEngine;

namespace Inventory
{
    /// <summary>
    /// ScriptableObject для визначення даних про предмет.
    /// Дозволяє створювати нові типи предметів як асети в Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Основна інформація")]
        public string id = "item_id";
        public string itemName = "Новий предмет";
        [TextArea(3, 5)]
        public string description = "Опис предмета.";
        public Sprite icon;

        [Header("Характеристики")]
        public float weight = 1.0f; // Вага одного предмета в кілограмах
        public int maxStackSize = 1; // Максимальна кількість у стаку
        public bool isStackable { get { return maxStackSize > 1; } }

        [Header("Використання")]
        public bool isUsable = false;
        public bool isEquippable = false;
        public EquipmentType equipmentType;

        [Header("Світовий об'єкт")]
        public GameObject worldPrefab; // Префаб, що з'являється при викиданні предмета
    }

    /// <summary>
    /// Типи екіпіровки для визначення слотів.
    /// </summary>
    public enum EquipmentType
    {
        None,
        Weapon,      // Зброя (в руці)
        Tool,        // Інструмент (в руці)
        Hat,         // Головний убір
        Shirt,       // Сорочка (вишиванка)
        Pants,       // Штани (шаровари)
        Boots,       // Взуття
        Backpack     // Рюкзак (збільшує макс. вагу)
    }
} 