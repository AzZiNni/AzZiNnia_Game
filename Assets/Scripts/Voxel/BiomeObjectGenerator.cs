using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

namespace Voxel
{
    [System.Serializable]
    public struct BiomeObject
    {
        public string name;
        public VoxelType primaryType;
        public VoxelType[] additionalTypes;
        public int3 size;
        public float spawnChance;
        public float minDistance;
        public float maxDistance;
        public bool requiresFertileSoil;
        public bool nearWater;
        public float minElevation;
        public float maxElevation;
        public BiomeType[] allowedBiomes;
    }

    public class BiomeObjectGenerator : MonoBehaviour
    {
        [Header("Ukrainian Flora Objects")]
        public BiomeObject[] ukrainianObjects;

        [Header("Kozak Structures")]
        public BiomeObject[] kozakStructures;

        [Header("Resource Deposits")]
        public BiomeObject[] resourceDeposits;

        private SoilLayerSystem soilSystem;
        private RealisticTerrainGenerator terrainGenerator;

        private void Awake()
        {
            soilSystem = GetComponent<SoilLayerSystem>();
            terrainGenerator = GetComponent<RealisticTerrainGenerator>();
            
            InitializeObjects();
        }

        private void InitializeObjects()
        {
            ukrainianObjects = new BiomeObject[]
            {
                new BiomeObject
                {
                    name = "Дуб (Oak)",
                    primaryType = VoxelType.Oak_Log,
                    additionalTypes = new VoxelType[] { VoxelType.Oak_Leaves, VoxelType.Oak_Sapling },
                    size = new int3(7, 12, 7),
                    spawnChance = 0.3f,
                    minDistance = 8f,
                    maxDistance = 20f,
                    requiresFertileSoil = true,
                    nearWater = false,
                    minElevation = 60f,
                    maxElevation = 120f,
                    allowedBiomes = new BiomeType[] { BiomeType.Forest, BiomeType.Plains }
                },
                new BiomeObject
                {
                    name = "Береза (Birch)",
                    primaryType = VoxelType.Birch_Log,
                    additionalTypes = new VoxelType[] { VoxelType.Birch_Leaves },
                    size = new int3(5, 10, 5),
                    spawnChance = 0.4f,
                    minDistance = 6f,
                    maxDistance = 15f,
                    requiresFertileSoil = true,
                    nearWater = true,
                    minElevation = 55f,
                    maxElevation = 110f,
                    allowedBiomes = new BiomeType[] { BiomeType.Forest, BiomeType.Swamp }
                },
                new BiomeObject
                {
                    name = "Верба (Willow)",
                    primaryType = VoxelType.Willow_Log,
                    additionalTypes = new VoxelType[] { VoxelType.Willow_Leaves },
                    size = new int3(6, 8, 6),
                    spawnChance = 0.6f,
                    minDistance = 4f,
                    maxDistance = 8f,
                    requiresFertileSoil = true,
                    nearWater = true,
                    minElevation = 50f,
                    maxElevation = 70f,
                    allowedBiomes = new BiomeType[] { BiomeType.Swamp, BiomeType.Plains }
                },
                new BiomeObject
                {
                    name = "Калина (Viburnum)",
                    primaryType = VoxelType.Viburnum_Log,
                    additionalTypes = new VoxelType[] { VoxelType.Viburnum_Leaves, VoxelType.Viburnum_Berries },
                    size = new int3(3, 4, 3),
                    spawnChance = 0.5f,
                    minDistance = 3f,
                    maxDistance = 10f,
                    requiresFertileSoil = true,
                    nearWater = true,
                    minElevation = 55f,
                    maxElevation = 100f,
                    allowedBiomes = new BiomeType[] { BiomeType.Forest, BiomeType.Plains, BiomeType.Swamp }
                },
                
                new BiomeObject
                {
                    name = "Ковила (Feather Grass)",
                    primaryType = VoxelType.Feather_Grass,
                    additionalTypes = new VoxelType[] { },
                    size = new int3(1, 2, 1),
                    spawnChance = 0.8f,
                    minDistance = 1f,
                    maxDistance = 3f,
                    requiresFertileSoil = false,
                    nearWater = false,
                    minElevation = 60f,
                    maxElevation = 150f,
                    allowedBiomes = new BiomeType[] { BiomeType.Plains, BiomeType.Desert }
                },
                new BiomeObject
                {
                    name = "Полин (Wormwood)",
                    primaryType = VoxelType.Wormwood,
                    additionalTypes = new VoxelType[] { },
                    size = new int3(1, 1, 1),
                    spawnChance = 0.6f,
                    minDistance = 2f,
                    maxDistance = 5f,
                    requiresFertileSoil = false,
                    nearWater = false,
                    minElevation = 65f,
                    maxElevation = 140f,
                    allowedBiomes = new BiomeType[] { BiomeType.Plains, BiomeType.Desert }
                },
                new BiomeObject
                {
                    name = "Очерет (Reed)",
                    primaryType = VoxelType.Reed,
                    additionalTypes = new VoxelType[] { },
                    size = new int3(1, 3, 1),
                    spawnChance = 0.9f,
                    minDistance = 1f,
                    maxDistance = 2f,
                    requiresFertileSoil = true,
                    nearWater = true,
                    minElevation = 45f,
                    maxElevation = 65f,
                    allowedBiomes = new BiomeType[] { BiomeType.Swamp }
                },
                
                new BiomeObject
                {
                    name = "Соняшник (Sunflower)",
                    primaryType = VoxelType.Sunflower,
                    additionalTypes = new VoxelType[] { },
                    size = new int3(1, 3, 1),
                    spawnChance = 0.3f,
                    minDistance = 3f,
                    maxDistance = 8f,
                    requiresFertileSoil = true,
                    nearWater = false,
                    minElevation = 60f,
                    maxElevation = 100f,
                    allowedBiomes = new BiomeType[] { BiomeType.Plains }
                },
                new BiomeObject
                {
                    name = "Мак (Poppy)",
                    primaryType = VoxelType.Poppy,
                    additionalTypes = new VoxelType[] { },
                    size = new int3(1, 1, 1),
                    spawnChance = 0.4f,
                    minDistance = 1f,
                    maxDistance = 4f,
                    requiresFertileSoil = true,
                    nearWater = false,
                    minElevation = 55f,
                    maxElevation = 110f,
                    allowedBiomes = new BiomeType[] { BiomeType.Plains, BiomeType.Forest }
                },
                new BiomeObject
                {
                    name = "Волошка (Cornflower)",
                    primaryType = VoxelType.Cornflower,
                    additionalTypes = new VoxelType[] { },
                    size = new int3(1, 1, 1),
                    spawnChance = 0.5f,
                    minDistance = 1f,
                    maxDistance = 3f,
                    requiresFertileSoil = true,
                    nearWater = false,
                    minElevation = 60f,
                    maxElevation = 105f,
                    allowedBiomes = new BiomeType[] { BiomeType.Plains }
                },
                
                new BiomeObject
                {
                    name = "Ялина (Spruce)",
                    primaryType = VoxelType.Spruce_Log,
                    additionalTypes = new VoxelType[] { VoxelType.Spruce_Leaves },
                    size = new int3(5, 15, 5),
                    spawnChance = 0.7f,
                    minDistance = 5f,
                    maxDistance = 12f,
                    requiresFertileSoil = false,
                    nearWater = false,
                    minElevation = 80f,
                    maxElevation = 200f,
                    allowedBiomes = new BiomeType[] { BiomeType.Mountains, BiomeType.Snow_Peaks }
                },
                new BiomeObject
                {
                    name = "Ялівець (Juniper)",
                    primaryType = VoxelType.Juniper_Log,
                    additionalTypes = new VoxelType[] { VoxelType.Juniper_Leaves },
                    size = new int3(2, 4, 2),
                    spawnChance = 0.4f,
                    minDistance = 3f,
                    maxDistance = 8f,
                    requiresFertileSoil = false,
                    nearWater = false,
                    minElevation = 90f,
                    maxElevation = 180f,
                    allowedBiomes = new BiomeType[] { BiomeType.Mountains }
                }
            };
            
            kozakStructures = new BiomeObject[]
            {
                new BiomeObject
                {
                    name = "Стародавній курган (Ancient Mound)",
                    primaryType = VoxelType.Weathered_Rock,
                    additionalTypes = new VoxelType[] { VoxelType.Rich_Soil, VoxelType.Grass },
                    size = new int3(8, 4, 8),
                    spawnChance = 0.05f,
                    minDistance = 50f,
                    maxDistance = 200f,
                    requiresFertileSoil = false,
                    nearWater = false,
                    minElevation = 70f,
                    maxElevation = 130f,
                    allowedBiomes = new BiomeType[] { BiomeType.Plains, BiomeType.Forest }
                },
                new BiomeObject
                {
                    name = "Козацький хрест (Kozak Cross)",
                    primaryType = VoxelType.Oak_Log,
                    additionalTypes = new VoxelType[] { },
                    size = new int3(1, 4, 1),
                    spawnChance = 0.02f,
                    minDistance = 100f,
                    maxDistance = 300f,
                    requiresFertileSoil = false,
                    nearWater = false,
                    minElevation = 80f,
                    maxElevation = 150f,
                    allowedBiomes = new BiomeType[] { BiomeType.Plains, BiomeType.Forest, BiomeType.Mountains }
                },
                new BiomeObject
                {
                    name = "Залишки хати (Hut Ruins)",
                    primaryType = VoxelType.Clay_Bricks,
                    additionalTypes = new VoxelType[] { VoxelType.Oak_Log, VoxelType.Thatch },
                    size = new int3(6, 3, 4),
                    spawnChance = 0.01f,
                    minDistance = 200f,
                    maxDistance = 500f,
                    requiresFertileSoil = false,
                    nearWater = true,
                    minElevation = 55f,
                    maxElevation = 100f,
                    allowedBiomes = new BiomeType[] { BiomeType.Plains, BiomeType.Forest }
                }
            };
            
            resourceDeposits = new BiomeObject[]
            {
                new BiomeObject
                {
                    name = "Глиняне родовище (Clay Deposit)",
                    primaryType = VoxelType.Clay,
                    additionalTypes = new VoxelType[] { },
                    size = new int3(4, 2, 4),
                    spawnChance = 0.3f,
                    minDistance = 20f,
                    maxDistance = 50f,
                    requiresFertileSoil = false,
                    nearWater = true,
                    minElevation = 50f,
                    maxElevation = 80f,
                    allowedBiomes = new BiomeType[] { BiomeType.Plains, BiomeType.Swamp }
                },
                new BiomeObject
                {
                    name = "Залізна руда (Iron Ore)",
                    primaryType = VoxelType.IronOre,
                    additionalTypes = new VoxelType[] { VoxelType.Stone },
                    size = new int3(3, 3, 3),
                    spawnChance = 0.1f,
                    minDistance = 30f,
                    maxDistance = 100f,
                    requiresFertileSoil = false,
                    nearWater = false,
                    minElevation = 40f,
                    maxElevation = 90f,
                    allowedBiomes = new BiomeType[] { BiomeType.Mountains, BiomeType.Forest }
                },
                new BiomeObject
                {
                    name = "Болотна руда (Bog Iron)",
                    primaryType = VoxelType.Bog_Iron,
                    additionalTypes = new VoxelType[] { VoxelType.Peat },
                    size = new int3(2, 1, 2),
                    spawnChance = 0.4f,
                    minDistance = 10f,
                    maxDistance = 25f,
                    requiresFertileSoil = false,
                    nearWater = true,
                    minElevation = 45f,
                    maxElevation = 65f,
                    allowedBiomes = new BiomeType[] { BiomeType.Swamp }
                }
            };
        }

