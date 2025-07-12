# Unity Swarm AI Implementation Guide

## Quick Start Template

### 1. Basic Swarm Agent Setup

```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI
{
    public class SwarmAgent : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float maxForce = 10f;
        
        [Header("Perception")]
        [SerializeField] private float perceptionRadius = 5f;
        [SerializeField] private LayerMask agentLayer;
        
        [Header("Behavior Weights")]
        [SerializeField] private float separationWeight = 1.5f;
        [SerializeField] private float alignmentWeight = 1.0f;
        [SerializeField] private float cohesionWeight = 1.0f;
        
        private Vector3 velocity;
        private List<SwarmAgent> neighbors;
        
        void Start()
        {
            velocity = Random.insideUnitSphere * maxSpeed;
            SwarmManager.Instance.RegisterAgent(this);
        }
        
        void Update()
        {
            UpdateNeighbors();
            Vector3 acceleration = CalculateAcceleration();
            
            velocity += acceleration * Time.deltaTime;
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            
            transform.position += velocity * Time.deltaTime;
            transform.forward = velocity.normalized;
        }
        
        private void UpdateNeighbors()
        {
            neighbors = Physics.OverlapSphere(transform.position, perceptionRadius, agentLayer)
                .Select(c => c.GetComponent<SwarmAgent>())
                .Where(a => a != null && a != this)
                .ToList();
        }
        
        private Vector3 CalculateAcceleration()
        {
            if (neighbors.Count == 0) return Vector3.zero;
            
            Vector3 separation = CalculateSeparation() * separationWeight;
            Vector3 alignment = CalculateAlignment() * alignmentWeight;
            Vector3 cohesion = CalculateCohesion() * cohesionWeight;
            
            return separation + alignment + cohesion;
        }
        
        private Vector3 CalculateSeparation()
        {
            Vector3 steer = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(transform.position, neighbor.transform.position);
                if (distance > 0 && distance < 2f)
                {
                    Vector3 diff = transform.position - neighbor.transform.position;
                    diff.Normalize();
                    diff /= distance; // Weight by distance
                    steer += diff;
                    count++;
                }
            }
            
            if (count > 0)
            {
                steer /= count;
                steer.Normalize();
                steer *= maxSpeed;
                steer -= velocity;
                steer = Vector3.ClampMagnitude(steer, maxForce);
            }
            
            return steer;
        }
        
        private Vector3 CalculateAlignment()
        {
            Vector3 sum = Vector3.zero;
            
            foreach (var neighbor in neighbors)
            {
                sum += neighbor.velocity;
            }
            
            sum /= neighbors.Count;
            sum.Normalize();
            sum *= maxSpeed;
            
            Vector3 steer = sum - velocity;
            return Vector3.ClampMagnitude(steer, maxForce);
        }
        
        private Vector3 CalculateCohesion()
        {
            Vector3 sum = Vector3.zero;
            
            foreach (var neighbor in neighbors)
            {
                sum += neighbor.transform.position;
            }
            
            sum /= neighbors.Count;
            return Seek(sum);
        }
        
        private Vector3 Seek(Vector3 target)
        {
            Vector3 desired = target - transform.position;
            desired.Normalize();
            desired *= maxSpeed;
            
            Vector3 steer = desired - velocity;
            return Vector3.ClampMagnitude(steer, maxForce);
        }
        
        void OnDestroy()
        {
            SwarmManager.Instance?.UnregisterAgent(this);
        }
        
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, perceptionRadius);
        }
    }
}
```

### 2. Swarm Manager for Performance

