# Unity Swarm AI - Troubleshooting Guide

**Quick solutions to common issues and problems when using Unity Swarm AI.** 🔧

This guide covers the most frequently encountered issues, their causes, and step-by-step solutions to get your swarms working perfectly.

## Quick Diagnostic Checklist

Before diving into specific issues, run through this quick checklist:

- ✅ **Unity Version**: 2021.3 LTS or newer
- ✅ **Plugin Installation**: Package appears in Package Manager
- ✅ **Components Added**: SwarmManager and SwarmAgent components present
- ✅ **Prefab Assigned**: AgentPrefab field is not null
- ✅ **Layers Set**: Agents and obstacles on correct layers
- ✅ **Console Clear**: No compilation errors in Console

## Installation Issues

### Issue: Package Not Found / Installation Failed

**Symptoms:**
- Error: "Package not found" when adding via Git URL
- Package Manager shows "Error" status
- Unity freezes during installation

**Solutions:**

#### Method 1: Check Internet Connection
```bash
# Test Git connectivity
git clone https://github.com/ruvnet/unity-swarm-ai.git

# If this fails, check your network settings
```

#### Method 2: Manual Installation
1. Download repository as ZIP
2. Extract to `Assets/SwarmAI/` folder
3. Unity will automatically import

#### Method 3: Package Manager Reset
1. Close Unity
2. Delete `Library/PackageCache/` folder
3. Reopen Unity and try again

#### Method 4: Alternative Git URL
```
# Try HTTPS instead of SSH
https://github.com/ruvnet/unity-swarm-ai.git

# Or use specific version
https://github.com/ruvnet/unity-swarm-ai.git#v1.0.0
```

### Issue: Missing Dependencies

**Symptoms:**
- Compilation errors about missing namespaces
- "The type or namespace 'Mathematics' could not be found"
- "Assembly 'Unity.Burst' not found"

**Solution:**
```bash
# Install required dependencies via Package Manager
# Window → Package Manager → Unity Registry

1. Unity Mathematics (com.unity.mathematics)
2. Unity Collections (com.unity.collections)  
3. Unity Burst (com.unity.burst)
4. Unity Jobs (com.unity.jobs)
```

### Issue: Wrong Unity Version

**Symptoms:**
- "This package requires Unity 2021.3 or newer"
- API compatibility errors
- Missing MonoBehaviour methods

**Solution:**
- **Upgrade Unity**: Download Unity 2021.3 LTS or newer
- **Version Check**: Help → About Unity → Version should be 2021.3+
- **Compatibility Mode**: Some features may be disabled in older versions

## Setup and Configuration Issues

### Issue: Agents Not Spawning

**Symptoms:**
- SwarmManager runs but no agents appear
- "Spawned 0 agents" in console
- Empty scene despite non-zero AgentCount

**Diagnosis Script:**
```csharp
public class SpawnDiagnostic : MonoBehaviour
{
    void Start()
    {
        var manager = GetComponent<SwarmManager>();
        
        Debug.Log($"Agent Prefab: {(manager.AgentPrefab ? "✓" : "❌ NULL")}");
        Debug.Log($"Agent Count: {manager.AgentCount}");
        Debug.Log($"Spawn Radius: {manager.SpawnRadius}");
        
        if (manager.AgentPrefab == null)
        {
            Debug.LogError("SOLUTION: Assign AgentPrefab in SwarmManager!");
        }
        
        if (manager.AgentCount <= 0)
        {
            Debug.LogError("SOLUTION: Set AgentCount > 0 in SwarmManager!");
        }
    }
}
```

**Solutions:**

#### Check 1: Prefab Assignment
```csharp
// Ensure AgentPrefab is assigned
SwarmManager manager = GetComponent<SwarmManager>();
if (manager.AgentPrefab == null)
{
    Debug.LogError("AgentPrefab is not assigned!");
    // Assign in inspector or via code:
    manager.AgentPrefab = Resources.Load<GameObject>("SwarmAgentPrefab");
}
```

#### Check 2: Agent Count
```csharp
// Verify agent count is valid
if (manager.AgentCount <= 0)
{
    manager.AgentCount = 100; // Set default
}
```

#### Check 3: Manual Spawn
```csharp
// Force spawn for testing
void ForceSpawn()
{
    SwarmManager manager = FindObjectOfType<SwarmManager>();
    manager.SpawnAgents(); // Call manually
}
```

### Issue: Agents Not Moving/Flocking

**Symptoms:**
- Agents spawn but remain stationary
- No flocking behavior visible
- Agents move randomly without coordination

