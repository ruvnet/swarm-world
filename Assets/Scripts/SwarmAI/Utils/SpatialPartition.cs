using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Core;

namespace SwarmAI.Utils
{
    /// <summary>
    /// Uniform grid spatial partitioning for efficient neighbor queries
    /// Optimized for swarm systems with many agents
    /// </summary>
    public class SpatialPartition
    {
        private Dictionary<int, List<ISwarmAgent>> cells;
        private float cellSize;
        private Bounds worldBounds;
        private int gridWidth;
        private int gridHeight;
        private int gridDepth;
        
        public SpatialPartition(Bounds bounds, float cellSize)
        {
            this.worldBounds = bounds;
            this.cellSize = Mathf.Max(cellSize, 0.1f);
            
            // Calculate grid dimensions
            gridWidth = Mathf.CeilToInt(bounds.size.x / cellSize);
            gridHeight = Mathf.CeilToInt(bounds.size.y / cellSize);
            gridDepth = Mathf.CeilToInt(bounds.size.z / cellSize);
            
            cells = new Dictionary<int, List<ISwarmAgent>>();
        }
        
        public void Clear()
        {
            foreach (var cell in cells.Values)
            {
                cell.Clear();
            }
        }
        
        public void Add(ISwarmAgent agent)
        {
            int hash = GetCellHash(agent.Position);
            if (!cells.ContainsKey(hash))
            {
                cells[hash] = new List<ISwarmAgent>();
            }
            cells[hash].Add(agent);
        }
        
        public List<ISwarmAgent> GetNeighbors(Vector3 position, float radius)
        {
            List<ISwarmAgent> neighbors = new List<ISwarmAgent>();
            
            // Calculate search bounds
            int cellRadius = Mathf.CeilToInt(radius / cellSize);
            Vector3Int centerCell = WorldToGrid(position);
            
            // Search neighboring cells
            for (int x = centerCell.x - cellRadius; x <= centerCell.x + cellRadius; x++)
            {
                for (int y = centerCell.y - cellRadius; y <= centerCell.y + cellRadius; y++)
                {
                    for (int z = centerCell.z - cellRadius; z <= centerCell.z + cellRadius; z++)
                    {
                        int hash = GetCellHash(x, y, z);
                        if (cells.ContainsKey(hash))
                        {
                            foreach (var agent in cells[hash])
                            {
                                float distance = Vector3.Distance(position, agent.Position);
                                if (distance <= radius)
                                {
                                    neighbors.Add(agent);
                                }
                            }
                        }
                    }
                }
            }
            
            return neighbors;
        }
        
        public List<ISwarmAgent> GetNeighborsExcluding(ISwarmAgent excludeAgent, float radius)
        {
            List<ISwarmAgent> neighbors = GetNeighbors(excludeAgent.Position, radius);
            neighbors.Remove(excludeAgent);
            return neighbors;
        }
        
        private Vector3Int WorldToGrid(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - worldBounds.min;
            return new Vector3Int(
                Mathf.FloorToInt(localPos.x / cellSize),
                Mathf.FloorToInt(localPos.y / cellSize),
                Mathf.FloorToInt(localPos.z / cellSize)
            );
        }
        
        private int GetCellHash(Vector3 position)
        {
            Vector3Int gridPos = WorldToGrid(position);
            return GetCellHash(gridPos.x, gridPos.y, gridPos.z);
        }
        
        private int GetCellHash(int x, int y, int z)
        {
            // Use large primes to reduce collisions
            return x * 73856093 ^ y * 19349663 ^ z * 83492791;
        }
        
        public void DrawGizmos()
        {
            // Draw grid bounds
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
            
            // Draw occupied cells
            Gizmos.color = new Color(1, 1, 0, 0.1f);
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
            // This is an approximation - for debugging only
            return worldBounds.center;
        }
        
        public int GetCellCount()
        {
            return cells.Count;
        }
        
        public int GetAgentCount()
        {
            int count = 0;
            foreach (var cell in cells.Values)
            {
                count += cell.Count;
            }
            return count;
        }
        
        public float GetAverageAgentsPerCell()
        {
            if (cells.Count == 0) return 0f;
            return (float)GetAgentCount() / cells.Count;
        }
        
        public float GetLoadFactor()
        {
            int totalCells = gridWidth * gridHeight * gridDepth;
            return (float)cells.Count / totalCells;
        }
    }
    
    /// <summary>
    /// Octree implementation for dynamic spatial partitioning
    /// Better for non-uniform agent distributions
    /// </summary>
    public class SwarmOctree
    {
        private class OctreeNode
        {
            public Bounds bounds;
            public List<ISwarmAgent> agents;
            public OctreeNode[] children;
            public bool isLeaf = true;
            public int depth;
            
