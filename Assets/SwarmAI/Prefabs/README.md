# Unity Swarm AI Prefabs

This directory contains pre-configured prefabs for various swarm AI scenarios. These prefabs are optimized for performance and ease of use, providing developers with ready-to-use components for their swarm intelligence projects.

## 📁 Prefab Categories

### 🐦 Basic Agent Prefabs

#### `BasicAgent.prefab`
**Description:** Simple sphere-based agent with basic swarm behaviors
- **Components:** Agent, Renderer, TrailRenderer
- **Behaviors:** FlockingBehavior
- **Scale:** 0.3x0.3x0.3 units
- **Materials:** BasicAgentMaterial (blue)
- **Performance:** High - suitable for 100+ agents
- **Use Cases:** Learning, prototyping, basic flocking

#### `LightweightAgent.prefab`  
**Description:** Minimal agent for large-scale simulations
- **Components:** Agent, Renderer (no trail)
- **Behaviors:** OptimizedFlockingBehavior
- **Scale:** 0.2x0.2x0.2 units
- **Materials:** LightweightMaterial (unlit)
- **Performance:** Very High - suitable for 1000+ agents
- **Use Cases:** Stress testing, large swarms, performance-critical scenarios

#### `DetailedAgent.prefab`
**Description:** Full-featured agent with visual effects
- **Components:** Agent, Renderer, TrailRenderer, ParticleSystem
- **Behaviors:** FlockingBehavior, ObstacleAvoidance
- **Scale:** 0.4x0.4x0.4 units
- **Materials:** DetailedAgentMaterial (animated)
- **Performance:** Medium - suitable for 50-100 agents
- **Use Cases:** Demonstrations, close-up scenarios, visual quality focus

### 🐜 Specialized Agent Prefabs

#### `AntAgent.prefab`
**Description:** Ant colony simulation agent
- **Components:** Agent, Renderer, TrailRenderer
- **Behaviors:** AntBehavior, PheromoneInteraction
- **Scale:** 0.15x0.05x0.3 units (ant-shaped)
- **Materials:** AntMaterial (brown/black)
- **Performance:** Medium - suitable for 60-100 agents
- **Use Cases:** Ant colony simulations, pathfinding demos

#### `FishAgent.prefab`
**Description:** Underwater flocking agent
- **Components:** Agent, Renderer, TrailRenderer
- **Behaviors:** FlockingBehavior, WaterEffects
- **Scale:** 0.2x0.1x0.4 units (fish-shaped)
- **Materials:** FishMaterial (iridescent)
- **Performance:** Medium - suitable for 80-120 agents
- **Use Cases:** Underwater scenes, fish schooling

#### `BirdAgent.prefab`
**Description:** Aerial flocking agent
- **Components:** Agent, Renderer, TrailRenderer, WindEffects
- **Behaviors:** FlockingBehavior, AerialNavigation
- **Scale:** 0.3x0.1x0.5 units (bird-shaped)
- **Materials:** BirdMaterial (feathered)
- **Performance:** Medium - suitable for 75-100 agents
- **Use Cases:** Sky scenes, migration patterns

### 🚁 Formation Flying Prefabs

#### `FighterJet.prefab`
**Description:** Military fighter aircraft for formation flying
- **Components:** Agent, Renderer, TrailRenderer, Lights
- **Behaviors:** FormationBehavior, WaypointNavigation
- **Scale:** 0.4x0.25x1.0 units
- **Materials:** MilitaryJetMaterial (camouflage)
- **Performance:** Medium - suitable for 20-40 agents
- **Use Cases:** Military simulations, formation demonstrations

#### `TransportAircraft.prefab`
**Description:** Large transport aircraft
- **Components:** Agent, Renderer, TrailRenderer, NavigationLights
- **Behaviors:** FormationBehavior, HeavyAircraftDynamics
- **Scale:** 0.6x0.4x1.5 units
- **Materials:** TransportMaterial (commercial)
- **Performance:** Low-Medium - suitable for 10-20 agents
- **Use Cases:** Civilian aviation, cargo formations

#### `LeaderAircraft.prefab`
**Description:** Formation leader with enhanced navigation
- **Components:** Agent, Renderer, TrailRenderer, LeaderBeacon
- **Behaviors:** FormationBehavior, LeadershipBehavior, WaypointNavigation
- **Scale:** 0.5x0.3x1.2 units
- **Materials:** LeaderMaterial (distinctive markings)
- **Performance:** Medium - 1 per formation
- **Use Cases:** Formation flying, leadership hierarchies

### 👥 Crowd Simulation Prefabs