**Diagnosis Script:**
```csharp
public class MovementDiagnostic : MonoBehaviour
{
    void Update()
    {
        var agents = FindObjectsOfType<SwarmAgent>();
        
        if (agents.Length == 0)
        {
            Debug.LogError("No SwarmAgent components found!");
            return;
        }
        
        var firstAgent = agents[0];
        Debug.Log($"Agent Velocity: {firstAgent.Velocity.magnitude:F2}");
        Debug.Log($"Max Speed: {firstAgent.MaxSpeed}");
        Debug.Log($"Neighbors: {firstAgent.GetNeighborCount()}");
        Debug.Log($"Perception Radius: {firstAgent.PerceptionRadius}");
    }
}
```

**Solutions:**

#### Check 1: Component Configuration
```csharp
// Verify SwarmAgent settings
SwarmAgent agent = GetComponent<SwarmAgent>();
agent.MaxSpeed = 5f;           // Must be > 0
agent.MaxForce = 10f;          // Must be > 0
agent.PerceptionRadius = 3f;   // Must be > 0

// Check behavior weights
agent.SeparationWeight = 1.5f;
agent.AlignmentWeight = 1.0f;
agent.CohesionWeight = 1.0f;
```

#### Check 2: Layer Setup
```csharp
// Verify agent layer configuration
LayerMask agentLayer = LayerMask.GetMask("SwarmAgents");
agent.AgentLayer = agentLayer;

// Ensure agents are on the correct layer
gameObject.layer = LayerMask.NameToLayer("SwarmAgents");
```

#### Check 3: Force Application
```csharp
// Test force application manually
void TestMovement()
{
    SwarmAgent agent = GetComponent<SwarmAgent>();
    agent.ApplyForce(Vector3.forward * agent.MaxForce);
}
```

### Issue: Performance Problems

**Symptoms:**
- Low FPS (< 30)
- Frame stuttering or freezing
- High CPU/GPU usage
- Unity becomes unresponsive

**Performance Diagnostic:**
```csharp
public class PerformanceDiagnostic : MonoBehaviour
{
    private float deltaTime;
    
    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        
        if (fps < 30f)
        {
            Debug.LogWarning($"LOW FPS: {fps:F1}");
            AnalyzePerformance();
        }
    }
    
    void AnalyzePerformance()
    {
        var swarmManager = FindObjectOfType<SwarmManager>();
        int agentCount = swarmManager.Agents.Count;
        
        Debug.Log($"Agent Count: {agentCount}");
        Debug.Log($"Spatial Partitioning: {swarmManager.UseSpatialPartitioning}");
        Debug.Log($"Update Rate: {swarmManager.UpdateRate}");
        
        // Performance recommendations
        if (agentCount > 200 && !swarmManager.UseSpatialPartitioning)
        {
            Debug.LogError("SOLUTION: Enable spatial partitioning for better performance!");
        }
        
        if (agentCount > 500 && swarmManager.UpdateRate == 1)
        {
            Debug.LogError("SOLUTION: Increase update rate to 2-3 for better performance!");
        }
    }
}
```

**Quick Performance Fixes:**

#### Fix 1: Enable Optimizations
```csharp
// Auto-optimize based on agent count
public void AutoOptimize()
{
    SwarmManager manager = GetComponent<SwarmManager>();
    int agentCount = manager.Agents.Count;
    
    if (agentCount > 100)
    {
        manager.UseSpatialPartitioning = true;
        manager.PartitionCellSize = 5f;
    }
    
    if (agentCount > 300)
    {
        manager.UpdateRate = 2; // Every 2 frames
    }
    
    if (agentCount > 500)
    {
        manager.UpdateRate = 3; // Every 3 frames
        
        // Reduce perception radius
        foreach (var agent in manager.Agents)
        {
            agent.PerceptionRadius = Mathf.Min(agent.PerceptionRadius, 3f);
        }
    }
}
```

#### Fix 2: LOD System
```csharp
// Simple distance-based LOD
public class SimpleLOD : MonoBehaviour
{
    public float maxDistance = 50f;
    private Camera playerCamera;
    
    void Start()
    {
        playerCamera = Camera.main;
        InvokeRepeating(nameof(UpdateLOD), 0.1f, 0.1f);
    }
    
    void UpdateLOD()
    {
        var agents = FindObjectsOfType<SwarmAgent>();
        
        foreach (var agent in agents)
        {
            float distance = Vector3.Distance(agent.transform.position, playerCamera.transform.position);
            
            if (distance > maxDistance)
            {
                agent.gameObject.SetActive(false); // Cull distant agents
            }
            else
            {
                agent.gameObject.SetActive(true);
            }
        }
    }
}
```

