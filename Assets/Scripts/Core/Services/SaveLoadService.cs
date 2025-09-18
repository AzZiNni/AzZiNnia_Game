using UnityEngine;

namespace Azurin.Core
{
    /// <summary>
    /// Basic save/load service. Initially mirrors PlayerPrefs logic to keep compatibility.
    /// Later can be swapped to WorldSaveSystem for world data and MessagePack for meta.
    /// </summary>
    public class SaveLoadService : MonoBehaviour
    {
        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        public void SavePlayerState(float gameTime, int blocksDestroyed, int blocksPlaced, float distanceTraveled, Vector3 position, float healthPercent, float staminaPercent)
        {
            PlayerPrefs.SetFloat("GameTime", gameTime);
            PlayerPrefs.SetInt("BlocksDestroyed", blocksDestroyed);
            PlayerPrefs.SetInt("BlocksPlaced", blocksPlaced);
            PlayerPrefs.SetFloat("DistanceTraveled", distanceTraveled);
            PlayerPrefs.SetFloat("PlayerPosX", position.x);
            PlayerPrefs.SetFloat("PlayerPosY", position.y);
            PlayerPrefs.SetFloat("PlayerPosZ", position.z);
            PlayerPrefs.SetFloat("PlayerHealth", healthPercent);
            PlayerPrefs.SetFloat("PlayerStamina", staminaPercent);
            PlayerPrefs.Save();
        }

        public bool TryLoadPlayerState(out float gameTime, out int blocksDestroyed, out int blocksPlaced, out float distanceTraveled, out Vector3 position)
        {
            if (!PlayerPrefs.HasKey("GameTime"))
            {
                gameTime = 0f;
                blocksDestroyed = blocksPlaced = 0;
                distanceTraveled = 0f;
                position = Vector3.zero;
                return false;
            }

            gameTime = PlayerPrefs.GetFloat("GameTime");
            blocksDestroyed = PlayerPrefs.GetInt("BlocksDestroyed");
            blocksPlaced = PlayerPrefs.GetInt("BlocksPlaced");
            distanceTraveled = PlayerPrefs.GetFloat("DistanceTraveled");
            position = new Vector3(
                PlayerPrefs.GetFloat("PlayerPosX"),
                PlayerPrefs.GetFloat("PlayerPosY"),
                PlayerPrefs.GetFloat("PlayerPosZ")
            );
            return true;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<SaveLoadService>();
        }
    }
}


