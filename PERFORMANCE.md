# Unity Swarm AI - Performance Optimization Guide

**Scale your swarms from hundreds to thousands of agents with these proven optimization techniques.** ⚡

This comprehensive guide covers everything from basic optimizations to advanced GPU computing, helping you achieve smooth 60+ FPS with massive swarms.

## Performance Overview

### Target Performance Metrics

| Agent Count | Target FPS | Architecture | Memory Usage |
|-------------|------------|--------------|--------------|
| 1-100 | 120+ FPS | MonoBehaviour | < 20 MB |
| 100-500 | 60+ FPS | MonoBehaviour + Spatial | < 50 MB |
| 500-2000 | 60+ FPS | Job System + LOD | < 150 MB |
| 2000-10000 | 60+ FPS | ECS + GPU Compute | < 400 MB |
| 10000+ | 30+ FPS | Full GPU Pipeline | < 800 MB |

### Performance Bottlenecks

Understanding where performance issues occur helps you optimize effectively:

1. **Neighbor Detection** (40-60% of CPU time)
2. **Behavior Calculations** (20-30% of CPU time)
3. **Physics Updates** (10-20% of CPU time)
4. **Rendering** (10-15% of GPU time)
5. **Memory Allocation** (5-10% overall impact)

## Level 1: Basic Optimizations (100-500 agents)

### Spatial Partitioning

The single most important optimization for swarm performance.

#### Enable Built-in Spatial Partitioning
```csharp
// In SwarmManager
public void OptimizeForBasicPerformance()
{
    // Enable spatial partitioning
    UseSpatialPartitioning = true;
    PartitionCellSize = CalculateOptimalCellSize();
    
    // Adjust update rates
    UpdateRate = 2; // Update every 2 frames instead of every frame
    
    // Limit perception radius
    foreach (var agent in Agents)
    {
        agent.PerceptionRadius = Mathf.Min(agent.PerceptionRadius, 5f);
    }
}

float CalculateOptimalCellSize()
{
    // Cell size should be roughly 2x the average perception radius
    float avgPerceptionRadius = GetAveragePerceptionRadius();
    return avgPerceptionRadius * 2f;
}
```

#### Custom Uniform Grid Implementation
```csharp
public class OptimizedUniformGrid
{
    private Dictionary<int, List<SwarmAgent>> cells;
    private float cellSize;
    private Vector3 worldSize;
    private Vector3 worldOffset;
    
    public OptimizedUniformGrid(Vector3 worldSize, float cellSize)
    {
        this.worldSize = worldSize;
        this.cellSize = cellSize;
        this.worldOffset = -worldSize * 0.5f;
        cells = new Dictionary<int, List<SwarmAgent>>(1024);
    }
    
    public void UpdateGrid(List<SwarmAgent> agents)
    {
        // Clear previous frame data
        foreach (var cell in cells.Values)
        {
            cell.Clear();
        }
        
        // Add agents to grid cells
        foreach (var agent in agents)
        {
            int hash = GetCellHash(agent.Position);
            
            if (!cells.ContainsKey(hash))
                cells[hash] = new List<SwarmAgent>(32);
                
            cells[hash].Add(agent);
        }
    }
    
    public List<SwarmAgent> GetNeighbors(Vector3 position, float radius)
    {
        List<SwarmAgent> neighbors = new List<SwarmAgent>();
        int cellRadius = Mathf.CeilToInt(radius / cellSize);
        
        int centerX = GetGridCoord(position.x);
        int centerZ = GetGridCoord(position.z);
        
        // Check surrounding cells
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int z = -cellRadius; z <= cellRadius; z++)
            {
                int hash = GetCellHash(centerX + x, 0, centerZ + z);
                
                if (cells.TryGetValue(hash, out List<SwarmAgent> cellAgents))
                {
                    foreach (var agent in cellAgents)
                    {
                        float distance = Vector3.Distance(position, agent.Position);
                        if (distance <= radius)
                            neighbors.Add(agent);
                    }
                }
            }
        }
        
        return neighbors;
    }
    
    private int GetGridCoord(float worldPos)
    {
        return Mathf.FloorToInt((worldPos - worldOffset.x) / cellSize);
    }
    
    private int GetCellHash(Vector3 position)
    {
        int x = GetGridCoord(position.x);
        int z = GetGridCoord(position.z);
        return GetCellHash(x, 0, z);
    }
    
    private int GetCellHash(int x, int y, int z)
    {
        // Use prime numbers for better distribution
        return x * 73856093 ^ y * 19349663 ^ z * 83492791;
    }
}
```

