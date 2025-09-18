using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using Voxel;

public class TerrainDebugger : EditorWindow
{
    private VoxelTerrain voxelTerrain;
    private UkrainianTerrainGenerator ukrainianGenerator;
    private SoilLayerSystem soilSystem;
    
    private Vector3 testPosition = Vector3.zero;
    private string debugInfo = "";
    
    [MenuItem("Tools/Terrain Debugger")]
    public static void ShowWindow()
    {
        TerrainDebugger window = GetWindow<TerrainDebugger>();
        window.titleContent = new GUIContent("Terrain Debugger");
        window.Show();
    }
    
    void OnEnable()
    {
        FindTerrainComponents();
    }
    
    void FindTerrainComponents()
    {
        voxelTerrain = FindFirstObjectByType<VoxelTerrain>();
        ukrainianGenerator = FindFirstObjectByType<UkrainianTerrainGenerator>();
        soilSystem = FindFirstObjectByType<SoilLayerSystem>();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Terrain Generation Debugger", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Find Terrain Components"))
        {
            FindTerrainComponents();
        }
        
        GUILayout.Space(10);
        
        // Показуємо стан компонентів
        EditorGUILayout.LabelField("VoxelTerrain:", voxelTerrain != null ? "Found" : "Not Found");
        EditorGUILayout.LabelField("UkrainianGenerator:", ukrainianGenerator != null ? "Found" : "Not Found");
        EditorGUILayout.LabelField("SoilSystem:", soilSystem != null ? "Found" : "Not Found");
        
        GUILayout.Space(10);
        
        // Тестова позиція
        testPosition = EditorGUILayout.Vector3Field("Test Position:", testPosition);
        
        if (GUILayout.Button("Test Terrain Generation at Position"))
        {
            TestTerrainGeneration();
        }
        
        GUILayout.Space(10);
        
        // Показуємо результати
        GUILayout.Label("Debug Info:", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(debugInfo, GUILayout.Height(300));
        
        if (GUILayout.Button("Test Player Position"))
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                testPosition = player.transform.position;
                TestTerrainGeneration();
            }
            else
            {
                debugInfo = "Player not found!";
            }
        }
    }
    
    void TestTerrainGeneration()
    {
        if (voxelTerrain == null || ukrainianGenerator == null)
        {
            debugInfo = "Components not found! Please ensure VoxelTerrain and UkrainianTerrainGenerator are in the scene.";
            return;
        }
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== TERRAIN DEBUG INFO ===");
        sb.AppendLine($"Test Position: {testPosition}");
        sb.AppendLine();
        
        // Тестуємо генерацію висоти
        float3 pos3D = new float3(testPosition);
        float terrainHeight = ukrainianGenerator.GenerateHeight(pos3D);
        sb.AppendLine($"Generated Height: {terrainHeight:F2}");
        
        // Тестуємо біоми
        var biome = ukrainianGenerator.GetRegionalBiome(testPosition.x, testPosition.z);
        sb.AppendLine($"Biome: {biome}");
        
        // Тестуємо український регіон
        var region = ukrainianGenerator.DetermineRegion(testPosition.x, testPosition.z);
        sb.AppendLine($"Ukrainian Region: {region}");
        
        sb.AppendLine();
        sb.AppendLine("=== VOXEL TYPES BY DEPTH ===");
        
        // Тестуємо типи вокселів на різних глибинах
        for (float depth = 0f; depth <= 20f; depth += 1f)
        {
            float3 voxelPos = new float3(testPosition.x, terrainHeight - depth, testPosition.z);
            
            // Метод 1: Через VoxelTerrain
            VoxelType voxelType1 = VoxelType.Air;
            try
            {
                int voxelX = Mathf.FloorToInt(voxelPos.x);
                int voxelY = Mathf.FloorToInt(voxelPos.y);
                int voxelZ = Mathf.FloorToInt(voxelPos.z);
                voxelType1 = voxelTerrain.GetVoxelType(new float3(voxelX, voxelY, voxelZ));
            }
            catch (System.Exception e)
            {
                sb.AppendLine($"Error getting voxel type: {e.Message}");
            }
            
            // Метод 2: Безпосередньо через Ukrainian Generator
            VoxelType voxelType2 = ukrainianGenerator.GetVoxelType(voxelPos, depth);
            
            // Метод 3: Через систему ґрунту (якщо є)
            VoxelType soilVoxelType = VoxelType.Air;
            if (soilSystem != null && depth <= 15f)
            {
                var soilLayer = soilSystem.GetSoilLayerAtDepth(voxelPos, depth);
                soilVoxelType = soilLayer.voxelType;
            }
            
            sb.AppendLine($"Depth {depth:F1}m: VoxelTerrain={voxelType1}, Ukrainian={voxelType2}, Soil={soilVoxelType}");
        }
        
        // Тестуємо систему ґрунту окремо
        if (soilSystem != null)
        {
            sb.AppendLine();
            sb.AppendLine("=== SOIL LAYER SYSTEM ===");
            
            for (float depth = 0f; depth <= 10f; depth += 0.5f)
            {
                var soilLayer = soilSystem.GetSoilLayerAtDepth(pos3D, depth);
                float fertility = soilSystem.GetSoilFertility(pos3D, depth);
                
                sb.AppendLine($"Depth {depth:F1}m: {soilLayer.horizon} - {soilLayer.voxelType} (Fertility: {fertility:F2})");
            }
        }
        
        debugInfo = sb.ToString();
    }
} 