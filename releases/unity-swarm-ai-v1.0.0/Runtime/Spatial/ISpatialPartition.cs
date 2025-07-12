using UnityEngine;
using System.Collections.Generic;
using UnitySwarmAI.Core;

namespace UnitySwarmAI.Spatial
{
    /// <summary>
    /// Interface for spatial partitioning systems that optimize neighbor queries
    /// </summary>
    public interface ISpatialPartition<T> where T : ISwarmAgent
    {
        /// <summary>
        /// Initialize the spatial partition with given bounds
        /// </summary>
        /// <param name="bounds">World bounds for the partition</param>
        void Initialize(Bounds bounds);
        
        /// <summary>
        /// Add an agent to the spatial partition
        /// </summary>
        /// <param name="agent">Agent to add</param>
        void Insert(T agent);
        
        /// <summary>
        /// Remove an agent from the spatial partition
        /// </summary>
        /// <param name="agent">Agent to remove</param>
        void Remove(T agent);
        
        /// <summary>
        /// Update an agent's position in the partition
        /// </summary>
        /// <param name="agent">Agent to update</param>
        void Update(T agent);
        
        /// <summary>
        /// Query agents within a specific radius from a point
        /// </summary>
        /// <param name="center">Center point of the query</param>
        /// <param name="radius">Radius of the query</param>
        /// <returns>List of agents within the radius</returns>
        List<T> Query(Vector3 center, float radius);
        
        /// <summary>
        /// Query agents within a bounding box
        /// </summary>
        /// <param name="bounds">Bounding box to query</param>
        /// <returns>List of agents within the bounds</returns>
        List<T> Query(Bounds bounds);
        
        /// <summary>
        /// Clear all agents from the partition
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Get debug information about the partition
        /// </summary>
        /// <returns>Debug data structure</returns>
        SpatialPartitionDebugInfo GetDebugInfo();
    }
    
    /// <summary>
    /// Debug information for spatial partitioning systems
    /// </summary>
    [System.Serializable]
    public struct SpatialPartitionDebugInfo
    {
        public int totalCells;
        public int occupiedCells;
        public int totalAgents;
        public float averageAgentsPerCell;
        public int maxAgentsInCell;
        public float lastUpdateTime;
        public long memoryUsage;
    }
}