## Behavior Issues

### Issue: Agents Clumping Together

**Symptoms:**
- All agents form a tight ball
- No separation between agents
- Agents overlap visually

**Solutions:**

#### Solution 1: Increase Separation
```csharp
// Increase separation weight and reduce cohesion
SwarmAgent agent = GetComponent<SwarmAgent>();
agent.SeparationWeight = 3f;  // Increase from default 1.5f
agent.CohesionWeight = 0.5f;  // Reduce from default 1.0f
agent.AlignmentWeight = 1f;   // Keep moderate
```

#### Solution 2: Adjust Perception Radius
```csharp
// Smaller perception radius = less crowding
agent.PerceptionRadius = 2f; // Reduce from default 3f
```

#### Solution 3: Add Minimum Distance
```csharp
public class MinimumDistance : SwarmBehavior
{
    public float minimumDistance = 1f;
    
    public override Vector3 CalculateForce(SwarmAgent agent, List<SwarmAgent> neighbors)
    {
        Vector3 force = Vector3.zero;
        
        foreach (var neighbor in neighbors)
        {
            float distance = Vector3.Distance(agent.Position, neighbor.Position);
            if (distance < minimumDistance && distance > 0)
            {
                Vector3 away = (agent.Position - neighbor.Position).normalized;
                force += away * (minimumDistance - distance);
            }
        }
        
        return force * agent.MaxForce;
    }
}
```

### Issue: Agents Flying Away/Dispersing

**Symptoms:**
- Agents spread out and never group
- Swarm breaks apart immediately
- No cohesive movement

**Solutions:**

#### Solution 1: Increase Cohesion
```csharp
// Increase cohesion and reduce separation
agent.CohesionWeight = 2f;    // Increase from default 1.0f
agent.SeparationWeight = 0.8f; // Reduce from default 1.5f
```

#### Solution 2: Add Boundary Force
```csharp
// Ensure boundary settings keep agents together
SwarmManager manager = GetComponent<SwarmManager>();
manager.BoundarySize = new Vector3(30, 15, 30); // Smaller boundary
manager.BoundaryForce = 20f; // Stronger boundary force
```

#### Solution 3: Center Attraction
```csharp
public class CenterAttraction : SwarmBehavior
{
    public Transform centerPoint;
    public float attractionStrength = 1f;
    
    public override Vector3 CalculateForce(SwarmAgent agent, List<SwarmAgent> neighbors)
    {
        if (centerPoint == null) return Vector3.zero;
        
        Vector3 toCenter = centerPoint.position - agent.Position;
        float distance = toCenter.magnitude;
        
        if (distance > 0)
        {
            return toCenter.normalized * attractionStrength * agent.MaxForce;
        }
        
        return Vector3.zero;
    }
}
```

### Issue: Agents Not Avoiding Obstacles

**Symptoms:**
- Agents pass through obstacles
- No collision detection
- Agents get stuck in walls

**Solutions:**

#### Solution 1: Layer Configuration
```csharp
// Set up layers correctly
// 1. Create "Obstacles" layer (Layer 9)
// 2. Set obstacle objects to Obstacles layer
// 3. Configure agent obstacle detection

SwarmAgent agent = GetComponent<SwarmAgent>();
agent.ObstacleLayer = LayerMask.GetMask("Obstacles");
agent.AvoidanceWeight = 3f; // Strong avoidance
```

#### Solution 2: Collider Setup
```csharp
// Ensure obstacles have colliders
public void SetupObstacle(GameObject obstacle)
{
    // Add collider if missing
    if (obstacle.GetComponent<Collider>() == null)
    {
        obstacle.AddComponent<BoxCollider>();
    }
    
    // Set to obstacle layer
    obstacle.layer = LayerMask.NameToLayer("Obstacles");
}
```

#### Solution 3: Enhanced Avoidance
```csharp
public class EnhancedObstacleAvoidance : SwarmBehavior
{
    public LayerMask obstacleLayer = -1;
    public float avoidDistance = 5f;
    public int rayCount = 5;
    
    public override Vector3 CalculateForce(SwarmAgent agent, List<SwarmAgent> neighbors)
    {
        Vector3 avoidForce = Vector3.zero;
        
        // Cast multiple rays in forward direction
        for (int i = 0; i < rayCount; i++)
        {
            float angle = -45f + (90f / rayCount) * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * agent.transform.forward;
            
            if (Physics.Raycast(agent.Position, direction, out RaycastHit hit, avoidDistance, obstacleLayer))
            {
                Vector3 avoidDirection = Vector3.Reflect(direction, hit.normal);
                float strength = 1f - (hit.distance / avoidDistance);
                avoidForce += avoidDirection * strength;
            }
        }
        
        return Vector3.ClampMagnitude(avoidForce, agent.MaxForce);
    }
}
```

