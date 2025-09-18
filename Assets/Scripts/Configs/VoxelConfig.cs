using UnityEngine;

namespace Azurin.Core.Configs
{
    /// <summary>
    /// Configuration for voxel terrain parameters.
    /// </summary>
    [CreateAssetMenu(menuName = "Azurin/Configs/VoxelConfig", fileName = "VoxelConfig")]
    public class VoxelConfig : ScriptableObject
    {
        public int chunkSize = 16;
        public float voxelSize = 1f;
        public int viewDistance = 4;
    }
}


