using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Core;

namespace SwarmAI.Utils
{
    /// <summary>
    /// Job System implementation for high-performance swarm calculations
    /// Use this for large swarms (1000+ agents) to utilize CPU parallelization
    /// </summary>
    public static class SwarmJobSystem
    {
        /// <summary>
        /// Parallel job for calculating swarm steering forces
        /// </summary>
        [BurstCompile]
        public struct SwarmSteeringJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> positions;
            [ReadOnly] public NativeArray<float3> velocities;
            [ReadOnly] public SwarmParameters parameters;
            
            public NativeArray<float3> steeringForces;
            
            public void Execute(int index)
            {
                float3 position = positions[index];
                float3 velocity = velocities[index];
                
                float3 separation = CalculateSeparation(index, position);
                float3 alignment = CalculateAlignment(index, position, velocity);
                float3 cohesion = CalculateCohesion(index, position);
                
                float3 totalForce = separation * parameters.separationWeight +
                                   alignment * parameters.alignmentWeight +
                                   cohesion * parameters.cohesionWeight;
                
                steeringForces[index] = math.clamp(totalForce, -parameters.maxForce, parameters.maxForce);
            }
            
            private float3 CalculateSeparation(int agentIndex, float3 position)
            {
                float3 steer = float3.zero;
                int count = 0;
                
                for (int i = 0; i < positions.Length; i++)
                {
                    if (i == agentIndex) continue;
                    
                    float3 diff = position - positions[i];
                    float distance = math.length(diff);
                    
                    if (distance > 0 && distance < parameters.separationRadius)
                    {
                        float3 normalized = math.normalize(diff);
                        steer += normalized / distance;
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    steer /= count;
                    steer = math.normalize(steer) * parameters.maxSpeed;
                    return steer;
                }
                
                return float3.zero;
            }
            
            private float3 CalculateAlignment(int agentIndex, float3 position, float3 velocity)
            {
                float3 sum = float3.zero;
                int count = 0;
                
                for (int i = 0; i < positions.Length; i++)
                {
                    if (i == agentIndex) continue;
                    
                    float distance = math.distance(position, positions[i]);
                    
                    if (distance < parameters.alignmentRadius)
                    {
                        sum += velocities[i];
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    float3 average = sum / count;
                    average = math.normalize(average) * parameters.maxSpeed;
                    return average - velocity;
                }
                
                return float3.zero;
            }
            
            private float3 CalculateCohesion(int agentIndex, float3 position)
            {
                float3 centerOfMass = float3.zero;
                int count = 0;
                
                for (int i = 0; i < positions.Length; i++)
                {
                    if (i == agentIndex) continue;
                    
                    float distance = math.distance(position, positions[i]);
                    
                    if (distance < parameters.cohesionRadius)
                    {
                        centerOfMass += positions[i];
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    centerOfMass /= count;
                    float3 desired = math.normalize(centerOfMass - position) * parameters.maxSpeed;
                    return desired;
                }
                
                return float3.zero;
            }
        }
        
        /// <summary>
        /// Job for updating agent positions and velocities
        /// </summary>
        [BurstCompile]
        public struct SwarmMovementJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> steeringForces;
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float maxSpeed;
            
            public NativeArray<float3> positions;
            public NativeArray<float3> velocities;
            
            public void Execute(int index)
            {
                float3 velocity = velocities[index];
                float3 force = steeringForces[index];
                
                // Update velocity
                velocity += force * deltaTime;
                velocity = math.normalize(velocity) * math.min(math.length(velocity), maxSpeed);
                
                // Update position
                float3 position = positions[index];
                position += velocity * deltaTime;
                
                velocities[index] = velocity;
                positions[index] = position;
            }
        }
        
        /// <summary>
        /// Job for spatial hash generation
        /// </summary>
        [BurstCompile]
        public struct SpatialHashJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> positions;
            [ReadOnly] public float cellSize;
            [ReadOnly] public float3 worldMin;
            
            public NativeArray<int> spatialHashes;
            
            public void Execute(int index)
            {
                float3 localPos = positions[index] - worldMin;
                int3 gridPos = new int3(
                    (int)(localPos.x / cellSize),
                    (int)(localPos.y / cellSize),
                    (int)(localPos.z / cellSize)
                );
                
                spatialHashes[index] = gridPos.x * 73856093 ^ gridPos.y * 19349663 ^ gridPos.z * 83492791;
            }
        }
        
        /// <summary>
        /// Boundary enforcement job
        /// </summary>
        [BurstCompile]
        public struct BoundaryEnforcementJob : IJobParallelFor
        {
            [ReadOnly] public float3 boundaryMin;
            [ReadOnly] public float3 boundaryMax;
            [ReadOnly] public float boundaryForce;
            
            public NativeArray<float3> positions;
            public NativeArray<float3> velocities;
            