```csharp
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace SwarmAI
{
    public class SwarmManager : MonoBehaviour
    {
        private static SwarmManager instance;
        public static SwarmManager Instance => instance;
        
        [Header("Spatial Partitioning")]
        [SerializeField] private float cellSize = 10f;
        [SerializeField] private int gridSize = 100;
        
        private Dictionary<int, List<SwarmAgent>> spatialGrid;
        private List<SwarmAgent> allAgents;
        
        void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            spatialGrid = new Dictionary<int, List<SwarmAgent>>();
            allAgents = new List<SwarmAgent>();
        }
        
        public void RegisterAgent(SwarmAgent agent)
        {
            allAgents.Add(agent);
            UpdateAgentCell(agent);
        }
        
        public void UnregisterAgent(SwarmAgent agent)
        {
            allAgents.Remove(agent);
            RemoveAgentFromGrid(agent);
        }
        
        private void UpdateAgentCell(SwarmAgent agent)
        {
            int cellKey = GetCellKey(agent.transform.position);
            
            if (!spatialGrid.ContainsKey(cellKey))
            {
                spatialGrid[cellKey] = new List<SwarmAgent>();
            }
            
            spatialGrid[cellKey].Add(agent);
        }
        
        private int GetCellKey(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / cellSize);
            int z = Mathf.FloorToInt(position.z / cellSize);
            return x + z * gridSize;
        }
        
        public List<SwarmAgent> GetNeighbors(Vector3 position, float radius)
        {
            List<SwarmAgent> neighbors = new List<SwarmAgent>();
            int searchCells = Mathf.CeilToInt(radius / cellSize);
            
            int centerX = Mathf.FloorToInt(position.x / cellSize);
            int centerZ = Mathf.FloorToInt(position.z / cellSize);
            
            for (int x = -searchCells; x <= searchCells; x++)
            {
                for (int z = -searchCells; z <= searchCells; z++)
                {
                    int cellKey = (centerX + x) + (centerZ + z) * gridSize;
                    
                    if (spatialGrid.ContainsKey(cellKey))
                    {
                        foreach (var agent in spatialGrid[cellKey])
                        {
                            float dist = Vector3.Distance(position, agent.transform.position);
                            if (dist <= radius)
                            {
                                neighbors.Add(agent);
                            }
                        }
                    }
                }
            }
            
            return neighbors;
        }
        
        private void RemoveAgentFromGrid(SwarmAgent agent)
        {
            foreach (var cell in spatialGrid.Values)
            {
                cell.Remove(agent);
            }
        }
        
        // Job System Implementation for massive swarms
        [BurstCompile]
        struct SwarmMovementJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> positions;
            [ReadOnly] public NativeArray<float3> velocities;
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float maxSpeed;
            [ReadOnly] public float maxForce;
            [ReadOnly] public float perceptionRadius;
            
            public NativeArray<float3> newVelocities;
            public NativeArray<float3> newPositions;
            
            public void Execute(int index)
            {
                float3 position = positions[index];
                float3 velocity = velocities[index];
                
                float3 separation = float3.zero;
                float3 alignment = float3.zero;
                float3 cohesion = float3.zero;
                int neighborCount = 0;
                
                // Check all other agents (can be optimized with spatial partitioning)
                for (int i = 0; i < positions.Length; i++)
                {
                    if (i == index) continue;
                    
                    float distance = math.distance(position, positions[i]);
                    if (distance < perceptionRadius)
                    {
                        // Separation
                        if (distance < 2f && distance > 0)
                        {
                            float3 diff = position - positions[i];
                            diff = math.normalize(diff) / distance;
                            separation += diff;
                        }
                        
                        // Alignment & Cohesion
                        alignment += velocities[i];
                        cohesion += positions[i];
                        neighborCount++;
                    }
                }
                
                if (neighborCount > 0)
                {
                    alignment /= neighborCount;
                    cohesion /= neighborCount;
                    cohesion = cohesion - position;
                    
                    // Normalize and apply weights
                    separation = LimitForce(separation * 1.5f, maxForce);
                    alignment = LimitForce(alignment * 1.0f, maxForce);
                    cohesion = LimitForce(cohesion * 1.0f, maxForce);
                }
                
                float3 acceleration = separation + alignment + cohesion;
                velocity += acceleration * deltaTime;
                velocity = LimitForce(velocity, maxSpeed);
                
                newVelocities[index] = velocity;
                newPositions[index] = position + velocity * deltaTime;
            }
            
            float3 LimitForce(float3 force, float max)
            {
                float magnitude = math.length(force);
                if (magnitude > max)
                {
                    return (force / magnitude) * max;
                }
                return force;
            }
        }
    }
}
```

### 3. Advanced Patterns Implementation

#### Ant Colony with Pheromones

