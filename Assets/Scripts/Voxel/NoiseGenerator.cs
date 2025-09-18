using UnityEngine;
using Unity.Mathematics;

namespace Voxel
{
    /// <summary>
    /// Генератор різних типів шуму для процедурної генерації терену
    /// </summary>
    public static class NoiseGenerator
{
    /// <summary>
    /// Генерує шум Перліна з кількома октавами
    /// </summary>
    public static float PerlinNoise3D(float3 position, float scale, int octaves, float persistence, float lacunarity)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;
        
        for (int i = 0; i < octaves; i++)
        {
            float3 samplePos = position * scale * frequency;
            
            // 3D Perlin noise
            float perlinValue = Mathf.PerlinNoise(samplePos.x, samplePos.y) * 0.5f +
                               Mathf.PerlinNoise(samplePos.y, samplePos.z) * 0.5f +
                               Mathf.PerlinNoise(samplePos.z, samplePos.x) * 0.5f;
            perlinValue /= 1.5f;
            
            total += perlinValue * amplitude;
            maxValue += amplitude;
            
            amplitude *= persistence;
            frequency *= lacunarity;
        }
        
        return total / maxValue;
    }
    
    /// <summary>
    /// Генерує шум Сімплекс
    /// </summary>
    public static float SimplexNoise(float3 position, float scale)
    {
        float3 p = position * scale;
        return noise.snoise(p);
    }
    
    /// <summary>
    /// Генерує шум Вороного (клітинний)
    /// </summary>
    public static float VoronoiNoise(float3 position, float scale)
    {
        float3 p = position * scale;
        float2 f = noise.cellular(p).xy;
        return f.y - f.x; // Різниця між найближчими точками
    }
    
    /// <summary>
    /// Генерує рідж-шум (гірський)
    /// </summary>
    public static float RidgeNoise(float3 position, float scale, int octaves)
    {
        float total = 0;
        float frequency = 1;
        float amplitude = 1;
        float maxValue = 0;
        
        for (int i = 0; i < octaves; i++)
        {
            float3 samplePos = position * scale * frequency;
            float perlinValue = Mathf.PerlinNoise(samplePos.x, samplePos.z);
            
            // Інвертуємо і робимо гострішим
            perlinValue = 1f - Mathf.Abs(perlinValue * 2f - 1f);
            perlinValue = perlinValue * perlinValue;
            
            total += perlinValue * amplitude;
            maxValue += amplitude;
            
            amplitude *= 0.5f;
            frequency *= 2f;
        }
        
        return total / maxValue;
    }
    
    /// <summary>
    /// Комбінований шум для реалістичного терену
    /// </summary>
    public static float TerrainNoise(float3 position, NoiseSettings settings)
    {
        float baseHeight = PerlinNoise3D(
            position + settings.offset,
            settings.baseScale,
            settings.octaves,
            settings.persistence,
            settings.lacunarity
        );
        
        // Додаємо деталі
        float detail = SimplexNoise(position, settings.detailScale) * settings.detailStrength;
        
        // Додаємо гори
        if (settings.useMountains)
        {
            float mountains = RidgeNoise(position, settings.mountainScale, 4);
            baseHeight = Mathf.Lerp(baseHeight, mountains, settings.mountainBlend);
        }
        
        return (baseHeight + detail) * settings.heightScale;
    }
}

/// <summary>
/// Налаштування для генератора шуму
/// </summary>
[System.Serializable]
public struct NoiseSettings
{
    public float3 offset;
    public float baseScale;
    public float detailScale;
    public float detailStrength;
    public float heightScale;
    public int octaves;
    public float persistence;
    public float lacunarity;
    
    // Гірські налаштування
    public bool useMountains;
    public float mountainScale;
    public float mountainBlend;
    
    public static NoiseSettings Default => new NoiseSettings
    {
        offset = float3.zero,
        baseScale = 0.05f,
        detailScale = 0.1f,
        detailStrength = 0.2f,
        heightScale = 20f,
        octaves = 4,
        persistence = 0.5f,
        lacunarity = 2f,
        useMountains = false,
        mountainScale = 0.03f,
        mountainBlend = 0.5f
    };
}
} 