#### `Pedestrian.prefab`
**Description:** Generic pedestrian for crowd simulations
- **Components:** Agent, Renderer, CapsuleCollider
- **Behaviors:** CrowdFlockingBehavior, GoalSeekingBehavior
- **Scale:** 0.6x1.0x0.6 units (human proportions)
- **Materials:** PedestrianMaterial (varied colors)
- **Performance:** Medium - suitable for 100-150 agents
- **Use Cases:** Urban simulations, crowd dynamics

#### `EmergencyPedestrian.prefab`
**Description:** Pedestrian with panic behavior capabilities
- **Components:** Agent, Renderer, EmergencyLights
- **Behaviors:** CrowdFlockingBehavior, PanicBehavior, EvacuationBehavior
- **Scale:** 0.6x1.0x0.6 units
- **Materials:** EmergencyMaterial (high visibility)
- **Performance:** Medium - suitable for emergency scenarios
- **Use Cases:** Evacuation simulations, emergency planning

### 🎮 Interactive Prefabs

#### `InteractiveAgent.prefab`
**Description:** Agent responsive to user interaction
- **Components:** Agent, Renderer, InteractionDetector
- **Behaviors:** MouseInteractionBehavior, FlockingBehavior
- **Scale:** 0.3x0.3x0.3 units
- **Materials:** InteractiveMaterial (color-changing)
- **Performance:** High - suitable for 100+ agents
- **Use Cases:** Interactive demonstrations, educational tools

#### `AttractorOrb.prefab`
**Description:** Static attractor for swarm interaction
- **Components:** Transform, Renderer, ParticleSystem
- **Scale:** 2.0x2.0x2.0 units
- **Materials:** AttractorMaterial (glowing)
- **Performance:** Very High - no movement logic
- **Use Cases:** Interactive swarms, force demonstrations

#### `RepellerCube.prefab`
**Description:** Static repeller for swarm interaction
- **Components:** Transform, Renderer, ParticleSystem
- **Scale:** 2.0x2.0x2.0 units  
- **Materials:** RepellerMaterial (warning colors)
- **Performance:** Very High - no movement logic
- **Use Cases:** Obstacle simulation, force demonstrations

### 🎯 Predator-Prey Prefabs

#### `PreyAgent.prefab`
**Description:** Prey species for ecosystem simulations
- **Components:** Agent, Renderer, TrailRenderer
- **Behaviors:** PreyBehavior, FlockingBehavior
- **Scale:** 0.25x0.15x0.4 units
- **Materials:** PreyMaterial (earth tones)
- **Performance:** Medium - suitable for 80-120 agents
- **Use Cases:** Ecosystem simulations, predator-prey dynamics

#### `PredatorAgent.prefab`
**Description:** Predator species for ecosystem simulations
- **Components:** Agent, Renderer, TrailRenderer, HuntingEffects
- **Behaviors:** PredatorBehavior, PackHunting
- **Scale:** 0.4x0.25x0.7 units
- **Materials:** PredatorMaterial (warning colors)
- **Performance:** Medium - suitable for 10-20 agents
- **Use Cases:** Ecosystem simulations, hunting behavior

### 🏭 Environmental Prefabs

#### `FoodSource.prefab`
**Description:** Food source for ant colony and ecosystem simulations
- **Components:** FoodSource, Renderer, ParticleSystem
- **Scale:** 0.8x0.8x0.8 units
- **Materials:** FoodMaterial (organic)
- **Performance:** High - minimal processing
- **Use Cases:** Resource management, foraging behavior

#### `Colony.prefab`
**Description:** Ant colony central structure
- **Components:** Transform, Renderer, ColonyManager
- **Scale:** 3.0x0.5x3.0 units
- **Materials:** ColonyMaterial (earthy)
- **Performance:** High - static structure
- **Use Cases:** Ant simulations, home base scenarios

#### `ObstacleBlock.prefab`
**Description:** Generic obstacle for navigation testing
- **Components:** Transform, Renderer, Collider
- **Scale:** Variable (1-4 units)
- **Materials:** ObstacleMaterial (concrete)
- **Performance:** Very High - static
- **Use Cases:** Obstacle avoidance, navigation challenges

## 🎨 Material Specifications

### Performance Materials
- **Unlit/Color:** Fastest rendering, solid colors
- **Standard:** Good balance of quality and performance
- **Lit/Specular:** Higher quality with reflections
- **Animated:** Highest quality with animations

### Color Coding Standards
- **Blue Tones:** Basic agents, water creatures
- **Green Tones:** Attractors, food sources, friendly entities
- **Red Tones:** Predators, repellers, danger indicators
- **Yellow/Orange:** Leaders, important entities
- **Gray/Brown:** Neutral objects, obstacles, environment