            public void Execute(int index)
            {
                float3 position = positions[index];
                float3 velocity = velocities[index];
                float3 force = float3.zero;
                
                // Check boundaries and apply force
                if (position.x < boundaryMin.x) force.x += boundaryForce;
                else if (position.x > boundaryMax.x) force.x -= boundaryForce;
                
                if (position.y < boundaryMin.y) force.y += boundaryForce;
                else if (position.y > boundaryMax.y) force.y -= boundaryForce;
                
                if (position.z < boundaryMin.z) force.z += boundaryForce;
                else if (position.z > boundaryMax.z) force.z -= boundaryForce;
                
                // Apply boundary force to velocity
                velocity += force * 0.016f; // Assume 60 FPS
                velocities[index] = velocity;
            }
        }
    }
    
    /// <summary>
    /// Parameters structure for swarm calculations
    /// </summary>
    [System.Serializable]
    public struct SwarmParameters
    {
        public float separationRadius;
        public float alignmentRadius;
        public float cohesionRadius;
        public float separationWeight;
        public float alignmentWeight;
        public float cohesionWeight;
        public float maxSpeed;
        public float maxForce;
        
        public static SwarmParameters Default => new SwarmParameters
        {
            separationRadius = 2f,
            alignmentRadius = 5f,
            cohesionRadius = 8f,
            separationWeight = 1.5f,
            alignmentWeight = 1f,
            cohesionWeight = 1f,
            maxSpeed = 5f,
            maxForce = 10f
        };
    }
    
    /// <summary>
    /// High-performance swarm manager using Job System
    /// Use this for very large swarms (1000+ agents)
    /// </summary>
    public class JobSystemSwarmManager : MonoBehaviour
    {
        [Header("Swarm Settings")]
        [SerializeField] private int agentCount = 1000;
        [SerializeField] private SwarmParameters swarmParameters = SwarmParameters.Default;
        [SerializeField] private bool enableBoundaries = true;
        [SerializeField] private Bounds boundaries = new Bounds(Vector3.zero, Vector3.one * 50f);
        
        [Header("Performance")]
        [SerializeField] private int jobBatchSize = 32;
        [SerializeField] private bool useJobSystem = true;
        
        [Header("Visualization")]
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private bool createVisualAgents = true;
        [SerializeField] private int maxVisualAgents = 500;
        
        // Native arrays for job system
        private NativeArray<float3> positions;
        private NativeArray<float3> velocities;
        private NativeArray<float3> steeringForces;
        private NativeArray<int> spatialHashes;
        
        // Visual representation
        private Transform[] visualAgents;
        
        // Job handles
        private JobHandle steeringJobHandle;
        private JobHandle movementJobHandle;
        private JobHandle boundaryJobHandle;
        
        private void Start()
        {
            InitializeSwarm();
        }
        
        private void InitializeSwarm()
        {
            // Allocate native arrays
            positions = new NativeArray<float3>(agentCount, Allocator.Persistent);
            velocities = new NativeArray<float3>(agentCount, Allocator.Persistent);
            steeringForces = new NativeArray<float3>(agentCount, Allocator.Persistent);
            spatialHashes = new NativeArray<int>(agentCount, Allocator.Persistent);
            
            // Initialize positions and velocities
            for (int i = 0; i < agentCount; i++)
            {
                Vector3 randomPos = Random.insideUnitSphere * 10f;
                positions[i] = new float3(randomPos.x, randomPos.y, randomPos.z);
                
                Vector3 randomVel = Random.insideUnitSphere * swarmParameters.maxSpeed;
                velocities[i] = new float3(randomVel.x, randomVel.y, randomVel.z);
            }
            
            // Create visual agents if enabled
            if (createVisualAgents && agentPrefab != null)
            {
                CreateVisualAgents();
            }
        }
        
        private void CreateVisualAgents()
        {
            int visualCount = Mathf.Min(agentCount, maxVisualAgents);
            visualAgents = new Transform[visualCount];
            
            for (int i = 0; i < visualCount; i++)
            {
                GameObject agent = Instantiate(agentPrefab);
                agent.name = $"VisualAgent_{i}";
                
                // Remove any SwarmAgent components to avoid conflicts
                SwarmAgent swarmAgent = agent.GetComponent<SwarmAgent>();
                if (swarmAgent != null)
                {
                    DestroyImmediate(swarmAgent);
                }
                
                visualAgents[i] = agent.transform;
                UpdateVisualAgent(i);
            }
        }
        
        private void Update()
        {
            if (!useJobSystem)
            {
                UpdateSwarmCPU();
            }
            else
            {
                UpdateSwarmJobSystem();
            }
            
            // Update visual agents
            if (createVisualAgents && visualAgents != null)
            {
                UpdateVisualAgents();
            }
        }
        
