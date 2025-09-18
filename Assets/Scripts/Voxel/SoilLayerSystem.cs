using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Voxel
{
    public enum SoilHorizon
    {
        O_Horizon,     // Органічний шар (лісова підстилка)
        A_Horizon,     // Гумусовий горизонт (верхній родючий шар)
        E_Horizon,     // Елювіальний горизонт (вилуговування)
        B_Horizon,     // Ілювіальний горизонт (накопичення)
        C_Horizon,     // Материнська порода (слабо змінена)
        R_Horizon      // Скельна основа
    }

    [System.Serializable]
    public struct SoilLayer
    {
        public SoilHorizon horizon;
        public VoxelType voxelType;
        public float minThickness;
        public float maxThickness;
        public float density;
        public Color color;
        public bool hasOrganicMatter;
        public float waterRetention;
        public float permeability;
    }

    public class SoilLayerSystem : MonoBehaviour
    {
        [Header("Soil Configuration")]
        public SoilLayer[] soilLayers = new SoilLayer[]
        {
            new SoilLayer 
            { 
                horizon = SoilHorizon.O_Horizon, 
                voxelType = VoxelType.Leaf_Litter, 
                minThickness = 0.1f, 
                maxThickness = 0.5f,
                density = 0.3f,
                color = new Color(0.4f, 0.3f, 0.2f),
                hasOrganicMatter = true,
                waterRetention = 0.8f,
                permeability = 0.9f
            },
            new SoilLayer 
            { 
                horizon = SoilHorizon.A_Horizon, 
                voxelType = VoxelType.Rich_Soil, 
                minThickness = 0.8f, 
                maxThickness = 2.5f,  // Більша товщина для чорнозему
                density = 0.7f,
                color = new Color(0.2f, 0.15f, 0.1f),  // Темніший колір
                hasOrganicMatter = true,
                waterRetention = 0.8f,  // Високе водоутримання
                permeability = 0.6f
            },
            new SoilLayer 
            { 
                horizon = SoilHorizon.E_Horizon, 
                voxelType = VoxelType.Sandy_Soil, 
                minThickness = 0.3f, 
                maxThickness = 1.0f,
                density = 0.8f,
                color = new Color(0.6f, 0.5f, 0.4f),
                hasOrganicMatter = false,
                waterRetention = 0.3f,
                permeability = 0.8f
            },
            new SoilLayer 
            { 
                horizon = SoilHorizon.B_Horizon, 
                voxelType = VoxelType.Clay, 
                minThickness = 1.0f, 
                maxThickness = 3.0f,  // Більша товщина
                density = 0.9f,
                color = new Color(0.5f, 0.3f, 0.2f),
                hasOrganicMatter = false,
                waterRetention = 0.9f,
                permeability = 0.2f
            },
            new SoilLayer 
            { 
                horizon = SoilHorizon.C_Horizon, 
                voxelType = VoxelType.Weathered_Rock, 
                minThickness = 2.0f, 
                maxThickness = 5.0f,
                density = 1.2f,
                color = new Color(0.7f, 0.6f, 0.5f),
                hasOrganicMatter = false,
                waterRetention = 0.4f,
                permeability = 0.3f
            },
            new SoilLayer 
            { 
                horizon = SoilHorizon.R_Horizon, 
                voxelType = VoxelType.Stone, 
                minThickness = 10.0f, 
                maxThickness = 50.0f,
                density = 2.5f,
                color = new Color(0.5f, 0.5f, 0.5f),
                hasOrganicMatter = false,
                waterRetention = 0.1f,
                permeability = 0.05f
            }
        };

        [Header("Environmental Factors")]
        public float temperatureEffect = 1.0f;
        public float precipitationEffect = 1.0f;
        public float vegetationDensity = 1.0f;
        public float erosionRate = 0.1f;
        
        [Header("Biome Modifiers")]
        public BiomeType currentBiome = BiomeType.Plains;

        public SoilLayer GetSoilLayerAtDepth(float3 worldPosition, float depth)
        {
            float currentDepth = 0f;
            
            // Модифікуємо товщину шарів залежно від біому
            float biomeModifier = GetBiomeModifier(currentBiome);
            
            // Додаємо варіацію через шум
            float noiseVariation = NoiseGenerator.SimplexNoise(
                new float3(worldPosition.x * 0.01f, worldPosition.y * 0.01f, worldPosition.z * 0.01f), 
                1f
            ) * 0.3f;
            
            for (int i = 0; i < soilLayers.Length; i++)
            {
                SoilLayer layer = soilLayers[i];
                
                // Розраховуємо товщину шару з урахуванням всіх факторів
                float layerThickness = CalculateLayerThickness(layer, biomeModifier, noiseVariation, worldPosition);
                
                if (depth >= currentDepth && depth < currentDepth + layerThickness)
                {
                    return layer;
                }
                
                currentDepth += layerThickness;
            }
            
            // Якщо глибше за всі шари, повертаємо скельну основу
            return soilLayers[soilLayers.Length - 1];
        }

        private float CalculateLayerThickness(SoilLayer layer, float biomeModifier, float noiseVariation, float3 position)
        {
            float baseThickness = Mathf.Lerp(layer.minThickness, layer.maxThickness, 0.5f);
            
            // Застосовуємо модифікатор біому
            baseThickness *= biomeModifier;
            
            // Додаємо варіацію
            baseThickness += noiseVariation;
            
            // Враховуємо кліматичні фактори
            switch (layer.horizon)
            {
                case SoilHorizon.O_Horizon:
                    baseThickness *= vegetationDensity;
                    break;
                case SoilHorizon.A_Horizon:
                    baseThickness *= (temperatureEffect * precipitationEffect);
                    break;
                case SoilHorizon.E_Horizon:
                    baseThickness *= precipitationEffect; // Більше опадів = більше вилуговування
                    break;
            }
            
            return Mathf.Clamp(baseThickness, layer.minThickness * 0.5f, layer.maxThickness * 2.0f);
        }

        private float GetBiomeModifier(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => 1.2f,        // Глибші грунти в лісі
                BiomeType.Plains => 1.0f,        // Стандартні грунти
                BiomeType.Desert => 0.3f,        // Тонкі грунти в пустелі
                BiomeType.Mountains => 0.5f,     // Тонкі гірські грунти
                BiomeType.Swamp => 1.5f,         // Глибокі органічні грунти
                BiomeType.Beach => 0.2f,         // Дуже тонкі піщані грунти
                BiomeType.Ocean => 0.0f,         // Немає грунту
                _ => 1.0f
            };
        }

        public float GetSoilFertility(float3 worldPosition, float depth)
        {
            SoilLayer layer = GetSoilLayerAtDepth(worldPosition, depth);
            
            float baseFertility = layer.horizon switch
            {
                SoilHorizon.O_Horizon => 0.9f,
                SoilHorizon.A_Horizon => 1.0f,
                SoilHorizon.E_Horizon => 0.3f,
                SoilHorizon.B_Horizon => 0.5f,
                SoilHorizon.C_Horizon => 0.1f,
                SoilHorizon.R_Horizon => 0.0f,
                _ => 0.0f
            };

            // Модифікуємо родючість залежно від органічної речовини
            if (layer.hasOrganicMatter)
            {
                baseFertility *= 1.3f;
            }

            // Враховуємо водоутримання
            baseFertility *= (0.5f + layer.waterRetention * 0.5f);

            return Mathf.Clamp01(baseFertility);
        }

        public float GetSoilStability(float3 worldPosition, float depth)
        {
            SoilLayer layer = GetSoilLayerAtDepth(worldPosition, depth);
            
            float baseStability = layer.density;
            
            // Органічна речовина збільшує стабільність
            if (layer.hasOrganicMatter)
            {
                baseStability *= 1.2f;
            }
            
            // Глина більш стабільна, пісок менш стабільний
            if (layer.voxelType == VoxelType.Clay)
            {
                baseStability *= 1.5f;
            }
            else if (layer.voxelType == VoxelType.Sand)
            {
                baseStability *= 0.7f;
            }
            
            return Mathf.Clamp01(baseStability);
        }

        public bool CanPlantGrow(float3 worldPosition, VoxelType plantType)
        {
            float surfaceFertility = GetSoilFertility(worldPosition, 0.1f);
            float rootZoneFertility = GetSoilFertility(worldPosition, 0.5f);
            
            float averageFertility = (surfaceFertility + rootZoneFertility) * 0.5f;
            
            // Різні рослини мають різні вимоги до родючості
            float requiredFertility = plantType switch
            {
                VoxelType.Oak_Sapling => 0.6f,
                VoxelType.Grass => 0.3f,
                VoxelType.Wheat => 0.7f,
                VoxelType.Flower_Red => 0.4f,
                _ => 0.5f
            };
            
            return averageFertility >= requiredFertility;
        }

        // Система ерозії грунту
        public void ApplyErosion(float3 worldPosition, float waterFlow, float windStrength)
        {
            SoilLayer surfaceLayer = GetSoilLayerAtDepth(worldPosition, 0.05f);
            
            float erosionResistance = GetSoilStability(worldPosition, 0.05f);
            float erosionForce = (waterFlow * 0.7f + windStrength * 0.3f) * erosionRate;
            
            if (erosionForce > erosionResistance)
            {
                // Тут можна додати логіку видалення верхнього шару грунту
                // та перенесення його в інше місце
            }
        }

        // Система відновлення грунту
        public void RegenerateTopsoil(float3 worldPosition, float organicInput, float timeMultiplier)
        {
            float regenerationRate = organicInput * timeMultiplier * vegetationDensity;
            
            // Поступове відновлення верхнього шару грунту
            // через накопичення органічної речовини
        }

        public Color GetSoilColor(float3 worldPosition, float depth)
        {
            SoilLayer layer = GetSoilLayerAtDepth(worldPosition, depth);
            
            // Додаємо варіацію кольору через шум
            float colorVariation = NoiseGenerator.SimplexNoise(
                new float3(worldPosition.x * 0.1f, worldPosition.y * 0.1f, worldPosition.z * 0.1f), 
                1f
            ) * 0.1f;
            
            Color baseColor = layer.color;
            baseColor.r += colorVariation;
            baseColor.g += colorVariation;
            baseColor.b += colorVariation;
            
            return baseColor;
        }

        // Отримання інформації про грунт для UI
        public string GetSoilInfo(float3 worldPosition, float depth)
        {
            SoilLayer layer = GetSoilLayerAtDepth(worldPosition, depth);
            float fertility = GetSoilFertility(worldPosition, depth);
            float stability = GetSoilStability(worldPosition, depth);
            
            return $"Горизонт: {layer.horizon}\n" +
                   $"Тип: {layer.voxelType}\n" +
                   $"Родючість: {fertility:F2}\n" +
                   $"Стабільність: {stability:F2}\n" +
                   $"Водоутримання: {layer.waterRetention:F2}\n" +
                   $"Проникність: {layer.permeability:F2}";
        }
    }
} 