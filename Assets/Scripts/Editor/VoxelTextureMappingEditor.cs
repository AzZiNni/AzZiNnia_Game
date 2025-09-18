using UnityEngine;
using UnityEditor;
using Voxel;
using System.Linq;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(VoxelTextureMapping))]
public class VoxelTextureMappingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        VoxelTextureMapping mapping = (VoxelTextureMapping)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🎨 Texture Mapping Tools", EditorStyles.boldLabel);
        
        // Показуємо статистику
        ShowMappingStatistics(mapping);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Populate All Voxel Types", GUILayout.Height(30)))
        {
            PopulateAllVoxelTypes(mapping);
        }
        
        if (GUILayout.Button("Assign Common Texture Indices", GUILayout.Height(30)))
        {
            AssignCommonTextureIndices(mapping);
        }
        
        if (GUILayout.Button("Validate Texture Indices", GUILayout.Height(30)))
        {
            ValidateTextureIndices(mapping);
        }
        
        if (GUILayout.Button("Auto-Assign from Texture Array", GUILayout.Height(30)))
        {
            AutoAssignFromTextureArray(mapping);
        }
    }
    
    private void ShowMappingStatistics(VoxelTextureMapping mapping)
    {
        if (mapping.textureMappings == null) return;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Total Mappings: {mapping.textureMappings.Count}");
        
        // Підраховуємо унікальні індекси текстур
        HashSet<int> uniqueIndices = new HashSet<int>();
        int maxIndex = 0;
        
        foreach (var m in mapping.textureMappings)
        {
            uniqueIndices.Add(m.faceTextures.topFaceTextureIndex);
            uniqueIndices.Add(m.faceTextures.sideFaceTextureIndex);
            uniqueIndices.Add(m.faceTextures.bottomFaceTextureIndex);
            
            maxIndex = Mathf.Max(maxIndex, m.faceTextures.topFaceTextureIndex);
            maxIndex = Mathf.Max(maxIndex, m.faceTextures.sideFaceTextureIndex);
            maxIndex = Mathf.Max(maxIndex, m.faceTextures.bottomFaceTextureIndex);
        }
        
        EditorGUILayout.LabelField($"Unique Texture Indices: {uniqueIndices.Count}");
        EditorGUILayout.LabelField($"Max Texture Index: {maxIndex}");
        
        // Перевіряємо наявність Texture2DArray
        var terrain = GameObject.FindObjectOfType<Voxel.VoxelTerrain>();
        if (terrain != null && terrain.textureArray != null)
        {
            EditorGUILayout.LabelField($"Texture Array Size: {terrain.textureArray.depth}");
            
            if (maxIndex >= terrain.textureArray.depth)
            {
                EditorGUILayout.HelpBox($"⚠️ Index {maxIndex} exceeds array size!", MessageType.Warning);
            }
        }
        
        EditorGUILayout.EndVertical();
    }

    private void PopulateAllVoxelTypes(VoxelTextureMapping mapping)
    {
        // Get all VoxelType enum values
        var allVoxelTypes = Enum.GetValues(typeof(VoxelType)).Cast<VoxelType>();

        // Get existing types in the mapping to avoid duplicates
        var existingTypes = new HashSet<VoxelType>(mapping.textureMappings.Select(m => m.voxelType));

        bool added = false;
        foreach (var voxelType in allVoxelTypes)
        {
            if (voxelType == VoxelType.Air) continue; // Skip Air

            if (!existingTypes.Contains(voxelType))
            {
                mapping.textureMappings.Add(new VoxelTextureMapping.VoxelTypeTextureMapping
                {
                    voxelType = voxelType,
                    faceTextures = new VoxelFaceTextures(0) // Default to index 0
                });
                added = true;
            }
        }

        if (added)
        {
            // Sort the list by enum value to keep it organized
            mapping.textureMappings = mapping.textureMappings.OrderBy(m => (int)m.voxelType).ToList();
            EditorUtility.SetDirty(mapping);
            AssetDatabase.SaveAssets();
            Debug.Log("Populated VoxelTextureMapping with missing voxel types.");
        }
        else
        {
            Debug.Log("All voxel types are already present in the mapping.");
        }
    }
    
    private void AssignCommonTextureIndices(VoxelTextureMapping mapping)
    {
        // Common texture indices for typical texture arrays
        var textureAssignments = new Dictionary<VoxelType, int>
        {
            { VoxelType.Dirt, 1 },
            { VoxelType.Grass, 2 },
            { VoxelType.Stone, 3 },
            { VoxelType.Cobblestone, 4 },
            { VoxelType.Bedrock, 5 },
            { VoxelType.Sand, 6 },
            { VoxelType.RedSand, 7 },
            { VoxelType.Gravel, 8 },
            { VoxelType.Clay, 9 },
            { VoxelType.CoalOre, 10 },
            { VoxelType.IronOre, 11 },
            { VoxelType.GoldOre, 12 },
            { VoxelType.DiamondOre, 13 },
            { VoxelType.EmeraldOre, 14 },
            { VoxelType.RedstoneOre, 15 },
            { VoxelType.LapisOre, 16 },
            { VoxelType.CopperOre, 17 },
            { VoxelType.OakLog, 18 },
            { VoxelType.BirchLog, 19 },
            { VoxelType.SpruceLog, 20 },
            { VoxelType.JungleLog, 21 },
            { VoxelType.AcaciaLog, 22 },
            { VoxelType.DarkOakLog, 23 },
            { VoxelType.OakLeaves, 24 },
            { VoxelType.BirchLeaves, 25 },
            { VoxelType.SpruceLeaves, 26 },
            { VoxelType.JungleLeaves, 27 },
            { VoxelType.AcaciaLeaves, 28 },
            { VoxelType.DarkOakLeaves, 29 },
            { VoxelType.Water, 30 },
            { VoxelType.Lava, 31 },
            { VoxelType.Oil, 32 },
            { VoxelType.Snow, 33 },
            { VoxelType.Ash, 34 },
            { VoxelType.Dust, 35 },
            { VoxelType.Glass, 36 },
            { VoxelType.Ice, 37 },
            { VoxelType.PackedIce, 38 },
            { VoxelType.Obsidian, 39 },
            { VoxelType.Brick, 40 },
            { VoxelType.StoneBrick, 41 },
            { VoxelType.Concrete, 42 },
            { VoxelType.Terracotta, 43 },
            { VoxelType.TallGrass, 44 },
            { VoxelType.Flower, 45 },
            { VoxelType.Mushroom, 46 },
            { VoxelType.Wool, 47 },
            { VoxelType.Sponge, 48 },
            { VoxelType.TNT, 49 }
        };

        bool changed = false;
        foreach (var mapping_item in mapping.textureMappings)
        {
            if (textureAssignments.TryGetValue(mapping_item.voxelType, out int textureIndex))
            {
                mapping_item.faceTextures.topFaceTextureIndex = textureIndex;
                mapping_item.faceTextures.sideFaceTextureIndex = textureIndex;
                mapping_item.faceTextures.bottomFaceTextureIndex = textureIndex;
                changed = true;
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(mapping);
            AssetDatabase.SaveAssets();
            Debug.Log("Assigned common texture indices to voxel types.");
        }
        else
        {
            Debug.Log("No changes made - texture indices already assigned or no matching voxel types found.");
        }
    }
    
    private void ValidateTextureIndices(VoxelTextureMapping mapping)
    {
        var terrain = GameObject.FindObjectOfType<Voxel.VoxelTerrain>();
        if (terrain == null || terrain.textureArray == null)
        {
            Debug.LogError("❌ VoxelTerrain or Texture2DArray not found!");
            return;
        }
        
        int arraySize = terrain.textureArray.depth;
        int fixedCount = 0;
        
        foreach (var m in mapping.textureMappings)
        {
            bool changed = false;
            
            if (m.faceTextures.topFaceTextureIndex >= arraySize)
            {
                m.faceTextures.topFaceTextureIndex = Mathf.Clamp(m.faceTextures.topFaceTextureIndex, 0, arraySize - 1);
                changed = true;
            }
            
            if (m.faceTextures.sideFaceTextureIndex >= arraySize)
            {
                m.faceTextures.sideFaceTextureIndex = Mathf.Clamp(m.faceTextures.sideFaceTextureIndex, 0, arraySize - 1);
                changed = true;
            }
            
            if (m.faceTextures.bottomFaceTextureIndex >= arraySize)
            {
                m.faceTextures.bottomFaceTextureIndex = Mathf.Clamp(m.faceTextures.bottomFaceTextureIndex, 0, arraySize - 1);
                changed = true;
            }
            
            if (changed) fixedCount++;
        }
        
        if (fixedCount > 0)
        {
            EditorUtility.SetDirty(mapping);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Fixed {fixedCount} invalid texture indices");
        }
        else
        {
            Debug.Log("✅ All texture indices are valid");
        }
    }
    
    private void AutoAssignFromTextureArray(VoxelTextureMapping mapping)
    {
        var terrain = GameObject.FindObjectOfType<Voxel.VoxelTerrain>();
        if (terrain == null || terrain.textureArray == null)
        {
            Debug.LogError("❌ VoxelTerrain or Texture2DArray not found!");
            return;
        }
        
        int arraySize = terrain.textureArray.depth;
        int index = 0;
        
        // Автоматично призначаємо індекси по порядку
        foreach (var m in mapping.textureMappings)
        {
            if (index < arraySize)
            {
                m.faceTextures.topFaceTextureIndex = index;
                m.faceTextures.sideFaceTextureIndex = index;
                m.faceTextures.bottomFaceTextureIndex = index;
                index++;
            }
        }
        
        EditorUtility.SetDirty(mapping);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Auto-assigned {index} texture indices from array");
    }
} 