### Update Rate Optimization

Control how often agents update to balance performance and quality.

```csharp
public class AdaptiveUpdateManager : MonoBehaviour
{
    [Header("Performance Targets")]
    public float targetFPS = 60f;
    public int minUpdateRate = 1;
    public int maxUpdateRate = 5;
    
    private SwarmManager swarmManager;
    private float[] frameTimes = new float[30];
    private int frameIndex = 0;
    
    void Start()
    {
        swarmManager = GetComponent<SwarmManager>();
    }
    
    void Update()
    {
        // Track frame times
        frameTimes[frameIndex] = Time.unscaledDeltaTime;
        frameIndex = (frameIndex + 1) % frameTimes.Length;
        
        // Adjust update rate every 30 frames
        if (frameIndex == 0)
        {
            float avgFrameTime = CalculateAverageFrameTime();
            float currentFPS = 1f / avgFrameTime;
            
            AdjustUpdateRate(currentFPS);
        }
    }
    
    void AdjustUpdateRate(float currentFPS)
    {
        int newUpdateRate = swarmManager.UpdateRate;
        
        if (currentFPS < targetFPS * 0.9f) // Below 90% of target
        {
            newUpdateRate = Mathf.Min(newUpdateRate + 1, maxUpdateRate);
        }
        else if (currentFPS > targetFPS * 1.1f) // Above 110% of target
        {
            newUpdateRate = Mathf.Max(newUpdateRate - 1, minUpdateRate);
        }
        
        if (newUpdateRate != swarmManager.UpdateRate)
        {
            swarmManager.SetUpdateRate(newUpdateRate);
            Debug.Log($"Update rate adjusted to: {newUpdateRate} (FPS: {currentFPS:F1})");
        }
    }
    
    float CalculateAverageFrameTime()
    {
        float sum = 0f;
        for (int i = 0; i < frameTimes.Length; i++)
        {
            sum += frameTimes[i];
        }
        return sum / frameTimes.Length;
    }
}
```

### Behavior Optimization

Optimize the most expensive behaviors for better performance.

```csharp
public class OptimizedFlockingBehavior : SwarmBehavior
{
    [Header("Performance Settings")]
    public int maxNeighborsToConsider = 15;
    public float separationRadiusSquared = 2.25f; // 1.5^2, avoid sqrt
    
    public override Vector3 CalculateForce(SwarmAgent agent, List<SwarmAgent> neighbors)
    {
        // Limit neighbor processing
        int neighborCount = Mathf.Min(neighbors.Count, maxNeighborsToConsider);
        
        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        
        int separationCount = 0;
        int alignmentCount = 0;
        
        // Process only closest neighbors
        for (int i = 0; i < neighborCount; i++)
        {
            SwarmAgent neighbor = neighbors[i];
            Vector3 diff = agent.Position - neighbor.Position;
            float distanceSquared = diff.sqrMagnitude; // Avoid sqrt
            
            // Separation (closest neighbors only)
            if (distanceSquared < separationRadiusSquared && distanceSquared > 0)
            {
                diff = diff.normalized / Mathf.Sqrt(distanceSquared); // Only sqrt when needed
                separation += diff;
                separationCount++;
            }
            
            // Alignment (all neighbors)
            alignment += neighbor.Velocity;
            alignmentCount++;
            
            // Cohesion (all neighbors)
            cohesion += neighbor.Position;
        }
        
        // Average and normalize forces
        if (separationCount > 0)
            separation = (separation / separationCount).normalized * agent.MaxForce;
            
        if (alignmentCount > 0)
        {
            alignment = (alignment / alignmentCount).normalized * agent.MaxSpeed;
            alignment = Vector3.ClampMagnitude(alignment - agent.Velocity, agent.MaxForce);
        }
        
        if (alignmentCount > 0)
        {
            cohesion = (cohesion / alignmentCount) - agent.Position;
            cohesion = Vector3.ClampMagnitude(cohesion.normalized * agent.MaxSpeed - agent.Velocity, agent.MaxForce);
        }
        
        return separation * SeparationWeight + 
               alignment * AlignmentWeight + 
               cohesion * CohesionWeight;
    }
}
```

## Level 2: Advanced Optimizations (500-2000 agents)

### Level of Detail (LOD) System

Reduce processing complexity based on distance and importance.

