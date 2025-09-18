using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    /// <summary>
    /// Керує відображенням одного слота з предметом в UI інвентарю.
    /// </summary>
    public class ItemSlotUI : MonoBehaviour
    {
        [Header("Компоненти слота")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private GameObject countBackground;

        /// <summary>
        /// Встановлює дані предмета для цього слота.
        /// </summary>
        /// <param name="item">Об'єкт InventoryItem для відображення.</param>
        public void SetItem(Inventory.InventoryItem item)
        {
            if (item == null || item.data == null)
            {
                // Очистити слот, якщо предмет недійсний
                iconImage.sprite = null;
                iconImage.enabled = false;
                nameText.text = "";
                weightText.text = "";
                countText.text = "";
                if (countBackground != null) countBackground.SetActive(false);
                return;
            }

            var itemData = item.data;

            // Іконка
            iconImage.sprite = itemData.icon;
            iconImage.enabled = true;

            // Назва та вага
            nameText.text = itemData.itemName;
            weightText.text = $"{item.GetTotalWeight():F2} кг";

            // Кількість (якщо стакається)
            if (item.stackSize > 1)
            {
                countText.text = $"x{item.stackSize}";
                if (countBackground != null) countBackground.SetActive(true);
            }
            else
            {
                countText.text = "";
                if (countBackground != null) countBackground.SetActive(false);
            }
        }
    }
} 