## Runtime Issues

### Issue: Memory Leaks

**Symptoms:**
- Memory usage increases over time
- Performance degrades after running for a while
- Unity eventually crashes

**Detection Script:**
```csharp
public class MemoryMonitor : MonoBehaviour
{
    private float startMemory;
    private float lastCheckTime;
    
    void Start()
    {
        startMemory = GetMemoryUsage();
        lastCheckTime = Time.time;
    }
    
    void Update()
    {
        if (Time.time - lastCheckTime > 5f) // Check every 5 seconds
        {
            float currentMemory = GetMemoryUsage();
            float increase = currentMemory - startMemory;
            
            if (increase > 50f) // More than 50MB increase
            {
                Debug.LogWarning($"Memory leak detected! Increased by {increase:F1}MB");
                AnalyzeMemoryLeaks();
            }
            
            lastCheckTime = Time.time;
        }
    }
    
    float GetMemoryUsage()
    {
        return System.GC.GetTotalMemory(false) / (1024f * 1024f);
    }
    
    void AnalyzeMemoryLeaks()
    {
        // Check for common leak sources
        var trails = FindObjectsOfType<TrailRenderer>();
        if (trails.Length > 1000)
        {
            Debug.LogError("Too many TrailRenderers! Disable trails or use object pooling.");
        }
        
        var particleSystems = FindObjectsOfType<ParticleSystem>();
        if (particleSystems.Length > 100)
        {
            Debug.LogError("Too many ParticleSystems! Clean up old particles.");
        }
    }
}
```

**Solutions:**

#### Solution 1: Object Pooling
```csharp
// Use object pooling for dynamic agents
public class AgentPool : MonoBehaviour
{
    private Queue<GameObject> pool = new Queue<GameObject>();
    public GameObject agentPrefab;
    
    public GameObject GetAgent()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        return Instantiate(agentPrefab);
    }
    
    public void ReturnAgent(GameObject agent)
    {
        agent.SetActive(false);
        pool.Enqueue(agent);
    }
}
```

#### Solution 2: Cleanup Trails
```csharp
// Clean up visual effects periodically
public class EffectCleanup : MonoBehaviour
{
    void Start()
    {
        InvokeRepeating(nameof(CleanupEffects), 10f, 10f);
    }
    
    void CleanupEffects()
    {
        // Clear old trail renderers
        var trails = FindObjectsOfType<TrailRenderer>();
        foreach (var trail in trails)
        {
            if (trail.time > 2f) // If trail is too long
            {
                trail.Clear();
            }
        }
    }
}
```

### Issue: Inconsistent Behavior

**Symptoms:**
- Agents behave differently each time
- Random performance variations
- Unpredictable movement patterns

**Solutions:**

#### Solution 1: Fixed Random Seed
```csharp
// Set consistent random seed for reproducible behavior
void Start()
{
    Random.InitState(12345); // Use fixed seed
    
    // Or use system time for true randomness
    // Random.InitState((int)System.DateTime.Now.Ticks);
}
```

#### Solution 2: Consistent Update Order
```csharp
// Ensure agents update in consistent order
public class OrderedUpdate : MonoBehaviour
{
    private List<SwarmAgent> agents;
    
    void Start()
    {
        agents = new List<SwarmAgent>(FindObjectsOfType<SwarmAgent>());
        agents.Sort((a, b) => a.GetInstanceID().CompareTo(b.GetInstanceID()));
    }
    
    void Update()
    {
        foreach (var agent in agents)
        {
            // Update in consistent order
            agent.ManualUpdate();
        }
    }
}
```

## Platform-Specific Issues

### Issue: Mobile Performance

**Symptoms:**
- Very low FPS on mobile devices
- App crashes on mobile
- Overheating issues

**Mobile Optimization Script:**
```csharp
public class MobileOptimizer : MonoBehaviour
{
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
        
        // Reduce agent count drastically
        swarmManager.AgentCount = Mathf.Min(swarmManager.AgentCount, 50);
        
        // Increase update intervals
        swarmManager.UpdateRate = 5; // Every 5 frames
        
        // Reduce quality
        foreach (var agent in swarmManager.Agents)
        {
            agent.PerceptionRadius = 2f;
            
            // Disable expensive visual effects
            var trail = agent.GetComponent<TrailRenderer>();
            if (trail) trail.enabled = false;
        }
        
        // Set target frame rate
        Application.targetFrameRate = 30;
    }
}
```