```csharp
public class SwarmLODSystem : MonoBehaviour
{
    public enum LODLevel { Full, Reduced, Minimal, Culled }
    
    [Header("LOD Distances")]
    public float fullDetailDistance = 20f;
    public float reducedDetailDistance = 40f;
    public float minimalDetailDistance = 80f;
    public float cullDistance = 120f;
    
    [Header("Quality Settings")]
    public int fullDetailNeighbors = 20;
    public int reducedDetailNeighbors = 10;
    public int minimalDetailNeighbors = 5;
    
    private Camera playerCamera;
    private SwarmManager swarmManager;
    
    void Start()
    {
        playerCamera = Camera.main;
        swarmManager = GetComponent<SwarmManager>();
        
        // Update LOD every few frames for performance
        InvokeRepeating(nameof(UpdateAllLODs), 0.1f, 0.1f);
    }
    
    void UpdateAllLODs()
    {
        Vector3 cameraPos = playerCamera.transform.position;
        
        foreach (var agent in swarmManager.Agents)
        {
            float distance = Vector3.Distance(agent.Position, cameraPos);
            LODLevel lod = GetLODLevel(distance);
            ApplyLOD(agent, lod);
        }
    }
    
    LODLevel GetLODLevel(float distance)
    {
        if (distance <= fullDetailDistance) return LODLevel.Full;
        if (distance <= reducedDetailDistance) return LODLevel.Reduced;
        if (distance <= minimalDetailDistance) return LODLevel.Minimal;
        return LODLevel.Culled;
    }
    
    void ApplyLOD(SwarmAgent agent, LODLevel lod)
    {
        switch (lod)
        {
            case LODLevel.Full:
                agent.MaxNeighborsToProcess = fullDetailNeighbors;
                agent.UpdateFrequency = 1; // Every frame
                agent.EnableComplexBehaviors = true;
                agent.gameObject.SetActive(true);
                break;
                
            case LODLevel.Reduced:
                agent.MaxNeighborsToProcess = reducedDetailNeighbors;
                agent.UpdateFrequency = 2; // Every 2 frames
                agent.EnableComplexBehaviors = true;
                agent.gameObject.SetActive(true);
                break;
                
            case LODLevel.Minimal:
                agent.MaxNeighborsToProcess = minimalDetailNeighbors;
                agent.UpdateFrequency = 4; // Every 4 frames
                agent.EnableComplexBehaviors = false;
                agent.gameObject.SetActive(true);
                break;
                
            case LODLevel.Culled:
                agent.gameObject.SetActive(false);
                break;
        }
    }
}
```

### Object Pooling System

Eliminate garbage collection by reusing agent objects.

```csharp
public class SwarmAgentPool : MonoBehaviour
{
    [Header("Pool Settings")]
    public GameObject agentPrefab;
    public int initialPoolSize = 200;
    public int maxPoolSize = 1000;
    
    private Queue<SwarmAgent> availableAgents;
    private List<SwarmAgent> activeAgents;
    private Transform poolParent;
    
    void Start()
    {
        availableAgents = new Queue<SwarmAgent>(initialPoolSize);
        activeAgents = new List<SwarmAgent>(initialPoolSize);
        
        // Create pool parent
        poolParent = new GameObject("Agent Pool").transform;
        poolParent.SetParent(transform);
        
        // Pre-instantiate agents
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewAgent();
        }
    }
    
    SwarmAgent CreateNewAgent()
    {
        GameObject agentObj = Instantiate(agentPrefab, poolParent);
        SwarmAgent agent = agentObj.GetComponent<SwarmAgent>();
        
        agentObj.SetActive(false);
        availableAgents.Enqueue(agent);
        
        return agent;
    }
    
    public SwarmAgent SpawnAgent(Vector3 position, Quaternion rotation)
    {
        SwarmAgent agent;
        
        // Get from pool or create new
        if (availableAgents.Count > 0)
        {
            agent = availableAgents.Dequeue();
        }
        else if (activeAgents.Count < maxPoolSize)
        {
            agent = CreateNewAgent();
            availableAgents.Dequeue(); // Remove from available
        }
        else
        {
            Debug.LogWarning("Agent pool at maximum capacity!");
            return null;
        }
        
        // Initialize agent
        agent.transform.position = position;
        agent.transform.rotation = rotation;
        agent.gameObject.SetActive(true);
        agent.ResetAgent(); // Custom reset method
        
        activeAgents.Add(agent);
        return agent;
    }
    
    public void DespawnAgent(SwarmAgent agent)
    {
        if (activeAgents.Remove(agent))
        {
            agent.gameObject.SetActive(false);
            agent.transform.SetParent(poolParent);
            availableAgents.Enqueue(agent);
        }
    }
    
    public void DespawnAllAgents()
    {
        for (int i = activeAgents.Count - 1; i >= 0; i--)
        {
            DespawnAgent(activeAgents[i]);
        }
    }
}

// Extension to SwarmAgent for pooling
public partial class SwarmAgent
{
    public void ResetAgent()
    {
        velocity = Vector3.zero;
        acceleration = Vector3.zero;
        currentTarget = null;
        // Reset any other state variables
    }
}
```

