# Unity Swarm AI Implementation Guide

## Table of Contents

1. [Getting Started](#getting-started)
2. [Basic Swarm Implementation](#basic-implementation)
3. [Advanced Techniques](#advanced-techniques)
4. [Performance Optimization](#performance-optimization)
5. [Common Patterns](#common-patterns)
6. [Debugging & Visualization](#debugging)
7. [Production Checklist](#production-checklist)

## Getting Started {#getting-started}

### Prerequisites

- Unity 2021.3 LTS or newer
- Basic understanding of Vector3 mathematics
- Familiarity with Unity's component system
- (Optional) DOTS knowledge for high-performance implementations

### Project Setup

```csharp
// 1. Create folder structure
Assets/
├── Scripts/
│   ├── SwarmAI/
│   │   ├── Core/
│   │   ├── Behaviors/
│   │   └── Utils/
│   └── Tests/
├── Prefabs/
│   └── Agents/
└── Materials/
```

### Basic Concepts

**Swarm Agent**: Individual entity following swarm rules  
**Swarm Manager**: Coordinates and optimizes agent interactions  
**Behavior**: Rule governing agent movement (separation, alignment, cohesion)  
**Neighborhood**: Agents within perception radius

## Basic Swarm Implementation {#basic-implementation}

### Step 1: Create the Swarm Agent

```csharp
using UnityEngine;
using System.Collections.Generic;

public class SwarmAgent : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float maxForce = 10f;
    
    [Header("Perception")]
    public float perceptionRadius = 5f;
    public LayerMask agentLayer;
    
    [Header("Behavior Weights")]
    public float separationWeight = 1.5f;
    public float alignmentWeight = 1.0f;
    public float cohesionWeight = 1.0f;
    
    private Vector3 velocity;
    private List<SwarmAgent> neighbors = new List<SwarmAgent>();
    
    void Start()
    {
        velocity = Random.insideUnitSphere * maxSpeed;
        SwarmManager.Instance.RegisterAgent(this);
    }
    
    void Update()
    {
        // Find neighbors
        UpdateNeighbors();
        
        // Calculate steering forces
        Vector3 acceleration = CalculateSteering();
        
        // Apply movement
        velocity += acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        transform.position += velocity * Time.deltaTime;
        
        // Face movement direction
        if (velocity.magnitude > 0.1f)
            transform.forward = velocity.normalized;
    }
    
    void UpdateNeighbors()
    {
        neighbors.Clear();
        Collider[] nearbyColliders = Physics.OverlapSphere(
            transform.position, perceptionRadius, agentLayer);
        
        foreach (var collider in nearbyColliders)
        {
            if (collider.gameObject != gameObject)
            {
                SwarmAgent agent = collider.GetComponent<SwarmAgent>();
                if (agent != null)
                    neighbors.Add(agent);
            }
        }
    }
    
    Vector3 CalculateSteering()
    {
        Vector3 separation = Separation() * separationWeight;
        Vector3 alignment = Alignment() * alignmentWeight;
        Vector3 cohesion = Cohesion() * cohesionWeight;
        
        return separation + alignment + cohesion;
    }
    
    // Avoid crowding neighbors
    Vector3 Separation()
    {
        Vector3 steer = Vector3.zero;
        int count = 0;
        
        foreach (var neighbor in neighbors)
        {
            float distance = Vector3.Distance(transform.position, neighbor.transform.position);
            if (distance > 0 && distance < perceptionRadius * 0.5f)
            {
                Vector3 diff = transform.position - neighbor.transform.position;
                diff.Normalize();
                diff /= distance; // Weight by distance
                steer += diff;
                count++;
            }
        }
        
        if (count > 0)
            steer /= count;
        
        return steer.normalized * maxForce;
    }
    
    // Steer towards average heading of neighbors
    Vector3 Alignment()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        foreach (var neighbor in neighbors)
        {
            sum += neighbor.velocity;
            count++;
        }
        
        if (count > 0)
        {
            sum /= count;
            sum.Normalize();
            sum *= maxSpeed;
            Vector3 steer = sum - velocity;
            return Vector3.ClampMagnitude(steer, maxForce);
        }
        
        return Vector3.zero;
    }
    
    // Steer towards average position of neighbors
    Vector3 Cohesion()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;
        
        foreach (var neighbor in neighbors)
        {
            sum += neighbor.transform.position;
            count++;
        }
        
        if (count > 0)
        {
            sum /= count;
            return Seek(sum);
        }
        
        return Vector3.zero;
    }
    
    Vector3 Seek(Vector3 target)
    {
        Vector3 desired = target - transform.position;
        desired.Normalize();
        desired *= maxSpeed;
        Vector3 steer = desired - velocity;
        return Vector3.ClampMagnitude(steer, maxForce);
    }
}
```

### Step 2: Create the Swarm Manager

```csharp
using UnityEngine;
using System.Collections.Generic;

public class SwarmManager : MonoBehaviour
{
    public static SwarmManager Instance { get; private set; }
    
    [Header("Spawn Settings")]
    public GameObject agentPrefab;
    public int agentCount = 100;
    public float spawnRadius = 10f;
    
    [Header("Boundaries")]
    public Vector3 boundarySize = new Vector3(50, 20, 50);
    public float boundaryForce = 10f;
    
    private List<SwarmAgent> agents = new List<SwarmAgent>();
    private SpatialPartition spatialPartition;
    
    void Awake()
    {
        Instance = this;
        spatialPartition = new SpatialPartition(boundarySize, 5f);
    }
    
    void Start()
    {
        SpawnAgents();
    }
    
    void SpawnAgents()
    {
        for (int i = 0; i < agentCount; i++)
        {
            Vector3 randomPos = Random.insideUnitSphere * spawnRadius;
            GameObject agent = Instantiate(agentPrefab, randomPos, Random.rotation);
            agent.name = $"Agent_{i}";
        }
    }
    
    public void RegisterAgent(SwarmAgent agent)
    {
        agents.Add(agent);
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, boundarySize);
    }
}
```

### Step 3: Spatial Partitioning for Performance

```csharp
public class SpatialPartition
{
    private Dictionary<int, List<SwarmAgent>> cells;
    private float cellSize;
    private Vector3 worldSize;
    
    public SpatialPartition(Vector3 worldSize, float cellSize)
    {
        this.worldSize = worldSize;
        this.cellSize = cellSize;
        cells = new Dictionary<int, List<SwarmAgent>>();
    }
    
    public void Clear()
    {
        cells.Clear();
    }
    
    public void Add(SwarmAgent agent)
    {
        int hash = GetCellHash(agent.transform.position);
        if (!cells.ContainsKey(hash))
            cells[hash] = new List<SwarmAgent>();
        cells[hash].Add(agent);
    }
    
    public List<SwarmAgent> GetNeighbors(Vector3 position, float radius)
    {
        List<SwarmAgent> neighbors = new List<SwarmAgent>();
        
        int cellRadius = Mathf.CeilToInt(radius / cellSize);
        int centerX = Mathf.FloorToInt(position.x / cellSize);
        int centerY = Mathf.FloorToInt(position.y / cellSize);
        int centerZ = Mathf.FloorToInt(position.z / cellSize);
        
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    int hash = GetCellHash(centerX + x, centerY + y, centerZ + z);
                    if (cells.ContainsKey(hash))
                    {
                        foreach (var agent in cells[hash])
                        {
                            float dist = Vector3.Distance(position, agent.transform.position);
                            if (dist <= radius)
                                neighbors.Add(agent);
                        }
                    }
                }
            }
        }
        
        return neighbors;
    }
    
    private int GetCellHash(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / cellSize);
        int y = Mathf.FloorToInt(position.y / cellSize);
        int z = Mathf.FloorToInt(position.z / cellSize);
        return GetCellHash(x, y, z);
    }
    
    private int GetCellHash(int x, int y, int z)
    {
        return x * 73856093 ^ y * 19349663 ^ z * 83492791;
    }
}
```

## Advanced Techniques {#advanced-techniques}

### Obstacle Avoidance

```csharp
public Vector3 AvoidObstacles()
{
    RaycastHit hit;
    Vector3 ahead = transform.position + velocity.normalized * avoidDistance;
    
    if (Physics.Raycast(transform.position, velocity.normalized, out hit, avoidDistance, obstacleLayer))
    {
        Vector3 avoidForce = Vector3.zero;
        avoidForce = hit.normal * maxForce;
        avoidForce.y = 0; // Keep on same plane
        return avoidForce;
    }
    
    return Vector3.zero;
}
```

### Target Following

```csharp
public class TargetFollowing : MonoBehaviour
{
    public Transform target;
    public float targetWeight = 2f;
    
    Vector3 FollowTarget()
    {
        if (target == null) return Vector3.zero;
        
        Vector3 desired = target.position - transform.position;
        float distance = desired.magnitude;
        
        // Slow down as we approach
        if (distance < slowingRadius)
        {
            float speed = maxSpeed * (distance / slowingRadius);
            desired = desired.normalized * speed;
        }
        else
        {
            desired = desired.normalized * maxSpeed;
        }
        
        Vector3 steer = desired - velocity;
        return Vector3.ClampMagnitude(steer, maxForce);
    }
}
```

### Formation Control

```csharp
public class FormationController : MonoBehaviour
{
    public enum Formation { Line, Circle, V, Square }
    public Formation currentFormation;
    
    Vector3 GetFormationPosition(int index, int total)
    {
        switch (currentFormation)
        {
            case Formation.Line:
                return new Vector3(index * spacing - (total * spacing / 2), 0, 0);
                
            case Formation.Circle:
                float angle = (index / (float)total) * Mathf.PI * 2;
                float radius = total * spacing / (2 * Mathf.PI);
                return new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                
            case Formation.V:
                bool isLeft = index % 2 == 0;
                int row = index / 2;
                float x = isLeft ? -row * spacing : row * spacing;
                return new Vector3(x, 0, -row * spacing);
                
            default:
                return Vector3.zero;
        }
    }
}
```

## Performance Optimization {#performance-optimization}

### Job System Implementation

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct SwarmUpdateJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<float3> velocities;
    [ReadOnly] public float deltaTime;
    [ReadOnly] public float maxSpeed;
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
        
        // Check all other agents
        for (int i = 0; i < positions.Length; i++)
        {
            if (i == index) continue;
            
            float3 diff = position - positions[i];
            float distance = math.length(diff);
            
            if (distance < perceptionRadius && distance > 0)
            {
                // Separation
                separation += diff / distance;
                
                // Alignment
                alignment += velocities[i];
                
                // Cohesion
                cohesion += positions[i];
                
                neighborCount++;
            }
        }
        
        if (neighborCount > 0)
        {
            separation /= neighborCount;
            alignment /= neighborCount;
            cohesion = (cohesion / neighborCount) - position;
        }
        
        float3 acceleration = separation + alignment + cohesion;
        velocity += acceleration * deltaTime;
        velocity = math.normalize(velocity) * math.min(math.length(velocity), maxSpeed);
        
        newVelocities[index] = velocity;
        newPositions[index] = position + velocity * deltaTime;
    }
}
```

### GPU Compute Shader

```hlsl
// SwarmCompute.compute
#pragma kernel UpdateSwarm

struct Agent
{
    float3 position;
    float3 velocity;
};

RWStructuredBuffer<Agent> agents;
float deltaTime;
float maxSpeed;
float perceptionRadius;
int agentCount;

[numthreads(64,1,1)]
void UpdateSwarm(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= agentCount) return;
    
    Agent agent = agents[id.x];
    float3 separation = float3(0, 0, 0);
    float3 alignment = float3(0, 0, 0);
    float3 cohesion = float3(0, 0, 0);
    int neighborCount = 0;
    
    for (int i = 0; i < agentCount; i++)
    {
        if (i == id.x) continue;
        
        float3 diff = agent.position - agents[i].position;
        float distance = length(diff);
        
        if (distance < perceptionRadius && distance > 0)
        {
            separation += normalize(diff) / distance;
            alignment += agents[i].velocity;
            cohesion += agents[i].position;
            neighborCount++;
        }
    }
    
    if (neighborCount > 0)
    {
        separation /= neighborCount;
        alignment = normalize(alignment / neighborCount) * maxSpeed;
        cohesion = (cohesion / neighborCount - agent.position);
    }
    
    float3 acceleration = separation + alignment + cohesion;
    agent.velocity += acceleration * deltaTime;
    agent.velocity = normalize(agent.velocity) * min(length(agent.velocity), maxSpeed);
    agent.position += agent.velocity * deltaTime;
    
    agents[id.x] = agent;
}
```

## Common Patterns {#common-patterns}

### Ant Colony Optimization

```csharp
public class AntColony : MonoBehaviour
{
    [Header("Pheromones")]
    public float pheromoneStrength = 1f;
    public float evaporationRate = 0.95f;
    public Texture2D pheromoneMap;
    
    void UpdatePheromones()
    {
        // Evaporate existing pheromones
        Color[] pixels = pheromoneMap.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] *= evaporationRate;
        }
        
        // Add new pheromones at agent positions
        foreach (var agent in agents)
        {
            Vector2 uv = WorldToUV(agent.transform.position);
            int x = (int)(uv.x * pheromoneMap.width);
            int y = (int)(uv.y * pheromoneMap.height);
            
            if (x >= 0 && x < pheromoneMap.width && y >= 0 && y < pheromoneMap.height)
            {
                Color current = pheromoneMap.GetPixel(x, y);
                current.r = Mathf.Min(current.r + pheromoneStrength, 1f);
                pheromoneMap.SetPixel(x, y, current);
            }
        }
        
        pheromoneMap.Apply();
    }
}
```

### Predator-Prey Dynamics

```csharp
public class PredatorPreySystem : MonoBehaviour
{
    public List<SwarmAgent> predators;
    public List<SwarmAgent> prey;
    
    void UpdatePredatorBehavior(SwarmAgent predator)
    {
        // Find nearest prey
        SwarmAgent nearestPrey = null;
        float minDistance = float.MaxValue;
        
        foreach (var p in prey)
        {
            float dist = Vector3.Distance(predator.transform.position, p.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestPrey = p;
            }
        }
        
        // Hunt behavior
        if (nearestPrey != null && minDistance < huntRadius)
        {
            Vector3 pursuit = Pursuit(predator, nearestPrey);
            predator.AddForce(pursuit * huntWeight);
        }
    }
    
    void UpdatePreyBehavior(SwarmAgent prey)
    {
        // Detect nearby predators
        Vector3 flee = Vector3.zero;
        
        foreach (var predator in predators)
        {
            float dist = Vector3.Distance(prey.transform.position, predator.transform.position);
            if (dist < fleeRadius)
            {
                Vector3 away = prey.transform.position - predator.transform.position;
                away = away.normalized / dist; // Stronger when closer
                flee += away;
            }
        }
        
        if (flee.magnitude > 0)
        {
            prey.AddForce(flee.normalized * fleeWeight * prey.maxForce);
        }
    }
}
```

## Debugging & Visualization {#debugging}

### Debug Visualization Component

```csharp
public class SwarmDebugger : MonoBehaviour
{
    public bool showPerceptionRadius = true;
    public bool showVelocity = true;
    public bool showNeighborConnections = true;
    public Color perceptionColor = new Color(0, 1, 0, 0.1f);
    public Color velocityColor = Color.blue;
    public Color connectionColor = new Color(1, 1, 0, 0.3f);
    
    void OnDrawGizmos()
    {
        SwarmAgent agent = GetComponent<SwarmAgent>();
        if (agent == null) return;
        
        // Perception radius
        if (showPerceptionRadius)
        {
            Gizmos.color = perceptionColor;
            Gizmos.DrawWireSphere(transform.position, agent.perceptionRadius);
        }
        
        // Velocity vector
        if (showVelocity && agent.velocity.magnitude > 0.1f)
        {
            Gizmos.color = velocityColor;
            Gizmos.DrawRay(transform.position, agent.velocity.normalized * 2f);
        }
        
        // Neighbor connections
        if (showNeighborConnections)
        {
            Gizmos.color = connectionColor;
            foreach (var neighbor in agent.neighbors)
            {
                if (neighbor != null)
                    Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
        }
    }
}
```

### Performance Monitor

```csharp
public class SwarmPerformanceMonitor : MonoBehaviour
{
    [SerializeField] private Text fpsText;
    [SerializeField] private Text agentCountText;
    [SerializeField] private Text avgNeighborsText;
    
    private float deltaTime;
    private SwarmManager swarmManager;
    
    void Start()
    {
        swarmManager = SwarmManager.Instance;
    }
    
    void Update()
    {
        // Calculate FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        
        // Update UI
        if (Time.frameCount % 30 == 0)
        {
            fpsText.text = $"FPS: {fps:0.}";
            agentCountText.text = $"Agents: {swarmManager.AgentCount}";
            avgNeighborsText.text = $"Avg Neighbors: {swarmManager.AverageNeighborCount:0.0}";
        }
    }
}
```

## Production Checklist {#production-checklist}

### Pre-Production

- [ ] Define target platform and performance requirements
- [ ] Determine maximum agent count needed
- [ ] Choose appropriate architecture (MonoBehaviour vs ECS vs GPU)
- [ ] Plan for scalability and optimization

### Implementation

- [ ] Implement core swarm behaviors
- [ ] Add spatial partitioning for > 100 agents
- [ ] Implement LOD system for distant agents
- [ ] Add obstacle avoidance
- [ ] Create boundary enforcement
- [ ] Implement formation controls (if needed)

### Optimization

- [ ] Profile on target hardware
- [ ] Implement object pooling
- [ ] Use appropriate data structures
- [ ] Minimize garbage allocation
- [ ] Consider Job System for CPU parallelization
- [ ] Implement GPU compute shaders for > 5000 agents

### Testing

- [ ] Test with minimum and maximum agent counts
- [ ] Verify behavior at boundaries
- [ ] Test obstacle avoidance edge cases
- [ ] Profile memory usage over time
- [ ] Test on all target platforms

### Polish

- [ ] Add runtime parameter tweaking
- [ ] Implement save/load for configurations
- [ ] Create visualization tools
- [ ] Add performance monitoring
- [ ] Document all parameters
- [ ] Create example scenes

### Common Issues & Solutions

**Issue**: Agents clumping together  
**Solution**: Increase separation weight, add minimum distance constraint

**Issue**: Jittery movement  
**Solution**: Smooth steering forces, limit acceleration

**Issue**: Poor performance with many agents  
**Solution**: Implement spatial partitioning, use Jobs/ECS

**Issue**: Agents leaving boundaries  
**Solution**: Add boundary force, implement wraparound or reflection

**Issue**: Unrealistic turning  
**Solution**: Limit angular velocity, smooth rotation

---

*Implementation guide compiled by Claude Flow Swarm System*  
*For production use in Unity 2021.3+*