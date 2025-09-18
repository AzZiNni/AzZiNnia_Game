using UnityEngine;
using UnityEditor;
using Voxel;

namespace Voxel.Editor
{
    public class VoxelTerrainSetup
    {
        [MenuItem("Voxel/Setup VoxelTerrain Components")]
        public static void SetupVoxelTerrainComponents()
        {
            // Find all VoxelTerrain components in the scene
            VoxelTerrain[] voxelTerrains = Object.FindObjectsOfType<VoxelTerrain>();
            
            if (voxelTerrains.Length == 0)
            {
                Debug.LogWarning("No VoxelTerrain components found in the scene!");
                return;
            }

            // Find the VoxelTextureMapping asset
            string[] guids = AssetDatabase.FindAssets("t:VoxelTextureMapping");
            VoxelTextureMapping textureMapping = null;
            
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                textureMapping = AssetDatabase.LoadAssetAtPath<VoxelTextureMapping>(path);
            }

            if (textureMapping == null)
            {
                Debug.LogError("VoxelTextureMapping asset not found! Please create one first.");
                return;
            }

            // Find the Texture2DArray asset
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2DArray");
            Texture2DArray textureArray = null;
            
            if (textureGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(textureGuids[0]);
                textureArray = AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
            }

            int setupCount = 0;
            
            foreach (VoxelTerrain terrain in voxelTerrains)
            {
                bool modified = false;
                
                // Set the texture mapping
                if (terrain.textureMapping == null)
                {
                    terrain.textureMapping = textureMapping;
                    modified = true;
                    Debug.Log($"Assigned VoxelTextureMapping to {terrain.name}");
                }

                // Set the texture array
                if (terrain.textureArray == null && textureArray != null)
                {
                    terrain.textureArray = textureArray;
                    modified = true;
                    Debug.Log($"Assigned Texture2DArray to {terrain.name}");
                }

                // Create material if needed
                if (terrain.terrainMaterial == null)
                {
                    // Try to find the voxel shader
                    Shader voxelShader = Shader.Find("Custom/VoxelArrayShader");
                    if (voxelShader == null)
                    {
                        Debug.LogWarning($"Voxel shader not found for {terrain.name}. Please create the shader first.");
                    }
                    else
                    {
                        terrain.terrainMaterial = new Material(voxelShader);
                        modified = true;
                        Debug.Log($"Created new material with voxel shader for {terrain.name}");
                    }
                }

                if (modified)
                {
                    setupCount++;
                    EditorUtility.SetDirty(terrain);
                }
            }

            if (setupCount > 0)
            {
                Debug.Log($"✅ Successfully set up {setupCount} VoxelTerrain component(s)");
            }
            else
            {
                Debug.Log("All VoxelTerrain components are already properly configured.");
            }
        }

        [MenuItem("Voxel/Create VoxelTextureMapping Asset")]
        public static void CreateVoxelTextureMappingAsset()
        {
            // Check if asset already exists
            string[] guids = AssetDatabase.FindAssets("t:VoxelTextureMapping");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Debug.Log($"VoxelTextureMapping asset already exists at: {path}");
                return;
            }

            // Create new asset
            VoxelTextureMapping mapping = ScriptableObject.CreateInstance<VoxelTextureMapping>();
            AssetDatabase.CreateAsset(mapping, "Assets/Settings/VoxelTextureMapping.asset");
            AssetDatabase.SaveAssets();
            
            Debug.Log("✅ Created new VoxelTextureMapping asset at Assets/Settings/VoxelTextureMapping.asset");
            
            // Select the new asset
            Selection.activeObject = mapping;
        }
    }
} 