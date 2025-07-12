# Swarm AI Unity Plugin

A high-performance swarm AI system for Unity featuring configurable behaviors, spatial partitioning, and Job System optimization.

## Features

### Core Components
- **SwarmAgent**: Modular agent system with configurable behaviors
- **SwarmManager**: Central coordination with spatial partitioning
- **Behavior System**: ScriptableObject-based behaviors for easy customization
- **Performance Monitoring**: Real-time performance tracking and optimization

### Built-in Behaviors
- **Separation**: Avoid crowding neighbors
- **Alignment**: Match neighbor velocities
- **Cohesion**: Move towards group center
- **Obstacle Avoidance**: Raycast-based obstacle detection
- **Seek**: Move towards target
- **Flee**: Escape from threats
- **Wander**: Random exploration

### Performance Features
- **Spatial Partitioning**: Uniform Grid and Octree implementations
- **Job System Integration**: Burst-compiled parallel processing
- **LOD System**: Distance-based performance scaling
- **Memory Optimization**: Object pooling and efficient data structures

## Quick Start

### 1. Basic Setup
1. Create an empty GameObject and add `SwarmManager` component
2. Configure boundary size and spawn settings
3. Create agent prefab with `SwarmAgent` component
4. Add desired behaviors to the agent

### 2. Creating Behaviors
```csharp
[CreateAssetMenu(fileName = "MyBehavior", menuName = "SwarmAI/Behaviors/My Behavior")]
public class MyBehavior : SwarmBehavior
{
    public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
    {
        // Your behavior logic here
        return Vector3.zero;
    }
}
```

### 3. Performance Optimization
- Enable spatial partitioning for >100 agents
- Use Job System for >1000 agents
- Implement LOD for visual optimization
- Monitor performance with included tools

## Architecture

```
SwarmAI/
├── Core/
│   ├── ISwarmAgent.cs          # Core agent interface
│   ├── SwarmAgent.cs           # Main agent MonoBehaviour
│   └── SwarmManager.cs         # Central coordination system
├── Behaviors/
│   ├── SwarmBehavior.cs        # Base behavior class
│   ├── SeparationBehavior.cs   # Avoid crowding
│   ├── AlignmentBehavior.cs    # Match velocities
│   ├── CohesionBehavior.cs     # Group cohesion
│   ├── ObstacleAvoidanceBehavior.cs # Collision avoidance
│   ├── SeekBehavior.cs         # Target seeking
│   ├── FleeBehavior.cs         # Threat avoidance
│   └── WanderBehavior.cs       # Random movement
└── Utils/
    ├── SpatialPartition.cs     # Spatial optimization
    ├── SwarmJobSystem.cs       # Job System integration
    ├── SwarmDebugger.cs        # Debugging tools
    └── SwarmPerformanceMonitor.cs # Performance tracking
```

## Performance Guidelines

| Agents | Recommended Architecture | Features |
|--------|-------------------------|----------|
| < 100 | MonoBehaviour | Basic behaviors, simple collision |
| 100-1000 | + Spatial Partitioning | Grid/Octree optimization |
| 1000-5000 | + Job System | Burst compilation, parallel processing |
| > 5000 | + GPU Compute | Compute shaders, massive parallelization |

## Configuration

### SwarmAgent Settings
- **Movement**: Max speed, force, mass
- **Perception**: Radius, layer masks
- **Behaviors**: Configurable behavior weights
- **Boundaries**: Automatic boundary enforcement
- **Debug**: Visualization options

### SwarmManager Settings
- **Spawning**: Count, radius, prefab
- **Spatial Partitioning**: Type, cell size, parameters
- **Performance**: Update intervals, neighbor limits
- **Monitoring**: Performance tracking, warnings

## Best Practices

1. **Start Simple**: Begin with basic flocking behaviors
2. **Profile Early**: Use performance monitoring tools
3. **Scale Gradually**: Add optimizations as needed
4. **Use Spatial Partitioning**: Essential for >100 agents
5. **Batch Operations**: Group similar operations together
6. **Limit Neighbors**: Cap neighbor count for performance
7. **Use Object Pooling**: Reuse agent objects when possible

## Examples

See the `Samples~` folder for complete examples including:
- Basic flocking demonstration
- Large-scale performance optimized swarms
- Custom behavior implementations
- Advanced spatial partitioning setups

## Dependencies

- Unity 2021.3 LTS or newer
- Unity Collections package
- Unity Jobs package  
- Unity Burst package
- Unity Mathematics package

## License

This plugin is provided under the MIT License. See LICENSE file for details.

---

For technical support and advanced implementations, contact: support@swarmworld.dev