## Level 3: Job System Integration (1000-5000 agents)

### Burst-Compiled Job Implementation

Use Unity's Job System for massive parallelization.

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct SwarmFlockingJob : IJobParallelFor
{
    // Read-only data
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<float3> velocities;
    [ReadOnly] public NativeArray<int> neighborCounts;
    [ReadOnly] public NativeArray<int> neighborStartIndices;
    [ReadOnly] public NativeArray<int> neighborList;
    
    // Job parameters
    [ReadOnly] public float deltaTime;
    [ReadOnly] public float maxSpeed;
    [ReadOnly] public float maxForce;
    [ReadOnly] public float separationWeight;
    [ReadOnly] public float alignmentWeight;
    [ReadOnly] public float cohesionWeight;
    [ReadOnly] public float separationRadius;
    
    // Write data
    public NativeArray<float3> newVelocities;
    public NativeArray<float3> newPositions;
    
    public void Execute(int index)
    {
        float3 position = positions[index];
        float3 velocity = velocities[index];
        
        float3 separation = float3.zero;
        float3 alignment = float3.zero;
        float3 cohesion = float3.zero;
        
        int neighborCount = neighborCounts[index];
        int startIndex = neighborStartIndices[index];
        
        int separationCount = 0;
        
        // Process neighbors
        for (int i = 0; i < neighborCount; i++)
        {
            int neighborIndex = neighborList[startIndex + i];
            float3 neighborPos = positions[neighborIndex];
            float3 neighborVel = velocities[neighborIndex];
            
            float3 diff = position - neighborPos;
            float distanceSq = math.lengthsq(diff);
            
            // Separation
            if (distanceSq < separationRadius * separationRadius && distanceSq > 0)
            {
                float distance = math.sqrt(distanceSq);
                separation += (diff / distance) / distance; // Weight by distance
                separationCount++;
            }
            
            // Alignment
            alignment += neighborVel;
            
            // Cohesion
            cohesion += neighborPos;
        }
        
        // Calculate final forces
        float3 totalForce = float3.zero;
        
        if (separationCount > 0)
        {
            separation = math.normalize(separation / separationCount) * maxForce;
            totalForce += separation * separationWeight;
        }
        
        if (neighborCount > 0)
        {
            // Alignment
            alignment = math.normalize(alignment / neighborCount) * maxSpeed;
            float3 alignmentForce = math.normalize(alignment - velocity) * maxForce;
            totalForce += alignmentForce * alignmentWeight;
            
            // Cohesion
            cohesion = (cohesion / neighborCount) - position;
            float3 cohesionForce = math.normalize(cohesion) * maxSpeed;
            cohesionForce = math.normalize(cohesionForce - velocity) * maxForce;
            totalForce += cohesionForce * cohesionWeight;
        }
        
        // Update velocity and position
        velocity += totalForce * deltaTime;
        float speed = math.length(velocity);
        if (speed > maxSpeed)
        {
            velocity = (velocity / speed) * maxSpeed;
        }
        
        position += velocity * deltaTime;
        
        newVelocities[index] = velocity;
        newPositions[index] = position;
    }
}

public class JobSystemSwarmManager : MonoBehaviour
{
    private NativeArray<float3> positions;
    private NativeArray<float3> velocities;
    private NativeArray<float3> newPositions;
    private NativeArray<float3> newVelocities;
    private NativeArray<int> neighborCounts;
    private NativeArray<int> neighborStartIndices;
    private NativeArray<int> neighborList;
    
    private JobHandle jobHandle;
    private bool jobScheduled = false;
    
    void Start()
    {
        InitializeNativeArrays();
    }
    
    void Update()
    {
        if (jobScheduled)
        {
            // Complete previous job
            jobHandle.Complete();
            ApplyJobResults();
            jobScheduled = false;
        }
        
        // Prepare data for next job
        UpdateNativeArrays();
        BuildNeighborList();
        
        // Schedule new job
        ScheduleFlockingJob();
    }
    
