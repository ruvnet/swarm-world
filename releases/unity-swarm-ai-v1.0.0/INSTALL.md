# Unity Swarm AI Plugin - Installation Guide

## 🚀 Quick Installation (Recommended)

### Method 1: Unity Package Manager
1. **Open Unity** (2021.3 LTS or newer recommended)
2. **Open Package Manager**: Window → Package Manager
3. **Add from Git**: Click '+' dropdown → "Add package from git URL"
4. **Enter URL**: `https://github.com/ruvnet/swarm-world.git?path=/UnitySwarmAI`
5. **Install**: Click "Add" and wait for Unity to import the package
6. **Verify**: Check that "Unity Swarm AI" appears in Package Manager

### Method 2: Manual Package Installation
1. **Download**: Clone or download the repository
   ```bash
   git clone https://github.com/ruvnet/swarm-world.git
   ```
2. **Copy Plugin**: Copy the `/UnitySwarmAI` folder to your project's `Packages/` directory
3. **Auto-Import**: Unity will automatically detect and import the package
4. **Verify**: Check Package Manager for "Unity Swarm AI" (In Project packages)

## 📋 System Requirements

### Unity Requirements
- **Unity Version**: 2021.3 LTS or newer (recommended: 2022.3 LTS)
- **Render Pipeline**: Built-in, URP, or HDRP supported
- **Platform**: All Unity-supported platforms

### Dependencies (Auto-installed)
- `com.unity.mathematics` (1.2.6+)
- `com.unity.collections` (1.2.4+)
- `com.unity.burst` (1.8.8+)
- `com.unity.jobs` (0.70.0+)

### Hardware Recommendations
- **CPU**: Multi-core processor (for Job System optimization)
- **RAM**: 8GB+ (16GB recommended for large swarms)
- **GPU**: DirectX 11/12 or Metal (for compute shader support)

## ✅ Installation Verification

### Quick Test (30 seconds)
1. **Open Package Manager**: Window → Package Manager
2. **Find Unity Swarm AI**: Look for it in "In Project" packages
3. **Import Sample**: Click on Unity Swarm AI → Samples → Import "Basic Flocking"
4. **Open Scene**: Navigate to imported sample scene
5. **Play**: Hit Play button - you should see agents flocking smoothly
6. **Success**: If agents are moving in coordinated patterns, installation is complete!

### Setup Wizard (Recommended)
1. **Open Wizard**: Tools → Unity Swarm AI → Setup Wizard
2. **Follow Steps**: Configure physics layers and performance settings
3. **Create Test Scene**: Let wizard create a basic swarm scene
4. **Test Performance**: Run the created scene to verify everything works

## 🔧 Configuration (Optional but Recommended)

### Physics Settings
```csharp
// Recommended layers for optimal performance
Layer 8: "SwarmAgents"    // For agent colliders
Layer 9: "SwarmObstacles" // For obstacles
Layer 10: "SwarmBounds"   // For boundary walls
```

### Performance Settings
- **Physics Timestep**: 0.02 (50Hz) for smooth movement
- **Maximum Allowed Timestep**: 0.1 for frame rate stability
- **Solver Iterations**: Velocity 8, Position 3 for accuracy

### Project Settings Optimization
```csharp
// Recommended settings for large swarms
Jobs → Job Worker Count: Use Job System
Physics → Default Solver Iterations: Velocity 8, Position 3
Graphics → Instancing: Enable for better rendering performance
```

## 🎮 Platform-Specific Notes

### Mobile (iOS/Android)
- **Agent Limit**: Recommended 100-300 agents
- **Spatial Partitioning**: Always enabled for performance
- **Graphics**: Use simplified materials and lower LOD distances

### WebGL
- **Agent Limit**: Recommended 50-150 agents
- **Job System**: Limited multithreading support
- **Memory**: Monitor memory usage with large swarms

### Console (PlayStation/Xbox/Switch)
- **Full Support**: All features available
- **Performance**: Excellent scaling up to 2000+ agents
- **Optimization**: Platform-specific optimizations included

## 🐛 Troubleshooting

### Installation Issues

**Package not found in Package Manager**
- Verify Unity version (2021.3+ required)
- Check internet connection
- Try manual installation method

**Missing dependencies error**
- Package Manager → Refresh packages
- Manually install required packages:
  - Mathematics: Window → Package Manager → Unity Registry → Mathematics

**Scripts not compiling**
- Check Unity version compatibility
- Verify all dependencies installed
- Restart Unity

### Setup Issues

**Agents not spawning**
- Check SwarmManager is in scene
- Verify agent prefab has SwarmAgent component
- Ensure spawn area bounds are large enough

**Poor performance**
- Enable spatial partitioning in SwarmManager
- Reduce agent count for testing
- Check Performance Monitor for bottlenecks

**Agents not moving**
- Verify behaviors are assigned and enabled
- Check behavior weights (not all zero)
- Ensure agent has valid perception radius

## 📞 Support

### Quick Help
- **Documentation**: [Complete Guides](Documentation/)
- **API Reference**: [API Documentation](Documentation/API.md)
- **Examples**: [7 Demo Scenes](Examples/)

### Community Support
- **GitHub Issues**: [Report bugs or request features](https://github.com/ruvnet/swarm-world/issues)
- **Discord**: [Join our community server](https://discord.gg/swarmworld)
- **Email**: support@swarmworld.dev

### Professional Support
- **Priority Support**: Available for commercial projects
- **Custom Development**: Specialized swarm solutions
- **Training**: Team training and consultation

## 🚀 Next Steps

### Getting Started
1. **Tutorial**: Follow [TUTORIAL.md](Documentation/TUTORIAL.md) for hands-on learning
2. **Examples**: Explore the 7 demo scenes in Examples folder
3. **API**: Review [API.md](Documentation/API.md) for advanced usage

### Performance Optimization
1. **Read**: [PERFORMANCE.md](Documentation/PERFORMANCE.md) optimization guide
2. **Profile**: Use built-in performance monitoring tools
3. **Scale**: Gradually increase agent count while monitoring performance

### Advanced Features
1. **Custom Behaviors**: Create your own swarm behaviors
2. **Job System**: Implement Burst-compiled parallel processing
3. **GPU Compute**: Scale to 10,000+ agents with compute shaders

---

**Installation Time**: ~2 minutes  
**First Swarm**: ~5 minutes  
**Ready for Production**: ~30 minutes with documentation

*Need help? Check our [Troubleshooting Guide](Documentation/TROUBLESHOOTING.md) or contact support.*