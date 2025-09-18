using UnityEngine;
using System.Collections.Generic;

namespace Voxel
{
    public enum VoxelType : byte
    {
        Air = 0,
        
        // Грунт і камінь
        Dirt = 1,
        Grass = 2,
        Stone = 3,
        Cobblestone = 4,
        Bedrock = 5,
        
        // Сипучі матеріали
        Sand = 10,
        RedSand = 11,
        Gravel = 12,
        Clay = 13,
        
        // Руди та мінерали
        CoalOre = 20,
        IronOre = 21,
        GoldOre = 22,
        DiamondOre = 23,
        EmeraldOre = 24,
        RedstoneOre = 25,
        LapisOre = 26,
        CopperOre = 27,
        
        // Дерево
        OakLog = 30,
        BirchLog = 31,
        SpruceLog = 32,
        JungleLog = 33,
        AcaciaLog = 34,
        DarkOakLog = 35,
        
        // Листя
        OakLeaves = 40,
        BirchLeaves = 41,
        SpruceLeaves = 42,
        JungleLeaves = 43,
        AcaciaLeaves = 44,
        DarkOakLeaves = 45,
        
        // Рідини
        Water = 50,
        Lava = 51,
        Oil = 52,
        
        // Порошкоподібні
        Snow = 60,
        Ash = 61,
        Dust = 62,
        
        // Спеціальні блоки
        Glass = 70,
        Ice = 71,
        PackedIce = 72,
        Obsidian = 73,
        
        // Декоративні
        Brick = 80,
        StoneBrick = 81,
        Concrete = 82,
        Terracotta = 83,
        
        // Рослинність
        TallGrass = 90,
        Flower = 91,
        Mushroom = 92,
        
        // Інші
        Wool = 100,
        Sponge = 101,
        TNT = 102,
        
        // Ukrainian Flora - Trees
        Oak_Log = 110,
        Oak_Leaves = 111,
        Oak_Sapling = 112,
        Birch_Log = 113,
        Birch_Leaves = 114,
        Willow_Log = 115,
        Willow_Leaves = 116,
        Spruce_Log = 117,
        Spruce_Leaves = 118,
        Juniper_Log = 119,
        Juniper_Leaves = 120,
        
        // Ukrainian Flora - Bushes and Plants
        Viburnum_Log = 130,
        Viburnum_Leaves = 131,
        Viburnum_Berries = 132,
        Feather_Grass = 133,
        Wormwood = 134,
        Reed = 135,
        
        // Ukrainian Flora - Flowers
        Sunflower = 140,
        Poppy = 141,
        Cornflower = 142,
        Flower_Red = 143,
        Flower_Blue = 144,
        Flower_Yellow = 145,
        
        // Ukrainian Flora - Crops
        Wheat = 150,
        Rye = 151,
        Barley = 152,
        Oats = 153,
        Buckwheat = 154,
        
        // Construction Materials
        Clay_Bricks = 160,
        Thatch = 161,
        Weathered_Stone = 162,
        Weathered_Rock = 163,
        Oak_Planks = 164,
        Birch_Planks = 165,
        
        // Soil types
        Rich_Soil = 170,
        Sandy_Soil = 171,
        Leaf_Litter = 172,
        Peat = 173,
        
        // Kozak-specific materials
        Bog_Iron = 180
    }
    
    [System.Serializable]
    public class VoxelTypeData
    {
        public string name;
        public Color color;
        public float hardness; // Час руйнування
        public bool isTransparent;
        public bool isSolid;
        public bool isFlammable;
        public float density; // Для фізики
        public VoxelPhysicsType physicsType;
        public Material material;
        public Texture2D texture;
        public Vector2 textureCoordinates; // UV координати в атласі текстур
        
        // Звукові ефекти
        public AudioClip breakSound;
        public AudioClip placeSound;
        public AudioClip stepSound;
    }
    
