using UnityEngine;

namespace Azurin.Core
{
    /// <summary>
    /// Tracks gameplay statistics such as time, distance and blocks counters.
    /// </summary>
    public class GameplayStatsService : MonoBehaviour
    {
        public float GameTimeSeconds { get; private set; }
        public int BlocksDestroyed { get; private set; }
        public int BlocksPlaced { get; private set; }
        public float DistanceTraveled { get; private set; }

        private Vector3? lastTrackedPosition;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        public void Tick(float deltaTime, Vector3 playerPosition)
        {
            GameTimeSeconds += deltaTime;
            if (lastTrackedPosition.HasValue)
            {
                DistanceTraveled += Vector3.Distance(lastTrackedPosition.Value, playerPosition);
            }
            lastTrackedPosition = playerPosition;
        }

        public void OnBlockDestroyed() => BlocksDestroyed++;
        public void OnBlockPlaced() => BlocksPlaced++;

        public void ResetAll()
        {
            GameTimeSeconds = 0f;
            BlocksDestroyed = 0;
            BlocksPlaced = 0;
            DistanceTraveled = 0f;
            lastTrackedPosition = null;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GameplayStatsService>();
        }
    }
}


