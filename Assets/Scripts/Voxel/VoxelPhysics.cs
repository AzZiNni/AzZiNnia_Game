#pragma warning disable 0414

using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

namespace Voxel
{
    public enum VoxelPhysicsType
    {
        Solid,      // Звичайні блоки
        Granular,   // Сипучі (пісок, гравій)
        Liquid,     // Рідини (вода, лава)
        Powder      // Дрібні частинки (сніг, попіл)
    }

    public class VoxelPhysics : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float granularFallSpeed = 5f;
        [SerializeField] private float liquidFlowSpeed = 3f;
        [SerializeField] private float particleSize = 0.1f;
        [SerializeField] private int maxParticlesPerVoxel = 8;
        [SerializeField] private float stabilityCheckRadius = 2f;
        
        [Header("Performance")]
        [SerializeField] private int maxActiveParticles = 10000;
        [SerializeField] private float updateInterval = 0.05f;
        
        private VoxelTerrain terrain;
        private Queue<VoxelParticle> particlePool;
        private List<VoxelParticle> activeParticles;
        private float nextUpdateTime;
        
        private struct VoxelParticle
        {
            public Vector3 position;
            public Vector3 velocity;
            public VoxelType type;
            public float lifeTime;
            public GameObject gameObject;
        }
        
        private void Awake()
        {
            terrain = GetComponent<VoxelTerrain>();
            particlePool = new Queue<VoxelParticle>();
            activeParticles = new List<VoxelParticle>();
            
            // Створюємо пул частинок
            for (int i = 0; i < maxActiveParticles; i++)
            {
                GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                particle.transform.localScale = Vector3.one * particleSize;
                particle.SetActive(false);
                
                VoxelParticle vp = new VoxelParticle
                {
                    gameObject = particle
                };
                particlePool.Enqueue(vp);
            }
        }
        
        private void Update()
        {
            if (Time.time < nextUpdateTime) return;
            nextUpdateTime = Time.time + updateInterval;
            
            UpdateParticles();
            CheckVoxelStability();
        }
        
        public void OnVoxelDestroyed(Vector3Int voxelPos, VoxelType voxelType)
        {
            // Перевіряємо фізичний тип вокселя
            VoxelPhysicsType physicsType = GetPhysicsType(voxelType);
            
            switch (physicsType)
            {
                case VoxelPhysicsType.Granular:
                    SpawnGranularParticles(voxelPos, voxelType);
                    break;
                case VoxelPhysicsType.Liquid:
                    StartLiquidFlow(voxelPos, voxelType);
                    break;
                case VoxelPhysicsType.Powder:
                    SpawnPowderParticles(voxelPos, voxelType);
                    break;
            }
            
            // Перевіряємо стабільність сусідніх блоків
            CheckNeighborStability(voxelPos);
        }
        
        private void SpawnGranularParticles(Vector3Int voxelPos, VoxelType voxelType)
        {
            int particleCount = UnityEngine.Random.Range(4, maxParticlesPerVoxel);
            Vector3 worldPos = voxelPos + Vector3.one * 0.5f;
            
            for (int i = 0; i < particleCount && particlePool.Count > 0; i++)
            {
                VoxelParticle particle = particlePool.Dequeue();
                particle.position = worldPos + UnityEngine.Random.insideUnitSphere * 0.3f;
                particle.velocity = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-0.5f, 0f),
                    UnityEngine.Random.Range(-1f, 1f)
                ) * 0.5f;
                particle.type = voxelType;
                particle.lifeTime = 0f;
                particle.gameObject.SetActive(true);
                particle.gameObject.transform.position = particle.position;
                
                // Встановлюємо матеріал частинки
                SetParticleMaterial(particle.gameObject, voxelType);
                
