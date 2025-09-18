using UnityEngine;
using UnityEditor;
using Voxel;

public class VoxelMaterialFixer : EditorWindow
{
    [MenuItem("Tools/Fix Voxel Materials")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(VoxelMaterialFixer));
    }

    void OnGUI()
    {
        GUILayout.Label("Voxel Material Fixer", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Assign Fixed Grass Material"))
        {
            AssignGrassMaterial();
        }
        
        if (GUILayout.Button("Fix All Cartoon Materials"))
        {
            FixAllCartoonMaterials();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("This will assign the fixed grass material\nto VoxelTerrain for proper rendering", EditorStyles.helpBox);
    }

    void AssignGrassMaterial()
    {
        // Знаходимо VoxelTerrain з новою системою
        VoxelTerrain voxelTerrain = FindFirstObjectByType<VoxelTerrain>();
        if (voxelTerrain == null)
        {
            Debug.LogError("VoxelTerrain not found in scene!");
            return;
        }

        // Завантажуємо робочий матеріал трави з Materials папки
        Material grassMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Materials/GrassMat.mat"
        );
        
        if (grassMaterial == null)
        {
            Debug.LogError("GrassMat material not found!");
            return;
        }

        // Призначаємо матеріал до VoxelTerrain
        voxelTerrain.terrainMaterial = grassMaterial;
        
        EditorUtility.SetDirty(voxelTerrain);
        Debug.Log("Working GrassMat material assigned to VoxelTerrain successfully!");
    }

    void FixAllCartoonMaterials()
    {
        // Знаходимо всі матеріали в Cartoon_Texture_Pack
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Cartoon_Texture_Pack" });
        
        int fixedCount = 0;
        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (material != null && material.shader.name == "Universal Render Pipeline/Lit")
            {
                // Перевіряємо чи потрібно виправити матеріал
                if (!material.HasProperty("_BaseColor") || !material.HasProperty("_BumpScale"))
                {
                    FixMaterial(material);
                    EditorUtility.SetDirty(material);
                    fixedCount++;
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"Fixed {fixedCount} materials in Cartoon_Texture_Pack");
    }

    void FixMaterial(Material material)
    {
        // Додаємо базові параметри URP якщо їх немає
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
        if (material.HasProperty("_BumpScale"))
            material.SetFloat("_BumpScale", 1.0f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.5f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0.0f);
        if (material.HasProperty("_OcclusionStrength"))
            material.SetFloat("_OcclusionStrength", 1.0f);
    }
} 