using UnityEngine;
using System.Collections.Generic;

namespace UnitySwarmAI.Core
{
    /// <summary>
    /// Interface for swarm coordination systems that manage multiple agents
    /// </summary>
    public interface ISwarmCoordinator
    {
        /// <summary>
        /// Total number of agents being managed
        /// </summary>
        int AgentCount { get; }
        
        /// <summary>
        /// Register a new agent with the coordinator
        /// </summary>
        /// <param name="agent">Agent to register</param>
        void RegisterAgent(ISwarmAgent agent);
        
        /// <summary>
        /// Unregister an agent from the coordinator
        /// </summary>
        /// <param name="agent">Agent to unregister</param>
        void UnregisterAgent(ISwarmAgent agent);
        
        /// <summary>
        /// Get all neighbors within an agent's perception radius
        /// </summary>
        /// <param name="agent">The requesting agent</param>
        /// <returns>List of neighboring agents</returns>
        List<ISwarmAgent> GetNeighbors(ISwarmAgent agent);
        
        /// <summary>
        /// Get all agents within a specific area
        /// </summary>
        /// <param name="center">Center of the query area</param>
        /// <param name="radius">Radius of the query area</param>
        /// <returns>List of agents in the area</returns>
        List<ISwarmAgent> GetAgentsInArea(Vector3 center, float radius);
        
        /// <summary>
        /// Get all registered agents
        /// </summary>
        /// <returns>All agents in the swarm</returns>
        List<ISwarmAgent> GetAllAgents();
        
        /// <summary>
        /// Update the spatial partitioning system
        /// </summary>
        void UpdateSpatialPartitioning();
        
        /// <summary>
        /// Broadcast a message to all agents in range
        /// </summary>
        /// <param name="message">Message to broadcast</param>
        /// <param name="origin">Origin point of the broadcast</param>
        /// <param name="radius">Broadcast radius</param>
        void BroadcastMessage(ISwarmMessage message, Vector3 origin, float radius);
        
        /// <summary>
        /// Set the global swarm target for seeking behaviors
        /// </summary>
        /// <param name="target">Target position</param>
        void SetGlobalTarget(Vector3 target);
        
        /// <summary>
        /// Get performance statistics for the swarm
        /// </summary>
        /// <returns>Performance data</returns>
        SwarmPerformanceData GetPerformanceData();
    }
    
    /// <summary>
    /// Performance data structure for swarm monitoring
    /// </summary>
    [System.Serializable]
    public struct SwarmPerformanceData
    {
        public int totalAgents;
        public float averageNeighborCount;
        public float spatialUpdateTime;
        public float behaviorUpdateTime;
        public float totalUpdateTime;
        public int spatialQueries;
        public float memoryUsage;
    }
}