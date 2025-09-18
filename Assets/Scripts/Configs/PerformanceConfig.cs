using UnityEngine;

namespace Azurin.Core.Configs
{
    /// <summary>
    /// Configuration for performance knobs.
    /// </summary>
    [CreateAssetMenu(menuName = "Azurin/Configs/PerformanceConfig", fileName = "PerformanceConfig")]
    public class PerformanceConfig : ScriptableObject
    {
        public bool useJobs = true;
        public bool useLOD = true;
        [Range(0.5f, 33.3f)] public float targetFrameTimeMs = 16.7f; // 60 FPS
        public int maxConcurrentJobs = 4;
    }
}


