using UnityEngine;
using System.Collections.Generic;

namespace UnitySwarmAI
{
    /// <summary>
    /// High-performance uniform spatial grid for fast neighbor queries
    /// </summary>
    public class UniformSpatialGrid
    {
        private Dictionary<int, List<ISwarmAgent>> cells;
        private Bounds bounds;
        private float cellSize;
        private int cellsX, cellsY, cellsZ;
        
        public UniformSpatialGrid(Bounds worldBounds, float cellSize)
        {
            this.bounds = worldBounds;
            this.cellSize = cellSize;
            
            // Calculate grid dimensions
            Vector3 size = bounds.size;
            cellsX = Mathf.CeilToInt(size.x / cellSize);
            cellsY = Mathf.CeilToInt(size.y / cellSize);
            cellsZ = Mathf.CeilToInt(size.z / cellSize);
            
            cells = new Dictionary<int, List<ISwarmAgent>>();
        }
        
        public void Clear()
        {
            foreach (var cellList in cells.Values)
            {
                cellList.Clear();
            }
        }
        
        public void Insert(ISwarmAgent agent)
        {
            if (agent == null) return;
            
            int hash = GetCellHash(agent.Position);
            if (!cells.ContainsKey(hash))
            {
                cells[hash] = new List<ISwarmAgent>();
            }
            
            cells[hash].Add(agent);
        }
        
        public List<ISwarmAgent> Query(Vector3 position, float radius)
        {
            var results = new List<ISwarmAgent>();
            
            // Calculate which cells to check
            int minX = GetCellX(position.x - radius);
            int maxX = GetCellX(position.x + radius);
            int minY = GetCellY(position.y - radius);
            int maxY = GetCellY(position.y + radius);
            int minZ = GetCellZ(position.z - radius);
            int maxZ = GetCellZ(position.z + radius);
            
            // Check all relevant cells
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        int hash = GetCellHash(x, y, z);
                        if (cells.ContainsKey(hash))
                        {
                            foreach (var agent in cells[hash])
                            {
                                if (agent != null && agent.IsActive)
                                {
                                    float distance = Vector3.Distance(position, agent.Position);
                                    if (distance <= radius)
                                    {
                                        results.Add(agent);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return results;
        }
        
        private int GetCellHash(Vector3 position)
        {
            int x = GetCellX(position.x);
            int y = GetCellY(position.y);
            int z = GetCellZ(position.z);
            return GetCellHash(x, y, z);
        }
        
        private int GetCellHash(int x, int y, int z)
        {
            // Ensure coordinates are within bounds
            x = Mathf.Clamp(x, 0, cellsX - 1);
            y = Mathf.Clamp(y, 0, cellsY - 1);
            z = Mathf.Clamp(z, 0, cellsZ - 1);
            
            // Use a simple hash function
            return x + y * cellsX + z * cellsX * cellsY;
        }
        
        private int GetCellX(float worldX)
        {
            float relativeX = worldX - bounds.min.x;
            return Mathf.FloorToInt(relativeX / cellSize);
        }
        
        private int GetCellY(float worldY)
        {
            float relativeY = worldY - bounds.min.y;
            return Mathf.FloorToInt(relativeY / cellSize);
        }
        
        private int GetCellZ(float worldZ)
        {
            float relativeZ = worldZ - bounds.min.z;
            return Mathf.FloorToInt(relativeZ / cellSize);
        }
        
        public void DrawGizmos()
        {
            if (!Application.isPlaying) return;
            
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            
            // Draw occupied cells
            foreach (var kvp in cells)
            {
                if (kvp.Value.Count > 0)
                {
                    Vector3 cellCenter = GetCellCenter(kvp.Key);
                    Gizmos.DrawWireCube(cellCenter, Vector3.one * cellSize);
                }
            }
        }
        
        private Vector3 GetCellCenter(int hash)
        {
            // Reverse the hash to get cell coordinates
            int z = hash / (cellsX * cellsY);
            int remainder = hash % (cellsX * cellsY);
            int y = remainder / cellsX;
            int x = remainder % cellsX;
            
            float worldX = bounds.min.x + (x + 0.5f) * cellSize;
            float worldY = bounds.min.y + (y + 0.5f) * cellSize;
            float worldZ = bounds.min.z + (z + 0.5f) * cellSize;
            
            return new Vector3(worldX, worldY, worldZ);
        }
        
        public int GetCellCount()
        {
            return cells.Count;
        }
        
        public int GetAgentCount()
        {
            int count = 0;
            foreach (var cellList in cells.Values)
            {
                count += cellList.Count;
            }
            return count;
        }
    }
}