    void ScheduleFlockingJob()
    {
        SwarmFlockingJob flockingJob = new SwarmFlockingJob
        {
            positions = positions,
            velocities = velocities,
            neighborCounts = neighborCounts,
            neighborStartIndices = neighborStartIndices,
            neighborList = neighborList,
            deltaTime = Time.deltaTime,
            maxSpeed = maxSpeed,
            maxForce = maxForce,
            separationWeight = separationWeight,
            alignmentWeight = alignmentWeight,
            cohesionWeight = cohesionWeight,
            separationRadius = separationRadius,
            newVelocities = newVelocities,
            newPositions = newPositions
        };
        
        // Schedule parallel job (64 agents per batch)
        jobHandle = flockingJob.Schedule(agents.Count, 64);
        jobScheduled = true;
    }
    
    void OnDestroy()
    {
        // Always complete jobs before destroying
        if (jobScheduled)
            jobHandle.Complete();
            
        // Dispose native arrays
        if (positions.IsCreated) positions.Dispose();
        if (velocities.IsCreated) velocities.Dispose();
        if (newPositions.IsCreated) newPositions.Dispose();
        if (newVelocities.IsCreated) newVelocities.Dispose();
        if (neighborCounts.IsCreated) neighborCounts.Dispose();
        if (neighborStartIndices.IsCreated) neighborStartIndices.Dispose();
        if (neighborList.IsCreated) neighborList.Dispose();
    }
}
```

## Level 4: GPU Compute Shaders (5000+ agents)

### Compute Shader Implementation

Move all calculations to the GPU for ultimate performance.

```hlsl
// SwarmCompute.compute
#pragma kernel UpdateSwarm
#pragma kernel BuildSpatialHash

struct Agent
{
    float3 position;
    float3 velocity;
    float3 acceleration;
    uint neighbors[32]; // Fixed-size neighbor list
    uint neighborCount;
};

struct SpatialCell
{
    uint agentIndices[64]; // Max agents per cell
    uint agentCount;
};

RWStructuredBuffer<Agent> agents;
RWStructuredBuffer<SpatialCell> spatialGrid;
RWStructuredBuffer<uint> spatialKeys;
RWStructuredBuffer<uint> spatialValues;

// Parameters
float deltaTime;
float maxSpeed;
float maxForce;
float perceptionRadius;
float separationWeight;
float alignmentWeight;
float cohesionWeight;
uint agentCount;
float3 worldSize;
float cellSize;
uint gridDimensions;

// Spatial hashing functions
uint HashPosition(float3 pos)
{
    uint3 gridPos = (uint3)((pos + worldSize * 0.5) / cellSize);
    return gridPos.x + gridPos.y * gridDimensions + gridPos.z * gridDimensions * gridDimensions;
}

[numthreads(256, 1, 1)]
void BuildSpatialHash(uint3 id : SV_DispatchThreadID)
{
    uint index = id.x;
    if (index >= agentCount) return;
    
    Agent agent = agents[index];
    uint cellHash = HashPosition(agent.position);
    
    spatialKeys[index] = cellHash;
    spatialValues[index] = index;
}

