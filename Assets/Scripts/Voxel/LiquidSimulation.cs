using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

namespace Voxel
{
    public class LiquidSimulation : MonoBehaviour
    {
        [Header("Liquid Settings")]
        [SerializeField] private float waterFlowSpeed = 2f;
        [SerializeField] private float lavaFlowSpeed = 0.5f;
        [SerializeField] private int maxFlowDistance = 7;
        [SerializeField] private float evaporationRate = 0.1f;
        [SerializeField] private float updateInterval = 0.1f;
        
        [Header("Performance")]
        [SerializeField] private int maxLiquidUpdatesPerFrame = 100;
        
        private VoxelTerrain terrain;
        private Queue<LiquidUpdate> liquidUpdateQueue;
        private HashSet<Vector3Int> liquidPositions;
        private float nextUpdateTime;
        
        private struct LiquidUpdate
        {
            public Vector3Int position;
            public VoxelType liquidType;
            public int flowLevel;
            public float timestamp;
        }
        
        private void Awake()
        {
            terrain = GetComponent<VoxelTerrain>();
            liquidUpdateQueue = new Queue<LiquidUpdate>();
            liquidPositions = new HashSet<Vector3Int>();
        }
        
        private void Update()
        {
            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + updateInterval;
            
            ProcessLiquidUpdates();
        }
        
        public void AddLiquidSource(Vector3Int position, VoxelType liquidType)
        {
            if (liquidType != VoxelType.Water && liquidType != VoxelType.Lava) return;
            
            liquidUpdateQueue.Enqueue(new LiquidUpdate
            {
                position = position,
                liquidType = liquidType,
                flowLevel = maxFlowDistance,
                timestamp = Time.time
            });
            
            liquidPositions.Add(position);
        }
        
        private void ProcessLiquidUpdates()
        {
            int updatesThisFrame = 0;
            
            while (liquidUpdateQueue.Count > 0 && updatesThisFrame < maxLiquidUpdatesPerFrame)
            {
                LiquidUpdate update = liquidUpdateQueue.Dequeue();
                
                // Перевіряємо чи позиція все ще містить рідину
                VoxelType currentType = terrain.GetVoxelType(new float3(update.position.x, update.position.y, update.position.z));
                if (currentType != update.liquidType) continue;
                
                // Спочатку намагаємося текти вниз
                Vector3Int downPos = update.position + Vector3Int.down;
                if (TryFlowTo(downPos, update.liquidType, update.flowLevel))
                {
                    // Якщо вдалося потекти вниз, продовжуємо течію звідти
                    liquidUpdateQueue.Enqueue(new LiquidUpdate
                    {
                        position = downPos,
                        liquidType = update.liquidType,
                        flowLevel = update.flowLevel,
                        timestamp = Time.time
                    });
                }
                else
                {
                    // Якщо не можемо текти вниз, течемо в сторони
                    FlowSideways(update);
                }
                
                updatesThisFrame++;
            }
        }
        
        private bool TryFlowTo(Vector3Int targetPos, VoxelType liquidType, int flowLevel)
        {
            VoxelType targetType = terrain.GetVoxelType(new float3(targetPos.x, targetPos.y, targetPos.z));
            
            // Можемо текти тільки в повітря або замінити воду лавою
            if (targetType == VoxelType.Air)
            {
                terrain.SetVoxel(targetPos.x, targetPos.y, targetPos.z, liquidType);
                liquidPositions.Add(targetPos);
                return true;
            }
            else if (targetType == VoxelType.Water && liquidType == VoxelType.Lava)
            {
                // Лава перетворює воду на камінь
                terrain.SetVoxel(targetPos.x, targetPos.y, targetPos.z, VoxelType.Cobblestone);
                CreateSteamParticles(targetPos);
                return false;
            }
            else if (targetType == VoxelType.Lava && liquidType == VoxelType.Water)
            {
                // Вода перетворює лаву на обсидіан
                terrain.SetVoxel(targetPos.x, targetPos.y, targetPos.z, VoxelType.Obsidian);
                CreateSteamParticles(targetPos);
                return false;
            }
            
            return false;
        }
        
