using UnityEngine;
using System;

namespace Azurin.Player
{
    /// <summary>
    /// Manages all character statistics like health, stamina, and mana.
    /// Provides methods to modify these stats and events to notify other systems of changes.
    /// </summary>
    public class CharacterStats : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        public float CurrentHealth { get; private set; }
        
        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        public float CurrentStamina { get; private set; }

        [Header("Mana")]
        [SerializeField] private float maxMana = 100f;
        public float CurrentMana { get; private set; }

        // Events for UI and other systems
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnStaminaChanged;
        public event Action<float, float> OnManaChanged;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            CurrentStamina = maxStamina;
            CurrentMana = maxMana;
        }

        // --- Health Management ---
        public void TakeDamage(float amount)
        {
            if (amount <= 0) return;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0)
            {
                HandleDeath();
            }
        }

        public void Heal(float amount)
        {
            if (amount <= 0) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        // --- Stamina Management ---
        public bool TryUseStamina(float amount)
        {
            if (CurrentStamina < amount)
            {
                return false;
            }
            
            CurrentStamina = Mathf.Max(0, CurrentStamina - amount);
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
            return true;
        }

        public void RestoreStamina(float amount)
        {
            if (amount <= 0) return;
            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + amount);
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        }

        // --- Mana Management ---
        public bool TryUseMana(float amount)
        {
            if (CurrentMana < amount)
            {
                return false;
            }

            CurrentMana = Mathf.Max(0, CurrentMana - amount);
            OnManaChanged?.Invoke(CurrentMana, maxMana);
            return true;
        }

        public void RestoreMana(float amount)
        {
            if (amount <= 0) return;
            CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
            OnManaChanged?.Invoke(CurrentMana, maxMana);
        }

        private void HandleDeath()
        {
            // TODO: Implement death logic (e.g., play animation, show game over screen, respawn)
            Debug.Log("Player has died.");
        }
        
        // --- Public Getters for UI ---
        public float GetHealthPercent() => CurrentHealth / maxHealth;
        public float GetStaminaPercent() => CurrentStamina / maxStamina;
        public float GetManaPercent() => CurrentMana / maxMana;
    }
} 