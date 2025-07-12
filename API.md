# Unity Swarm AI - API Reference

Complete API documentation for Unity Swarm AI Plugin v1.0.0

## Table of Contents

1. [Core Classes](#core-classes)
2. [Behavior System](#behavior-system)
3. [Spatial Partitioning](#spatial-partitioning)
4. [Formation System](#formation-system)
5. [Performance](#performance)
6. [Utilities](#utilities)
7. [Events & Callbacks](#events--callbacks)
8. [Editor Tools](#editor-tools)

## Core Classes

### SwarmAgent

The main component representing an individual agent in the swarm.

```csharp
public class SwarmAgent : MonoBehaviour, ISwarmAgent
```

#### Properties

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `Position` | `Vector3` | Current world position (read-only) | - |
| `Velocity` | `Vector3` | Current velocity vector (read-only) | - |
| `MaxSpeed` | `float` | Maximum movement speed | 5.0f |
| `MaxForce` | `float` | Maximum steering force | 10.0f |
| `PerceptionRadius` | `float` | Neighbor detection radius | 3.0f |
| `AgentLayer` | `LayerMask` | Layer for agent detection | Everything |
| `SeparationWeight` | `float` | Separation behavior weight | 1.5f |
| `AlignmentWeight` | `float` | Alignment behavior weight | 1.0f |
| `CohesionWeight` | `float` | Cohesion behavior weight | 1.0f |
| `AvoidanceWeight` | `float` | Obstacle avoidance weight | 2.0f |
| `AgentID` | `int` | Unique identifier for this agent (read-only) | Auto-assigned |
| `IsActive` | `bool` | Whether agent is actively updating | true |

#### Core Methods

```csharp
// Movement and steering
public void ApplyForce(Vector3 force)
public void SetVelocity(Vector3 newVelocity)
public void SetTarget(Transform target)
public void ClearTarget()

// Behavior control
public void AddBehavior(SwarmBehavior behavior)
public void RemoveBehavior(SwarmBehavior behavior)
public void SetBehaviorWeight(Type behaviorType, float weight)
public T GetBehavior<T>() where T : SwarmBehavior

// State management
public void SetState(SwarmState newState)
public SwarmState GetCurrentState()
public void Pause()
public void Resume()

// Neighbor management
public List<SwarmAgent> GetNeighbors()
public List<SwarmAgent> GetNeighborsInRadius(float radius)
public SwarmAgent GetNearestNeighbor()
public int GetNeighborCount()
```

#### Events

```csharp
public event Action<SwarmAgent> OnTargetReached;
public event Action<SwarmAgent, Collision> OnObstacleHit;
public event Action<SwarmAgent> OnStateChanged;
public event Action<SwarmAgent> OnAgentDestroyed;
```

#### Example Usage

```csharp
// Basic agent setup
var agent = gameObject.AddComponent<SwarmAgent>();
agent.MaxSpeed = 8f;
agent.PerceptionRadius = 5f;
agent.SeparationWeight = 2f;

// Add custom behavior
agent.AddBehavior(new WanderBehavior());
agent.AddBehavior(new ObstacleAvoidance());

// Set target
agent.SetTarget(targetTransform);

// Event handling
agent.OnTargetReached += (agent) => Debug.Log("Target reached!");
```

### SwarmManager

Central coordinator for managing multiple swarm agents and global behaviors.

```csharp
public class SwarmManager : MonoBehaviour
```

#### Properties

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `AgentPrefab` | `GameObject` | Prefab to spawn for new agents | null |
| `AgentCount` | `int` | Number of agents to spawn | 100 |
| `SpawnRadius` | `float` | Radius for initial agent placement | 10f |
| `BoundarySize` | `Vector3` | World boundary dimensions | (50,20,50) |
| `BoundaryForce` | `float` | Force applied at boundaries | 10f |
| `UseSpatialPartitioning` | `bool` | Enable spatial optimization | true |
| `PartitionCellSize` | `float` | Size of spatial grid cells | 5f |
| `UpdateRate` | `int` | Frames between swarm updates | 1 |
| `Agents` | `List<SwarmAgent>` | All managed agents (read-only) | - |

#### Core Methods

```csharp
// Agent management
public void SpawnAgents()
public void SpawnAgents(int count, Vector3 center, float radius)
public SwarmAgent SpawnSingleAgent(Vector3 position)
public void DestroyAgent(SwarmAgent agent)
public void DestroyAllAgents()

// Swarm control
public void SetGlobalTarget(Transform target)
public void ClearGlobalTarget()
public void SetFormation(FormationType formation)
public void SetFormation(IFormation customFormation)
public void AddGlobalBehavior(SwarmBehavior behavior)
public void RemoveGlobalBehavior(SwarmBehavior behavior)

// Performance
public void SetUpdateRate(int framesPerUpdate)
public void EnableSpatialPartitioning(bool enable, float cellSize = 5f)
public void SetLODEnabled(bool enabled)

// Utility
public List<SwarmAgent> GetAgentsInRadius(Vector3 center, float radius)
public SwarmAgent GetNearestAgent(Vector3 position)
public Vector3 GetSwarmCenter()
public Vector3 GetSwarmVelocity()
public float GetSwarmRadius()
```

#### Events

```csharp
public event Action<SwarmManager> OnSwarmInitialized;
public event Action<SwarmAgent> OnAgentSpawned;
public event Action<SwarmAgent> OnAgentDestroyed;
public event Action<FormationType> OnFormationChanged;
```

#### Example Usage

```csharp
// Setup swarm manager
var manager = gameObject.AddComponent<SwarmManager>();
manager.AgentPrefab = agentPrefab;
manager.AgentCount = 200;
manager.SpawnRadius = 15f;

// Initialize swarm
manager.SpawnAgents();

// Set formation
manager.SetFormation(FormationType.VFormation);

// Add global behavior
manager.AddGlobalBehavior(new FlockingBehavior());
```

## Behavior System

### SwarmBehavior (Abstract Base)

Base class for all swarm behaviors.

```csharp
public abstract class SwarmBehavior : ScriptableObject
{
    public float Weight { get; set; } = 1.0f;
    public bool IsEnabled { get; set; } = true;
    
    public abstract Vector3 CalculateForce(SwarmAgent agent, List<SwarmAgent> neighbors);
    public virtual void OnBehaviorAdded(SwarmAgent agent) { }
    public virtual void OnBehaviorRemoved(SwarmAgent agent) { }
}
```

### Built-in Behaviors

#### FlockingBehavior

Classic Reynolds boids implementation with separation, alignment, and cohesion.

```csharp
public class FlockingBehavior : SwarmBehavior
{
    [Range(0f, 5f)] public float SeparationRadius = 1.5f;
    [Range(0f, 5f)] public float AlignmentRadius = 2.5f;
    [Range(0f, 5f)] public float CohesionRadius = 3.0f;
    
    public override Vector3 CalculateForce(SwarmAgent agent, List<SwarmAgent> neighbors)
    {
        Vector3 separation = CalculateSeparation(agent, neighbors);
        Vector3 alignment = CalculateAlignment(agent, neighbors);
        Vector3 cohesion = CalculateCohesion(agent, neighbors);
        
        return separation + alignment + cohesion;
    }
}
```

#### SeekBehavior

Moves agent toward a specific target.

```csharp
public class SeekBehavior : SwarmBehavior
{
    public Transform Target { get; set; }
    [Range(0f, 10f)] public float SlowingRadius = 3f;
    [Range(0f, 1f)] public float ArrivalThreshold = 0.1f;
    
    public override Vector3 CalculateForce(SwarmAgent agent, List<SwarmAgent> neighbors)
    {
        if (Target == null) return Vector3.zero;
        
        Vector3 desired = Target.position - agent.Position;
        float distance = desired.magnitude;
        
        if (distance < ArrivalThreshold)
        {
            agent.OnTargetReached?.Invoke(agent);
            return Vector3.zero;
        }
        
        // Slow down as we approach
        if (distance < SlowingRadius)
        {
            desired = desired.normalized * agent.MaxSpeed * (distance / SlowingRadius);
        }
        else
        {
            desired = desired.normalized * agent.MaxSpeed;
        }
        
        return Vector3.ClampMagnitude(desired - agent.Velocity, agent.MaxForce);
    }
}
```

#### ObstacleAvoidance

Prevents agents from colliding with obstacles.

```csharp
public class ObstacleAvoidance : SwarmBehavior
{
    [Range(0.5f, 10f)] public float AvoidDistance = 3f;
    [Range(0f, 180f)] public float ViewAngle = 45f;
    public LayerMask ObstacleLayer = -1;
    
    public override Vector3 CalculateForce(SwarmAgent agent, List<SwarmAgent> neighbors)
    {
        Vector3 ahead = agent.Position + agent.Velocity.normalized * AvoidDistance;
        
        // Raycast for obstacles
        if (Physics.Raycast(agent.Position, agent.Velocity.normalized, out RaycastHit hit, AvoidDistance, ObstacleLayer))
        {
            Vector3 avoidForce = hit.normal * agent.MaxForce;
            return avoidForce;
        }
        
        return Vector3.zero;
    }
}
```

#### WanderBehavior

Adds random wandering movement to agents.

```csharp
public class WanderBehavior : SwarmBehavior
{
    [Range(0.1f, 5f)] public float WanderRadius = 1f;
    [Range(0.1f, 5f)] public float WanderDistance = 2f;
    [Range(0.1f, 1f)] public float WanderJitter = 0.3f;
    
    private Vector3 wanderTarget;
    
    public override Vector3 CalculateForce(SwarmAgent agent, List<SwarmAgent> neighbors)
    {
        // Add random jitter to wander target
        wanderTarget += new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ) * WanderJitter;
        
        // Normalize and scale
        wanderTarget = wanderTarget.normalized * WanderRadius;
        
        // Project in front of agent
        Vector3 wanderForce = agent.Position + agent.Velocity.normalized * WanderDistance + wanderTarget;
        
        return Vector3.ClampMagnitude(wanderForce - agent.Position, agent.MaxForce);
    }
}
```

### Custom Behavior Creation

```csharp
[CreateAssetMenu(menuName = "Swarm AI/Custom Behavior")]
public class CustomBehavior : SwarmBehavior
{
    [Header("Custom Parameters")]
    public float customParameter = 1f;
    
    public override Vector3 CalculateForce(SwarmAgent agent, List<SwarmAgent> neighbors)
    {
        // Implement your custom logic here
        Vector3 customForce = Vector3.zero;
        
        // Example: Move away from center
        Vector3 center = SwarmManager.Instance.GetSwarmCenter();
        Vector3 awayFromCenter = (agent.Position - center).normalized;
        customForce = awayFromCenter * customParameter;
        
        return Vector3.ClampMagnitude(customForce, agent.MaxForce);
    }
    
    public override void OnBehaviorAdded(SwarmAgent agent)
    {
        Debug.Log($"Custom behavior added to {agent.name}");
    }
}
```

## Spatial Partitioning

### SpatialPartition

High-performance neighbor detection system.

```csharp
public class SpatialPartition<T> where T : ISwarmAgent
{
    public SpatialPartition(Vector3 worldSize, float cellSize)
    
    // Core methods
    public void Clear()
    public void Add(T agent)
    public List<T> Query(Vector3 position, float radius)
    public List<T> QueryBox(Bounds bounds)
    public int GetCellCount()
    public float GetLoadFactor()
}
```

### UniformGrid

Grid-based spatial partitioning for uniform distributions.

```csharp
public class UniformGrid : SpatialPartition<SwarmAgent>
{
    // Specialized for uniform agent distributions
    public void OptimizeForDensity(float agentDensity)
    public void SetDynamicCellSize(bool enabled)
    public Vector3 GetOptimalCellSize(List<SwarmAgent> agents)
}
```

### Octree

Hierarchical spatial partitioning for clustered distributions.

```csharp
public class SwarmOctree : SpatialPartition<SwarmAgent>
{
    public int MaxDepth { get; set; } = 6;
    public int MaxAgentsPerNode { get; set; } = 10;
    
    public void SetMaxDepth(int depth)
    public void SetCapacity(int maxAgents)
    public OctreeStats GetStatistics()
}
```

## Formation System

### IFormation Interface

```csharp
public interface IFormation
{
    Vector3 GetPositionForAgent(int agentIndex, int totalAgents, Vector3 center);
    Vector3 GetDirectionForAgent(int agentIndex, int totalAgents, Vector3 center);
    float GetSpacing();
}
```

### Built-in Formations

#### LineFormation

```csharp
public class LineFormation : IFormation
{
    public float Spacing { get; set; } = 2f;
    public Vector3 Direction { get; set; } = Vector3.right;
    
    public Vector3 GetPositionForAgent(int agentIndex, int totalAgents, Vector3 center)
    {
        float offset = (agentIndex - totalAgents / 2f) * Spacing;
        return center + Direction * offset;
    }
}
```

#### CircleFormation

```csharp
public class CircleFormation : IFormation
{
    public float Radius { get; set; } = 5f;
    
    public Vector3 GetPositionForAgent(int agentIndex, int totalAgents, Vector3 center)
    {
        float angle = (agentIndex / (float)totalAgents) * Mathf.PI * 2;
        return center + new Vector3(
            Mathf.Cos(angle) * Radius,
            0,
            Mathf.Sin(angle) * Radius
        );
    }
}
```

#### VFormation

```csharp
public class VFormation : IFormation
{
    public float Spacing { get; set; } = 2f;
    public float Angle { get; set; } = 45f;
    
    public Vector3 GetPositionForAgent(int agentIndex, int totalAgents, Vector3 center)
    {
        if (agentIndex == 0) return center; // Leader
        
        bool isLeftSide = agentIndex % 2 == 1;
        int row = (agentIndex + 1) / 2;
        
        float x = isLeftSide ? -row * Spacing : row * Spacing;
        float z = -row * Spacing * Mathf.Tan(Angle * Mathf.Deg2Rad);
        
        return center + new Vector3(x, 0, z);
    }
}
```

### Formation Controller

```csharp
public class FormationController : MonoBehaviour
{
    public IFormation CurrentFormation { get; private set; }
    public float TransitionSpeed = 2f;
    public bool MaintainFormation = true;
    
    public void SetFormation(IFormation formation)
    public void TransitionToFormation(IFormation formation, float duration)
    public void BreakFormation()
    public void ReformFormation()
    
    // Formation analysis
    public float GetFormationCohesion()
    public float GetFormationAccuracy()
    public Vector3 GetFormationCenter()
}
```

## Performance

### SwarmLODSystem

Level-of-Detail system for performance scaling.

```csharp
public class SwarmLODSystem : MonoBehaviour
{
    public enum LODLevel { Full, Reduced, Minimal, Culled }
    
    [Header("LOD Distances")]
    public float FullDetailDistance = 20f;
    public float ReducedDetailDistance = 50f;
    public float MinimalDetailDistance = 100f;
    public float CullDistance = 200f;
    
    public LODLevel GetLODLevel(SwarmAgent agent, Camera camera)
    public void ApplyLOD(SwarmAgent agent, LODLevel level)
    public void UpdateAllAgentLODs()
    
    // Performance monitoring
    public int GetActiveAgentCount()
    public float GetAverageUpdateTime()
    public void LogPerformanceStats()
}
```

### Job System Integration

```csharp
[BurstCompile]
public struct SwarmUpdateJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> positions;
    [ReadOnly] public NativeArray<float3> velocities;
    [ReadOnly] public float deltaTime;
    [ReadOnly] public float maxSpeed;
    [ReadOnly] public float maxForce;
    
    public NativeArray<float3> newVelocities;
    public NativeArray<float3> newPositions;
    
    public void Execute(int index)
    {
        // High-performance swarm update logic
        float3 position = positions[index];
        float3 velocity = velocities[index];
        
        // Calculate steering forces
        float3 steering = CalculateSteering(index);
        
        // Update velocity and position
        velocity += steering * deltaTime;
        velocity = math.normalize(velocity) * math.min(math.length(velocity), maxSpeed);
        position += velocity * deltaTime;
        
        newVelocities[index] = velocity;
        newPositions[index] = position;
    }
}
```

## Utilities

### SwarmMath

Mathematical utilities for swarm calculations.

```csharp
public static class SwarmMath
{
    // Vector operations
    public static Vector3 ClampMagnitude(Vector3 vector, float maxMagnitude)
    public static Vector3 RandomOnUnitCircle()
    public static Vector3 RandomOnUnitSphere()
    
    // Interpolation
    public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 velocity, float smoothTime)
    public static float AngleBetween(Vector3 a, Vector3 b)
    
    // Spatial calculations
    public static bool IsInCone(Vector3 position, Vector3 forward, Vector3 target, float angle)
    public static Vector3 ProjectOnPlane(Vector3 vector, Vector3 planeNormal)
    public static Vector3 GetClosestPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
}
```

### SwarmDebugger

Comprehensive debugging and visualization tools.

```csharp
public class SwarmDebugger : MonoBehaviour
{
    [Header("Visualization")]
    public bool ShowPerceptionRadius = true;
    public bool ShowVelocityVectors = true;
    public bool ShowNeighborConnections = false;
    public bool ShowForces = false;
    public bool ShowFormation = true;
    
    [Header("Colors")]
    public Color PerceptionColor = Color.green;
    public Color VelocityColor = Color.blue;
    public Color ConnectionColor = Color.yellow;
    public Color ForceColor = Color.red;
    
    // Debug methods
    public void DrawPerceptionRadius(SwarmAgent agent)
    public void DrawVelocityVector(SwarmAgent agent)
    public void DrawNeighborConnections(SwarmAgent agent)
    public void DrawForceVector(SwarmAgent agent, Vector3 force, Color color)
    
    // Performance debugging
    public void LogSwarmStats()
    public void StartPerformanceProfiler()
    public void StopPerformanceProfiler()
}
```

## Events & Callbacks

### SwarmEvents

Global event system for swarm coordination.

```csharp
public static class SwarmEvents
{
    // Agent events
    public static event Action<SwarmAgent> OnAgentCreated;
    public static event Action<SwarmAgent> OnAgentDestroyed;
    public static event Action<SwarmAgent, Vector3> OnAgentMoved;
    
    // Swarm events
    public static event Action<SwarmManager> OnSwarmCreated;
    public static event Action<SwarmManager> OnSwarmDestroyed;
    public static event Action<FormationType> OnFormationChanged;
    
    // Performance events
    public static event Action<int> OnAgentCountChanged;
    public static event Action<float> OnFrameTimeChanged;
    
    // Trigger methods
    public static void TriggerAgentCreated(SwarmAgent agent)
    public static void TriggerFormationChanged(FormationType formation)
}
```

## Editor Tools

### SwarmAgentEditor

Custom inspector for SwarmAgent components.

```csharp
[CustomEditor(typeof(SwarmAgent))]
public class SwarmAgentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Custom inspector with real-time preview
        SwarmAgent agent = (SwarmAgent)target;
        
        // Behavior management UI
        EditorGUILayout.LabelField("Behaviors", EditorStyles.boldLabel);
        DrawBehaviorList(agent);
        DrawAddBehaviorButton(agent);
        
        // Real-time stats
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Stats", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Speed: {agent.Velocity.magnitude:F2}");
            EditorGUILayout.LabelField($"Neighbors: {agent.GetNeighborCount()}");
        }
    }
    
    public override bool HasPreviewGUI() => true;
    
    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        // Draw agent preview with behavior visualization
    }
}
```

### SwarmManagerEditor

Enhanced editor for SwarmManager with scene view tools.

```csharp
[CustomEditor(typeof(SwarmManager))]
public class SwarmManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SwarmManager manager = (SwarmManager)target;
        
        // Quick setup buttons
        if (GUILayout.Button("Auto-Setup Demo Scene"))
        {
            SetupDemoScene(manager);
        }
        
        if (GUILayout.Button("Performance Test"))
        {
            RunPerformanceTest(manager);
        }
        
        // Real-time monitoring
        if (Application.isPlaying && manager.Agents.Count > 0)
        {
            DrawPerformanceStats(manager);
        }
    }
    
    void OnSceneGUI()
    {
        SwarmManager manager = (SwarmManager)target;
        
        // Draw spawn area
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(manager.transform.position, Vector3.up, manager.SpawnRadius);
        
        // Draw boundary
        Handles.color = Color.red;
        Handles.DrawWireCube(manager.transform.position, manager.BoundarySize);
    }
}
```

---

## Quick Reference

### Most Common Usage Patterns

```csharp
// Basic flocking setup
var agent = gameObject.AddComponent<SwarmAgent>();
agent.AddBehavior(new FlockingBehavior());

// Target following
agent.AddBehavior(new SeekBehavior { Target = targetTransform });

// Obstacle avoidance
agent.AddBehavior(new ObstacleAvoidance { ObstacleLayer = obstacleLayer });

// Formation flying
var manager = GetComponent<SwarmManager>();
manager.SetFormation(FormationType.VFormation);

// Performance optimization
manager.EnableSpatialPartitioning(true, 5f);
manager.SetLODEnabled(true);
```

### Performance Tips

1. **Use spatial partitioning** for >50 agents
2. **Enable LOD system** for >200 agents  
3. **Consider Job System** for >500 agents
4. **Use GPU compute** for >2000 agents
5. **Limit behavior count** per agent (3-5 max)
6. **Pool agent GameObjects** for dynamic spawning
7. **Update at fixed intervals** instead of every frame

*This API documentation covers Unity Swarm AI Plugin v1.0.0. For the latest updates, visit our [GitHub repository](https://github.com/ruvnet/unity-swarm-ai).*