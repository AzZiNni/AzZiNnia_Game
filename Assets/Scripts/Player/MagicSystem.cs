using UnityEngine;
using System;
using Voxel;

namespace Azurin.Player
{
    public enum MagicType
    {
        Lightning,      // Блискавка - руйнує вокселі
        Healing,        // Лікування - відновлює здоров'я
        Building,       // Будівництво - створює вокселі
        Protection,     // Захист - тимчасова міцність
        Detection       // Виявлення - показує порчені місця
    }

    /// <summary>
    /// Handles the player's magic abilities, including switching spells and casting them.
    /// Interacts with CharacterStats for mana management and VoxelTerrain for world interaction.
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class MagicSystem : MonoBehaviour
    {
        public MagicType CurrentMagic { get; private set; } = MagicType.Lightning;

        public event Action<MagicType> OnMagicTypeChanged;
        public event Action<MagicType> OnMagicUsed;

        private CharacterStats stats;
        private VoxelTerrain voxelTerrain; // Optional, for magic that affects the world

        private void Awake()
        {
            stats = GetComponent<CharacterStats>();
        }

        private void Start()
        {
            // It's better to find it once than on every cast
            voxelTerrain = FindFirstObjectByType<VoxelTerrain>();
        }
        
        public void SetMagicType(MagicType newType)
        {
            if (CurrentMagic == newType) return;
            CurrentMagic = newType;
            OnMagicTypeChanged?.Invoke(CurrentMagic);
            Debug.Log($"Magic type switched to: {newType}");
        }

        public void UseCurrentMagic(Camera playerCamera, float reachDistance)
        {
            float manaCost = GetManaCost(CurrentMagic);
            if (!stats.TryUseMana(manaCost))
            {
                Debug.Log($"Not enough mana for {CurrentMagic}. Required: {manaCost}, Have: {stats.CurrentMana}");
                return;
            }

            OnMagicUsed?.Invoke(CurrentMagic);

            switch (CurrentMagic)
            {
                case MagicType.Lightning:
                    CastLightning(playerCamera, reachDistance);
                    break;
                case MagicType.Healing:
                    CastHealing();
                    break;
                case MagicType.Building:
                    CastBuilding(playerCamera, reachDistance);
                    break;
                case MagicType.Protection:
                    CastProtection();
                    break;
                case MagicType.Detection:
                    CastDetection();
                    break;
            }
        }

        private float GetManaCost(MagicType magic) => magic switch
        {
            MagicType.Lightning => 20f,
            MagicType.Healing => 15f,
            MagicType.Building => 25f,
            MagicType.Protection => 30f,
            MagicType.Detection => 10f,
            _ => 10f,
        };
        
        // --- Spell Implementations ---

        private void CastLightning(Camera playerCamera, float reachDistance)
        {
            if (voxelTerrain == null || playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, reachDistance * 2))
            {
                voxelTerrain.ModifyTerrain(hit.point, 5f, -1f); // Larger radius for lightning
                Debug.Log("Casted Lightning!");
            }
        }

        private void CastHealing()
        {
            stats.Heal(25f);
            Debug.Log("Casted Healing!");
        }

        private void CastBuilding(Camera playerCamera, float reachDistance)
        {
             if (voxelTerrain == null || playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, reachDistance * 2))
            {
                voxelTerrain.ModifyTerrain(hit.point, 2f, 1f);
                Debug.Log("Casted Building!");
            }
        }

        private void CastProtection()
        {
            // TODO: Implement temporary protection
            Debug.Log("Casted Protection!");
        }

        private void CastDetection()
        {
            // TODO: Implement detection of hidden objects
            Debug.Log("Casted Detection!");
        }
    }
} 