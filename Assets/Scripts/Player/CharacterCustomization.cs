using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Player
{
    /// <summary>
    /// Система кастомізації персонажа з українською тематикою
    /// </summary>
    public class CharacterCustomization : MonoBehaviour
    {
        [Header("Персонаж")]
        [SerializeField] private GameObject characterModel;
        [SerializeField] private Renderer characterRenderer;
        
        [Header("Зачіски")]
        [SerializeField] private GameObject[] hairstyles;
        [SerializeField] private int currentHairstyle = 0;
        
        [Header("Одяг")]
        [SerializeField] private Material[] shirtMaterials;
        [SerializeField] private Material[] pantsMaterials;
        [SerializeField] private int currentShirt = 0;
        [SerializeField] private int currentPants = 0;
        
        [Header("Кольори")]
        [SerializeField] private Color[] skinColors;
        [SerializeField] private Color[] hairColors;
        [SerializeField] private int currentSkinColor = 0;
        [SerializeField] private int currentHairColor = 0;
        
        [Header("Українські елементи")]
        [SerializeField] private GameObject[] ukrainianAccessories; // Вишиванки, шапки тощо
        [SerializeField] private bool[] accessoryEnabled;
        
        [Header("UI")]
        [SerializeField] private Canvas customizationUI;
        [SerializeField] private Button nextHairstyleButton;
        [SerializeField] private Button prevHairstyleButton;
        [SerializeField] private Button nextShirtButton;
        [SerializeField] private Button prevShirtButton;
        [SerializeField] private Button nextPantsButton;
        [SerializeField] private Button prevPantsButton;
        [SerializeField] private Button nextSkinColorButton;
        [SerializeField] private Button prevSkinColorButton;
        [SerializeField] private Button nextHairColorButton;
        [SerializeField] private Button prevHairColorButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;
        
        [Header("Текст")]
        [SerializeField] private TextMeshProUGUI hairstyleText;
        [SerializeField] private TextMeshProUGUI shirtText;
        [SerializeField] private TextMeshProUGUI pantsText;
        
        // Назви елементів українською
        private string[] hairstyleNames = {
            "Коротка стрижка",
            "Довге волосся",
            "Коса",
            "Чуб козацький",
            "Оселедець",
            "Сучасна стрижка"
        };
        
        private string[] shirtNames = {
            "Проста сорочка",
            "Вишиванка",
            "Козацька сорочка",
            "Селянська сорочка",
            "Святкова вишиванка"
        };
        
        private string[] pantsNames = {
            "Прості штани",
            "Шаровари",
            "Козацькі штани",
            "Селянські штани"
        };
        
        // Збережені налаштування
        private CharacterData savedData;
        private CharacterData originalData;
        
        [System.Serializable]
        public class CharacterData
        {
            public int hairstyle;
            public int shirt;
            public int pants;
            public int skinColor;
            public int hairColor;
            public bool[] accessories;
        }
        
        void Start()
        {
            InitializeCustomization();
            SetupUI();
            LoadCharacterData();
        }
        
        void InitializeCustomization()
        {
            // Ініціалізуємо масиви якщо не задані
            if (skinColors == null || skinColors.Length == 0)
            {
                skinColors = new Color[] {
                    new Color(1f, 0.8f, 0.6f),      // Світла шкіра
                    new Color(0.9f, 0.7f, 0.5f),    // Середня шкіра
                    new Color(0.8f, 0.6f, 0.4f),    // Темніша шкіра
                    new Color(0.95f, 0.75f, 0.55f)   // Рожевувата шкіра
                };
            }
            
            if (hairColors == null || hairColors.Length == 0)
            {
                hairColors = new Color[] {
                    new Color(0.2f, 0.1f, 0.05f),   // Темно-коричневе
                    new Color(0.4f, 0.2f, 0.1f),    // Коричневе
                    new Color(0.8f, 0.6f, 0.2f),    // Світло-коричневе
                    new Color(0.9f, 0.8f, 0.3f),    // Блонд
                    new Color(0.1f, 0.1f, 0.1f),    // Чорне
                    new Color(0.6f, 0.6f, 0.6f)     // Сиве
                };
            }
            
            // Ініціалізуємо аксесуари
            if (ukrainianAccessories != null && accessoryEnabled == null)
            {
                accessoryEnabled = new bool[ukrainianAccessories.Length];
            }
            
            // Зберігаємо оригінальні налаштування
            SaveCurrentDataAsOriginal();
        }
        
        void SetupUI()
        {
            if (customizationUI != null)
            {
                customizationUI.gameObject.SetActive(false);
            }
            
            // Налаштовуємо кнопки
            if (nextHairstyleButton != null)
                nextHairstyleButton.onClick.AddListener(() => ChangeHairstyle(1));
            if (prevHairstyleButton != null)
                prevHairstyleButton.onClick.AddListener(() => ChangeHairstyle(-1));
                
            if (nextShirtButton != null)
                nextShirtButton.onClick.AddListener(() => ChangeShirt(1));
            if (prevShirtButton != null)
                prevShirtButton.onClick.AddListener(() => ChangeShirt(-1));
                
            if (nextPantsButton != null)
                nextPantsButton.onClick.AddListener(() => ChangePants(1));
            if (prevPantsButton != null)
                prevPantsButton.onClick.AddListener(() => ChangePants(-1));
                
            if (nextSkinColorButton != null)
                nextSkinColorButton.onClick.AddListener(() => ChangeSkinColor(1));
            if (prevSkinColorButton != null)
                prevSkinColorButton.onClick.AddListener(() => ChangeSkinColor(-1));
                
            if (nextHairColorButton != null)
                nextHairColorButton.onClick.AddListener(() => ChangeHairColor(1));
            if (prevHairColorButton != null)
                prevHairColorButton.onClick.AddListener(() => ChangeHairColor(-1));
                
            if (saveButton != null)
                saveButton.onClick.AddListener(SaveChanges);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelChanges);
        }
        
        public void OpenCustomization()
        {
            if (customizationUI != null)
            {
                customizationUI.gameObject.SetActive(true);
                Time.timeScale = 0f; // Пауза гри
                UI.CursorManager.Instance.UnlockCursor();
                UI.CursorManager.Instance.ShowCursor();
            }
            
            UpdateUI();
        }
        
        public void CloseCustomization()
        {
            if (customizationUI != null)
            {
                customizationUI.gameObject.SetActive(false);
                Time.timeScale = 1f; // Відновлення гри
                UI.CursorManager.Instance.LockCursor();
                UI.CursorManager.Instance.HideCursor();
            }
        }
        
        void ChangeHairstyle(int direction)
        {
            if (hairstyles == null || hairstyles.Length == 0) return;
            
            currentHairstyle = (currentHairstyle + direction + hairstyles.Length) % hairstyles.Length;
            ApplyHairstyle();
            UpdateUI();
        }
        
        void ChangeShirt(int direction)
        {
            if (shirtMaterials == null || shirtMaterials.Length == 0) return;
            
            currentShirt = (currentShirt + direction + shirtMaterials.Length) % shirtMaterials.Length;
            ApplyClothing();
            UpdateUI();
        }
        
        void ChangePants(int direction)
        {
            if (pantsMaterials == null || pantsMaterials.Length == 0) return;
            
            currentPants = (currentPants + direction + pantsMaterials.Length) % pantsMaterials.Length;
            ApplyClothing();
            UpdateUI();
        }
        
        void ChangeSkinColor(int direction)
        {
            currentSkinColor = (currentSkinColor + direction + skinColors.Length) % skinColors.Length;
            ApplySkinColor();
        }
        
        void ChangeHairColor(int direction)
        {
            currentHairColor = (currentHairColor + direction + hairColors.Length) % hairColors.Length;
            ApplyHairColor();
        }
        
        void ApplyHairstyle()
        {
            if (hairstyles == null) return;
            
            // Вимикаємо всі зачіски
            foreach (var hair in hairstyles)
            {
                if (hair != null)
                    hair.SetActive(false);
            }
            
            // Вмикаємо поточну зачіску
            if (currentHairstyle < hairstyles.Length && hairstyles[currentHairstyle] != null)
            {
                hairstyles[currentHairstyle].SetActive(true);
            }
        }
        
        void ApplyClothing()
        {
            if (characterRenderer == null) return;
            
            // Застосовуємо матеріали одягу
            Material[] materials = characterRenderer.materials;
            
            if (currentShirt < shirtMaterials.Length && shirtMaterials[currentShirt] != null)
            {
                // Припускаємо що сорочка - це перший матеріал
                if (materials.Length > 0)
                    materials[0] = shirtMaterials[currentShirt];
            }
            
            if (currentPants < pantsMaterials.Length && pantsMaterials[currentPants] != null)
            {
                // Припускаємо що штани - це другий матеріал
                if (materials.Length > 1)
                    materials[1] = pantsMaterials[currentPants];
            }
            
            characterRenderer.materials = materials;
        }
        
        void ApplySkinColor()
        {
            if (characterRenderer == null || currentSkinColor >= skinColors.Length) return;
            
            // Застосовуємо колір шкіри до відповідного матеріалу
            Material[] materials = characterRenderer.materials;
            if (materials.Length > 2) // Припускаємо що шкіра - це третій матеріал
            {
                materials[2].color = skinColors[currentSkinColor];
                characterRenderer.materials = materials;
            }
        }
        
        void ApplyHairColor()
        {
            if (hairstyles == null || currentHairstyle >= hairstyles.Length) return;
            if (currentHairColor >= hairColors.Length) return;
            
            GameObject currentHair = hairstyles[currentHairstyle];
            if (currentHair != null)
            {
                Renderer hairRenderer = currentHair.GetComponent<Renderer>();
                if (hairRenderer != null)
                {
                    hairRenderer.material.color = hairColors[currentHairColor];
                }
            }
        }
        
        void UpdateUI()
        {
            if (hairstyleText != null)
            {
                string name = currentHairstyle < hairstyleNames.Length 
                    ? hairstyleNames[currentHairstyle] 
                    : $"Зачіска {currentHairstyle + 1}";
                hairstyleText.text = name;
            }
            
            if (shirtText != null)
            {
                string name = currentShirt < shirtNames.Length 
                    ? shirtNames[currentShirt] 
                    : $"Сорочка {currentShirt + 1}";
                shirtText.text = name;
            }
            
            if (pantsText != null)
            {
                string name = currentPants < pantsNames.Length 
                    ? pantsNames[currentPants] 
                    : $"Штани {currentPants + 1}";
                pantsText.text = name;
            }
        }
        
        void SaveCurrentDataAsOriginal()
        {
            originalData = new CharacterData
            {
                hairstyle = currentHairstyle,
                shirt = currentShirt,
                pants = currentPants,
                skinColor = currentSkinColor,
                hairColor = currentHairColor,
                accessories = (bool[])accessoryEnabled?.Clone()
            };
        }
        
        void SaveChanges()
        {
            SaveCharacterData();
            SaveCurrentDataAsOriginal();
            CloseCustomization();
            
            Debug.Log("🎨 Зміни персонажа збережено!");
        }
        
        void CancelChanges()
        {
            // Відновлюємо оригінальні налаштування
            if (originalData != null)
            {
                currentHairstyle = originalData.hairstyle;
                currentShirt = originalData.shirt;
                currentPants = originalData.pants;
                currentSkinColor = originalData.skinColor;
                currentHairColor = originalData.hairColor;
                accessoryEnabled = (bool[])originalData.accessories?.Clone();
                
                ApplyAllCustomizations();
            }
            
            CloseCustomization();
            Debug.Log("🔄 Зміни персонажа скасовано!");
        }
        
        void ApplyAllCustomizations()
        {
            ApplyHairstyle();
            ApplyClothing();
            ApplySkinColor();
            ApplyHairColor();
            UpdateUI();
        }
        
        void SaveCharacterData()
        {
            PlayerPrefs.SetInt("CharacterHairstyle", currentHairstyle);
            PlayerPrefs.SetInt("CharacterShirt", currentShirt);
            PlayerPrefs.SetInt("CharacterPants", currentPants);
            PlayerPrefs.SetInt("CharacterSkinColor", currentSkinColor);
            PlayerPrefs.SetInt("CharacterHairColor", currentHairColor);
            
            // Зберігаємо аксесуари
            if (accessoryEnabled != null)
            {
                for (int i = 0; i < accessoryEnabled.Length; i++)
                {
                    PlayerPrefs.SetInt($"CharacterAccessory{i}", accessoryEnabled[i] ? 1 : 0);
                }
            }
            
            PlayerPrefs.Save();
        }
        
        void LoadCharacterData()
        {
            if (PlayerPrefs.HasKey("CharacterHairstyle"))
            {
                currentHairstyle = PlayerPrefs.GetInt("CharacterHairstyle", 0);
                currentShirt = PlayerPrefs.GetInt("CharacterShirt", 0);
                currentPants = PlayerPrefs.GetInt("CharacterPants", 0);
                currentSkinColor = PlayerPrefs.GetInt("CharacterSkinColor", 0);
                currentHairColor = PlayerPrefs.GetInt("CharacterHairColor", 0);
                
                // Завантажуємо аксесуари
                if (accessoryEnabled != null)
                {
                    for (int i = 0; i < accessoryEnabled.Length; i++)
                    {
                        accessoryEnabled[i] = PlayerPrefs.GetInt($"CharacterAccessory{i}", 0) == 1;
                    }
                }
                
                ApplyAllCustomizations();
                Debug.Log("📁 Дані персонажа завантажено!");
            }
        }
        
        void Update()
        {
            // Відкриваємо кастомізацію на клавішу C
            if (Input.GetKeyDown(KeyCode.C) && !customizationUI.gameObject.activeInHierarchy)
            {
                OpenCustomization();
            }
        }
        
        // Публічні методи для зовнішнього доступу
        public void ToggleAccessory(int index)
        {
            if (ukrainianAccessories != null && index < ukrainianAccessories.Length)
            {
                accessoryEnabled[index] = !accessoryEnabled[index];
                ukrainianAccessories[index].SetActive(accessoryEnabled[index]);
            }
        }
        
        public CharacterData GetCurrentData()
        {
            return new CharacterData
            {
                hairstyle = currentHairstyle,
                shirt = currentShirt,
                pants = currentPants,
                skinColor = currentSkinColor,
                hairColor = currentHairColor,
                accessories = (bool[])accessoryEnabled?.Clone()
            };
        }
        
        public void SetCharacterData(CharacterData data)
        {
            currentHairstyle = data.hairstyle;
            currentShirt = data.shirt;
            currentPants = data.pants;
            currentSkinColor = data.skinColor;
            currentHairColor = data.hairColor;
            accessoryEnabled = (bool[])data.accessories?.Clone();
            
            ApplyAllCustomizations();
        }
    }
} 