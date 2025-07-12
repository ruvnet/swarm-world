# Swarm AI Architecture Quick Reference

## Core Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    SWARM AI SYSTEM                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │
│  │   Agents    │  │Communication│  │ Coordination│       │
│  │             │  │             │  │             │       │
│  │ • SwarmAgent│  │ • Direct    │  │ • Consensus │       │
│  │ • Behaviors │  │ • Stigmergic│  │ • Leader    │       │
│  │ • States    │  │ • Broadcast │  │ • Auction   │       │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘       │
│         │                 │                 │               │
│         └─────────────────┴─────────────────┘               │
│                           │                                 │
│                    ┌──────▼──────┐                         │
│                    │   Swarm     │                         │
│                    │  Manager    │                         │
│                    └──────┬──────┘                         │
│                           │                                 │
│         ┌─────────────────┴─────────────────┐             │
│         │                                   │             │
│  ┌──────▼──────┐                   ┌───────▼──────┐      │
│  │Performance  │                   │  Rendering   │      │
│  │             │                   │              │      │
│  │• Spatial    │                   │• GPU Instance│      │
│  │• Job System │                   │• LOD System  │      │
│  │• GPU Compute│                   │• Culling     │      │
│  └─────────────┘                   └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

## Performance Scaling Guide

| Agent Count | Recommended Approach | Update Method | Rendering |
|------------|---------------------|---------------|-----------|
| 1-100      | MonoBehaviour       | Update()      | Standard  |
| 100-500    | Spatial Partitioning| Job System    | Instanced |
| 500-5000   | GPU Compute + Jobs  | Compute Shader| GPU Inst. |
| 5000+      | Full GPU Simulation | Compute Only  | Indirect  |

## Key Design Decisions

### 1. Agent Architecture
```
SwarmAgent
├── Movement Component
│   ├── Velocity
│   ├── Acceleration
│   └── Max Speed/Force
├── Perception Component
│   ├── Neighbor Detection
│   ├── Obstacle Avoidance
│   └── Goal Seeking
└── Behavior Component
    ├── Separation
    ├── Alignment
    ├── Cohesion
    └── Custom Behaviors
```

### 2. Communication Patterns
```
Direct Communication:     A ←→ B
Broadcast:               A → [B,C,D,E]
Stigmergic:              A → Environment → B
Gossip:                  A → B → C → D
```

### 3. Optimization Priorities
1. **Spatial Partitioning** - Reduce neighbor checks from O(n²) to O(n)
2. **Object Pooling** - Eliminate allocation overhead
3. **GPU Acceleration** - Parallel processing for massive swarms
4. **LOD Systems** - Reduce complexity based on distance
5. **Batch Rendering** - Single draw call for all agents

## Implementation Checklist

### Phase 1: Prototype (1-2 days)
- [ ] Basic agent with flocking behaviors
- [ ] Simple swarm manager
- [ ] Debug visualization

### Phase 2: Core System (3-5 days)
- [ ] Spatial partitioning
- [ ] Behavior system architecture
- [ ] Communication framework
- [ ] Basic optimization

### Phase 3: Scale & Polish (1 week)
- [ ] Job System integration
- [ ] GPU compute shaders
- [ ] Advanced patterns (ACO, PSO)
- [ ] Performance profiling

### Phase 4: Production Ready (2 weeks)
- [ ] Full optimization pass
- [ ] Extensive testing
- [ ] Documentation
- [ ] Integration tools

## Quick Performance Tips

1. **Neighbor Queries**: Cache for multiple frames if possible
2. **Physics**: Disable Unity physics for swarm agents
3. **Updates**: Stagger updates across frames for large swarms
4. **Memory**: Pre-allocate all arrays and lists
5. **Rendering**: Use MaterialPropertyBlocks for variation

## Common Gotchas

- ❌ Using FindObjectsOfType every frame
- ❌ Individual physics colliders per agent
- ❌ Instantiate/Destroy during runtime
- ❌ Complex math in inner loops
- ❌ Global knowledge access

## Unity-Specific Optimizations

```csharp
// FAST: Spatial lookup
neighbors = spatialGrid.GetNeighbors(position, radius);

// SLOW: Physics overlap
neighbors = Physics.OverlapSphere(position, radius);

// FAST: Job System batch update
var job = new SwarmUpdateJob { ... };
job.Schedule(agentCount, 64);

// SLOW: Individual Update() calls
foreach(agent in agents) agent.Update();

// FAST: GPU Instancing
Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);

// SLOW: Individual renderers
foreach(agent in agents) agent.GetComponent<Renderer>().Draw();
```

## Emergency Performance Fixes

If frame rate drops:
1. Reduce perception radius
2. Skip every other frame for distant agents
3. Simplify behaviors temporarily
4. Reduce agent count dynamically
5. Switch to lower LOD behaviors

Remember: Profile first, optimize second!