    public class VoxelTypeManager : MonoBehaviour
    {
        private static VoxelTypeManager instance;
        public static VoxelTypeManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<VoxelTypeManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("VoxelTypeManager");
                        instance = go.AddComponent<VoxelTypeManager>();
                    }
                }
                return instance;
            }
        }
        
        [SerializeField] private List<VoxelTypeData> voxelTypes = new List<VoxelTypeData>();
        private Dictionary<VoxelType, VoxelTypeData> voxelTypeDict;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            InitializeVoxelTypes();
        }
        
        private void InitializeVoxelTypes()
        {
            voxelTypeDict = new Dictionary<VoxelType, VoxelTypeData>();
            
            // Ініціалізуємо базові типи вокселів
            AddVoxelType(VoxelType.Dirt, new VoxelTypeData
            {
                name = "Dirt",
                color = new Color(0.478f, 0.290f, 0.141f),
                hardness = 0.5f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 1.5f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Grass, new VoxelTypeData
            {
                name = "Grass",
                color = new Color(0.239f, 0.561f, 0.125f),
                hardness = 0.6f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 1.4f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Stone, new VoxelTypeData
            {
                name = "Stone",
                color = new Color(0.5f, 0.5f, 0.5f),
                hardness = 1.5f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 2.6f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Sand, new VoxelTypeData
            {
                name = "Sand",
                color = new Color(0.761f, 0.698f, 0.502f),
                hardness = 0.5f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 1.6f,
                physicsType = VoxelPhysicsType.Granular
            });
            
            AddVoxelType(VoxelType.Water, new VoxelTypeData
            {
                name = "Water",
                color = new Color(0.247f, 0.463f, 0.894f, 0.7f),
                hardness = 100f, // Не можна зруйнувати
                isTransparent = true,
                isSolid = false,
                isFlammable = false,
                density = 1.0f,
                physicsType = VoxelPhysicsType.Liquid
            });
            
            AddVoxelType(VoxelType.Gravel, new VoxelTypeData
            {
                name = "Gravel",
                color = new Color(0.502f, 0.502f, 0.502f),
                hardness = 0.6f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 1.8f,
                physicsType = VoxelPhysicsType.Granular
            });
            
            AddVoxelType(VoxelType.OakLog, new VoxelTypeData
            {
                name = "Oak Log",
                color = new Color(0.411f, 0.322f, 0.184f),
                hardness = 2.0f,
                isTransparent = false,
                isSolid = true,
                isFlammable = true,
                density = 0.7f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.OakLeaves, new VoxelTypeData
            {
                name = "Oak Leaves",
                color = new Color(0.145f, 0.502f, 0.082f, 0.9f),
                hardness = 0.2f,
                isTransparent = true,
                isSolid = true,
                isFlammable = true,
                density = 0.2f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.CoalOre, new VoxelTypeData
            {
                name = "Coal Ore",
                color = new Color(0.3f, 0.3f, 0.3f),
                hardness = 3.0f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 2.5f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.IronOre, new VoxelTypeData
            {
                name = "Iron Ore",
                color = new Color(0.533f, 0.388f, 0.349f),
                hardness = 3.0f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 3.8f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Snow, new VoxelTypeData
            {
                name = "Snow",
                color = Color.white,
                hardness = 0.1f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 0.5f,
                physicsType = VoxelPhysicsType.Powder
            });
            
            AddVoxelType(VoxelType.Clay, new VoxelTypeData
            {
                name = "Clay",
                color = new Color(0.631f, 0.663f, 0.702f),
                hardness = 0.6f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 1.7f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Lava, new VoxelTypeData
            {
                name = "Lava",
                color = new Color(0.812f, 0.361f, 0f, 0.9f),
                hardness = 100f,
                isTransparent = true,
                isSolid = false,
                isFlammable = false,
                density = 3.1f,
                physicsType = VoxelPhysicsType.Liquid
            });
            
            // Ukrainian Flora - Trees
            AddVoxelType(VoxelType.Oak_Log, new VoxelTypeData
            {
                name = "Oak Log",
                color = new Color(0.45f, 0.35f, 0.2f),
                hardness = 2.0f,
                isTransparent = false,
                isSolid = true,
                isFlammable = true,
                density = 0.8f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Oak_Leaves, new VoxelTypeData
            {
                name = "Oak Leaves",
                color = new Color(0.2f, 0.5f, 0.1f, 0.8f),
                hardness = 0.2f,
                isTransparent = true,
                isSolid = true,
                isFlammable = true,
                density = 0.2f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Birch_Log, new VoxelTypeData
            {
                name = "Birch Log",
                color = new Color(0.9f, 0.9f, 0.8f),
                hardness = 1.8f,
                isTransparent = false,
                isSolid = true,
                isFlammable = true,
                density = 0.7f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Birch_Leaves, new VoxelTypeData
            {
                name = "Birch Leaves",
                color = new Color(0.3f, 0.6f, 0.2f, 0.8f),
                hardness = 0.2f,
                isTransparent = true,
                isSolid = true,
                isFlammable = true,
                density = 0.2f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Willow_Log, new VoxelTypeData
            {
                name = "Willow Log",
                color = new Color(0.4f, 0.3f, 0.2f),
                hardness = 1.5f,
                isTransparent = false,
                isSolid = true,
                isFlammable = true,
                density = 0.6f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Willow_Leaves, new VoxelTypeData
            {
                name = "Willow Leaves",
                color = new Color(0.4f, 0.7f, 0.3f, 0.8f),
                hardness = 0.1f,
                isTransparent = true,
                isSolid = true,
                isFlammable = true,
                density = 0.15f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            // Ukrainian Flora - Flowers and Plants
            AddVoxelType(VoxelType.Sunflower, new VoxelTypeData
            {
                name = "Sunflower",
                color = new Color(1f, 0.8f, 0f),
                hardness = 0.1f,
                isTransparent = true,
                isSolid = false,
                isFlammable = true,
                density = 0.1f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Poppy, new VoxelTypeData
            {
                name = "Poppy",
                color = new Color(0.8f, 0.1f, 0.1f),
                hardness = 0.05f,
                isTransparent = true,
                isSolid = false,
                isFlammable = true,
                density = 0.05f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Cornflower, new VoxelTypeData
            {
                name = "Cornflower",
                color = new Color(0.2f, 0.4f, 0.8f),
                hardness = 0.05f,
                isTransparent = true,
                isSolid = false,
                isFlammable = true,
                density = 0.05f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Feather_Grass, new VoxelTypeData
            {
                name = "Feather Grass",
                color = new Color(0.7f, 0.6f, 0.4f),
                hardness = 0.05f,
                isTransparent = true,
                isSolid = false,
                isFlammable = true,
                density = 0.05f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            // Construction Materials
            AddVoxelType(VoxelType.Clay_Bricks, new VoxelTypeData
            {
                name = "Clay Bricks",
                color = new Color(0.7f, 0.4f, 0.3f),
                hardness = 2.5f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 2.0f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Thatch, new VoxelTypeData
            {
                name = "Thatch",
                color = new Color(0.8f, 0.7f, 0.4f),
                hardness = 0.8f,
                isTransparent = false,
                isSolid = true,
                isFlammable = true,
                density = 0.4f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Weathered_Rock, new VoxelTypeData
            {
                name = "Weathered Rock",
                color = new Color(0.6f, 0.55f, 0.5f),
                hardness = 3.0f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 2.4f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            // Additional Ukrainian materials
            AddVoxelType(VoxelType.Oak_Sapling, new VoxelTypeData
            {
                name = "Oak Sapling",
                color = new Color(0.2f, 0.5f, 0.1f),
                hardness = 0.05f,
                isTransparent = true,
                isSolid = false,
                isFlammable = true,
                density = 0.1f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Viburnum_Log, new VoxelTypeData
            {
                name = "Viburnum Log",
                color = new Color(0.5f, 0.3f, 0.2f),
                hardness = 1.2f,
                isTransparent = false,
                isSolid = true,
                isFlammable = true,
                density = 0.6f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Viburnum_Leaves, new VoxelTypeData
            {
                name = "Viburnum Leaves",
                color = new Color(0.3f, 0.6f, 0.2f, 0.8f),
                hardness = 0.1f,
                isTransparent = true,
                isSolid = true,
                isFlammable = true,
                density = 0.15f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Viburnum_Berries, new VoxelTypeData
            {
                name = "Viburnum Berries",
                color = new Color(0.8f, 0.1f, 0.1f),
                hardness = 0.05f,
                isTransparent = true,
                isSolid = false,
                isFlammable = true,
                density = 0.05f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Wormwood, new VoxelTypeData
            {
                name = "Wormwood",
                color = new Color(0.5f, 0.6f, 0.4f),
                hardness = 0.05f,
                isTransparent = true,
                isSolid = false,
                isFlammable = true,
                density = 0.05f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Reed, new VoxelTypeData
            {
                name = "Reed",
                color = new Color(0.4f, 0.5f, 0.3f),
                hardness = 0.1f,
                isTransparent = true,
                isSolid = false,
                isFlammable = true,
                density = 0.1f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Spruce_Log, new VoxelTypeData
            {
                name = "Spruce Log",
                color = new Color(0.3f, 0.25f, 0.15f),
                hardness = 1.8f,
                isTransparent = false,
                isSolid = true,
                isFlammable = true,
                density = 0.65f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Spruce_Leaves, new VoxelTypeData
            {
                name = "Spruce Leaves",
                color = new Color(0.1f, 0.4f, 0.1f, 0.9f),
                hardness = 0.2f,
                isTransparent = true,
                isSolid = true,
                isFlammable = true,
                density = 0.2f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Juniper_Log, new VoxelTypeData
            {
                name = "Juniper Log",
                color = new Color(0.6f, 0.4f, 0.3f),
                hardness = 1.5f,
                isTransparent = false,
                isSolid = true,
                isFlammable = true,
                density = 0.7f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Juniper_Leaves, new VoxelTypeData
            {
                name = "Juniper Leaves",
                color = new Color(0.2f, 0.5f, 0.3f, 0.9f),
                hardness = 0.3f,
                isTransparent = true,
                isSolid = true,
                isFlammable = true,
                density = 0.25f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Bog_Iron, new VoxelTypeData
            {
                name = "Bog Iron",
                color = new Color(0.6f, 0.5f, 0.4f),
                hardness = 1.8f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 2.2f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            // Soil types
            AddVoxelType(VoxelType.Rich_Soil, new VoxelTypeData
            {
                name = "Rich Soil",
                color = new Color(0.3f, 0.2f, 0.1f),
                hardness = 0.4f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 1.3f,
                physicsType = VoxelPhysicsType.Solid
            });
            
            AddVoxelType(VoxelType.Sandy_Soil, new VoxelTypeData
            {
                name = "Sandy Soil",
                color = new Color(0.7f, 0.6f, 0.4f),
                hardness = 0.3f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 1.4f,
                physicsType = VoxelPhysicsType.Granular
            });
            
            AddVoxelType(VoxelType.Bog_Iron, new VoxelTypeData
            {
                name = "Bog Iron",
                color = new Color(0.4f, 0.2f, 0.1f),
                hardness = 2.5f,
                isTransparent = false,
                isSolid = true,
                isFlammable = false,
                density = 3.5f,
                physicsType = VoxelPhysicsType.Solid
            });
        }
        
        private void AddVoxelType(VoxelType type, VoxelTypeData data)
        {
            if (!voxelTypeDict.ContainsKey(type))
            {
                voxelTypeDict.Add(type, data);
                voxelTypes.Add(data);
            }
        }
        
        public VoxelTypeData GetVoxelTypeData(VoxelType type)
        {
            if (voxelTypeDict.TryGetValue(type, out VoxelTypeData data))
            {
                return data;
            }
            return null;
        }
        
        public Color GetVoxelColor(VoxelType type)
        {
            VoxelTypeData data = GetVoxelTypeData(type);
            return data != null ? data.color : Color.magenta;
        }
        
        public float GetVoxelHardness(VoxelType type)
        {
            VoxelTypeData data = GetVoxelTypeData(type);
            return data != null ? data.hardness : 1f;
        }
        
        public bool IsVoxelTransparent(VoxelType type)
        {
            VoxelTypeData data = GetVoxelTypeData(type);
            return data != null ? data.isTransparent : false;
        }
        
        public VoxelPhysicsType GetVoxelPhysicsType(VoxelType type)
        {
            VoxelTypeData data = GetVoxelTypeData(type);
            return data != null ? data.physicsType : VoxelPhysicsType.Solid;
        }
    }
} 