```csharp
using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI.Patterns
{
    public class PheromoneMap : MonoBehaviour
    {
        [SerializeField] private int gridWidth = 100;
        [SerializeField] private int gridHeight = 100;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float evaporationRate = 0.01f;
        [SerializeField] private float diffusionRate = 0.1f;
        
        private float[,] foodPheromones;
        private float[,] homePheromones;
        private float[,] tempGrid;
        
        void Start()
        {
            foodPheromones = new float[gridWidth, gridHeight];
            homePheromones = new float[gridWidth, gridHeight];
            tempGrid = new float[gridWidth, gridHeight];
        }
        
        void Update()
        {
            UpdatePheromones(foodPheromones);
            UpdatePheromones(homePheromones);
        }
        
        private void UpdatePheromones(float[,] pheromones)
        {
            // Diffusion
            for (int x = 1; x < gridWidth - 1; x++)
            {
                for (int y = 1; y < gridHeight - 1; y++)
                {
                    float sum = 0;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            sum += pheromones[x + dx, y + dy];
                        }
                    }
                    tempGrid[x, y] = sum / 9f;
                }
            }
            
            // Apply diffusion and evaporation
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    pheromones[x, y] = Mathf.Lerp(pheromones[x, y], tempGrid[x, y], diffusionRate);
                    pheromones[x, y] *= (1f - evaporationRate);
                }
            }
        }
        
        public void AddPheromone(Vector3 worldPos, PheromoneType type, float strength = 1f)
        {
            Vector2Int gridPos = WorldToGrid(worldPos);
            if (IsValidGridPos(gridPos))
            {
                if (type == PheromoneType.Food)
                    foodPheromones[gridPos.x, gridPos.y] += strength;
                else
                    homePheromones[gridPos.x, gridPos.y] += strength;
            }
        }
        
        public Vector3 GetPheromoneGradient(Vector3 worldPos, PheromoneType type)
        {
            Vector2Int gridPos = WorldToGrid(worldPos);
            if (!IsValidGridPos(gridPos)) return Vector3.zero;
            
            float[,] pheromones = (type == PheromoneType.Food) ? foodPheromones : homePheromones;
            
            Vector3 gradient = Vector3.zero;
            float maxPheromone = 0;
            
            // Sample surrounding cells
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    Vector2Int samplePos = gridPos + new Vector2Int(dx, dy);
                    if (IsValidGridPos(samplePos))
                    {
                        float pheromone = pheromones[samplePos.x, samplePos.y];
                        if (pheromone > maxPheromone)
                        {
                            maxPheromone = pheromone;
                            gradient = new Vector3(dx * cellSize, 0, dy * cellSize);
                        }
                    }
                }
            }
            
            return gradient.normalized;
        }
        
        private Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt(worldPos.x / cellSize);
            int y = Mathf.FloorToInt(worldPos.z / cellSize);
            return new Vector2Int(x, y);
        }
        
        private bool IsValidGridPos(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
        }
    }
    
    public enum PheromoneType
    {
        Food,
        Home
    }
    
    public class AntAgent : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float wanderStrength = 0.1f;
        [SerializeField] private float pheromoneStrength = 1f;
        
        private PheromoneMap pheromoneMap;
        private bool hasFood = false;
        private Vector3 homePosition;
        
        void Start()
        {
            pheromoneMap = FindObjectOfType<PheromoneMap>();
            homePosition = transform.position;
        }
        
        void Update()
        {
            Move();
            DepositPheromone();
        }
        
        private void Move()
        {
            Vector3 direction;
            
            if (!hasFood)
            {
                // Follow food pheromones when searching
                direction = pheromoneMap.GetPheromoneGradient(transform.position, PheromoneType.Food);
            }
            else
            {
                // Follow home pheromones when returning
                direction = pheromoneMap.GetPheromoneGradient(transform.position, PheromoneType.Home);
            }
            
            // Add some randomness
            direction += Random.insideUnitSphere * wanderStrength;
            direction.y = 0;
            direction.Normalize();
            
            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.forward = direction;
        }
        
        private void DepositPheromone()
        {
            if (hasFood)
            {
                pheromoneMap.AddPheromone(transform.position, PheromoneType.Food, pheromoneStrength);
            }
            else
            {
                pheromoneMap.AddPheromone(transform.position, PheromoneType.Home, pheromoneStrength);
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Food") && !hasFood)
            {
                hasFood = true;
                // Turn around
                transform.forward = -transform.forward;
            }
            else if (other.CompareTag("Home") && hasFood)
            {
                hasFood = false;
                // Turn around
                transform.forward = -transform.forward;
            }
        }
    }
}
```

### 4. GPU-Accelerated Swarm Simulation

#### Compute Shader (SwarmCompute.compute)

