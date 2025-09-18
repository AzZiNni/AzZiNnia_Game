using UnityEngine;
using Azurin.Core.Configs;

namespace Azurin.Core
{
    /// <summary>
    /// Minimal bootstrap component to register core services and route early events.
    /// Place on an always-present GameObject.
    /// </summary>
    public class ProjectBootstrap : MonoBehaviour
    {
        [Header("Configs")]
        public VoxelConfig voxelConfig;
        public NoiseConfig noiseConfig;
        public PerformanceConfig performanceConfig;
        public InputConfig inputConfig;

        private void Awake()
        {
            // Register configs for easy access
            if (voxelConfig != null) ServiceLocator.Register(voxelConfig);
            if (noiseConfig != null) ServiceLocator.Register(noiseConfig);
            if (performanceConfig != null) ServiceLocator.Register(performanceConfig);
            if (inputConfig != null) ServiceLocator.Register(inputConfig);
        }
    }
}