[numthreads(256, 1, 1)]
void UpdateSwarm(uint3 id : SV_DispatchThreadID)
{
    uint index = id.x;
    if (index >= agentCount) return;
    
    Agent agent = agents[index];
    
    // Find neighbors using spatial hash
    uint cellHash = HashPosition(agent.position);
    
    float3 separation = float3(0, 0, 0);
    float3 alignment = float3(0, 0, 0);
    float3 cohesion = float3(0, 0, 0);
    uint neighborCount = 0;
    uint separationCount = 0;
    
    // Check surrounding cells (3x3x3 = 27 cells)
    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int z = -1; z <= 1; z++)
            {
                uint3 gridPos = (uint3)((agent.position + worldSize * 0.5) / cellSize);
                uint3 neighborCell = gridPos + int3(x, y, z);
                
                if (any(neighborCell >= gridDimensions)) continue;
                
                uint neighborHash = neighborCell.x + neighborCell.y * gridDimensions + 
                                  neighborCell.z * gridDimensions * gridDimensions;
                
                SpatialCell cell = spatialGrid[neighborHash];
                
                for (uint i = 0; i < cell.agentCount; i++)
                {
                    uint neighborIndex = cell.agentIndices[i];
                    if (neighborIndex == index) continue;
                    
                    Agent neighbor = agents[neighborIndex];
                    float3 diff = agent.position - neighbor.position;
                    float distance = length(diff);
                    
                    if (distance < perceptionRadius && distance > 0)
                    {
                        // Separation
                        if (distance < perceptionRadius * 0.5)
                        {
                            separation += normalize(diff) / distance;
                            separationCount++;
                        }
                        
                        // Alignment
                        alignment += neighbor.velocity;
                        
                        // Cohesion
                        cohesion += neighbor.position;
                        
                        neighborCount++;
                    }
                }
            }
        }
    }
    
    // Calculate final forces
    float3 totalForce = float3(0, 0, 0);
    
    if (separationCount > 0)
    {
        separation = normalize(separation / separationCount) * maxForce;
        totalForce += separation * separationWeight;
    }
    
    if (neighborCount > 0)
    {
        // Alignment
        alignment = normalize(alignment / neighborCount) * maxSpeed;
        float3 alignmentForce = normalize(alignment - agent.velocity) * maxForce;
        totalForce += alignmentForce * alignmentWeight;
        
        // Cohesion
        cohesion = (cohesion / neighborCount) - agent.position;
        float3 cohesionForce = normalize(cohesion) * maxSpeed;
        cohesionForce = normalize(cohesionForce - agent.velocity) * maxForce;
        totalForce += cohesionForce * cohesionWeight;
    }
    
    // Update agent
    agent.acceleration = totalForce;
    agent.velocity += agent.acceleration * deltaTime;
    
    float speed = length(agent.velocity);
    if (speed > maxSpeed)
    {
        agent.velocity = normalize(agent.velocity) * maxSpeed;
    }
    
    agent.position += agent.velocity * deltaTime;
    
    agents[index] = agent;
}
```

### GPU Swarm Manager

```csharp
public class GPUSwarmManager : MonoBehaviour
{
    [Header("GPU Compute")]
    public ComputeShader swarmComputeShader;
    
    [Header("Swarm Settings")]
    public int agentCount = 5000;
    public float maxSpeed = 5f;
    public float maxForce = 10f;
    public float perceptionRadius = 3f;
    public Vector3 worldSize = new Vector3(100, 50, 100);
    
    [Header("Behavior Weights")]
    public float separationWeight = 1.5f;
    public float alignmentWeight = 1f;
    public float cohesionWeight = 1f;
    
    private ComputeBuffer agentBuffer;
    private ComputeBuffer spatialGridBuffer;
    private ComputeBuffer spatialKeysBuffer;
    private ComputeBuffer spatialValuesBuffer;
    
    private int updateKernel;
    private int spatialHashKernel;
    
    private Agent[] agentData;
    private int gridDimensions;
    private float cellSize;
    
    struct Agent
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
        // Note: GPU struct padding considerations
    }
    
    void Start()
    {
        InitializeGPUCompute();
        SpawnAgents();
    }
    
    void InitializeGPUCompute()
    {
        // Calculate grid dimensions
        cellSize = perceptionRadius * 2f;
        gridDimensions = Mathf.CeilToInt(worldSize.x / cellSize);
        
        // Find kernels
        updateKernel = swarmComputeShader.FindKernel("UpdateSwarm");
        spatialHashKernel = swarmComputeShader.FindKernel("BuildSpatialHash");
        
        // Create compute buffers
        agentBuffer = new ComputeBuffer(agentCount, sizeof(float) * 9); // 3 Vector3s
        spatialGridBuffer = new ComputeBuffer(gridDimensions * gridDimensions * gridDimensions, sizeof(uint) * 65);
        spatialKeysBuffer = new ComputeBuffer(agentCount, sizeof(uint));
        spatialValuesBuffer = new ComputeBuffer(agentCount, sizeof(uint));
        
        // Set compute shader buffers
        swarmComputeShader.SetBuffer(updateKernel, "agents", agentBuffer);
        swarmComputeShader.SetBuffer(updateKernel, "spatialGrid", spatialGridBuffer);
        
        swarmComputeShader.SetBuffer(spatialHashKernel, "agents", agentBuffer);
        swarmComputeShader.SetBuffer(spatialHashKernel, "spatialKeys", spatialKeysBuffer);
        swarmComputeShader.SetBuffer(spatialHashKernel, "spatialValues", spatialValuesBuffer);
        
        // Set constant parameters
        swarmComputeShader.SetFloat("maxSpeed", maxSpeed);
        swarmComputeShader.SetFloat("maxForce", maxForce);
        swarmComputeShader.SetFloat("perceptionRadius", perceptionRadius);
        swarmComputeShader.SetVector("worldSize", worldSize);
        swarmComputeShader.SetFloat("cellSize", cellSize);
        swarmComputeShader.SetInt("gridDimensions", gridDimensions);
        swarmComputeShader.SetInt("agentCount", agentCount);
    }
    
    void Update()
    {
        // Update dynamic parameters
        swarmComputeShader.SetFloat("deltaTime", Time.deltaTime);
        swarmComputeShader.SetFloat("separationWeight", separationWeight);
        swarmComputeShader.SetFloat("alignmentWeight", alignmentWeight);
        swarmComputeShader.SetFloat("cohesionWeight", cohesionWeight);
        
        // Dispatch compute shaders
        int threadGroups = Mathf.CeilToInt(agentCount / 256f);
        
        // Build spatial hash
        swarmComputeShader.Dispatch(spatialHashKernel, threadGroups, 1, 1);
        
        // Update swarm
        swarmComputeShader.Dispatch(updateKernel, threadGroups, 1, 1);
    }
    
    void OnDestroy()
    {
        // Always release compute buffers
        agentBuffer?.Release();
        spatialGridBuffer?.Release();
        spatialKeysBuffer?.Release();
        spatialValuesBuffer?.Release();
    }
}
```

## Performance Monitoring

### Comprehensive Performance Profiler

```csharp
public class SwarmPerformanceProfiler : MonoBehaviour
{
    [Header("Monitoring")]
    public bool enableProfiling = true;
    public float updateInterval = 1f;
    
