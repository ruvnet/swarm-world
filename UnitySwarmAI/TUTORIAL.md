# Unity Swarm AI - 10-Minute Tutorial

🎯 **Goal**: Create your first working swarm in under 10 minutes!

## Prerequisites
- Unity 2021.3+ installed
- Unity Swarm AI package installed ([Installation Guide](INSTALL.md))
- Basic Unity knowledge

## Step 1: Create New Scene (1 minute)

1. **Create Scene**
   - File → New Scene
   - Choose "3D Core" template
   - Save as "MyFirstSwarm"

2. **Setup Camera**
   - Position Main Camera at (0, 10, -10)
   - Rotate to look down at origin (X: 45°)

## Step 2: Add Swarm Manager (2 minutes)

1. **Create Manager GameObject**
   - Right-click in Hierarchy
   - Create Empty → Rename to "SwarmManager"

2. **Add SwarmManager Component**
   - Select SwarmManager GameObject
   - Inspector → Add Component → "Swarm Manager"

3. **Configure Manager**
   ```
   ✅ Use Spatial Partitioning: true
   ✅ Cell Size: 5
   ✅ World Bounds: (0,0,0) Size (50,20,50)
   ✅ Auto Spawn: true
   ✅ Target Agent Count: 30
   ✅ Spawn Radius: 15
   ```

## Step 3: Create Agent Prefab (3 minutes)

1. **Create Agent GameObject**
   - Right-click Hierarchy → 3D Object → Sphere
   - Rename to "SwarmAgent"
   - Scale to (0.5, 0.5, 0.5)

2. **Add SwarmAgent Component**
   - Inspector → Add Component → "Swarm Agent"
   - Configure basic settings:
   ```
   Movement:
   ✅ Max Speed: 8
   ✅ Max Force: 10
   ✅ Mass: 1
   
   Perception:
   ✅ Perception Radius: 3
   ✅ Agent Layer: Default
   
   Boundaries:
   ✅ Enforce Boundaries: true
   ✅ Boundaries: (0,0,0) Size (50,20,50)
   ✅ Boundary Force: 15
   
   Debug:
   ✅ Show Debug Gizmos: true
   ```

3. **Create Flocking Behavior**
   - Right-click Project → Create → Unity Swarm AI → Behaviors → Flocking
   - Name it "BasicFlocking"
   - Configure in Inspector:
   ```
   Behavior Settings:
   ✅ Weight: 1
   ✅ Enabled: true
   
   Flocking Weights:
   ✅ Separation Weight: 1.5
   ✅ Alignment Weight: 1.0
   ✅ Cohesion Weight: 1.0
   
   Flocking Distances:
   ✅ Separation Distance: 2
   ✅ Alignment Distance: 4
   ✅ Cohesion Distance: 4
   ```

4. **Assign Behavior to Agent**
   - Select SwarmAgent in Hierarchy
   - Expand "Behaviors" section
   - Set Size to 1
   - Drag "BasicFlocking" asset to Element 0

5. **Make Prefab**
   - Drag SwarmAgent from Hierarchy to Project window
   - Delete original SwarmAgent from Hierarchy

## Step 4: Connect Prefab to Manager (1 minute)

1. **Assign Agent Prefab**
   - Select SwarmManager in Hierarchy
   - Drag SwarmAgent prefab to "Agent Prefab" field

## Step 5: Test Your Swarm! (1 minute)

1. **Press Play**
   - You should see 30 sphere agents spawn
   - They should start flocking together
   - Green wireframe spheres show perception radius

2. **What You Should See**
   - Agents moving in coordinated groups
   - Smooth flocking behavior (no clumping)
   - Agents staying within boundaries
   - Realistic bird/fish-like movement

## Step 6: Real-Time Tuning (2 minutes)

### **Experiment with Parameters** (while playing):

1. **Flocking Behavior**
   - Select BasicFlocking asset
   - Try different weight combinations:
     - **Tight Flocks**: Separation=0.5, Alignment=2, Cohesion=2
     - **Loose Swarms**: Separation=2, Alignment=0.5, Cohesion=0.5
     - **Formation Flying**: Separation=1, Alignment=3, Cohesion=1

2. **Agent Movement**
   - Select any agent in Scene
   - Try different speeds:
     - **Slow & Steady**: Max Speed=4, Max Force=5
     - **Fast & Agile**: Max Speed=15, Max Force=20

3. **Perception**
   - Change Perception Radius (1-8)
   - See how it affects group cohesion

### **Visual Debugging**
- Green spheres = perception radius
- Lines = neighbor connections  
- Colored rays = steering forces (if debug enabled)

## Troubleshooting

### **No Agents Spawning**
- Check Agent Prefab is assigned to SwarmManager
- Verify Auto Spawn is enabled
- Check Target Agent Count > 0

### **Agents Not Moving**
- Verify FlockingBehavior is assigned
- Check behavior Weight > 0 and Enabled = true
- Ensure Max Speed > 0

### **Agents Clumping**
- Increase Separation Weight to 2-3
- Reduce Separation Distance to 1-1.5
- Increase Perception Radius

### **Agents Dispersing**
- Increase Cohesion Weight to 2-3
- Increase Cohesion Distance to 5-8
- Reduce Separation Weight to 0.5-1

### **Poor Performance**
- Reduce agent count to 20
- Disable Debug Gizmos
- Enable Spatial Partitioning

## Next Steps

### **🎨 Visual Improvements**
1. **Add Materials**
   - Create colorful materials
   - Assign to agent prefab

2. **Add Trails**
   - Add Trail Renderer to agents
   - Configure for visual trails

### **🧠 Behavior Expansion**
1. **Obstacle Avoidance**
   - Add cubes as obstacles
   - Create ObstacleAvoidance behavior

2. **Target Following**
   - Create empty GameObject as target
   - Add SeekBehavior to agents

### **⚡ Performance Testing**
1. **Scale Up**
   - Increase agent count to 100
   - Monitor FPS in Game view

2. **Optimize**
   - Enable LOD system
   - Tune update rates

## Example Scenes

Check out the included examples:
- **BasicFlocking** - What you just built
- **AntColony** - Pheromone trails and resource gathering
- **FormationFlying** - Military aircraft formations  
- **PredatorPrey** - Ecosystem simulation
- **StressTest** - 1000+ agent performance demo

## API Quick Reference

```csharp
// Get swarm manager
var manager = SwarmManager.Instance;

// Spawn more agents
manager.SpawnAgents(50);

// Get agent neighbors
var neighbors = agent.GetNeighborsInRadius(5f);

// Send message
agent.ReceiveMessage(new SwarmMessage("Danger", dangerPos));

// Check performance
var perf = manager.GetPerformanceData();
Debug.Log($"FPS: {perf.AverageFPS}, Agents: {perf.ActiveAgents}");
```

## 🎉 Congratulations!

You've successfully created your first Unity swarm! The agents should be flocking naturally with realistic bird/fish-like behavior.

**What's Next?**
- Read the [Performance Guide](PERFORMANCE.md) to optimize for larger swarms
- Check out [API Documentation](API.md) for advanced features  
- Explore [Example Scenes](Examples/) for inspiration
- Join our community for support and ideas

**Share Your Creation!** Tag us @claudeflow with your swarm videos! 🐝✨