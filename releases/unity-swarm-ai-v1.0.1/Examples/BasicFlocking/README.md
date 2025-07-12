# Basic Flocking Demo

This sample demonstrates the classic boids flocking algorithm using Unity Swarm AI.

## What's Included

- **FlockingScene.unity** - Ready-to-use scene with 50 flocking agents
- **SwarmAgent.prefab** - Configured swarm agent prefab
- **BasicFlocking.asset** - Flocking behavior configuration
- **README.md** - This file

## How to Use

1. **Open Scene**: Open `FlockingScene.unity`
2. **Press Play**: Watch 50 agents flock together
3. **Adjust Parameters**: Modify behavior settings in real-time

## Customization

### Agent Settings (SwarmAgent prefab)
- **Max Speed**: How fast agents can move
- **Max Force**: Steering force strength
- **Perception Radius**: How far agents can "see" neighbors

### Flocking Behavior (BasicFlocking asset)
- **Separation Weight**: Avoid crowding (higher = more spread out)
- **Alignment Weight**: Match neighbor direction (higher = more aligned)
- **Cohesion Weight**: Move toward group center (higher = tighter groups)

## Performance Tips

- Start with 50 agents for smooth performance
- Enable "Use Spatial Partitioning" for larger swarms
- Reduce perception radius for better performance
- Use "Show Debug Gizmos" to visualize behavior

## Next Steps

1. Try different parameter combinations
2. Add obstacles for agents to avoid
3. Create predator-prey scenarios
4. Scale up to 500+ agents with optimization

Happy swarming! 🐝