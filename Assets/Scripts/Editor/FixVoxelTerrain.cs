using UnityEngine;
using UnityEditor;
using Voxel;

namespace Voxel.Editor
{
    public class FixVoxelTerrain
    {
        [MenuItem("Voxel/Fix VoxelTerrain Setup")]
        public static void FixVoxelTerrainSetup()
        {
            // Find VoxelTerrain in the scene
            VoxelTerrain voxelTerrain = Object.FindObjectOfType<VoxelTerrain>();
            if (voxelTerrain == null)
            {
                Debug.LogError("No VoxelTerrain found in the scene!");
                return;
            }

            // Find the VoxelTextureMapping asset
            string[] guids = AssetDatabase.FindAssets("t:VoxelTextureMapping");
            if (guids.Length == 0)
            {
                Debug.LogError("No VoxelTextureMapping asset found! Please create one first.");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            VoxelTextureMapping textureMapping = AssetDatabase.LoadAssetAtPath<VoxelTextureMapping>(path);
            
            if (textureMapping == null)
            {
                Debug.LogError("Failed to load VoxelTextureMapping asset!");
                return;
            }

            // Assign the texture mapping
            if (voxelTerrain.textureMapping != textureMapping)
            {
                voxelTerrain.textureMapping = textureMapping;
                EditorUtility.SetDirty(voxelTerrain);
                Debug.Log($"✅ Assigned VoxelTextureMapping to {voxelTerrain.name}");
            }
            else
            {
                Debug.Log("VoxelTextureMapping is already assigned correctly.");
            }

            // Find and assign Texture2DArray if available
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2DArray");
            if (textureGuids.Length > 0)
            {
                string texturePath = AssetDatabase.GUIDToAssetPath(textureGuids[0]);
                Texture2DArray textureArray = AssetDatabase.LoadAssetAtPath<Texture2DArray>(texturePath);
                
                if (textureArray != null && voxelTerrain.textureArray != textureArray)
                {
                    voxelTerrain.textureArray = textureArray;
                    EditorUtility.SetDirty(voxelTerrain);
                    Debug.Log($"✅ Assigned Texture2DArray to {voxelTerrain.name}");
                }
            }

                            // Create material if needed
                if (voxelTerrain.terrainMaterial == null)
                {
                    Shader voxelShader = Shader.Find("Custom/SimpleVoxelShader");
                    if (voxelShader == null)
                    {
                        voxelShader = Shader.Find("Custom/VoxelArrayShader");
                    }
                    if (voxelShader == null)
                    {
                        voxelShader = Shader.Find("Custom/VoxelTerrainArrayMaterial");
                    }
                    
                    if (voxelShader != null)
                    {
                        voxelTerrain.terrainMaterial = new Material(voxelShader);
                        EditorUtility.SetDirty(voxelTerrain);
                        Debug.Log($"✅ Created new material with shader: {voxelShader.name}");
                    }
                    else
                    {
                        Debug.LogWarning("No voxel shader found. Please create the shader first.");
                    }
                }

            Debug.Log("🎉 VoxelTerrain setup completed successfully!");
        }
    }
} 