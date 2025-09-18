using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Voxel.Editor
{
    public class CreateSimpleTextureArray
    {
        [MenuItem("Voxel/Create Simple Texture Array")]
        public static void RunCreateSimpleTextureArray()
        {
            Debug.Log("🔧 Creating simple Texture2DArray for testing...");
            
            // Створюємо простий Texture2DArray з базовими кольорами
            int width = 256;
            int height = 256;
            int depth = 10; // 10 різних текстур
            
            Texture2DArray textureArray = new Texture2DArray(width, height, depth, TextureFormat.RGBA32, false);
            
            // Створюємо прості текстури для тестування
            for (int i = 0; i < depth; i++)
            {
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                
                // Різні кольори для різних індексів
                Color[] pixels = new Color[width * height];
                Color baseColor = GetColorForIndex(i);
                
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        // Додаємо трохи шуму для різноманітності
                        float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.3f;
                        pixels[y * width + x] = baseColor + new Color(noise, noise, noise, 0);
                    }
                }
                
                texture.SetPixels(pixels);
                texture.Apply();
                
                // Копіюємо текстуру в масив
                Graphics.CopyTexture(texture, 0, 0, textureArray, i, 0);
                
                // Очищаємо пам'ять
                Object.DestroyImmediate(texture);
            }
            
            // Зберігаємо Texture2DArray
            string path = "Assets/for atlas/SimpleVoxelTextureArray.asset";
            AssetDatabase.CreateAsset(textureArray, path);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"✅ Created simple Texture2DArray at: {path}");
            Debug.Log($"   - Size: {width}x{height}");
            Debug.Log($"   - Depth: {depth} textures");
            
            // Вибираємо створений ассет
            Selection.activeObject = textureArray;
            
            // Автоматично призначаємо до VoxelTerrain
            VoxelTerrain voxelTerrain = Object.FindObjectOfType<VoxelTerrain>();
            if (voxelTerrain != null)
            {
                voxelTerrain.textureArray = textureArray;
                EditorUtility.SetDirty(voxelTerrain);
                Debug.Log("✅ Automatically assigned to VoxelTerrain");
            }
        }
        
        private static Color GetColorForIndex(int index)
        {
            switch (index)
            {
                case 0: return Color.gray;      // Air/Empty
                case 1: return Color.green;     // Grass
                case 2: return new Color(0.6f, 0.4f, 0.2f); // Dirt
                case 3: return Color.gray;      // Stone
                case 4: return Color.white;     // Snow
                case 5: return Color.yellow;    // Sand
                case 6: return new Color(0.2f, 0.2f, 0.8f); // Water
                case 7: return Color.red;       // Lava
                case 8: return new Color(0.4f, 0.2f, 0.1f); // Wood
                case 9: return Color.black;     // Coal
                default: return Color.magenta;  // Unknown
            }
        }
    }
} 