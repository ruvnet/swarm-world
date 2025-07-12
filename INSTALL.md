# Unity Swarm AI Plugin - Installation Guide

## System Requirements

### Unity Compatibility
- **Unity 2021.3 LTS** or newer (recommended)
- **Unity 2020.3 LTS** (minimum, with limitations)
- **Unity 2022.3 LTS** or **2023.3 LTS** (fully supported)

### Platform Support
- ✅ **Windows** (10/11, 64-bit)
- ✅ **macOS** (10.15+, Intel/Apple Silicon)
- ✅ **Linux** (Ubuntu 18.04+, CentOS 7+)
- ✅ **Mobile** (iOS 12+, Android API 21+)
- ✅ **WebGL** (with performance limitations)
- ✅ **Console** (PlayStation, Xbox, Nintendo Switch)

### Hardware Requirements
| Component | Minimum | Recommended | High-Performance |
|-----------|---------|-------------|------------------|
| CPU | Dual-core 2.5GHz | Quad-core 3.0GHz | 8+ core 3.5GHz |
| RAM | 4GB | 8GB | 16GB+ |
| GPU | DirectX 11 | DirectX 12 | RTX/RX series |
| Storage | 100MB free | 500MB free | 1GB+ free |

## Installation Methods

### Method 1: Unity Package Manager (Recommended)

This is the easiest and most reliable installation method.

#### Via Git URL
1. Open Unity and your project
2. Navigate to **Window → Package Manager**
3. Click the **+** button in the top-left corner
4. Select **Add package from git URL...**
5. Enter: `https://github.com/ruvnet/unity-swarm-ai.git`
6. Click **Add**

#### Via Package Manager UI
1. Open **Window → Package Manager**
2. Change dropdown from **In Project** to **Unity Registry**
3. Search for **"Swarm AI"**
4. Select **Unity Swarm AI** by ruvnet
5. Click **Install**

#### Via manifest.json (Advanced)
1. Close Unity
2. Open `Packages/manifest.json` in your project
3. Add this line to the dependencies:
   ```json
   {
     "dependencies": {
       "com.ruvnet.swarm-ai": "https://github.com/ruvnet/unity-swarm-ai.git",
       ...other dependencies
     }
   }
   ```
4. Save the file and reopen Unity

### Method 2: Unity Package (.unitypackage)

For users who prefer traditional package files or have restricted internet access.