        public void GenerateBiomeObjects(TerrainChunkV2 chunk, BiomeType biome, float3 chunkPosition)
        {
            GenerateObjectsFromArray(chunk, ukrainianObjects, biome, chunkPosition, "flora");
            GenerateObjectsFromArray(chunk, kozakStructures, biome, chunkPosition, "structures");
            GenerateObjectsFromArray(chunk, resourceDeposits, biome, chunkPosition, "resources");
        }

        private void GenerateObjectsFromArray(TerrainChunkV2 chunk, BiomeObject[] objects, BiomeType biome, 
                                            float3 chunkPosition, string category)
        {
            foreach (var obj in objects)
            {
                if (!IsObjectAllowedInBiome(obj, biome)) continue;

                uint seed = (uint)(chunkPosition.x * 73856093 + chunkPosition.z * 19349663 + category.GetHashCode());
                Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed);

                int attempts = GetAttemptsForObject(obj, category);
                
                for (int i = 0; i < attempts; i++)
                {
                    if (random.NextFloat() > obj.spawnChance) continue;

                    float3 localPosition = new float3(
                        random.NextFloat(0f, chunk.ChunkSize),
                        0f,
                        random.NextFloat(0f, chunk.ChunkSize)
                    );

                    float3 worldPosition = chunkPosition + localPosition;
                    float elevation = terrainGenerator.GenerateTerrainHeight(worldPosition.x, worldPosition.z);
                    
                    if (CanPlaceObject(obj, worldPosition, elevation, biome))
                    {
                        PlaceObject(chunk, obj, localPosition, worldPosition, random);
                    }
                }
            }
        }

