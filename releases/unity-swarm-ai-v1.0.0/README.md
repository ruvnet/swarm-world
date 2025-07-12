# Unity Swarm AI

A comprehensive, modular swarm AI solution for Unity with performance optimizations, spatial partitioning, and extensible behavior system.

## 🚀 Features

- **Modular Architecture**: Component-based design with ScriptableObject behaviors
- **High Performance**: Spatial partitioning with uniform grids and octrees
- **Scalable**: Support for 10-10,000+ agents with LOD and GPU optimizations
- **Unity Package Manager**: Easy installation and updates
- **Editor Tools**: Custom inspectors, setup wizards, and debugging tools
- **Extensible**: Create custom behaviors with simple ScriptableObject inheritance
- **Well Documented**: Comprehensive API documentation and examples

## 📦 Installation

### Via Unity Package Manager

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL`
3. Enter: `https://github.com/ruvnet/unity-swarm-ai.git`

### Via Package Manager Manifest

Add to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.claude-flow.unity-swarm-ai": "1.0.0"
  }
}
```

### Manual Installation

1. Download the latest release
2. Extract to your project's `Packages` folder
3. Unity will automatically import the package

## 🎯 Quick Start

### 1. Setup Wizard

Use the built-in setup wizard for quick scene creation:
- `Tools > Unity Swarm AI > Setup Wizard`
- Configure agent count, spawn area, and behaviors
- Click "Create Swarm" to generate a complete swarm system

### 2. Manual Setup

```csharp
// Create a basic swarm agent
GameObject agentObj = new GameObject("SwarmAgent");
SwarmAgent agent = agentObj.AddComponent<SwarmAgent>();

// Create flocking behavior
FlockingBehavior flocking = ScriptableObject.CreateInstance<FlockingBehavior>();

// Assign behavior to agent
agent.behaviors = new SwarmBehavior[] { flocking };
agent.behaviorWeights = new float[] { 1.0f };
```

### 3. Custom Behaviors

Create custom behaviors by inheriting from `SwarmBehavior`:

```csharp
[CreateAssetMenu(fileName = "CustomBehavior", menuName = "Unity Swarm AI/Behaviors/Custom")]
public class CustomBehavior : SwarmBehavior
{
    public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
    {
        // Your custom logic here
        return Vector3.zero;
    }
}
```

## 🏗️ Architecture

### Core Components

- **ISwarmAgent**: Core interface defining agent contract
- **SwarmAgent**: MonoBehaviour implementation with configurable behaviors
- **SwarmBehavior**: ScriptableObject base class for all behaviors
- **ISwarmCoordinator**: Interface for swarm management systems
- **ISpatialPartition**: Interface for spatial optimization systems

### Behavior System

The behavior system uses ScriptableObjects for modularity:

```
SwarmBehavior (Base)
├── FlockingBehavior
├── SeekingBehavior  
├── WanderingBehavior
└── CustomBehavior (Your implementations)
```

### Spatial Partitioning

Efficient neighbor queries using:
- **Uniform Grid**: Best for evenly distributed agents
- **Octree**: Best for clustered agents
- **Hybrid Systems**: Automatic selection based on density

## 🔧 Performance Guidelines

| Agent Count | Recommended Architecture | Features |
|-------------|-------------------------|----------|
| < 100 | MonoBehaviour | Full features, easy debugging |
| 100-1000 | MonoBehaviour + Spatial Partitioning | Optimized neighbor queries |
| 1000-5000 | Job System + Spatial Partitioning | CPU parallelization |
| 5000+ | ECS/DOTS or GPU Compute | Maximum performance |

## 📚 Examples

### Basic Flocking
Simple flocking demonstration with separation, alignment, and cohesion.

### Advanced Behaviors
Multiple agent types with different behaviors and interaction patterns.

### Performance Demo
Large-scale swarm optimization showcase with 10,000+ agents.

### Custom Behavior Example
Template for creating your own behavior systems.

## 🛠️ Editor Tools

### Custom Inspectors
- Enhanced SwarmAgent inspector with behavior configuration
- Real-time performance monitoring
- Debug visualization controls

### Setup Wizard
- Quick scene generation
- Prefab creation tools
- Behavior asset management

### Debugging Tools
- Gizmo visualization
- Performance profiling
- Neighbor highlighting

## 📖 API Reference

### Core Interfaces

#### ISwarmAgent
```csharp
Vector3 Position { get; }
Vector3 Velocity { get; }
float PerceptionRadius { get; }
void ApplyForce(Vector3 force);
List<ISwarmAgent> GetNeighbors();
```

#### SwarmBehavior
```csharp
abstract Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors);
virtual bool ShouldExecute(ISwarmAgent agent);
```

### Spatial Systems

#### ISpatialPartition<T>
```csharp
void Initialize(Bounds bounds);
void Insert(T agent);
List<T> Query(Vector3 center, float radius);
```

## 🔄 Update Patterns

### Frame-based Updates
```csharp
void Update()
{
    UpdateAgent(Time.deltaTime);
}
```

### Fixed Updates for Physics
```csharp
void FixedUpdate()
{
    UpdateAgent(Time.fixedDeltaTime);
}
```

### Custom Update Rates
```csharp
public class LODSwarmAgent : SwarmAgent
{
    public int updateRate = 1; // Update every N frames
    private int frameCounter = 0;
    
    void Update()
    {
        if (++frameCounter >= updateRate)
        {
            UpdateAgent(Time.deltaTime * updateRate);
            frameCounter = 0;
        }
    }
}
```

## 🐛 Debugging

### Enable Debug Visualization
```csharp
// In SwarmAgent inspector
showGizmos = true;
gizmoColor = Color.white;
```

### Performance Monitoring
```csharp
var debugInfo = spatialPartition.GetDebugInfo();
Debug.Log($"Occupied cells: {debugInfo.occupiedCells}/{debugInfo.totalCells}");
```

### Neighbor Analysis
```csharp
// Use the custom inspector's "Log Neighbors" button
// Or programmatically:
var neighbors = agent.GetNeighbors();
foreach (var neighbor in neighbors)
{
    Debug.Log($"Neighbor {neighbor.AgentId} at distance {Vector3.Distance(agent.Position, neighbor.Position)}");
}
```

## 🔧 Configuration

### Behavior Weights
Fine-tune behavior influence:
```csharp
agent.behaviorWeights = new float[] { 
    2.0f, // Separation (stronger)
    1.0f, // Alignment (normal)
    0.5f  // Cohesion (weaker)
};
```

### Spatial Partitioning
```csharp
var grid = new UniformGrid<SwarmAgent>(cellSize: 5f);
grid.Initialize(new Bounds(Vector3.zero, Vector3.one * 100f));
```

## 🚀 Performance Tips

1. **Use Spatial Partitioning**: Essential for > 50 agents
2. **Limit Update Rates**: Use LOD for distant agents
3. **Batch Operations**: Update multiple agents together
4. **Profile Regularly**: Use Unity Profiler to identify bottlenecks
5. **Consider ECS**: For very large swarms (> 1000 agents)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## 🆘 Support

- **Documentation**: [GitHub Wiki](https://github.com/your-repo/unity-swarm-ai/wiki)
- **Issues**: [GitHub Issues](https://github.com/your-repo/unity-swarm-ai/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-repo/unity-swarm-ai/discussions)

## 🏆 Credits

Developed with Claude Flow AI Architecture System

---

**Unity Swarm AI** - Bringing intelligent swarm behavior to Unity with performance and ease-of-use in mind.