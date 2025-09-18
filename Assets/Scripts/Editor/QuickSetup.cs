using UnityEngine;
using UnityEditor;
using Voxel;
using Azurin.Player;

public class QuickSetupWindow : EditorWindow
{
    [MenuItem("Tools/AzZiNnia/Quick Setup")]
    public static void ShowWindow()
    {
        GetWindow<QuickSetupWindow>("AzZiNnia Quick Setup");
    }
    
    void OnGUI()
    {
        GUILayout.Label("AzZiNnia Quick Setup", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("1. Setup VoxelTerrain", GUILayout.Height(30)))
        {
            SetupVoxelTerrain();
        }
        
        if (GUILayout.Button("2. Setup Texture Mapping", GUILayout.Height(30)))
        {
            SetupTextureMapping();
        }
        if (GUILayout.Button("Assign Material + Texture Array to Terrain", GUILayout.Height(30)))
        {
            AssignMaterialAndArray();
        }
        
        if (GUILayout.Button("3. Setup Player", GUILayout.Height(30)))
        {
            SetupPlayer();
        }
        
        if (GUILayout.Button("4. Setup Camera", GUILayout.Height(30)))
        {
            SetupCamera();
        }
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Setup Everything", GUILayout.Height(40)))
        {
            SetupVoxelTerrain();
            SetupTextureMapping();
            SetupPlayer();
            SetupCamera();
            
            Debug.Log("✅ AzZiNnia setup complete! Press Play to start.");
        }
    }
    
    void SetupVoxelTerrain()
    {
        GameObject voxelTerrain = GameObject.Find("VoxelTerrain");
        if (voxelTerrain == null)
        {
            voxelTerrain = new GameObject("VoxelTerrain");
        }
        
        VoxelTerrain terrain = voxelTerrain.GetComponent<VoxelTerrain>();
        if (terrain == null)
        {
            terrain = voxelTerrain.AddComponent<VoxelTerrain>();
        }
        
        // Налаштування
        terrain.useUkrainianGenerator = true;
        terrain.chunkSize = 16;
        terrain.viewDistance = 3;
        terrain.voxelSize = 1f;
        
        Debug.Log("✅ VoxelTerrain налаштовано");
    }
    
    void SetupTextureMapping()
    {
        // Спробуємо знайти існуючий VoxelTextureMapping
        string[] guids = AssetDatabase.FindAssets("t:VoxelTextureMapping");
        VoxelTextureMapping mapping = null;
        
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            mapping = AssetDatabase.LoadAssetAtPath<VoxelTextureMapping>(path);
        }
        
        if (mapping == null)
        {
            // Створюємо новий VoxelTextureMapping
            mapping = ScriptableObject.CreateInstance<VoxelTextureMapping>();
            AssetDatabase.CreateAsset(mapping, "Assets/Settings/MainVoxelTextureMapping.asset");
            AssetDatabase.SaveAssets();
        }
        
        // Знаходимо VoxelTerrain і призначаємо mapping
        VoxelTerrain terrain = FindFirstObjectByType<VoxelTerrain>();
        if (terrain != null)
        {
            terrain.textureMapping = mapping;
            EditorUtility.SetDirty(terrain);
        }
        
        Debug.Log($"✅ VoxelTextureMapping створено: {AssetDatabase.GetAssetPath(mapping)}");
    }
    
    void AssignMaterialAndArray()
    {
        var terrain = FindFirstObjectByType<VoxelTerrain>();
        if (terrain == null)
        {
            Debug.LogError("VoxelTerrain not found in scene!");
            return;
        }
        
        // Smart shader detection with validation
        Shader voxelShader = null;
        string[] shaderPriority = { "Custom/VoxelArrayShader", "Custom/SimpleVoxelShader", "Custom/VoxelTerrainArrayMaterial" };
        
        foreach (string shaderName in shaderPriority)
        {
            voxelShader = Shader.Find(shaderName);
            if (voxelShader != null)
            {
                Debug.Log($"✅ Found shader: {shaderName}");
                break;
            }
        }
        
        // Create or validate material
        if (terrain.terrainMaterial == null || terrain.terrainMaterial.shader != voxelShader)
        {
            if (voxelShader != null)
            {
                terrain.terrainMaterial = new Material(voxelShader);
                terrain.terrainMaterial.name = "VoxelTerrainMaterial";
                
                // Save material as asset
                string materialPath = "Assets/Materials/VoxelTerrainMaterial.mat";
                if (!AssetDatabase.LoadAssetAtPath<Material>(materialPath))
                {
                    AssetDatabase.CreateAsset(terrain.terrainMaterial, materialPath);
                }
                
                EditorUtility.SetDirty(terrain);
                Debug.Log($"✅ Created material with shader: {voxelShader.name}");
            }
            else
            {
                Debug.LogError("❌ No voxel shader found! Please check shaders.");
                return;
            }
        }
        
        // Find and validate Texture2DArray
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2DArray");
        if (terrain.textureArray == null && textureGuids.Length > 0)
        {
            // Find the largest texture array (usually the most complete)
            Texture2DArray bestArray = null;
            int maxTextures = 0;
            
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var array = AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
                if (array != null && array.depth > maxTextures)
                {
                    bestArray = array;
                    maxTextures = array.depth;
                }
            }
            
            if (bestArray != null)
            {
                terrain.textureArray = bestArray;
                EditorUtility.SetDirty(terrain);
                Debug.Log($"✅ Found Texture2DArray with {maxTextures} textures");
            }
        }
        
        // Validate and assign to material
        if (terrain.terrainMaterial != null && terrain.textureArray != null)
        {
            terrain.terrainMaterial.SetTexture("_MainTex", terrain.textureArray);
            terrain.terrainMaterial.SetFloat("_TextureScale", terrain.textureScale);
            
            // Apply to all active chunks
            if (Application.isPlaying && terrain.Chunks != null)
            {
                foreach (var chunk in terrain.Chunks.Values)
                {
                    if (chunk != null && chunk.MeshRenderer != null)
                    {
                        chunk.MeshRenderer.material = terrain.terrainMaterial;
                    }
                }
            }
            
            Debug.Log($"✅ Material setup complete: {terrain.textureArray.depth} textures available");
        }
        else
        {
            Debug.LogWarning("⚠️ Material or TextureArray missing. Please check setup.");
        }
    }
    
    void SetupPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
            if (player == null)
            {
                player = new GameObject("Player");
            }
            player.tag = "Player";
        }
        
        CossackPlayer cossack = player.GetComponent<CossackPlayer>();
        if (cossack == null)
        {
            cossack = player.AddComponent<CossackPlayer>();
        }
        
        // Встановлюємо безпечну позицію
        player.transform.position = new Vector3(0, 50, 0);
        
        Debug.Log("✅ Player налаштовано");
    }
    
    void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                mainCamera = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            else
            {
                mainCamera = camObj.GetComponent<Camera>();
            }
        }
        
        // Прив'язуємо до гравця
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            mainCamera.transform.SetParent(player.transform);
            mainCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
            mainCamera.transform.localRotation = Quaternion.identity;
        }
        
        Debug.Log("✅ Camera налаштована");
    }
} 