        private int GetAttemptsForObject(BiomeObject obj, string category)
        {
            return category switch
            {
                "flora" => obj.size.x > 5 ? 3 : 8,
                "structures" => 1,
                "resources" => 5,
                _ => 5
            };
        }

        private bool IsObjectAllowedInBiome(BiomeObject obj, BiomeType biome)
        {
            foreach (var allowedBiome in obj.allowedBiomes)
            {
                if (allowedBiome == biome) return true;
            }
            return false;
        }

        private bool CanPlaceObject(BiomeObject obj, float3 worldPosition, float elevation, BiomeType biome)
        {
            if (elevation < obj.minElevation || elevation > obj.maxElevation)
                return false;

            if (obj.nearWater)
            {
                float distanceToWater = GetDistanceToWater(worldPosition);
                if (distanceToWater > 20f) return false;
            }

            if (obj.requiresFertileSoil)
            {
                float fertility = soilSystem.GetSoilFertility(worldPosition, 0.2f);
                if (fertility < 0.5f) return false;
            }

            return CheckMinimumDistance(worldPosition, obj.minDistance);
        }

        private float GetDistanceToWater(float3 position)
        {
            float minDistance = float.MaxValue;
            
            for (int x = -5; x <= 5; x++)
            {
                for (int z = -5; z <= 5; z++)
                {
                    float3 checkPos = position + new float3(x * 4, 0, z * 4);
                    float elevation = terrainGenerator.GenerateTerrainHeight(checkPos.x, checkPos.z);
                    
                    if (elevation < 62f)
                    {
                        float distance = math.distance(position.xz, checkPos.xz);
                        minDistance = math.min(minDistance, distance);
                    }
                }
            }
            
            return minDistance;
        }

