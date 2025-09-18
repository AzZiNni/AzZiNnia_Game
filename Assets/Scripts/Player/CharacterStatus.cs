using UnityEngine;

namespace Player
{
    /// <summary>
    /// Керує основними показниками стану персонажа: здоров'я, витривалість, голод, спрага.
    /// </summary>
    public class CharacterStatus : MonoBehaviour
    {
        [Header("Основні показники")]
        public float maxHealth = 100f;
        public float maxStamina = 100f;
        public float maxHunger = 100f;
        public float maxThirst = 100f;

        [Header("Поточні значення")]
        [SerializeField] private float currentHealth;
        [SerializeField] private float currentStamina;
        [SerializeField] private float currentHunger;
        [SerializeField] private float currentThirst;

        [Header("Швидкість змін")]
        [SerializeField] private float hungerRate = 0.1f;  // одиниць голоду в секунду
        [SerializeField] private float thirstRate = 0.2f;  // одиниць спраги в секунду
        [SerializeField] private float staminaRegenRate = 5.0f; // відновлення витривалості в секунду
        [SerializeField] private float staminaDrainRate = 10.0f; // витрата витривалості при бігу

        // Події для UI та інших систем
        public System.Action OnStatusChanged;

        public float CurrentHealth => currentHealth;
        public float CurrentStamina => currentStamina;
        public float CurrentHunger => currentHunger;
        public float CurrentThirst => currentThirst;
        
        public float MaxHealth => maxHealth;
        public float MaxStamina => maxStamina;
        public float MaxHunger => maxHunger;
        public float MaxThirst => maxThirst;

        private bool isRunning = false;

        void Start()
        {
            // Ініціалізація показників
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentHunger = maxHunger;
            currentThirst = maxThirst;
        }

        void Update()
        {
            // --- Симуляція потреб ---
            currentHunger -= hungerRate * Time.deltaTime;
            currentThirst -= thirstRate * Time.deltaTime;

            currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
            currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);
            
            // --- Симуляція витривалості ---
            if (isRunning)
            {
                currentStamina -= staminaDrainRate * Time.deltaTime;
            }
            else
            {
                // Відновлюємо, тільки якщо не голодні і не хочемо пити
                if (currentHunger > 20 && currentThirst > 20)
                {
                    currentStamina += staminaRegenRate * Time.deltaTime;
                }
            }
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            
            // TODO: Додати вплив голоду та спраги на здоров'я
            
            // Викликаємо подію, щоб оновити UI (можна оптимізувати, щоб не викликати кожен кадр)
            if (Time.frameCount % 10 == 0) // Оновлюємо UI не кожен кадр
            {
                OnStatusChanged?.Invoke();
            }
        }

        public void SetRunning(bool running)
        {
            isRunning = running;
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            OnStatusChanged?.Invoke();
            // TODO: Додати логіку смерті, якщо здоров'я <= 0
        }

        public void Heal(float amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            OnStatusChanged?.Invoke();
        }

        public void ChangeHunger(float amount)
        {
            currentHunger += amount;
            currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
            OnStatusChanged?.Invoke();
        }

        public void ChangeThirst(float amount)
        {
            currentThirst += amount;
            currentThirst = Mathf.Clamp(currentThirst, 0, maxThirst);
            OnStatusChanged?.Invoke();
        }
    }
} 