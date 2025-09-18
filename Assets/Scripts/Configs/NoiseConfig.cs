using UnityEngine;

namespace Azurin.Core.Configs
{
    /// <summary>
    /// Configuration for terrain noise and height settings.
    /// </summary>
    [CreateAssetMenu(menuName = "Azurin/Configs/NoiseConfig", fileName = "NoiseConfig")]
    public class NoiseConfig : ScriptableObject
    {
        public float noiseScale = 0.05f;
        public float heightScale = 20f;
        public float groundLevel = 0f;
        public int seed = 12345;
    }
}