        private bool CheckMinimumDistance(float3 position, float minDistance)
        {
            float density = NoiseGenerator.SimplexNoise(new float3(position.x * 0.02f, 0, position.z * 0.02f), 1f);
            return density > -0.3f;
        }

        private void PlaceObject(TerrainChunkV2 chunk, BiomeObject obj, float3 localPosition, 
                               float3 worldPosition, Unity.Mathematics.Random random)
        {
            switch (obj.name)
            {
                case "Дуб (Oak)":
                    PlaceOakTree(chunk, localPosition, random);
                    break;
                case "Береза (Birch)":
                    PlaceBirchTree(chunk, localPosition, random);
                    break;
                case "Верба (Willow)":
                    PlaceWillowTree(chunk, localPosition, random);
                    break;
                case "Калина (Viburnum)":
                    PlaceViburnum(chunk, localPosition, random);
                    break;
                case "Ковила (Feather Grass)":
                    PlaceFeatherGrass(chunk, localPosition);
                    break;
                case "Стародавній курган (Ancient Mound)":
                    PlaceAncientMound(chunk, localPosition, random);
                    break;
                case "Глиняне родовище (Clay Deposit)":
                    PlaceClayDeposit(chunk, localPosition, random);
                    break;
                default:
                    PlaceSimpleObject(chunk, obj, localPosition);
                    break;
            }
        }