    [Header("Display")]
    public bool showOnScreen = true;
    public GUIStyle textStyle;
    
    private SwarmManager swarmManager;
    private float[] frameTimes = new float[60];
    private int frameIndex = 0;
    private float lastUpdateTime;
    
    // Performance metrics
    private float averageFPS;
    private float memoryUsage;
    private int activeAgents;
    private float averageNeighbors;
    private float updateTime;
    
    void Start()
    {
        swarmManager = FindObjectOfType<SwarmManager>();
        textStyle.fontSize = 16;
        textStyle.normal.textColor = Color.white;
    }
    
    void Update()
    {
        if (!enableProfiling) return;
        
        // Track frame times
        frameTimes[frameIndex] = Time.unscaledDeltaTime;
        frameIndex = (frameIndex + 1) % frameTimes.Length;
        
        // Update metrics periodically
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateMetrics();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdateMetrics()
    {
        // Calculate average FPS
        float totalTime = 0f;
        for (int i = 0; i < frameTimes.Length; i++)
        {
            totalTime += frameTimes[i];
        }
        averageFPS = frameTimes.Length / totalTime;
        
        // Memory usage
        memoryUsage = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        
        // Swarm metrics
        if (swarmManager != null)
        {
            activeAgents = swarmManager.Agents.Count;
            averageNeighbors = CalculateAverageNeighbors();
        }
        
        // Performance warnings
        CheckPerformanceWarnings();
    }
    
    float CalculateAverageNeighbors()
    {
        if (swarmManager.Agents.Count == 0) return 0;
        
        float total = 0;
        foreach (var agent in swarmManager.Agents)
        {
            total += agent.GetNeighborCount();
        }
        return total / swarmManager.Agents.Count;
    }
    
    void CheckPerformanceWarnings()
    {
        if (averageFPS < 30)
        {
            Debug.LogWarning($"Low FPS detected: {averageFPS:F1}. Consider optimizations.");
        }
        
        if (memoryUsage > 500)
        {
            Debug.LogWarning($"High memory usage: {memoryUsage:F1}MB. Check for memory leaks.");
        }
        
        if (averageNeighbors > 20)
        {
            Debug.LogWarning($"High neighbor count: {averageNeighbors:F1}. Consider reducing perception radius.");
        }
    }
    
    void OnGUI()
    {
        if (!showOnScreen || !enableProfiling) return;
        
        string stats = $"FPS: {averageFPS:F1}\n" +
                      $"Memory: {memoryUsage:F1}MB\n" +
                      $"Agents: {activeAgents}\n" +
                      $"Avg Neighbors: {averageNeighbors:F1}\n" +
                      $"Update Time: {updateTime:F2}ms";
        
        GUI.Label(new Rect(10, 10, 300, 200), stats, textStyle);
    }
}
```

## Platform-Specific Optimizations

### Mobile Optimization

```csharp
public class MobileSwarmOptimizer : MonoBehaviour
{
    [Header("Mobile Settings")]
    public int maxAgentsOnMobile = 150;
    public float mobileUpdateRate = 3f;
    public bool useSimplifiedBehaviors = true;
    
    void Start()
    {
        if (Application.isMobilePlatform)
        {
            ApplyMobileOptimizations();
        }
    }
    
