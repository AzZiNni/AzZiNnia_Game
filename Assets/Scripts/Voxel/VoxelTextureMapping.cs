using UnityEngine;
using System.Collections.Generic;

namespace Voxel
{
    /// <summary>
    /// Stores the mapping of a VoxelType to its corresponding texture indices in a Texture2DArray.
    /// Supports different textures for top, side, and bottom faces.
    /// </summary>
    [System.Serializable]
    public class VoxelFaceTextures
    {
        public int topFaceTextureIndex;
        public int sideFaceTextureIndex;
        public int bottomFaceTextureIndex;

        // Constructor for convenience
        public VoxelFaceTextures(int top, int side, int bottom)
        {
            topFaceTextureIndex = top;
            sideFaceTextureIndex = side;
            bottomFaceTextureIndex = bottom;
        }

        // A single index for all faces
        public VoxelFaceTextures(int allFacesIndex)
        {
            topFaceTextureIndex = allFacesIndex;
            sideFaceTextureIndex = allFacesIndex;
            bottomFaceTextureIndex = allFacesIndex;
        }
    }

    /// <summary>
    /// A ScriptableObject that holds a list of VoxelType-to-texture mappings.
    /// This acts as a central database for which textures to use for each voxel type.
    /// </summary>
    [CreateAssetMenu(fileName = "VoxelTextureMapping", menuName = "Voxel/Voxel Texture Mapping", order = 1)]
    public class VoxelTextureMapping : ScriptableObject
    {
        [System.Serializable]
        public struct VoxelTypeTextureMapping
        {
            public VoxelType voxelType;
            public VoxelFaceTextures faceTextures;
        }

        public List<VoxelTypeTextureMapping> textureMappings;

        // Runtime dictionary for fast lookups.
        private Dictionary<VoxelType, VoxelFaceTextures> _mappingDictionary;
        private bool _isInitialized = false;

        private void OnEnable()
        {
            // We'll initialize on first use.
        }

        private void Initialize()
        {
            _mappingDictionary = new Dictionary<VoxelType, VoxelFaceTextures>();
            
            if (textureMappings != null)
            {
                foreach (var mapping in textureMappings)
                {
                    if (!_mappingDictionary.ContainsKey(mapping.voxelType))
                    {
                        _mappingDictionary.Add(mapping.voxelType, mapping.faceTextures);
                    }
                }
            }
            
            _isInitialized = true;
        }

        /// <summary>
        /// Gets the texture indices for a given voxel type.
        /// </summary>
        /// <param name="type">The VoxelType to look up.</param>
        /// <returns>A VoxelFaceTextures object with the indices, or a default if not found.</returns>
        public VoxelFaceTextures GetFaceTextures(VoxelType type)
        {
            // Check if textureMappings is null or empty
            if (textureMappings == null || textureMappings.Count == 0)
            {
                Debug.LogWarning("VoxelTextureMapping: textureMappings is null or empty. Returning default texture index 0.");
                return new VoxelFaceTextures(0);
            }

            if (!_isInitialized || _mappingDictionary == null)
            {
                Initialize();
            }

            // Double-check that dictionary was created successfully
            if (_mappingDictionary == null)
            {
                Debug.LogError("VoxelTextureMapping: Failed to initialize mapping dictionary. Returning default texture index 0.");
                return new VoxelFaceTextures(0);
            }

            if (_mappingDictionary.TryGetValue(type, out VoxelFaceTextures faces))
            {
                return faces;
            }
            
            // Return a default "missing texture" mapping (e.g., index 0)
            return new VoxelFaceTextures(0);
        }
    }
} 