        private void PlaceOakTree(TerrainChunkV2 chunk, float3 position, Unity.Mathematics.Random random)
        {
            int height = random.NextInt(8, 15);
            int3 pos = new int3((int)position.x, (int)position.y, (int)position.z);
            
            for (int y = 0; y < height; y++)
            {
                chunk.SetVoxel(pos.x, pos.y + y, pos.z, VoxelType.Oak_Log);
            }
            
            int crownRadius = random.NextInt(3, 5);
            int crownHeight = random.NextInt(4, 7);
            
            for (int y = height - crownHeight; y < height + 2; y++)
            {
                for (int x = -crownRadius; x <= crownRadius; x++)
                {
                    for (int z = -crownRadius; z <= crownRadius; z++)
                    {
                        float distance = math.sqrt(x * x + z * z);
                        if (distance <= crownRadius && random.NextFloat() > 0.3f)
                        {
                            chunk.SetVoxel(pos.x + x, pos.y + y, pos.z + z, VoxelType.Oak_Leaves);
                        }
                    }
                }
            }
        }

        private void PlaceBirchTree(TerrainChunkV2 chunk, float3 position, Unity.Mathematics.Random random)
        {
            int height = random.NextInt(6, 12);
            int3 pos = new int3((int)position.x, (int)position.y, (int)position.z);
            
            for (int y = 0; y < height; y++)
            {
                chunk.SetVoxel(pos.x, pos.y + y, pos.z, VoxelType.Birch_Log);
            }
            
            int crownRadius = random.NextInt(2, 4);
            
            for (int y = height - 4; y < height + 1; y++)
            {
                for (int x = -crownRadius; x <= crownRadius; x++)
                {
                    for (int z = -crownRadius; z <= crownRadius; z++)
                    {
                        float distance = math.sqrt(x * x + z * z);
                        if (distance <= crownRadius && random.NextFloat() > 0.4f)
                        {
                            chunk.SetVoxel(pos.x + x, pos.y + y, pos.z + z, VoxelType.Birch_Leaves);
                        }
                    }
                }
            }
        }

