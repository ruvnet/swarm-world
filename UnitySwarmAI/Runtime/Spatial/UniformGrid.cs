using UnityEngine;
using System.Collections.Generic;
using UnitySwarmAI.Core;

namespace UnitySwarmAI.Spatial
{
    /// <summary>
    /// Uniform grid spatial partitioning implementation for efficient neighbor queries
    /// </summary>
    [System.Serializable]
    public class UniformGrid<T> : ISpatialPartition<T> where T : ISwarmAgent
    {
        [SerializeField] private float cellSize;
        [SerializeField] private Bounds bounds;
        [SerializeField] private int gridWidth;
        [SerializeField] private int gridHeight;
        [SerializeField] private int gridDepth;
        
        private Dictionary<int, List<T>> cells;
        private Dictionary<T, int> agentToCells;
        private readonly List<T> queryResults = new List<T>();
        private readonly HashSet<int> visitedCells = new HashSet<int>();
        
        /// <summary>
        /// Create a new uniform grid with specified cell size
        /// </summary>
        /// <param name="cellSize">Size of each grid cell</param>
        public UniformGrid(float cellSize = 5f)
        {
            this.cellSize = cellSize;
            cells = new Dictionary<int, List<T>>();
            agentToCells = new Dictionary<T, int>();
        }
        
        public void Initialize(Bounds bounds)
        {
            this.bounds = bounds;
            gridWidth = Mathf.CeilToInt(bounds.size.x / cellSize);
            gridHeight = Mathf.CeilToInt(bounds.size.y / cellSize);
            gridDepth = Mathf.CeilToInt(bounds.size.z / cellSize);
            
            Clear();
        }
        
        public void Insert(T agent)
        {
            int cellHash = GetCellHash(agent.Position);
            
            if (!cells.ContainsKey(cellHash))
                cells[cellHash] = new List<T>();
            
            cells[cellHash].Add(agent);
            agentToCells[agent] = cellHash;
        }
        
        public void Remove(T agent)
        {
            if (agentToCells.TryGetValue(agent, out int cellHash))
            {
                if (cells.ContainsKey(cellHash))
                {
                    cells[cellHash].Remove(agent);
                    if (cells[cellHash].Count == 0)
                        cells.Remove(cellHash);
                }
                agentToCells.Remove(agent);
            }
        }
        
        public void Update(T agent)
        {
            int newCellHash = GetCellHash(agent.Position);
            
            if (agentToCells.TryGetValue(agent, out int oldCellHash))
            {
                if (oldCellHash != newCellHash)
                {
                    // Remove from old cell
                    if (cells.ContainsKey(oldCellHash))
                    {
                        cells[oldCellHash].Remove(agent);
                        if (cells[oldCellHash].Count == 0)
                            cells.Remove(oldCellHash);
                    }
                    
                    // Add to new cell
                    if (!cells.ContainsKey(newCellHash))
                        cells[newCellHash] = new List<T>();
                    
                    cells[newCellHash].Add(agent);
                    agentToCells[agent] = newCellHash;
                }
            }
            else
            {
                // Agent not tracked, insert it
                Insert(agent);
            }
        }
        
        public List<T> Query(Vector3 center, float radius)
        {
            queryResults.Clear();
            visitedCells.Clear();
            
            // Calculate grid range to check
            Vector3 min = center - Vector3.one * radius;
            Vector3 max = center + Vector3.one * radius;
            
            int minX = GetGridCoordinate(min.x, 0);
            int maxX = GetGridCoordinate(max.x, 0);
            int minY = GetGridCoordinate(min.y, 1);
            int maxY = GetGridCoordinate(max.y, 1);
            int minZ = GetGridCoordinate(min.z, 2);
            int maxZ = GetGridCoordinate(max.z, 2);
            
            // Check all cells in range
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        int cellHash = HashCell(x, y, z);
                        
                        if (visitedCells.Contains(cellHash) || !cells.ContainsKey(cellHash))
                            continue;
                        
                        visitedCells.Add(cellHash);
                        
                        foreach (T agent in cells[cellHash])
                        {
                            float distance = Vector3.Distance(center, agent.Position);
                            if (distance <= radius)
                                queryResults.Add(agent);
                        }
                    }
                }
            }
            
            return new List<T>(queryResults);
        }
        
        public List<T> Query(Bounds queryBounds)
        {
            queryResults.Clear();
            visitedCells.Clear();
            
            Vector3 min = queryBounds.min;
            Vector3 max = queryBounds.max;
            
            int minX = GetGridCoordinate(min.x, 0);
            int maxX = GetGridCoordinate(max.x, 0);
            int minY = GetGridCoordinate(min.y, 1);
            int maxY = GetGridCoordinate(max.y, 1);
            int minZ = GetGridCoordinate(min.z, 2);
            int maxZ = GetGridCoordinate(max.z, 2);
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        int cellHash = HashCell(x, y, z);
                        
                        if (visitedCells.Contains(cellHash) || !cells.ContainsKey(cellHash))
                            continue;
                        
                        visitedCells.Add(cellHash);
                        
                        foreach (T agent in cells[cellHash])
                        {
                            if (queryBounds.Contains(agent.Position))
                                queryResults.Add(agent);
                        }
                    }
                }
            }
            
            return new List<T>(queryResults);
        }
        
        public void Clear()
        {
            cells.Clear();
            agentToCells.Clear();
        }
        
        public SpatialPartitionDebugInfo GetDebugInfo()
        {
            return new SpatialPartitionDebugInfo
            {
                totalCells = gridWidth * gridHeight * gridDepth,
                occupiedCells = cells.Count,
                totalAgents = agentToCells.Count,
                averageAgentsPerCell = cells.Count > 0 ? (float)agentToCells.Count / cells.Count : 0,
                maxAgentsInCell = GetMaxAgentsInCell(),
                lastUpdateTime = Time.time,
                memoryUsage = EstimateMemoryUsage()
            };
        }
        
        private int GetCellHash(Vector3 position)
        {
            int x = GetGridCoordinate(position.x, 0);
            int y = GetGridCoordinate(position.y, 1);
            int z = GetGridCoordinate(position.z, 2);
            return HashCell(x, y, z);
        }
        
        private int GetGridCoordinate(float value, int axis)
        {
            float minBound = axis == 0 ? bounds.min.x : (axis == 1 ? bounds.min.y : bounds.min.z);
            return Mathf.FloorToInt((value - minBound) / cellSize);
        }
        
        private int HashCell(int x, int y, int z)
        {
            // Simple hash function for 3D coordinates
            return x * 73856093 ^ y * 19349663 ^ z * 83492791;
        }
        
        private int GetMaxAgentsInCell()
        {
            int max = 0;
            foreach (var cell in cells.Values)
            {
                if (cell.Count > max)
                    max = cell.Count;
            }
            return max;
        }
        
        private long EstimateMemoryUsage()
        {
            // Rough estimation of memory usage
            long cellMemory = cells.Count * (sizeof(int) + sizeof(System.IntPtr)); // Dictionary overhead
            long agentMemory = agentToCells.Count * (sizeof(System.IntPtr) + sizeof(int));
            return cellMemory + agentMemory;
        }
    }
}