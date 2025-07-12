# Changelog - Unity Swarm AI

All notable changes to this project will be documented in this file.

## [1.0.1] - 2025-07-12

### 🔧 Fixed
- **Package Manager Compatibility**: Fixed package.json structure for Unity Package Manager tarball installation
- **Dependency Issues**: Removed invalid Unity package references (com.unity.jobs@0.70.0, com.unity.entities@1.0.16)
- **Sample Import**: Fixed sample paths and created complete BasicFlocking demo with working scene, prefabs, and assets
- **Installation Errors**: Resolved NullReferenceException from Unity's render pipeline during package import

### ✨ Added
- **Complete BasicFlocking Sample**: Ready-to-use scene with 50 flocking agents, SwarmAgent prefab, and behavior asset
- **Minimal Package Version**: Zero-dependency package for maximum compatibility
- **Comprehensive Documentation**: Installation guides, troubleshooting, and usage tutorials
- **Debug Visualization**: Gizmos showing agent perception, neighbor connections, and steering forces

### 🚀 Improved
- **Package Structure**: Proper Unity package format with package.json at root level
- **Performance Optimization**: Enhanced SwarmAgent with spatial partitioning integration
- **Editor Integration**: Professional Unity component menus and custom inspectors
- **Cross-Platform Compatibility**: Supports Unity 2021.3+ on all platforms

### 📦 Package Formats
- **Standard Version**: 25KB with Mathematics, Collections, Burst dependencies
- **Minimal Version**: 21KB with zero dependencies
- **ZIP Version**: 38KB for manual installation

### 🛠️ Technical Changes
- Updated UnitySwarmAI namespace organization
- Enhanced SwarmBehavior base class with utility methods
- Improved FlockingBehavior with weighted algorithms
- Added proper assembly definitions for compilation
- Fixed sample directory structure (Examples/BasicFlocking)

---

## [1.0.0] - 2025-07-12

### 🎉 Initial Release
- Complete Unity Swarm AI system with modular architecture
- Classic boids flocking algorithm implementation
- Spatial partitioning for performance optimization
- Professional Unity editor tools and setup wizard
- Comprehensive documentation and API reference