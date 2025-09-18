using UnityEngine;
using UnityEditor;
using System.IO;

public class MaterialURPConverter : EditorWindow
{
    [MenuItem("Tools/Convert Materials to URP")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(MaterialURPConverter));
    }

    void OnGUI()
    {
        GUILayout.Label("Material URP Converter", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Convert All Materials in Cartoon_Texture_Pack"))
        {
            ConvertMaterials();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("This will convert all materials from Built-in\nStandard shader to URP/Lit shader", EditorStyles.helpBox);
    }

    void ConvertMaterials()
    {
        string[] materialPaths = Directory.GetFiles("Assets/Cartoon_Texture_Pack", "*.mat", SearchOption.AllDirectories);
        
        int convertedCount = 0;
        
        foreach (string path in materialPaths)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (material != null && material.shader.name == "Standard")
            {
                // URP Lit shader GUID
                Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
                
                if (urpLitShader != null)
                {
                    material.shader = urpLitShader;
                    
                    // Remove URP incompatible properties
                    if (material.HasProperty("_ParallaxMap"))
                    {
                        material.SetTexture("_ParallaxMap", null);
                    }
                    
                    EditorUtility.SetDirty(material);
                    convertedCount++;
                    
                    Debug.Log($"Converted material: {material.name}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Conversion completed! Converted {convertedCount} materials to URP.");
        EditorUtility.DisplayDialog("Conversion Complete", 
            $"Successfully converted {convertedCount} materials to URP/Lit shader.", "OK");
    }
} 