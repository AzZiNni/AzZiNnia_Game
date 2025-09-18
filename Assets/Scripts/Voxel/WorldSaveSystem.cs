using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;

namespace Voxel
{
    /// <summary>
    /// Система збереження та завантаження світу
    /// </summary>
    public class WorldSaveSystem : MonoBehaviour
{
    [Header("Налаштування збереження")]
    public string saveFolderName = "WorldSaves";
    public string currentSaveName = "World1";
    
    private VoxelTerrain terrain;
    private string SavePath => Path.Combine(Application.persistentDataPath, saveFolderName, currentSaveName);
    
    // Зберігаємо тільки модифікації, не весь світ
    private Dictionary<int3, ChunkModifications> modifiedChunks = new Dictionary<int3, ChunkModifications>();
    
    void Start()
    {
        terrain = GetComponent<VoxelTerrain>();
        
        // Створюємо папку для збережень якщо її немає
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, saveFolderName));
        // Ensure save directory exists for current save
        Directory.CreateDirectory(SavePath);
    }
    
    /// <summary>
    /// Записує модифікацію чанка
    /// </summary>
    public void RecordModification(int3 chunkCoord, int3 localPos, float newValue)
    {
        if (!modifiedChunks.TryGetValue(chunkCoord, out ChunkModifications mods))
        {
            mods = new ChunkModifications();
            modifiedChunks[chunkCoord] = mods;
        }
        
        int index = localPos.x + localPos.y * 17 + localPos.z * 17 * 17;
        mods.modifications[index] = newValue;
    }
    
    /// <summary>
    /// Зберігає світ на диск
    /// </summary>
    public void SaveWorld()
    {
        Directory.CreateDirectory(SavePath);
        
        // Зберігаємо метадані
        SaveMetadata metadata = new SaveMetadata
        {
            version = 1,
            seed = terrain.seed,
            saveTime = System.DateTime.Now.ToString(),
            modifiedChunkCount = modifiedChunks.Count
        };
        
        string metadataJson = JsonUtility.ToJson(metadata, true);
        File.WriteAllText(Path.Combine(SavePath, "metadata.json"), metadataJson);
        
        // Зберігаємо модифіковані чанки
        foreach (var kvp in modifiedChunks)
        {
            string chunkFileName = $"chunk_{kvp.Key.x}_{kvp.Key.y}_{kvp.Key.z}.dat";
            string chunkPath = Path.Combine(SavePath, chunkFileName);
            
            using (BinaryWriter writer = new BinaryWriter(File.Open(chunkPath, FileMode.Create)))
            {
                // Записуємо координати чанка
                writer.Write(kvp.Key.x);
                writer.Write(kvp.Key.y);
                writer.Write(kvp.Key.z);
                
                // Записуємо модифікації
                var mods = kvp.Value.modifications;
                writer.Write(mods.Count);
                
                foreach (var mod in mods)
                {
                    writer.Write(mod.Key);
                    writer.Write(mod.Value);
                }
            }
        }
        
        Debug.Log($"Світ збережено: {modifiedChunks.Count} модифікованих чанків");
    }
    
    /// <summary>
    /// Завантажує світ з диска
    /// </summary>
    public void LoadWorld()
    {
        if (!Directory.Exists(SavePath))
        {
            Debug.LogWarning("Збереження не знайдено");
            return;
        }
        
        // Очищаємо поточні модифікації
        modifiedChunks.Clear();
        
        // Завантажуємо метадані
        string metadataPath = Path.Combine(SavePath, "metadata.json");
        if (File.Exists(metadataPath))
        {
            string metadataJson = File.ReadAllText(metadataPath);
            SaveMetadata metadata = JsonUtility.FromJson<SaveMetadata>(metadataJson);
            Debug.Log($"Завантаження світу: версія {metadata.version}, збережено {metadata.saveTime}");
        }
        
        // Завантажуємо модифіковані чанки
        string[] chunkFiles = Directory.GetFiles(SavePath, "chunk_*.dat");
        foreach (string chunkFile in chunkFiles)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(chunkFile, FileMode.Open)))
            {
                // Читаємо координати чанка
                int3 coord = new int3(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );
                
                ChunkModifications mods = new ChunkModifications();
                
                // Читаємо модифікації
                int modCount = reader.ReadInt32();
                for (int i = 0; i < modCount; i++)
                {
                    int index = reader.ReadInt32();
                    float value = reader.ReadSingle();
                    mods.modifications[index] = value;
                }
                
                modifiedChunks[coord] = mods;
            }
        }
        
        Debug.Log($"Світ завантажено: {modifiedChunks.Count} модифікованих чанків");
        
        // Оновлюємо всі завантажені чанки – застосувати модифікації до активних
        ApplyLoadedModifications();
    }

    private void ApplyLoadedModifications()
    {
        if (terrain == null || modifiedChunks.Count == 0) return;
        
        foreach (var kvp in modifiedChunks)
        {
            int3 chunkCoord = kvp.Key;
            if (!terrain.Chunks.TryGetValue(chunkCoord, out var chunk))
                continue; // chunk not loaded yet
            
            var mods = kvp.Value.modifications;
            int dataSize = terrain.chunkSize + 1;
            foreach (var entry in mods)
            {
                int index = entry.Key;
                float value = entry.Value;
                
                int x = index % dataSize;
                int y = (index / dataSize) % dataSize;
                int z = index / (dataSize * dataSize);
                
                // Apply density change directly to chunk
                // We can use SetVoxel as an approximation: value > 0 -> solid else air
                chunk.SetVoxel(x, y, z, value > 0 ? VoxelType.Stone : VoxelType.Air);
            }
            
            // Regenerate mesh after applying modifications
            chunk.GenerateMesh();
        }
    }
    
    /// <summary>
    /// Отримує модифікації для чанка
    /// </summary>
    public bool TryGetChunkModifications(int3 chunkCoord, out ChunkModifications modifications)
    {
        return modifiedChunks.TryGetValue(chunkCoord, out modifications);
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveWorld();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveWorld();
        }
    }
}

/// <summary>
/// Модифікації окремого чанка
/// </summary>
[System.Serializable]
public class ChunkModifications
{
    public Dictionary<int, float> modifications = new Dictionary<int, float>();
}

/// <summary>
/// Метадані збереження
/// </summary>
[System.Serializable]
public struct SaveMetadata
{
    public int version;
    public int seed;
    public string saveTime;
    public int modifiedChunkCount;
}
} 