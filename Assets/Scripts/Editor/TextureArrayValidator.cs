using UnityEngine;
using UnityEditor;

public class TextureArrayValidator : EditorWindow
{
    private Texture2DArray textureArray;

    [MenuItem("Voxel/Validate Texture2DArray")]
    public static void ShowWindow()
    {
        GetWindow<TextureArrayValidator>("Texture2DArray Validator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Validate Texture2DArray (Base Color Only)", EditorStyles.boldLabel);
        textureArray = (Texture2DArray)EditorGUILayout.ObjectField("Texture Array", textureArray, typeof(Texture2DArray), false);

        if (textureArray == null)
        {
            EditorGUILayout.HelpBox("Assign a Texture2DArray to validate.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Size", $"{textureArray.width} x {textureArray.height}");
        EditorGUILayout.LabelField("Depth", textureArray.depth.ToString());
        EditorGUILayout.LabelField("Format", textureArray.format.ToString());
        EditorGUILayout.LabelField("Mipmaps", textureArray.mipmapCount > 1 ? "Yes" : "No");

        if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
        {
            Validate(textureArray);
        }
    }

    private void Validate(Texture2DArray arr)
    {
        bool ok = true;
        string report = "";

        // Allowed formats for albedo-like
        var fmt = arr.format;
        switch (fmt)
        {
            case TextureFormat.RGBA32:
            case TextureFormat.ARGB32:
            case TextureFormat.BGRA32:
            case TextureFormat.RGB24:
            case TextureFormat.RGBA4444:
                break;
            default:
                ok = false;
                report += $"- Unsupported format for BaseColor array: {fmt}. Prefer RGBA32.\n";
                break;
        }

        if (arr.mipmapCount > 1)
        {
            report += "- Mipmaps present. This is OK, but ensure all source textures had same mip settings.\n";
        }

        // We cannot inspect per-layer content cheaply; warn user
        report += "- Ensure ALL layers are Albedo/BaseColor only. Do NOT pack normals/metallic/height here.\n";
        report += "- Ensure ALL layers have same resolution and color space.\n";

        if (ok)
            EditorUtility.DisplayDialog("Validation", "Texture2DArray looks OK for BaseColor usage.\n\n" + report, "OK");
        else
            EditorUtility.DisplayDialog("Validation", "Issues detected:\n\n" + report, "OK");
    }
}