    void ApplyMobileOptimizations()
    {
        var swarmManager = GetComponent<SwarmManager>();
        
        // Limit agent count
        swarmManager.AgentCount = Mathf.Min(swarmManager.AgentCount, maxAgentsOnMobile);
        
        // Reduce update frequency
        swarmManager.SetUpdateRate((int)mobileUpdateRate);
        
        // Disable expensive features
        swarmManager.UseSpatialPartitioning = true; // More important on mobile
        
        // Simplify behaviors
        if (useSimplifiedBehaviors)
        {
            foreach (var agent in swarmManager.Agents)
            {
                agent.PerceptionRadius = Mathf.Min(agent.PerceptionRadius, 3f);
                agent.MaxNeighborsToProcess = 8;
            }
        }
        
        // Adjust quality settings
        QualitySettings.vSyncCount = 0; // Disable VSync for consistent performance
        Application.targetFrameRate = 60;
    }
}
```

## Troubleshooting Performance Issues

### Common Problems and Solutions

| Problem | Symptoms | Solution |
|---------|----------|----------|
| **Low FPS** | < 30 FPS with moderate agent count | Enable spatial partitioning, reduce perception radius |
| **Memory leaks** | Rising memory usage over time | Check object pooling, dispose compute buffers |
| **Stuttering** | Irregular frame times | Use fixed update rates, avoid garbage allocation |
| **GPU bottleneck** | GPU usage > 90% | Reduce visual effects, use GPU compute for logic |
| **CPU bottleneck** | Single core maxed out | Implement Job System, reduce single-threaded work |

### Performance Debugging Checklist

```csharp
// Performance audit checklist
public class PerformanceAudit : MonoBehaviour
{
    [Button("Run Performance Audit")]
    void RunAudit()
    {
        var swarmManager = FindObjectOfType<SwarmManager>();
        
        Debug.Log("=== SWARM PERFORMANCE AUDIT ===");
        
        // Agent count check
        int agentCount = swarmManager.Agents.Count;
        Debug.Log($"Agent Count: {agentCount} {GetAgentCountStatus(agentCount)}");
        
        // Spatial partitioning check
        Debug.Log($"Spatial Partitioning: {swarmManager.UseSpatialPartitioning} {(swarmManager.UseSpatialPartitioning ? "✓" : "❌ ENABLE THIS")}");
        
        // Update rate check
        Debug.Log($"Update Rate: {swarmManager.UpdateRate} {GetUpdateRateStatus(swarmManager.UpdateRate, agentCount)}");
        
        // Memory check
        float memory = System.GC.GetTotalMemory(false) / (1024f * 1024f);
        Debug.Log($"Memory Usage: {memory:F1}MB {GetMemoryStatus(memory)}");
        
        // Neighbor analysis
        AnalyzeNeighborDistribution();
        
        Debug.Log("=== END AUDIT ===");
    }
    
    string GetAgentCountStatus(int count)
    {
        if (count < 100) return "✓ Good";
        if (count < 500) return "⚠ Monitor performance";
        if (count < 2000) return "⚠ Consider Job System";
        return "❌ Use GPU Compute";
    }
    
    void AnalyzeNeighborDistribution()
    {
        var swarmManager = FindObjectOfType<SwarmManager>();
        var neighborCounts = new List<int>();
        
        foreach (var agent in swarmManager.Agents)
        {
            neighborCounts.Add(agent.GetNeighborCount());
        }
        
        float avg = neighborCounts.Average();
        int max = neighborCounts.Max();
        
        Debug.Log($"Neighbors - Avg: {avg:F1}, Max: {max} {(max > 25 ? "❌ Too high" : "✓")}");
    }
}
```

---

## Summary

With these optimization techniques, you can scale Unity Swarm AI from hundreds to thousands of agents while maintaining smooth 60+ FPS performance:

### Quick Optimization Checklist ✅

**For 100-500 agents:**
- ✅ Enable spatial partitioning
- ✅ Set appropriate update rates
- ✅ Limit perception radius to 3-5 units

**For 500-2000 agents:**
- ✅ Implement LOD system
- ✅ Use object pooling
- ✅ Optimize behavior calculations

**For 2000+ agents:**
- ✅ Integrate Unity Job System
- ✅ Use Burst compiler
- ✅ Consider GPU compute shaders

**Always:**
- ✅ Profile regularly
- ✅ Monitor memory usage
- ✅ Test on target hardware

*Ready to push the limits? Try our GPU compute examples to see 10,000+ agents in action!*