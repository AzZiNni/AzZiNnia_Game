using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace Voxel
{
    /// <summary>
    /// Система генерації українських річок з реалістичною течією та рибалкою
    /// Включає основні річки України: Дніпро, Дністер, Південний Буг, Сіверський Донець
    /// </summary>
    public class UkrainianRiverSystem : MonoBehaviour
    {
        [Header("Основні річки України")]
        [SerializeField] private bool generateDnipro = true;
        [SerializeField] private bool generateDniester = true;
        [SerializeField] private bool generateSouthernBug = true;
        [SerializeField] private bool generateSiverskyDonets = true;
        
        [Header("Параметри річок")]
        [SerializeField] private float riverWidth = 20f;
        [SerializeField] private float riverDepth = 5f;
        [SerializeField] private float riverFlowSpeed = 2f;
        [SerializeField] private int riverSegmentLength = 50;
        [SerializeField] private float riverCurvature = 0.3f;
        
        [Header("Притоки")]
        [SerializeField] private bool generateTributaries = true;
        [SerializeField] private float tributaryWidth = 8f;
        [SerializeField] private float tributaryDepth = 2f;
        [SerializeField] private float tributaryLength = 200f;
        
        [Header("Рибалка")]
        [SerializeField] private bool enableFishing = true;
        [SerializeField] private GameObject[] fishPrefabs;
        [SerializeField] private float fishSpawnRate = 0.1f;
        
        [Header("Візуальні ефекти")]
        [SerializeField] private Material riverMaterial;
        [SerializeField] private Material riverBedMaterial;
        
        [Header("Фізика води")]
        [SerializeField] private bool enableWaterPhysics = true;
        [SerializeField] private float waterFlowForce = 5f;
        
        // Приватні змінні
        private VoxelTerrain terrain;
        private UkrainianTerrainGenerator terrainGenerator;
        private Dictionary<string, RiverData> rivers;
        private List<RiverSegment> allRiverSegments;
        private List<GameObject> fishObjects;
        
        // Дані річок
        public struct RiverData
        {
            public string name;
            public Vector3 source;
            public Vector3 mouth;
            public float width;
            public float depth;
            public List<Vector3> mainPath;
            public List<RiverTributary> tributaries;
            public RiverType type;
        }
        
        public struct RiverTributary
        {
            public Vector3 junction;
            public List<Vector3> path;
            public float width;
            public string name;
        }
        
        public struct RiverSegment
        {
            public Vector3 start;
            public Vector3 end;
            public float width;
            public float depth;
            public Vector3 flowDirection;
            public float flowSpeed;
            public string riverName;
            public bool canFish;
            public bool hasShallows;
        }
        
        public enum RiverType
        {
            Major,      // Основна річка (Дніпро, Дністер)
            Secondary,  // Вторинна річка (Десна, Прип'ять)
            Tributary,  // Притока
            Stream      // Струмок
        }
        
        void Start()
        {
            terrain = GetComponent<VoxelTerrain>();
            terrainGenerator = GetComponent<UkrainianTerrainGenerator>();
            
            InitializeRiverSystem();
            GenerateUkrainianRivers();
            
            if (enableFishing)
            {
                SpawnFish();
            }
            
            Debug.Log("🌊 Система українських річок ініціалізована");
        }
        
        void InitializeRiverSystem()
        {
            rivers = new Dictionary<string, RiverData>();
            allRiverSegments = new List<RiverSegment>();
            fishObjects = new List<GameObject>();
            
            // Створюємо матеріали якщо не призначені
            if (riverMaterial == null)
            {
                riverMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                riverMaterial.color = new Color(0.2f, 0.4f, 0.8f, 0.7f);
                riverMaterial.SetFloat("_Metallic", 0.1f);
                riverMaterial.SetFloat("_Smoothness", 0.9f);
            }
            
            if (riverBedMaterial == null)
            {
                riverBedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                riverBedMaterial.color = new Color(0.4f, 0.3f, 0.2f);
            }
        }
        
        void GenerateUkrainianRivers()
        {
            // Генеруємо основні річки України
            if (generateDnipro) GenerateDnipro();
            if (generateDniester) GenerateDniester();
            if (generateSouthernBug) GenerateSouthernBug();
            if (generateSiverskyDonets) GenerateSiverskyDonets();
        }
        
        void GenerateDnipro()
        {
            // Дніпро - найбільша річка України
            Vector3 source = new Vector3(0, 100, 1000);  // Північ
            Vector3 mouth = new Vector3(200, 0, -1000);  // Південь
            
            List<Vector3> mainPath = GenerateRiverPath(source, mouth, 30f, 2000f);
            
            RiverData dnipro = new RiverData
            {
                name = "Дніпро",
                source = source,
                mouth = mouth,
                width = 40f,
                depth = 8f,
                mainPath = mainPath,
                tributaries = new List<RiverTributary>(),
                type = RiverType.Major
            };
            
            rivers["Дніпро"] = dnipro;
            CreateRiverMesh(dnipro);
            
            Debug.Log("🌊 Згенеровано річку Дніпро");
        }
        
        void GenerateDniester()
        {
            // Дністер - річка на заході України
            Vector3 source = new Vector3(-800, 80, 600);  // Карпати
            Vector3 mouth = new Vector3(-600, 0, -400);   // Чорне море
            
            List<Vector3> mainPath = GenerateRiverPath(source, mouth, 25f, 1200f);
            
            RiverData dniester = new RiverData
            {
                name = "Дністер",
                source = source,
                mouth = mouth,
                width = 30f,
                depth = 6f,
                mainPath = mainPath,
                tributaries = new List<RiverTributary>(),
                type = RiverType.Major
            };
            
            rivers["Дністер"] = dniester;
            CreateRiverMesh(dniester);
            
            Debug.Log("🌊 Згенеровано річку Дністер");
        }
        
        void GenerateSouthernBug()
        {
            // Південний Буг - річка центральної України
            Vector3 source = new Vector3(-200, 60, 300);  // Подільська височина
            Vector3 mouth = new Vector3(-100, 0, -600);   // Чорне море
            
            List<Vector3> mainPath = GenerateRiverPath(source, mouth, 20f, 900f);
            
            RiverData southernBug = new RiverData
            {
                name = "Південний Буг",
                source = source,
                mouth = mouth,
                width = 25f,
                depth = 5f,
                mainPath = mainPath,
                tributaries = new List<RiverTributary>(),
                type = RiverType.Secondary
            };
            
            rivers["Південний Буг"] = southernBug;
            CreateRiverMesh(southernBug);
            
            Debug.Log("🌊 Згенеровано річку Південний Буг");
        }
        
        void GenerateSiverskyDonets()
        {
            // Сіверський Донець - річка на сході України
            Vector3 source = new Vector3(600, 70, 400);   // Середньоруська височина
            Vector3 mouth = new Vector3(800, 0, -200);    // Дон
            
            List<Vector3> mainPath = GenerateRiverPath(source, mouth, 22f, 1000f);
            
            RiverData siverskyDonets = new RiverData
            {
                name = "Сіверський Донець",
                source = source,
                mouth = mouth,
                width = 28f,
                depth = 6f,
                mainPath = mainPath,
                tributaries = new List<RiverTributary>(),
                type = RiverType.Secondary
            };
            
            rivers["Сіверський Донець"] = siverskyDonets;
            CreateRiverMesh(siverskyDonets);
            
            Debug.Log("🌊 Згенеровано річку Сіверський Донець");
        }
        
        List<Vector3> GenerateRiverPath(Vector3 source, Vector3 mouth, float curvature, float totalLength)
        {
            List<Vector3> path = new List<Vector3>();
            
            int segments = Mathf.RoundToInt(totalLength / riverSegmentLength);
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                
                // Основна лінія від джерела до гирла
                Vector3 basePoint = Vector3.Lerp(source, mouth, t);
                
                // Додаємо природні вигини
                float noiseX = Mathf.PerlinNoise(t * 3f, 0f) - 0.5f;
                float noiseZ = Mathf.PerlinNoise(0f, t * 3f) - 0.5f;
                
                Vector3 curvatureOffset = new Vector3(noiseX, 0, noiseZ) * curvature * 100f;
                
                // Зменшуємо вигини біля джерела та гирла
                float curvatureMultiplier = Mathf.Sin(t * Mathf.PI);
                curvatureOffset *= curvatureMultiplier;
                
                Vector3 finalPoint = basePoint + curvatureOffset;
                
                // Коригуємо висоту на основі рельєфу
                if (terrainGenerator != null)
                {
                    float terrainHeight = terrainGenerator.GenerateHeight(new float3(finalPoint.x, 0, finalPoint.z));
                    finalPoint.y = terrainHeight - riverDepth;
                }
                
                path.Add(finalPoint);
            }
            
            return path;
        }
        
        void CreateRiverMesh(RiverData river)
        {
            // Створюємо GameObject для річки
            GameObject riverObj = new GameObject($"River_{river.name}");
            riverObj.transform.SetParent(transform);
            
            // Створюємо меш для основного русла
            CreateRiverSegmentMesh(riverObj, river.mainPath, river.width, river.depth, river.name);
        }
        
        void CreateRiverSegmentMesh(GameObject parent, List<Vector3> path, float width, float depth, string riverName)
        {
            if (path.Count < 2) return;
            
            // Створюємо меш для водної поверхні
            GameObject waterSurface = new GameObject("WaterSurface");
            waterSurface.transform.SetParent(parent.transform);
            
            MeshFilter meshFilter = waterSurface.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = waterSurface.AddComponent<MeshRenderer>();
            meshRenderer.material = riverMaterial;
            
            // Генеруємо вершини та трикутники
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 point = path[i];
                Vector3 direction = Vector3.forward;
                
                if (i < path.Count - 1)
                {
                    direction = (path[i + 1] - point).normalized;
                }
                else if (i > 0)
                {
                    direction = (point - path[i - 1]).normalized;
                }
                
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
                
                // Вершини для водної поверхні
                Vector3 leftSurface = point + perpendicular * width * 0.5f;
                Vector3 rightSurface = point - perpendicular * width * 0.5f;
                
                vertices.Add(leftSurface);
                vertices.Add(rightSurface);
                
                // UV координати
                float uvY = (float)i / (path.Count - 1);
                uvs.Add(new Vector2(0, uvY));
                uvs.Add(new Vector2(1, uvY));
                
                // Трикутники
                if (i < path.Count - 1)
                {
                    int baseIndex = i * 2;
                    
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 1);
                    
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);
                    
                    // Створюємо дані сегменту
                    RiverSegment segment = new RiverSegment
                    {
                        start = point,
                        end = path[i + 1],
                        width = width,
                        depth = depth,
                        flowDirection = direction,
                        flowSpeed = riverFlowSpeed,
                        riverName = riverName,
                        canFish = enableFishing,
                        hasShallows = false
                    };
                    
                    allRiverSegments.Add(segment);
                }
            }
            
            // Створюємо меш
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
            
            // Додаємо колайдер для води
            if (enableWaterPhysics)
            {
                BoxCollider waterCollider = waterSurface.AddComponent<BoxCollider>();
                waterCollider.isTrigger = true;
            }
        }
        
        void SpawnFish()
        {
            if (fishPrefabs == null || fishPrefabs.Length == 0) return;
            
            foreach (var segment in allRiverSegments)
            {
                if (!segment.canFish) continue;
                
                if (UnityEngine.Random.value < fishSpawnRate)
                {
                    Vector3 fishPosition = Vector3.Lerp(segment.start, segment.end, UnityEngine.Random.value);
                    fishPosition.y -= segment.depth * 0.5f;
                    
                    GameObject fishPrefab = fishPrefabs[UnityEngine.Random.Range(0, fishPrefabs.Length)];
                    GameObject fish = Instantiate(fishPrefab, fishPosition, Quaternion.identity);
                    fish.transform.SetParent(transform);
                    
                    fish.transform.rotation = Quaternion.LookRotation(segment.flowDirection);
                    fishObjects.Add(fish);
                }
            }
            
            Debug.Log($"🐟 Заспавнено {fishObjects.Count} риб у річках");
        }
        
        // Публічні методи
        public bool IsPositionInRiver(Vector3 position, out RiverSegment segment)
        {
            segment = default(RiverSegment);
            
            foreach (var riverSegment in allRiverSegments)
            {
                Vector3 segmentCenter = (riverSegment.start + riverSegment.end) * 0.5f;
                float distanceToSegment = Vector3.Distance(position, segmentCenter);
                
                if (distanceToSegment < riverSegment.width * 0.5f)
                {
                    segment = riverSegment;
                    return true;
                }
            }
            
            return false;
        }
        
        public Vector3 GetWaterFlowDirection(Vector3 position)
        {
            if (IsPositionInRiver(position, out RiverSegment segment))
            {
                return segment.flowDirection;
            }
            return Vector3.zero;
        }
        
        public float GetWaterFlowSpeed(Vector3 position)
        {
            if (IsPositionInRiver(position, out RiverSegment segment))
            {
                return segment.flowSpeed;
            }
            return 0f;
        }
        
        public bool CanFishAtPosition(Vector3 position)
        {
            if (IsPositionInRiver(position, out RiverSegment segment))
            {
                return segment.canFish;
            }
            return false;
        }
        
        public List<string> GetRiverNames()
        {
            return rivers.Keys.ToList();
        }
        
        void OnDrawGizmosSelected()
        {
            if (rivers == null) return;
            
            foreach (var river in rivers.Values)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < river.mainPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(river.mainPath[i], river.mainPath[i + 1]);
                }
            }
        }
    }
} 