            public OctreeNode(Bounds bounds, int depth)
            {
                this.bounds = bounds;
                this.depth = depth;
                this.agents = new List<ISwarmAgent>();
                this.children = new OctreeNode[8];
            }
            
            public void Subdivide()
            {
                if (!isLeaf) return;
                
                Vector3 center = bounds.center;
                Vector3 halfSize = bounds.size * 0.5f;
                Vector3 quarterSize = halfSize * 0.5f;
                
                children[0] = new OctreeNode(new Bounds(center + new Vector3(-quarterSize.x, quarterSize.y, quarterSize.z), halfSize), depth + 1);
                children[1] = new OctreeNode(new Bounds(center + new Vector3(quarterSize.x, quarterSize.y, quarterSize.z), halfSize), depth + 1);
                children[2] = new OctreeNode(new Bounds(center + new Vector3(-quarterSize.x, quarterSize.y, -quarterSize.z), halfSize), depth + 1);
                children[3] = new OctreeNode(new Bounds(center + new Vector3(quarterSize.x, quarterSize.y, -quarterSize.z), halfSize), depth + 1);
                children[4] = new OctreeNode(new Bounds(center + new Vector3(-quarterSize.x, -quarterSize.y, quarterSize.z), halfSize), depth + 1);
                children[5] = new OctreeNode(new Bounds(center + new Vector3(quarterSize.x, -quarterSize.y, quarterSize.z), halfSize), depth + 1);
                children[6] = new OctreeNode(new Bounds(center + new Vector3(-quarterSize.x, -quarterSize.y, -quarterSize.z), halfSize), depth + 1);
                children[7] = new OctreeNode(new Bounds(center + new Vector3(quarterSize.x, -quarterSize.y, -quarterSize.z), halfSize), depth + 1);
                
                isLeaf = false;
                
                // Redistribute agents to children
                foreach (var agent in agents)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (children[i].bounds.Contains(agent.Position))
                        {
                            children[i].agents.Add(agent);
                            break;
                        }
                    }
                }
                
                agents.Clear();
            }
        }
        
        private OctreeNode root;
        private int maxAgentsPerNode;
        private int maxDepth;
        
        public SwarmOctree(Bounds bounds, int maxAgentsPerNode = 10, int maxDepth = 6)
        {
            this.maxAgentsPerNode = maxAgentsPerNode;
            this.maxDepth = maxDepth;
            root = new OctreeNode(bounds, 0);
        }
        
        public void Clear()
        {
            root = new OctreeNode(root.bounds, 0);
        }
        
        public void Insert(ISwarmAgent agent)
        {
            InsertRecursive(root, agent);
        }
        
        private void InsertRecursive(OctreeNode node, ISwarmAgent agent)
        {
            if (!node.bounds.Contains(agent.Position))
                return;
            
            if (node.isLeaf)
            {
                node.agents.Add(agent);
                
                // Subdivide if necessary
                if (node.agents.Count > maxAgentsPerNode && node.depth < maxDepth)
                {
                    node.Subdivide();
                }
            }
            else
            {
                // Insert into appropriate child
                for (int i = 0; i < 8; i++)
                {
                    if (node.children[i].bounds.Contains(agent.Position))
                    {
                        InsertRecursive(node.children[i], agent);
                        break;
                    }
                }
            }
        }
        
        public List<ISwarmAgent> Query(Vector3 position, float radius)
        {
            List<ISwarmAgent> result = new List<ISwarmAgent>();
            Bounds queryBounds = new Bounds(position, Vector3.one * radius * 2);
            QueryRecursive(root, queryBounds, position, radius, result);
            return result;
        }
        
        private void QueryRecursive(OctreeNode node, Bounds queryBounds, Vector3 position, float radius, List<ISwarmAgent> result)
        {
            if (!node.bounds.Intersects(queryBounds))
                return;
            
            if (node.isLeaf)
            {
                foreach (var agent in node.agents)
                {
                    if (Vector3.Distance(position, agent.Position) <= radius)
                    {
                        result.Add(agent);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    QueryRecursive(node.children[i], queryBounds, position, radius, result);
                }
            }
        }
        
        public void DrawGizmos()
        {
            DrawNodeGizmos(root);
        }
        
        private void DrawNodeGizmos(OctreeNode node)
        {
            if (node.isLeaf && node.agents.Count > 0)
            {
                Gizmos.color = new Color(1, 0, 1, 0.2f);
                Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);
            }
            else if (!node.isLeaf)
            {
                for (int i = 0; i < 8; i++)
                {
                    DrawNodeGizmos(node.children[i]);
                }
            }
        }
    }
}