1. **Download** the latest `.unitypackage` from [Releases](https://github.com/ruvnet/unity-swarm-ai/releases)
2. In Unity, go to **Assets → Import Package → Custom Package...**
3. **Select** the downloaded `.unitypackage` file
4. In the import dialog, ensure all items are checked
5. Click **Import**

### Method 3: Manual Installation

For development or when you need to modify the source code.

1. **Clone** or download the repository:
   ```bash
   git clone https://github.com/ruvnet/unity-swarm-ai.git
   ```
2. **Copy** the entire plugin folder to your project's `Assets/` directory
3. **Rename** the folder to `SwarmAI` (optional but recommended)
4. Unity will automatically detect and import the assets

## Verification Steps

### 1. Check Package Installation
After installation, verify the package is properly loaded:

1. Open **Window → Package Manager**
2. Switch to **In Project** view
3. Look for **Unity Swarm AI** in the list
4. Version should show (e.g., "1.0.0")

### 2. Verify Core Components
Check that essential scripts are available:

1. Go to **Assets → Create → Swarm AI**
2. You should see these options:
   - Swarm Agent
   - Swarm Manager
   - Behavior Scripts
   - Example Scenes

### 3. Test Basic Functionality
Create a simple test scene:

1. **Create new scene**: File → New Scene
2. **Add Swarm Manager**: GameObject → Swarm AI → Swarm Manager
3. **Create agent prefab**: GameObject → 3D Object → Capsule
4. **Add SwarmAgent script** to the capsule
5. **Assign prefab** to Swarm Manager
6. **Press Play** - you should see agents spawning and moving

### 4. Run Example Scene
Test with the included example:

1. Navigate to **Assets/SwarmAI/Examples/Scenes/**
2. Open **BasicFlocking.unity**
3. Press **Play**
4. You should see 100 agents flocking together

## Configuration

### Initial Setup
After installation, configure the plugin for your project:

#### 1. Physics Settings
The plugin works best with these physics settings:

1. **Edit → Project Settings → Physics**
2. Set **Default Solver Iterations**: 8
3. Set **Default Velocity Iterations**: 2
4. Enable **Auto Simulation** (unless using custom timing)

#### 2. Layer Setup
Create dedicated layers for swarm agents:

1. **Edit → Project Settings → Tags and Layers**
2. Add these layers:
   - `SwarmAgents` (Layer 8)
   - `SwarmObstacles` (Layer 9)
   - `SwarmTargets` (Layer 10)

#### 3. Performance Settings
For optimal performance:

1. **Edit → Project Settings → Quality**
2. For mobile: Reduce **Pixel Light Count** to 1
3. For desktop: Set **V Sync Count** to "Every V Blank"
4. Adjust **LOD Bias** based on your target platform

### Project Structure
The plugin will create this folder structure:

```
Assets/
├── SwarmAI/
│   ├── Scripts/
│   │   ├── Core/          # Core swarm system
│   │   ├── Behaviors/     # Behavior implementations
│   │   ├── Utilities/     # Helper classes
│   │   └── Editor/        # Editor tools
│   ├── Prefabs/
│   │   ├── Agents/        # Predefined agent types
│   │   └── Managers/      # Swarm managers
│   ├── Examples/
│   │   ├── Scenes/        # Demo scenes
│   │   ├── Scripts/       # Example code
│   │   └── Materials/     # Visual materials
│   └── Documentation/     # Additional docs
```

## Dependencies

### Required Dependencies
These are automatically installed with the package:

- **Unity Mathematics** (1.2.6+)
- **Unity Collections** (1.4.0+)
- **Unity Burst** (1.6.6+) - for performance
- **Unity Jobs** (0.8.0+) - for multithreading

### Optional Dependencies
Install these for enhanced functionality:

#### DOTS (Data-Oriented Tech Stack)
For high-performance scenarios (1000+ agents):
```bash
# Via Package Manager → Add by name
com.unity.entities
com.unity.rendering.hybrid
```

#### Visual Scripting
For node-based behavior creation:
```bash
com.unity.visualscripting
```

#### Addressables
For runtime asset loading:
```bash
com.unity.addressables
```

## Platform-Specific Notes

### Mobile Optimization
For iOS/Android deployment:

1. **Enable IL2CPP** scripting backend
2. **Set API Compatibility** to .NET Standard 2.1
3. **Limit agent count** to 100-500 depending on device
4. **Use simplified behaviors** on lower-end devices

### WebGL Considerations
When targeting WebGL:

1. **Reduce agent count** significantly (< 200)
2. **Disable multithreading** (not supported)
3. **Use MonoBehaviour** implementation instead of Jobs
4. **Enable memory optimization** in Player Settings

### Console Platforms
For PlayStation/Xbox/Switch:

1. **Contact support** for platform-specific builds
2. **Performance profiles** available for each console
3. **Memory management** optimizations included

## Troubleshooting

### Common Installation Issues

#### "Package not found" Error
- **Solution**: Check internet connection and try again
- **Alternative**: Use manual installation method

#### Missing Dependencies
- **Solution**: Unity should auto-resolve, but if not:
  1. Window → Package Manager
  2. Switch to "All" packages
  3. Search for missing packages manually

#### Compilation Errors
- **Solution**: 
  1. Check Unity version compatibility
  2. Reimport all assets: Assets → Reimport All
  3. Clear Library folder and reopen project

#### Performance Issues
- **Solution**:
  1. Check hardware requirements
  2. Reduce agent count in examples
  3. Enable "Development Build" to see performance stats

### Getting Help

#### Documentation
- **API Reference**: See `API.md`
- **Examples**: Check `Examples/` folder
- **Tutorials**: See `TUTORIAL.md`

#### Community Support
- **GitHub Issues**: Report bugs and request features
- **Unity Forum**: Community discussions
- **Discord**: Real-time help and tips

#### Professional Support
- **Email**: support@ruvnet.com
- **Documentation**: Full API documentation available
- **Custom Development**: Available for enterprise customers

## Quick Start

Now that installation is complete, you're ready to create your first swarm! 

### Next Steps
1. 📖 Read the **[Tutorial Guide](TUTORIAL.md)** for your first swarm
2. 🔍 Explore the **[API Documentation](API.md)** for detailed reference
3. 🎮 Try the **example scenes** in `Assets/SwarmAI/Examples/`
4. 🚀 Check **[Performance Guide](PERFORMANCE.md)** for optimization tips

---

**Installation Complete!** 🎉  
Your Unity Swarm AI plugin is ready to use. Create intelligent, emergent behaviors with just a few lines of code!

*For technical support, visit our [GitHub repository](https://github.com/ruvnet/unity-swarm-ai) or contact support@ruvnet.com*