        private void UpdateSwarmJobSystem()
        {
            // Complete previous frame's jobs
            steeringJobHandle.Complete();
            movementJobHandle.Complete();
            boundaryJobHandle.Complete();
            
            // Schedule steering calculation job
            var steeringJob = new SwarmJobSystem.SwarmSteeringJob
            {
                positions = positions,
                velocities = velocities,
                parameters = swarmParameters,
                steeringForces = steeringForces
            };
            steeringJobHandle = steeringJob.Schedule(agentCount, jobBatchSize);
            
            // Schedule movement job (depends on steering)
            var movementJob = new SwarmJobSystem.SwarmMovementJob
            {
                steeringForces = steeringForces,
                deltaTime = Time.deltaTime,
                maxSpeed = swarmParameters.maxSpeed,
                positions = positions,
                velocities = velocities
            };
            movementJobHandle = movementJob.Schedule(agentCount, jobBatchSize, steeringJobHandle);
            
            // Schedule boundary enforcement if enabled
            if (enableBoundaries)
            {
                var boundaryJob = new SwarmJobSystem.BoundaryEnforcementJob
                {
                    boundaryMin = new float3(boundaries.min.x, boundaries.min.y, boundaries.min.z),
                    boundaryMax = new float3(boundaries.max.x, boundaries.max.y, boundaries.max.z),
                    boundaryForce = 15f,
                    positions = positions,
                    velocities = velocities
                };
                boundaryJobHandle = boundaryJob.Schedule(agentCount, jobBatchSize, movementJobHandle);
            }
        }
        
        private void UpdateSwarmCPU()
        {
            // Fallback CPU implementation for comparison
            for (int i = 0; i < agentCount; i++)
            {
                Vector3 pos = new Vector3(positions[i].x, positions[i].y, positions[i].z);
                Vector3 vel = new Vector3(velocities[i].x, velocities[i].y, velocities[i].z);
                
                // Simple flocking calculation
                Vector3 force = CalculateSimpleFlocking(i, pos, vel);
                vel += force * Time.deltaTime;
                vel = Vector3.ClampMagnitude(vel, swarmParameters.maxSpeed);
                pos += vel * Time.deltaTime;
                
                velocities[i] = new float3(vel.x, vel.y, vel.z);
                positions[i] = new float3(pos.x, pos.y, pos.z);
            }
        }
        
        private Vector3 CalculateSimpleFlocking(int index, Vector3 position, Vector3 velocity)
        {
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int neighborCount = 0;
            
            for (int i = 0; i < agentCount; i++)
            {
                if (i == index) continue;
                
                Vector3 otherPos = new Vector3(positions[i].x, positions[i].y, positions[i].z);
                float distance = Vector3.Distance(position, otherPos);
                
                if (distance < swarmParameters.cohesionRadius)
                {
                    if (distance < swarmParameters.separationRadius && distance > 0)
                    {
                        Vector3 diff = (position - otherPos).normalized;
                        separation += diff / distance;
                    }
                    
                    Vector3 otherVel = new Vector3(velocities[i].x, velocities[i].y, velocities[i].z);
                    alignment += otherVel;
                    cohesion += otherPos;
                    neighborCount++;
                }
            }
            
            if (neighborCount > 0)
            {
                alignment /= neighborCount;
                cohesion = (cohesion / neighborCount) - position;
            }
            
            return separation * swarmParameters.separationWeight +
                   alignment * swarmParameters.alignmentWeight +
                   cohesion * swarmParameters.cohesionWeight;
        }
        
        private void UpdateVisualAgents()
        {
            for (int i = 0; i < visualAgents.Length; i++)
            {
                UpdateVisualAgent(i);
            }
        }
        
        private void UpdateVisualAgent(int index)
        {
            if (index < positions.Length && visualAgents[index] != null)
            {
                Vector3 position = new Vector3(positions[index].x, positions[index].y, positions[index].z);
                Vector3 velocity = new Vector3(velocities[index].x, velocities[index].y, velocities[index].z);
                
                visualAgents[index].position = position;
                
                if (velocity.magnitude > 0.1f)
                {
                    visualAgents[index].forward = velocity.normalized;
                }
            }
        }
        
        private void OnDestroy()
        {
            // Complete any running jobs
            if (steeringJobHandle.IsCompleted == false) steeringJobHandle.Complete();
            if (movementJobHandle.IsCompleted == false) movementJobHandle.Complete();
            if (boundaryJobHandle.IsCompleted == false) boundaryJobHandle.Complete();
            
            // Dispose native arrays
            if (positions.IsCreated) positions.Dispose();
            if (velocities.IsCreated) velocities.Dispose();
            if (steeringForces.IsCreated) steeringForces.Dispose();
            if (spatialHashes.IsCreated) spatialHashes.Dispose();
        }
        
        private void OnDrawGizmos()
        {
            if (enableBoundaries)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(boundaries.center, boundaries.size);
            }
        }
        
        public int GetAgentCount() => agentCount;
        public bool IsUsingJobSystem() => useJobSystem;
        public SwarmParameters GetParameters() => swarmParameters;
    }
}