### Issue: WebGL Limitations

**Symptoms:**
- Threading errors in WebGL build
- Performance much worse than editor
- Features not working in browser

**WebGL Compatibility:**
```csharp
public class WebGLCompatibility : MonoBehaviour
{
    void Start()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            ApplyWebGLOptimizations();
        }
    }
    
    void ApplyWebGLOptimizations()
    {
        var swarmManager = GetComponent<SwarmManager>();
        
        // Disable Job System (not supported in WebGL)
        swarmManager.UseJobSystem = false;
        
        // Reduce agent count significantly
        swarmManager.AgentCount = Mathf.Min(swarmManager.AgentCount, 100);
        
        // Use simpler behaviors
        foreach (var agent in swarmManager.Agents)
        {
            // Remove complex behaviors
            var behaviors = agent.GetComponents<SwarmBehavior>();
            foreach (var behavior in behaviors)
            {
                if (behavior is ComplexBehavior)
                {
                    Destroy(behavior);
                }
            }
        }
    }
}
```

## Debugging Tools

### Visual Debugging

```csharp
public class SwarmVisualDebugger : MonoBehaviour
{
    [Header("Debug Options")]
    public bool showPerceptionRadius = true;
    public bool showVelocityVectors = true;
    public bool showForces = false;
    public bool showNeighborConnections = false;
    
    void OnDrawGizmos()
    {
        var agents = FindObjectsOfType<SwarmAgent>();
        
        foreach (var agent in agents)
        {
            if (showPerceptionRadius)
            {
                Gizmos.color = new Color(0, 1, 0, 0.1f);
                Gizmos.DrawSphere(agent.Position, agent.PerceptionRadius);
            }
            
            if (showVelocityVectors && agent.Velocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(agent.Position, agent.Velocity.normalized * 2f);
            }
            
            if (showNeighborConnections)
            {
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                var neighbors = agent.GetNeighbors();
                foreach (var neighbor in neighbors)
                {
                    Gizmos.DrawLine(agent.Position, neighbor.Position);
                }
            }
        }
    }
}
```

### Performance Profiler

```csharp
public class SwarmProfiler : MonoBehaviour
{
    private System.Diagnostics.Stopwatch stopwatch;
    private float[] updateTimes = new float[60];
    private int frameIndex = 0;
    
    void Start()
    {
        stopwatch = new System.Diagnostics.Stopwatch();
    }
    
    void Update()
    {
        stopwatch.Restart();
        
        // Your swarm update code here
        
        stopwatch.Stop();
        updateTimes[frameIndex] = (float)stopwatch.Elapsed.TotalMilliseconds;
        frameIndex = (frameIndex + 1) % updateTimes.Length;
        
        // Log performance every 60 frames
        if (frameIndex == 0)
        {
            float avgTime = 0f;
            for (int i = 0; i < updateTimes.Length; i++)
            {
                avgTime += updateTimes[i];
            }
            avgTime /= updateTimes.Length;
            
            Debug.Log($"Average swarm update time: {avgTime:F2}ms");
            
            if (avgTime > 16.67f) // More than one frame at 60fps
            {
                Debug.LogWarning("Swarm update is taking too long! Consider optimizations.");
            }
        }
    }
}
```

## Getting Help

### Before Asking for Help

1. **Check Console**: Look for error messages or warnings
2. **Test with Examples**: Try the included example scenes
3. **Simplify Setup**: Start with basic configuration
4. **Read Documentation**: Check API.md and README.md
5. **Search Issues**: Look through GitHub issues for similar problems

### Where to Get Help

- **GitHub Issues**: Report bugs and ask questions
- **Unity Forums**: Community discussions
- **Discord**: Real-time chat support
- **Email**: support@ruvnet.com for urgent issues

### Information to Include

When reporting issues, please include:

```
Unity Version: 2022.3.5f1
Platform: Windows 11 / macOS / Mobile
Swarm AI Version: 1.0.0
Agent Count: 100
Error Messages: [Copy exact error text]
Steps to Reproduce:
1. [Step 1]
2. [Step 2]
3. [Issue occurs]
```

---

**Most issues can be resolved quickly with the right approach!** 🔧

*Still having problems? Don't hesitate to reach out - we're here to help make your swarms work perfectly!*