        private void PlaceWillowTree(TerrainChunkV2 chunk, float3 position, Unity.Mathematics.Random random)
        {
            int height = random.NextInt(5, 9);
            int3 pos = new int3((int)position.x, (int)position.y, (int)position.z);
            
            for (int y = 0; y < height; y++)
            {
                chunk.SetVoxel(pos.x, pos.y + y, pos.z, VoxelType.Willow_Log);
            }
            
            for (int y = height - 3; y < height + 1; y++)
            {
                for (int x = -4; x <= 4; x++)
                {
                    for (int z = -4; z <= 4; z++)
                    {
                        float distance = math.sqrt(x * x + z * z);
                        if (distance <= 4 && random.NextFloat() > 0.2f)
                        {
                            chunk.SetVoxel(pos.x + x, pos.y + y, pos.z + z, VoxelType.Willow_Leaves);
                            
                            if (y == height - 1 && random.NextFloat() > 0.7f)
                            {
                                for (int dropY = 1; dropY <= 3; dropY++)
                                {
                                    if (pos.y + y - dropY >= 0)
                                        chunk.SetVoxel(pos.x + x, pos.y + y - dropY, pos.z + z, VoxelType.Willow_Leaves);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void PlaceViburnum(TerrainChunkV2 chunk, float3 position, Unity.Mathematics.Random random)
        {
            int height = random.NextInt(3, 5);
            int3 pos = new int3((int)position.x, (int)position.y, (int)position.z);
            
            for (int y = 0; y < height; y++)
            {
                chunk.SetVoxel(pos.x, pos.y + y, pos.z, VoxelType.Viburnum_Log);
            }
            
            for (int y = height - 2; y < height + 1; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    for (int z = -2; z <= 2; z++)
                    {
                        if (x == 0 && z == 0) continue;
                        
                        float distance = math.sqrt(x * x + z * z);
                        if (distance <= 2 && random.NextFloat() > 0.3f)
                        {
                            VoxelType leafType = random.NextFloat() > 0.8f ? VoxelType.Viburnum_Berries : VoxelType.Viburnum_Leaves;
                            chunk.SetVoxel(pos.x + x, pos.y + y, pos.z + z, leafType);
                        }
                    }
                }
            }
        }

        private void PlaceFeatherGrass(TerrainChunkV2 chunk, float3 position)
        {
            int3 pos = new int3((int)position.x, (int)position.y, (int)position.z);
            
            chunk.SetVoxel(pos.x, pos.y, pos.z, VoxelType.Feather_Grass);
            if (UnityEngine.Random.value > 0.5f)
            {
                chunk.SetVoxel(pos.x, pos.y + 1, pos.z, VoxelType.Feather_Grass);
            }
        }

        private void PlaceAncientMound(TerrainChunkV2 chunk, float3 position, Unity.Mathematics.Random random)
        {
            int3 pos = new int3((int)position.x, (int)position.y, (int)position.z);
            int radius = random.NextInt(3, 5);
            int height = random.NextInt(2, 4);
            
            for (int y = 0; y < height; y++)
            {
                int currentRadius = radius - y;
                for (int x = -currentRadius; x <= currentRadius; x++)
                {
                    for (int z = -currentRadius; z <= currentRadius; z++)
                    {
                        float distance = math.sqrt(x * x + z * z);
                        if (distance <= currentRadius)
                        {
                            VoxelType material = y == height - 1 ? VoxelType.Rich_Soil : VoxelType.Weathered_Rock;
                            chunk.SetVoxel(pos.x + x, pos.y + y, pos.z + z, material);
                        }
                    }
                }
            }
            
            chunk.SetVoxel(pos.x, pos.y + height, pos.z, VoxelType.Grass);
        }

        private void PlaceClayDeposit(TerrainChunkV2 chunk, float3 position, Unity.Mathematics.Random random)
        {
            int3 pos = new int3((int)position.x, (int)position.y, (int)position.z);
            int width = random.NextInt(3, 6);
            int depth = random.NextInt(1, 3);
            
            for (int y = -depth; y <= 0; y++)
            {
                for (int x = -width/2; x <= width/2; x++)
                {
                    for (int z = -width/2; z <= width/2; z++)
                    {
                        float distance = math.sqrt(x * x + z * z);
                        if (distance <= width/2)
                        {
                            chunk.SetVoxel(pos.x + x, pos.y + y, pos.z + z, VoxelType.Clay);
                        }
                    }
                }
            }
        }

        private void PlaceSimpleObject(TerrainChunkV2 chunk, BiomeObject obj, float3 position)
        {
            int3 pos = new int3((int)position.x, (int)position.y, (int)position.z);
            
            for (int x = 0; x < obj.size.x; x++)
            {
                for (int y = 0; y < obj.size.y; y++)
                {
                    for (int z = 0; z < obj.size.z; z++)
                    {
                        chunk.SetVoxel(pos.x + x, pos.y + y, pos.z + z, obj.primaryType);
                    }
                }
            }
        }

        public List<string> GetBiomeObjectsInfo(BiomeType biome)
        {
            List<string> info = new List<string>();
            
            info.Add($"=== Біом: {biome} ===");
            info.Add("");
            
            info.Add("ФЛОРА:");
            foreach (var obj in ukrainianObjects)
            {
                if (IsObjectAllowedInBiome(obj, biome))
                {
                    info.Add($"• {obj.name} (шанс: {obj.spawnChance:P0})");
                }
            }
            
            info.Add("");
            info.Add("СТРУКТУРИ:");
            foreach (var obj in kozakStructures)
            {
                if (IsObjectAllowedInBiome(obj, biome))
                {
                    info.Add($"• {obj.name} (шанс: {obj.spawnChance:P1})");
                }
            }
            
            info.Add("");
            info.Add("РЕСУРСИ:");
            foreach (var obj in resourceDeposits)
            {
                if (IsObjectAllowedInBiome(obj, biome))
                {
                    info.Add($"• {obj.name} (шанс: {obj.spawnChance:P0})");
                }
            }
            
            return info;
        }
    }
} 