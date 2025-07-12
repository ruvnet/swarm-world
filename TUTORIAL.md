# Unity Swarm AI - Getting Started Tutorial

**Learn to create intelligent swarm behaviors in just 10 minutes!** 🚀

This hands-on tutorial will guide you through creating your first swarm from scratch, step by step. By the end, you'll have a fully functional flocking simulation with 100 agents moving in realistic group patterns.

## What You'll Build

![Tutorial Result](https://github.com/ruvnet/unity-swarm-ai/raw/main/Documentation/Images/tutorial-result.gif)

- **100 flocking agents** with emergent group behavior
- **Obstacle avoidance** around 3D objects
- **Target following** - agents follow your mouse cursor
- **Real-time parameter tweaking** with visual debugging
- **Performance monitoring** to understand system limits

## Prerequisites

- ✅ Unity 2021.3 LTS or newer
- ✅ Unity Swarm AI Plugin installed ([Installation Guide](INSTALL.md))
- ✅ Basic Unity knowledge (creating GameObjects, adding components)
- ⏱️ **Time needed: 10-15 minutes**

## Step 1: Create the Scene (2 minutes)

### 1.1 New Scene Setup
1. **Create a new scene**: `File → New Scene → 3D`
2. **Save the scene**: `Ctrl+S → "MyFirstSwarm.unity"`
3. **Setup camera**: Position Main Camera at `(0, 10, -15)`, rotate to `(25, 0, 0)`

### 1.2 Add Environment
```csharp
// Create a ground plane
GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
ground.name = "Ground";
ground.transform.localScale = new Vector3(10, 1, 10);

// Add some obstacles
GameObject obstacle1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
obstacle1.name = "Obstacle1";
obstacle1.transform.position = new Vector3(5, 1, 5);
obstacle1.transform.localScale = new Vector3(2, 2, 2);

GameObject obstacle2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
obstacle2.name = "Obstacle2";  
obstacle2.transform.position = new Vector3(-8, 1, -3);
obstacle2.transform.localScale = new Vector3(3, 3, 3);

GameObject obstacle3 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
obstacle3.name = "Obstacle3";
obstacle3.transform.position = new Vector3(0, 1, 8);
obstacle3.transform.localScale = new Vector3(2, 2, 2);
```

### 1.3 Create Layers
1. Go to **Edit → Project Settings → Tags and Layers**
2. Add these layers:
   - **Layer 8**: `SwarmAgents`
   - **Layer 9**: `Obstacles`
3. Set obstacle objects to the `Obstacles` layer

## Step 2: Create the Agent Prefab (3 minutes)

### 2.1 Basic Agent GameObject
1. **Create agent**: `GameObject → 3D Object → Capsule`
2. **Rename** to "SwarmAgent"
3. **Scale** to `(0.5, 1, 0.5)` - smaller agents look better in groups
4. **Set layer** to `SwarmAgents`

### 2.2 Add SwarmAgent Component
```csharp
// Add the SwarmAgent component
SwarmAgent agent = agentObject.AddComponent<SwarmAgent>();

// Configure basic movement
agent.MaxSpeed = 8f;
agent.MaxForce = 15f;
agent.PerceptionRadius = 4f;

// Configure flocking weights
agent.SeparationWeight = 2f;    // Avoid crowding
agent.AlignmentWeight = 1f;     // Match neighbor movement
agent.CohesionWeight = 0.8f;    // Stay with group
agent.AvoidanceWeight = 3f;     // Avoid obstacles strongly

// Set layer detection
agent.AgentLayer = LayerMask.GetMask("SwarmAgents");
```

### 2.3 Add Visual Flair
```csharp
// Add a trail renderer for visual appeal
TrailRenderer trail = agentObject.AddComponent<TrailRenderer>();
trail.time = 1f;
trail.startWidth = 0.1f;
trail.endWidth = 0.01f;
trail.material = new Material(Shader.Find("Standard"));
trail.material.color = Color.cyan;

// Add a simple material
Renderer renderer = agentObject.GetComponent<Renderer>();
renderer.material.color = new Color(0.3f, 0.8f, 1f); // Light blue
```

### 2.4 Create Prefab
1. **Drag** the agent from Scene to Project window
2. **Name** the prefab "SwarmAgentPrefab"
3. **Delete** the agent from the scene (we'll spawn them via script)

## Step 3: Setup the Swarm Manager (2 minutes)

### 3.1 Create Manager GameObject
1. **Create empty GameObject**: `GameObject → Create Empty`
2. **Rename** to "SwarmManager"
3. **Position** at `(0, 0, 0)`

### 3.2 Add SwarmManager Component
```csharp
// Add SwarmManager component
SwarmManager manager = managerObject.AddComponent<SwarmManager>();

// Configure spawning
manager.AgentPrefab = swarmAgentPrefab;  // Assign your prefab
manager.AgentCount = 100;
manager.SpawnRadius = 12f;

// Configure boundaries
manager.BoundarySize = new Vector3(40, 15, 40);
manager.BoundaryForce = 20f;

// Enable performance optimizations
manager.UseSpatialPartitioning = true;
manager.PartitionCellSize = 6f;
```

### 3.3 Auto-Spawn Script
Create a simple script to spawn agents automatically:

```csharp
using UnityEngine;

public class SwarmAutoSpawner : MonoBehaviour
{
    public SwarmManager swarmManager;
    
    void Start()
    {
        // Wait a frame for everything to initialize
        Invoke("SpawnSwarm", 0.1f);
    }
    
    void SpawnSwarm()
    {
        swarmManager.SpawnAgents();
        Debug.Log($"Spawned {swarmManager.AgentCount} agents!");
    }
}
```

Add this component to the SwarmManager and assign the SwarmManager reference.

## Step 4: Add Target Following (2 minutes)

Let's make the swarm follow your mouse cursor for interactive fun!

### 4.1 Create Target Object
```csharp
// Create a target indicator
GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
target.name = "SwarmTarget";
target.transform.localScale = Vector3.one * 0.5f;
target.GetComponent<Renderer>().material.color = Color.red;

// Remove collider so it doesn't interfere
DestroyImmediate(target.GetComponent<Collider>());
```

### 4.2 Mouse Following Script
Create a script to move the target with your mouse:

```csharp
using UnityEngine;

public class MouseTargetController : MonoBehaviour
{
    public LayerMask groundLayer = 1; // Default layer
    public float targetHeight = 2f;
    
    private Camera mainCamera;
    private SwarmManager swarmManager;
    
    void Start()
    {
        mainCamera = Camera.main;
        swarmManager = FindObjectOfType<SwarmManager>();
        
        // Set this object as the global target
        swarmManager.SetGlobalTarget(transform);
    }
    
    void Update()
    {
        // Cast ray from mouse position to ground
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            Vector3 targetPosition = hit.point;
            targetPosition.y = targetHeight; // Keep target above ground
            transform.position = targetPosition;
        }
    }
}
```

Add this script to your target object.

## Step 5: Add Obstacle Avoidance (1 minute)

### 5.1 Configure Obstacle Layers
1. **Select all obstacle objects** (Cube, Sphere, Cylinder)
2. **Set their layer** to `Obstacles`
3. **Ensure they have colliders** (should be automatic for primitives)

### 5.2 Enhanced Agent Behavior
The SwarmAgent component already includes obstacle avoidance! Just make sure the `ObstacleLayer` field includes the `Obstacles` layer:

```csharp
// This should already be configured, but verify:
agent.ObstacleLayer = LayerMask.GetMask("Obstacles");
```

## Step 6: Add Debugging & Polish (2 minutes)

### 6.1 Visual Debugging
Add the SwarmDebugger component to see what's happening:

```csharp
// Add to any agent to visualize its behavior
SwarmDebugger debugger = agentPrefab.AddComponent<SwarmDebugger>();
debugger.ShowPerceptionRadius = true;
debugger.ShowVelocityVectors = true;
debugger.ShowNeighborConnections = false; // Can be performance heavy
debugger.PerceptionColor = new Color(0, 1, 0, 0.1f); // Transparent green
debugger.VelocityColor = Color.blue;
```

### 6.2 Performance Monitor
Create a simple performance display:

```csharp
using UnityEngine;
using UnityEngine.UI;

public class SwarmPerformanceMonitor : MonoBehaviour
{
    [Header("UI References")]
    public Text fpsText;
    public Text agentCountText;
    public Text neighborText;
    
    private SwarmManager swarmManager;
    private float deltaTime;
    
    void Start()
    {
        swarmManager = FindObjectOfType<SwarmManager>();
        
        // Create simple UI if not assigned
        if (fpsText == null) CreateSimpleUI();
    }
    
    void Update()
    {
        // Calculate FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        
        // Update display every 10 frames
        if (Time.frameCount % 10 == 0)
        {
            fpsText.text = $"FPS: {fps:0}";
            agentCountText.text = $"Agents: {swarmManager.Agents.Count}";
            
            if (swarmManager.Agents.Count > 0)
            {
                int avgNeighbors = CalculateAverageNeighbors();
                neighborText.text = $"Avg Neighbors: {avgNeighbors}";
            }
        }
    }
    
    int CalculateAverageNeighbors()
    {
        int total = 0;
        foreach (var agent in swarmManager.Agents)
        {
            total += agent.GetNeighborCount();
        }
        return total / swarmManager.Agents.Count;
    }
    
    void CreateSimpleUI()
    {
        // Create canvas and text elements
        GameObject canvas = new GameObject("UI Canvas");
        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        
        // Create text objects
        fpsText = CreateTextElement(canvas, "FPS", new Vector2(-200, 100));
        agentCountText = CreateTextElement(canvas, "Agents", new Vector2(-200, 70));
        neighborText = CreateTextElement(canvas, "Neighbors", new Vector2(-200, 40));
    }
    
    Text CreateTextElement(GameObject parent, string name, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchoredPosition = position;
        
        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 16;
        text.color = Color.white;
        text.text = name + ": 0";
        
        return text;
    }
}
```

## Step 7: Test and Play! (1 minute)

### 7.1 Final Setup Check
Before pressing Play, verify:

- ✅ SwarmManager has agent prefab assigned
- ✅ Agent prefab has SwarmAgent component configured
- ✅ Target object has MouseTargetController script
- ✅ Obstacles are on the correct layer
- ✅ Camera is positioned to see the action

### 7.2 Press Play!
Hit the **Play button** and you should see:

1. **100 agents spawn** in a circle around the origin
2. **Agents start flocking** with realistic group behavior
3. **Move your mouse** - agents follow the red target
4. **Agents avoid obstacles** as they navigate
5. **Performance stats** display in the top-right corner

### 7.3 Experiment!
Try adjusting these values in real-time while playing:

**Agent Settings** (on any SwarmAgent):
- `MaxSpeed`: How fast agents move (try 3-15)
- `SeparationWeight`: How much agents avoid each other (try 0.5-3)
- `AlignmentWeight`: How much agents match neighbors (try 0-2)
- `CohesionWeight`: How much agents group together (try 0-2)
- `PerceptionRadius`: How far agents can see neighbors (try 2-8)

**Manager Settings** (on SwarmManager):
- `BoundaryForce`: How strongly agents stay in bounds (try 5-50)
- `UpdateRate`: Performance vs accuracy trade-off (try 1-5)

## Troubleshooting

### ❌ Agents not spawning?
- Check that `AgentPrefab` is assigned on SwarmManager
- Verify the prefab has a SwarmAgent component
- Make sure `AgentCount` > 0

### ❌ Agents flying everywhere?
- Reduce `MaxForce` (try 5-10)
- Increase `SeparationWeight` to prevent clustering
- Check that `BoundarySize` is appropriate for your scene

### ❌ Poor performance?
- Reduce `AgentCount` (try 50 instead of 100)
- Increase `UpdateRate` to 2 or 3
- Enable `UseSpatialPartitioning` on SwarmManager
- Disable `ShowNeighborConnections` on debugger

### ❌ Agents not following target?
- Verify `MouseTargetController` is added to target object
- Check that `SetGlobalTarget` is called in the script
- Make sure the target object has the correct ground layer mask

## What's Next?

Congratulations! 🎉 You've created your first intelligent swarm. Here are some ideas to expand your creation:

### Immediate Improvements
- **Add more behaviors**: Try `WanderBehavior` for random exploration
- **Create predators**: Add agents that hunt the flock
- **Different species**: Multiple swarms with different parameters
- **Food sources**: Agents seek and consume resources

### Advanced Features
- **Formation flying**: Try `VFormation` or `CircleFormation`
- **Obstacle spawning**: Runtime obstacle creation
- **Agent health**: Damage and healing systems
- **Environmental effects**: Wind, currents, temperature

### Performance Optimization
- **LOD system**: Distance-based quality reduction
- **Job System**: Multi-threading for 1000+ agents
- **GPU Compute**: Shader-based updates for massive swarms

### Learn More
- 📖 **[API Documentation](API.md)** - Complete reference guide
- 🎮 **[Example Scenes](Examples/)** - 20+ pre-built scenarios
- ⚡ **[Performance Guide](PERFORMANCE.md)** - Optimization techniques
- 🔧 **[Troubleshooting](TROUBLESHOOTING.md)** - Common issues and fixes

## Code Summary

Here's the complete setup code for quick reference:

```csharp
// SwarmAgent setup
SwarmAgent agent = agentObject.AddComponent<SwarmAgent>();
agent.MaxSpeed = 8f;
agent.MaxForce = 15f;
agent.PerceptionRadius = 4f;
agent.SeparationWeight = 2f;
agent.AlignmentWeight = 1f;
agent.CohesionWeight = 0.8f;
agent.AvoidanceWeight = 3f;

// SwarmManager setup
SwarmManager manager = managerObject.AddComponent<SwarmManager>();
manager.AgentPrefab = swarmAgentPrefab;
manager.AgentCount = 100;
manager.SpawnRadius = 12f;
manager.BoundarySize = new Vector3(40, 15, 40);
manager.BoundaryForce = 20f;
manager.UseSpatialPartitioning = true;

// Mouse target controller
public class MouseTargetController : MonoBehaviour
{
    public LayerMask groundLayer = 1;
    public float targetHeight = 2f;
    
    void Start()
    {
        FindObjectOfType<SwarmManager>().SetGlobalTarget(transform);
    }
    
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
        {
            transform.position = hit.point + Vector3.up * targetHeight;
        }
    }
}
```

---

**You did it!** 🚀 You've successfully created an intelligent swarm system in Unity. The emergent behaviors you see are the result of simple rules creating complex, lifelike group intelligence.

*Ready for more advanced techniques? Check out our [Performance Guide](PERFORMANCE.md) to learn how to scale to thousands of agents, or explore our [Example Scenes](Examples/) for inspiration!*