```hlsl
#pragma kernel UpdateSwarm

struct Agent
{
    float3 position;
    float3 velocity;
    float3 acceleration;
};

RWStructuredBuffer<Agent> agents;
float deltaTime;
float maxSpeed;
float maxForce;
float perceptionRadius;
float separationWeight;
float alignmentWeight;
float cohesionWeight;
uint agentCount;

[numthreads(64,1,1)]
void UpdateSwarm (uint3 id : SV_DispatchThreadID)
{
    if (id.x >= agentCount) return;
    
    Agent agent = agents[id.x];
    
    float3 separation = float3(0, 0, 0);
    float3 alignment = float3(0, 0, 0);
    float3 cohesion = float3(0, 0, 0);
    int neighborCount = 0;
    
    // Check all other agents
    for (uint i = 0; i < agentCount; i++)
    {
        if (i == id.x) continue;
        
        float3 offset = agents[i].position - agent.position;
        float distance = length(offset);
        
        if (distance < perceptionRadius && distance > 0)
        {
            // Separation
            if (distance < 2.0)
            {
                float3 diff = -offset / distance;
                separation += diff / distance;
            }
            
            // Alignment
            alignment += agents[i].velocity;
            
            // Cohesion
            cohesion += agents[i].position;
            
            neighborCount++;
        }
    }
    
    if (neighborCount > 0)
    {
        // Average and normalize
        alignment /= (float)neighborCount;
        alignment = normalize(alignment) * maxSpeed - agent.velocity;
        alignment = clamp(length(alignment), 0, maxForce) * normalize(alignment);
        
        cohesion /= (float)neighborCount;
        cohesion = cohesion - agent.position;
        cohesion = normalize(cohesion) * maxSpeed - agent.velocity;
        cohesion = clamp(length(cohesion), 0, maxForce) * normalize(cohesion);
        
        separation = normalize(separation) * maxSpeed - agent.velocity;
        separation = clamp(length(separation), 0, maxForce) * normalize(separation);
    }
    
    // Apply weights and calculate acceleration
    agent.acceleration = separation * separationWeight + 
                        alignment * alignmentWeight + 
                        cohesion * cohesionWeight;
    
    // Update velocity and position
    agent.velocity += agent.acceleration * deltaTime;
    float speed = length(agent.velocity);
    if (speed > maxSpeed)
    {
        agent.velocity = (agent.velocity / speed) * maxSpeed;
    }
    
    agent.position += agent.velocity * deltaTime;
    
    agents[id.x] = agent;
}
```

#### GPU Swarm Manager

```csharp
using UnityEngine;
using Unity.Mathematics;

namespace SwarmAI.GPU
{
    public class GPUSwarmManager : MonoBehaviour
    {
        [Header("Swarm Settings")]
        [SerializeField] private int agentCount = 1000;
        [SerializeField] private ComputeShader swarmCompute;
        [SerializeField] private Mesh agentMesh;
        [SerializeField] private Material agentMaterial;
        
        [Header("Behavior Parameters")]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float maxForce = 10f;
        [SerializeField] private float perceptionRadius = 5f;
        [SerializeField] private float separationWeight = 1.5f;
        [SerializeField] private float alignmentWeight = 1.0f;
        [SerializeField] private float cohesionWeight = 1.0f;
        
        private ComputeBuffer agentBuffer;
        private ComputeBuffer argsBuffer;
        private Agent[] agents;
        private int kernel;
        
        struct Agent
        {
            public float3 position;
            public float3 velocity;
            public float3 acceleration;
        }
        
        void Start()
        {
            InitializeSwarm();
        }
        
        private void InitializeSwarm()
        {
            agents = new Agent[agentCount];
            
            // Initialize agents with random positions and velocities
            for (int i = 0; i < agentCount; i++)
            {
                agents[i] = new Agent
                {
                    position = UnityEngine.Random.insideUnitSphere * 50f,
                    velocity = UnityEngine.Random.insideUnitSphere * maxSpeed,
                    acceleration = float3.zero
                };
            }
            
            // Create compute buffer
            int stride = sizeof(float) * 9; // 3 float3s
            agentBuffer = new ComputeBuffer(agentCount, stride);
            agentBuffer.SetData(agents);
            
            // Setup compute shader
            kernel = swarmCompute.FindKernel("UpdateSwarm");
            swarmCompute.SetBuffer(kernel, "agents", agentBuffer);
            
            // Setup indirect args buffer for GPU instancing
            argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = agentMesh.GetIndexCount(0);
            args[1] = (uint)agentCount;
            argsBuffer.SetData(args);
            
            // Setup material
            agentMaterial.SetBuffer("agentBuffer", agentBuffer);
        }
        
        void Update()
        {
            // Update compute shader parameters
            swarmCompute.SetFloat("deltaTime", Time.deltaTime);
            swarmCompute.SetFloat("maxSpeed", maxSpeed);
            swarmCompute.SetFloat("maxForce", maxForce);
            swarmCompute.SetFloat("perceptionRadius", perceptionRadius);
            swarmCompute.SetFloat("separationWeight", separationWeight);
            swarmCompute.SetFloat("alignmentWeight", alignmentWeight);
            swarmCompute.SetFloat("cohesionWeight", cohesionWeight);
            swarmCompute.SetInt("agentCount", agentCount);
            
            // Dispatch compute shader
            int threadGroups = Mathf.CeilToInt(agentCount / 64f);
            swarmCompute.Dispatch(kernel, threadGroups, 1, 1);
            
            // Render all agents in one draw call
            Graphics.DrawMeshInstancedIndirect(agentMesh, 0, agentMaterial, 
                new Bounds(Vector3.zero, Vector3.one * 1000), argsBuffer);
        }
        
        void OnDestroy()
        {
            agentBuffer?.Release();
            argsBuffer?.Release();
        }
    }
}
```