        private void FlowSideways(LiquidUpdate update)
        {
            if (update.flowLevel <= 0) return;
            
            Vector3Int[] directions = new Vector3Int[]
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1)
            };
            
            // Перемішуємо напрямки для випадкового розтікання
            for (int i = 0; i < directions.Length; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, directions.Length);
                Vector3Int temp = directions[i];
                directions[i] = directions[randomIndex];
                directions[randomIndex] = temp;
            }
            
            foreach (Vector3Int dir in directions)
            {
                Vector3Int targetPos = update.position + dir;
                
                // Перевіряємо чи можемо текти в цьому напрямку
                if (CanFlowSideways(targetPos, update.liquidType))
                {
                    if (TryFlowTo(targetPos, update.liquidType, update.flowLevel - 1))
                    {
                        liquidUpdateQueue.Enqueue(new LiquidUpdate
                        {
                            position = targetPos,
                            liquidType = update.liquidType,
                            flowLevel = update.flowLevel - 1,
                            timestamp = Time.time
                        });
                    }
                }
            }
        }
        
        private bool CanFlowSideways(Vector3Int targetPos, VoxelType liquidType)
        {
            VoxelType targetType = terrain.GetVoxelType(new float3(targetPos.x, targetPos.y, targetPos.z));
            if (targetType != VoxelType.Air && targetType != VoxelType.Water && targetType != VoxelType.Lava)
                return false;
            
            // Перевіряємо чи є твердий блок під цільовою позицією
            Vector3Int belowTarget = targetPos + Vector3Int.down;
            VoxelType belowType = terrain.GetVoxelType(new float3(belowTarget.x, belowTarget.y, belowTarget.z));
            
            // Рідина може текти тільки якщо під нею є твердий блок або інша рідина
            return belowType != VoxelType.Air;
        }
        
        private void CreateSteamParticles(Vector3Int position)
        {
            // Створюємо ефект пари коли вода зустрічається з лавою
            GameObject steamEffect = new GameObject("SteamEffect");
            steamEffect.transform.position = position + Vector3.one * 0.5f;
            
            ParticleSystem particles = steamEffect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 2f;
            main.startLifetime = 1f;
            main.startSpeed = 2f;
            main.maxParticles = 50;
            main.startSize = 0.5f;
            main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;
            
            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, 3f);
            
            Destroy(steamEffect, 3f);
        }
        
        public void RemoveLiquid(Vector3Int position)
        {
            liquidPositions.Remove(position);
        }
        
        public bool IsLiquid(VoxelType type)
        {
            return type == VoxelType.Water || type == VoxelType.Lava;
        }
        
        // Оптимізована версія для обробки великої кількості рідини
        [BurstCompile]
        public struct LiquidFlowJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> voxelData;
            [ReadOnly] public int dataSize;
            [ReadOnly] public float flowSpeed;
            
            [NativeDisableParallelForRestriction]
            public NativeArray<float> newVoxelData;
            
            public void Execute(int index)
            {
                int x = index % dataSize;
                int y = (index / dataSize) % dataSize;
                int z = index / (dataSize * dataSize);
                
                float currentValue = voxelData[index];
                
                // Перевіряємо чи це рідина
                if (currentValue > 0.5f && currentValue < 0.7f) // Діапазон для рідин
                {
                    // Логіка течії рідини
                    int belowIndex = GetIndex(x, y - 1, z, dataSize);
                    if (belowIndex >= 0 && belowIndex < voxelData.Length)
                    {
                        float belowValue = voxelData[belowIndex];
                        if (belowValue < 0.1f) // Повітря
                        {
                            // Рідина тече вниз
                            newVoxelData[belowIndex] = currentValue;
                            newVoxelData[index] = 0f;
                        }
                    }
                }
            }
            
            private int GetIndex(int x, int y, int z, int size)
            {
                if (x < 0 || x >= size || y < 0 || y >= size || z < 0 || z >= size)
                    return -1;
                return x + y * size + z * size * size;
            }
        }
    }
} 