## 📊 Performance Comparison

| Prefab Type | Triangle Count | Texture Size | Materials | Est. Max Agents |
|-------------|----------------|--------------|-----------|----------------|
| LightweightAgent | 80 | 32x32 | 1 | 2000+ |
| BasicAgent | 160 | 64x64 | 1 | 1000+ |
| DetailedAgent | 320 | 128x128 | 2 | 500+ |
| FighterJet | 800 | 256x256 | 3 | 100+ |
| Pedestrian | 400 | 128x128 | 2 | 300+ |

## 🔧 Customization Guide

### Modifying Existing Prefabs
1. **Select Prefab** in Project window
2. **Open Prefab Mode** by double-clicking
3. **Modify Components** as needed
4. **Save Changes** using Ctrl+S
5. **Test Performance** with target agent counts

### Creating Custom Prefabs
1. **Create Base GameObject** in scene
2. **Add Agent Component** with required settings
3. **Add Behavior Scripts** for desired functionality
4. **Configure Visual Components** (Renderer, Trail, etc.)
5. **Drag to Project** to create prefab
6. **Test and Optimize** for your use case

### Optimization Tips
- **Reduce Polygon Count** for large swarms
- **Use Smaller Textures** when agents are distant
- **Remove Unnecessary Components** for performance
- **Combine Materials** to reduce draw calls
- **Use LOD Groups** for distance-based optimization

## 🚀 Quick Setup Guide

### For Beginners
1. **Drag BasicAgent.prefab** into your scene
2. **Add SwarmManager** component to empty GameObject
3. **Assign BasicAgent** to SwarmManager's agentPrefab field
4. **Set agent count** to 50-100
5. **Press Play** to see basic flocking behavior

### For Advanced Users
1. **Choose appropriate prefab** for your scenario
2. **Configure SwarmManager** with optimization settings
3. **Customize behavior parameters** via inspector
4. **Implement custom behaviors** if needed
5. **Profile performance** and optimize as required

### For Large-Scale Simulations
1. **Use LightweightAgent.prefab** as base
2. **Enable spatial hashing** in SwarmManager
3. **Implement LOD system** for distant agents
4. **Consider instanced rendering** for 1000+ agents
5. **Monitor performance** continuously

## 🔍 Debugging Features

### Visual Debug Options
- **Gizmo Visualization:** Show neighbor radius, forces
- **Color Coding:** Agent states, behavior priorities
- **Trail Rendering:** Movement history visualization
- **Debug UI:** Real-time parameter display

### Performance Monitoring
- **Frame Rate Display:** Built into demo scripts
- **Agent Count Tracking:** Dynamic population monitoring
- **Memory Usage:** Allocation tracking
- **Behavior Timing:** Update frequency measurement

## 📱 Platform Considerations

### Mobile Optimization
- **Use LightweightAgent** prefabs
- **Reduce agent counts** (50-200 recommended)
- **Disable trails** and particle effects
- **Use unlit materials** for best performance

### Desktop Performance
- **Standard prefabs** work well
- **Higher agent counts** supported (500-2000)
- **Full visual effects** available
- **Advanced behaviors** feasible

### VR Optimization
- **Maintain 90+ FPS** for comfort
- **Use medium-detail prefabs** 
- **Optimize for close viewing** distances
- **Consider motion sickness** in behaviors

## 📚 Usage Examples

### Basic Flocking Setup
```csharp
// Assign in SwarmManager
public GameObject agentPrefab = BasicAgent.prefab;
public int agentCount = 100;
```

### Custom Behavior Assignment
```csharp
// In spawning code
Agent agent = Instantiate(agentPrefab).GetComponent<Agent>();
agent.AddBehavior(new CustomBehavior());
```

### Performance Optimization
```csharp
// For large swarms
if (agentCount > 500)
{
    agentPrefab = LightweightAgent.prefab;
    swarmManager.enableLOD = true;
}
```

## 🤝 Contributing New Prefabs

### Submission Guidelines
1. **Follow naming conventions** (TypeAgent.prefab)
2. **Include performance metrics** in documentation
3. **Test with various agent counts** (10, 100, 500+)
4. **Provide usage examples** and best practices
5. **Submit with materials** and textures

### Quality Standards
- **Optimized polygon count** for intended use
- **Appropriate texture resolution** for viewing distance
- **Sensible default parameters** for behaviors
- **Comprehensive component setup** out-of-box
- **Clear documentation** and examples

---

*Unity Swarm AI Prefabs v1.0.0 - Ready-to-use components for swarm intelligence projects*