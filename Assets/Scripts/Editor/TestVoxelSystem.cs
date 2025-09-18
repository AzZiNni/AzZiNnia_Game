using UnityEngine;
using UnityEditor;
using Voxel;

namespace Voxel.Editor
{
    public class TestVoxelSystem
    {
        [MenuItem("Voxel/Test Voxel System")]
        public static void RunVoxelSystemTest()
        {
            Debug.Log("🧪 Testing Voxel System...");
            
            // Find VoxelTerrain
            VoxelTerrain voxelTerrain = Object.FindObjectOfType<VoxelTerrain>();
            if (voxelTerrain == null)
            {
                Debug.LogError("❌ No VoxelTerrain found in scene!");
                return;
            }
            
            Debug.Log($"✅ Found VoxelTerrain: {voxelTerrain.name}");
            
            // Check Texture Mapping
            if (voxelTerrain.textureMapping == null)
            {
                Debug.LogError("❌ VoxelTextureMapping is not assigned!");
            }
            else
            {
                Debug.Log($"✅ VoxelTextureMapping assigned: {voxelTerrain.textureMapping.name}");
                Debug.Log($"   - Has {voxelTerrain.textureMapping.textureMappings?.Count ?? 0} mappings");
            }
            
            // Check Texture Array
            if (voxelTerrain.textureArray == null)
            {
                Debug.LogError("❌ Texture2DArray is not assigned!");
            }
            else
            {
                Debug.Log($"✅ Texture2DArray assigned: {voxelTerrain.textureArray.name}");
                Debug.Log($"   - Size: {voxelTerrain.textureArray.width}x{voxelTerrain.textureArray.height}");
                Debug.Log($"   - Depth: {voxelTerrain.textureArray.depth}");
            }
            
            // Check Material
            if (voxelTerrain.terrainMaterial == null)
            {
                Debug.LogError("❌ Terrain material is not assigned!");
            }
            else
            {
                Debug.Log($"✅ Terrain material assigned: {voxelTerrain.terrainMaterial.name}");
                Debug.Log($"   - Shader: {voxelTerrain.terrainMaterial.shader?.name ?? "None"}");
                
                // Check if texture is set in material
                Texture mainTex = voxelTerrain.terrainMaterial.GetTexture("_MainTex");
                if (mainTex == null)
                {
                    Debug.LogError("❌ _MainTex is not set in material!");
                }
                else
                {
                    Debug.Log($"✅ _MainTex set in material: {mainTex.name}");
                }
            }
            
            // Check Chunks
            int chunkCount = voxelTerrain.Chunks?.Count ?? 0;
            Debug.Log($"✅ Active chunks: {chunkCount}");
            
            // Test texture mapping
            if (voxelTerrain.textureMapping != null)
            {
                var testTextures = voxelTerrain.textureMapping.GetFaceTextures(VoxelType.Dirt);
                Debug.Log($"✅ Test texture mapping for Dirt: Top={testTextures.topFaceTextureIndex}, Side={testTextures.sideFaceTextureIndex}, Bottom={testTextures.bottomFaceTextureIndex}");
            }
            
            Debug.Log("🎉 Voxel System Test Complete!");
        }
    }
} 