                activeParticles.Add(particle);
            }
        }
        
        private void SpawnPowderParticles(Vector3Int voxelPos, VoxelType voxelType)
        {
            int particleCount = UnityEngine.Random.Range(6, maxParticlesPerVoxel);
            Vector3 worldPos = voxelPos + Vector3.one * 0.5f;
            
            for (int i = 0; i < particleCount && particlePool.Count > 0; i++)
            {
                VoxelParticle particle = particlePool.Dequeue();
                particle.position = worldPos + UnityEngine.Random.insideUnitSphere * 0.4f;
                particle.velocity = new Vector3(
                    UnityEngine.Random.Range(-2f, 2f),
                    UnityEngine.Random.Range(-1f, 0.5f),
                    UnityEngine.Random.Range(-2f, 2f)
                ) * 0.3f;
                particle.type = voxelType;
                particle.lifeTime = 0f;
                particle.gameObject.SetActive(true);
                particle.gameObject.transform.position = particle.position;
                particle.gameObject.transform.localScale = Vector3.one * particleSize * 0.7f;
                
                SetParticleMaterial(particle.gameObject, voxelType);
                activeParticles.Add(particle);
            }
        }
        
        private void UpdateParticles()
        {
            List<VoxelParticle> toRemove = new List<VoxelParticle>();
            
            for (int i = 0; i < activeParticles.Count; i++)
            {
                VoxelParticle particle = activeParticles[i];
                
                // Фізика частинки
                particle.velocity.y -= 9.81f * Time.deltaTime;
                particle.position += particle.velocity * Time.deltaTime;
                particle.lifeTime += Time.deltaTime;
                
                // Перевірка колізії з землею
                Vector3Int voxelPos = Vector3Int.FloorToInt(particle.position);
                if (terrain.GetVoxelValue(voxelPos.x, voxelPos.y, voxelPos.z) > 0)
                {
                    // Частинка досягла твердої поверхні
                    if (particle.lifeTime > 0.5f)
                    {
                        // Намагаємося розмістити воксель
                        TryPlaceVoxel(particle.position, particle.type);
                        toRemove.Add(particle);
                    }
                    else
                    {
                        // Відскок
                        particle.velocity.y = Mathf.Abs(particle.velocity.y) * 0.3f;
                        particle.velocity.x *= 0.7f;
                        particle.velocity.z *= 0.7f;
                        particle.position.y = voxelPos.y + 1f;
                    }
                }
                
                // Оновлюємо позицію GameObject
                particle.gameObject.transform.position = particle.position;
                activeParticles[i] = particle;
                
                // Видаляємо старі частинки
                if (particle.lifeTime > 5f || particle.position.y < -10f)
                {
                    toRemove.Add(particle);
                }
            }
            
            // Повертаємо частинки в пул
            foreach (var particle in toRemove)
            {
                particle.gameObject.SetActive(false);
                activeParticles.Remove(particle);
                particlePool.Enqueue(particle);
            }
        }
        
        private void CheckNeighborStability(Vector3Int centerPos)
        {
            int radius = Mathf.CeilToInt(stabilityCheckRadius);
            
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;
                        
                        Vector3Int checkPos = centerPos + new Vector3Int(x, y, z);
                        VoxelType voxelType = terrain.GetVoxelType(new float3(checkPos.x, checkPos.y, checkPos.z));
                        
                        if (voxelType != VoxelType.Air)
                        {
                            VoxelPhysicsType physicsType = GetPhysicsType(voxelType);
                            
                            // Перевіряємо чи блок може впасти
                            if (physicsType == VoxelPhysicsType.Granular || physicsType == VoxelPhysicsType.Powder)
                            {
                                if (!HasSupport(checkPos))
                                {
                                    // Блок падає
                                    terrain.SetVoxel(checkPos.x, checkPos.y, checkPos.z, VoxelType.Air);
                                    SpawnGranularParticles(checkPos, voxelType);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private bool HasSupport(Vector3Int pos)
        {
            // Перевіряємо чи є підтримка знизу
            return terrain.GetVoxelValue(pos.x, pos.y - 1, pos.z) > 0;
        }
        
        private void TryPlaceVoxel(Vector3 position, VoxelType type)
        {
            Vector3Int voxelPos = Vector3Int.FloorToInt(position);
            
            // Шукаємо найближче вільне місце
            for (int y = 0; y >= -3; y--)
            {
                Vector3Int checkPos = voxelPos + Vector3Int.up * y;
                if (terrain.GetVoxelValue(checkPos.x, checkPos.y, checkPos.z) == 0)
                {
                    terrain.SetVoxel(checkPos.x, checkPos.y, checkPos.z, type);
                    break;
                }
            }
        }
        
        private void StartLiquidFlow(Vector3Int startPos, VoxelType liquidType)
        {
            // Це буде реалізовано в наступному класі LiquidSimulation
        }
        
        private VoxelPhysicsType GetPhysicsType(VoxelType voxelType)
        {
            switch (voxelType)
            {
                case VoxelType.Sand:
                case VoxelType.Gravel:
                    return VoxelPhysicsType.Granular;
                    
                case VoxelType.Water:
                case VoxelType.Lava:
                    return VoxelPhysicsType.Liquid;
                    
                case VoxelType.Snow:
                case VoxelType.Ash:
                    return VoxelPhysicsType.Powder;
                    
                default:
                    return VoxelPhysicsType.Solid;
            }
        }
        
        private void SetParticleMaterial(GameObject particle, VoxelType type)
        {
            Renderer renderer = particle.GetComponent<Renderer>();
            if (renderer != null && terrain.terrainMaterial != null)
            {
                renderer.material = terrain.terrainMaterial;
                // Можна додати різні матеріали для різних типів
            }
        }
        
        private void CheckVoxelStability()
        {
            // Періодична перевірка стабільності для всіх активних чанків
            // Реалізується за потреби
        }
    }
    
    [BurstCompile]
    public struct PhysicsUpdateJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> voxelData;
        [ReadOnly] public int dataSize;
        [ReadOnly] public float deltaTime;
        
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> particlePositions;
        public NativeArray<float3> particleVelocities;
        
        public void Execute(int index)
        {
            float3 pos = particlePositions[index];
            float3 vel = particleVelocities[index];
            
            // Гравітація
            vel.y -= 9.81f * deltaTime;
            
            // Оновлення позиції
            pos += vel * deltaTime;
            
            particlePositions[index] = pos;
            particleVelocities[index] = vel;
        }
    }
} 