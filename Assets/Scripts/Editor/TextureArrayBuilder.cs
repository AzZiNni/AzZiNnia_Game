using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class TextureArrayBuilder : EditorWindow
{
    [SerializeField]
    private List<Object> sourceAssets = new List<Object>(); // Can hold both folders and textures
    [SerializeField]
    private string savePath = "Assets/Textures/VoxelTextureArray.asset";
    private Vector2 scrollPosition;

    // --- Serialized Properties ---
    private SerializedObject serializedObject;
    private SerializedProperty sourceAssetsProperty;
    private SerializedProperty savePathProperty;

    [MenuItem("Voxel/Create Texture Array")]
    public static void ShowWindow()
    {
        GetWindow<TextureArrayBuilder>("Texture Array Builder");
    }

    private void OnEnable()
    {
        // Initialize SerializedObject and SerializedProperty
        serializedObject = new SerializedObject(this);
        sourceAssetsProperty = serializedObject.FindProperty("sourceAssets");
        savePathProperty = serializedObject.FindProperty("savePath");
    }

    private void OnGUI()
    {
        serializedObject.Update();

        GUILayout.Label("Texture Array Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Drag and drop folders or individual textures below. The script will find all textures inside the folders and process them one by one to save memory.", MessageType.Info);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(savePathProperty, new GUIContent("Save Path"));

        EditorGUILayout.Space();

        // --- Textures List ---
        GUILayout.Label("Source Folders & Textures", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        
        EditorGUILayout.PropertyField(sourceAssetsProperty, true);

        EditorGUILayout.EndScrollView();
        // --- End Textures List ---

        EditorGUILayout.Space();

        if (GUILayout.Button("Build Texture Array", GUILayout.Height(40)))
        {
            BuildTextureArray();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void BuildTextureArray()
    {
        List<string> allTexturePaths = GetTexturePathsFromSources();

        if (allTexturePaths.Count == 0)
        {
            Debug.LogError("No textures found in the specified sources.");
            return;
        }

        // --- Step 1: Find the most common resolution to act as the standard ---
        var resolutionCounts = new Dictionary<Vector2Int, int>();
        foreach (var path in allTexturePaths)
        {
            // Load header only to get dimensions, this is memory-safe
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                var size = new Vector2Int(tex.width, tex.height);
                if (!resolutionCounts.ContainsKey(size))
                    resolutionCounts[size] = 0;
                resolutionCounts[size]++;
                Resources.UnloadAsset(tex); // Unload immediately
            }
        }

        if (resolutionCounts.Count == 0)
        {
            Debug.LogError("Could not read dimensions from any of the source textures.");
            return;
        }

        Vector2Int targetResolution = resolutionCounts.OrderByDescending(kv => kv.Value).First().Key;
        Debug.Log($"Target resolution determined to be {targetResolution.x}x{targetResolution.y}. Textures of other sizes will be skipped.");

        // --- Step 2: Filter the list to only include valid textures ---
        var validTexturePaths = allTexturePaths.Where(path => {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return false;
            bool isValid = tex.width == targetResolution.x && tex.height == targetResolution.y;
            Resources.UnloadAsset(tex);
            return isValid;
        }).ToList();
        
        if (validTexturePaths.Count == 0)
        {
             Debug.LogError("No textures matched the target resolution. Texture array not created.");
             return;
        }
        
        Debug.Log($"Found {validTexturePaths.Count} textures matching the target resolution. Starting build...");

        // --- Step 3: Creation ---
        Texture2D firstTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(validTexturePaths[0]);
        if (firstTexture == null)
        {
            Debug.LogError($"Could not load the first texture at path: {validTexturePaths[0]}");
            return;
        }
        bool hasMips = firstTexture.mipmapCount > 1;
        Resources.UnloadAsset(firstTexture);

        Texture2DArray textureArray = new Texture2DArray(targetResolution.x, targetResolution.y, validTexturePaths.Count, TextureFormat.RGBA32, hasMips);

        for (int i = 0; i < validTexturePaths.Count; i++)
        {
            var path = validTexturePaths[i];
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogWarning($"Could not load texture at path: {path}. Skipping.");
                continue;
            }

            if (!tex.isReadable)
            {
                Debug.LogWarning($"Texture '{tex.name}' is not readable and will be skipped. Please enable 'Read/Write Enabled' in its import settings.");
                Resources.UnloadAsset(tex);
                continue;
            }

            textureArray.SetPixels(tex.GetPixels(), i);
            Resources.UnloadAsset(tex); // CRITICAL: Unload asset to free memory
        }
        textureArray.Apply();

        // --- Saving Asset ---
        string directory = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        AssetDatabase.CreateAsset(textureArray, savePath);
        Debug.Log($"Successfully created Texture2DArray at '{savePath}' with {validTexturePaths.Count} textures.");
        
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2DArray>(savePath));
    }

    private List<string> GetTexturePathsFromSources()
    {
        var texturePaths = new List<string>();
        foreach (var sourceAsset in sourceAssets)
        {
            if (sourceAsset == null) continue;

            string path = AssetDatabase.GetAssetPath(sourceAsset);

            if (AssetDatabase.IsValidFolder(path))
            {
                // It's a folder, find all textures inside
                string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { path });
                foreach (string guid in guids)
                {
                    texturePaths.Add(AssetDatabase.GUIDToAssetPath(guid));
                }
            }
            else if (sourceAsset is Texture2D)
            {
                // It's a single texture
                texturePaths.Add(path);
            }
        }
        return texturePaths.Distinct().ToList(); // Remove duplicates
    }
} 