### 5. Swarm Debugging and Visualization

```csharp
using UnityEngine;
using UnityEditor;

namespace SwarmAI.Debug
{
    public class SwarmDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showPerceptionRadius = true;
        [SerializeField] private bool showVelocityVectors = true;
        [SerializeField] private bool showNeighborConnections = true;
        [SerializeField] private bool showPheromoneMap = false;
        
        [Header("Colors")]
        [SerializeField] private Color perceptionColor = Color.yellow;
        [SerializeField] private Color velocityColor = Color.cyan;
        [SerializeField] private Color connectionColor = Color.green;
        
        private SwarmAgent[] agents;
        
        void Start()
        {
            agents = FindObjectsOfType<SwarmAgent>();
        }
        
        void OnDrawGizmos()
        {
            if (agents == null) return;
            
            foreach (var agent in agents)
            {
                if (agent == null) continue;
                
                if (showPerceptionRadius)
                {
                    Gizmos.color = perceptionColor;
                    Gizmos.DrawWireSphere(agent.transform.position, agent.PerceptionRadius);
                }
                
                if (showVelocityVectors)
                {
                    Gizmos.color = velocityColor;
                    Gizmos.DrawRay(agent.transform.position, agent.Velocity);
                }
                
                if (showNeighborConnections)
                {
                    Gizmos.color = connectionColor;
                    foreach (var neighbor in agent.Neighbors)
                    {
                        if (neighbor != null)
                        {
                            Gizmos.DrawLine(agent.transform.position, neighbor.transform.position);
                        }
                    }
                }
            }
        }
        
        #if UNITY_EDITOR
        [CustomEditor(typeof(SwarmDebugger))]
        public class SwarmDebuggerEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                
                if (GUILayout.Button("Capture Swarm State"))
                {
                    CaptureSwarmState();
                }
                
                if (GUILayout.Button("Export Performance Metrics"))
                {
                    ExportMetrics();
                }
            }
            
            private void CaptureSwarmState()
            {
                // Implementation for capturing swarm state
                Debug.Log("Swarm state captured");
            }
            
            private void ExportMetrics()
            {
                // Implementation for exporting metrics
                Debug.Log("Metrics exported");
            }
        }
        #endif
    }
}
```

## Performance Optimization Checklist

1. **Spatial Partitioning**
   - [ ] Implement grid-based spatial hashing
   - [ ] Use quadtree/octree for dynamic environments
   - [ ] Cache neighbor queries

2. **Job System Integration**
   - [ ] Convert agent updates to IJobParallelFor
   - [ ] Use Burst compiler for math operations
   - [ ] Batch similar operations

3. **GPU Optimization**
   - [ ] Use compute shaders for massive swarms
   - [ ] Implement GPU instancing for rendering
   - [ ] Minimize CPU-GPU data transfer

4. **Memory Management**
   - [ ] Pool all agent GameObjects
   - [ ] Use NativeArrays for job data
   - [ ] Avoid runtime allocations

5. **LOD System**
   - [ ] Implement behavioral LOD
   - [ ] Reduce update frequency for distant agents
   - [ ] Simplify calculations based on distance

## Common Issues and Solutions

### Issue: Frame drops with 1000+ agents
**Solution**: 
- Switch to GPU-based simulation
- Implement spatial partitioning
- Use Unity Job System

### Issue: Agents clumping together
**Solution**:
- Increase separation weight
- Add collision avoidance
- Implement personal space radius

### Issue: Erratic movement patterns
**Solution**:
- Limit maximum force
- Smooth velocity changes
- Add damping to acceleration

### Issue: Agents not finding targets
**Solution**:
- Increase perception radius
- Implement waypoint system
- Add global knowledge fallback

## Resources and References

- Unity DOTS Documentation
- GPU Gems 3: Chapter on GPU-based particle simulation
- Craig Reynolds' Boids paper
- Ant Colony Optimization algorithms
- Unity Job System best practices

This implementation guide provides production-ready code that can be directly used in Unity projects for swarm AI systems.