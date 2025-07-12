using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI
{
    public class SwarmManager : MonoBehaviour
    {
        private static SwarmManager instance;
        public static SwarmManager Instance => instance;
        
        [Header("Spatial Partitioning")]
        [SerializeField] private float cellSize = 10f;
        [SerializeField] private int gridSize = 100;
        [SerializeField] private bool useSpatialPartitioning = true;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxAgentsPerFrame = 100;
        [SerializeField] private float updateFrequency = 60f;
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodDistance = 50f;
        
        [Header("Debug & Monitoring")]
        [SerializeField] private bool showPerformanceMetrics = false;
        [SerializeField] private bool logMetrics = false;
        
        private Dictionary<int, List<SwarmAgent>> spatialGrid;
        private List<SwarmAgent> allAgents;
        private float lastUpdateTime;
        private int agentsUpdatedThisFrame;
        
        // Performance metrics
        public int TotalAgents => allAgents?.Count ?? 0;
        public float AverageNeighbors { get; private set; }
        public float UpdateTime { get; private set; }
        public float MemoryUsage { get; private set; }
        public Dictionary<int, List<SwarmAgent>> SpatialGrid => spatialGrid;
        
        void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            spatialGrid = new Dictionary<int, List<SwarmAgent>>();
            allAgents = new List<SwarmAgent>();
        }
        
        void Update()
        {
            if (Time.time - lastUpdateTime >= 1f / updateFrequency)
            {
                var startTime = Time.realtimeSinceStartup;
                UpdatePerformanceMetrics();
                UpdateTime = (Time.realtimeSinceStartup - startTime) * 1000f; // Convert to ms
                lastUpdateTime = Time.time;
            }
        }
        
        public void RegisterAgent(SwarmAgent agent)
        {
            if (allAgents == null) allAgents = new List<SwarmAgent>();
            allAgents.Add(agent);
            
            if (useSpatialPartitioning)
            {
                UpdateAgentCell(agent);
            }
        }
        
        public void UnregisterAgent(SwarmAgent agent)
        {
            allAgents?.Remove(agent);
            if (useSpatialPartitioning)
            {
                RemoveAgentFromGrid(agent);
            }
        }
        
        private void UpdateAgentCell(SwarmAgent agent)
        {
            int cellKey = GetCellKey(agent.transform.position);
            
            if (!spatialGrid.ContainsKey(cellKey))
            {
                spatialGrid[cellKey] = new List<SwarmAgent>();
            }
            
            spatialGrid[cellKey].Add(agent);
        }
        
        private int GetCellKey(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / cellSize);
            int z = Mathf.FloorToInt(position.z / cellSize);
            return x + z * gridSize;
        }
        
        public List<SwarmAgent> GetNeighbors(Vector3 position, float radius)
        {
            if (!useSpatialPartitioning)
            {
                return allAgents.Where(a => Vector3.Distance(position, a.transform.position) <= radius).ToList();
            }
            
            List<SwarmAgent> neighbors = new List<SwarmAgent>();
            int searchCells = Mathf.CeilToInt(radius / cellSize);
            
            int centerX = Mathf.FloorToInt(position.x / cellSize);
            int centerZ = Mathf.FloorToInt(position.z / cellSize);
            
            for (int x = -searchCells; x <= searchCells; x++)
            {
                for (int z = -searchCells; z <= searchCells; z++)
                {
                    int cellKey = (centerX + x) + (centerZ + z) * gridSize;
                    
                    if (spatialGrid.ContainsKey(cellKey))
                    {
                        foreach (var agent in spatialGrid[cellKey])
                        {
                            float dist = Vector3.Distance(position, agent.transform.position);
                            if (dist <= radius)
                            {
                                neighbors.Add(agent);
                            }
                        }
                    }
                }
            }
            
            return neighbors;
        }
        
        private void RemoveAgentFromGrid(SwarmAgent agent)
        {
            foreach (var cell in spatialGrid.Values)
            {
                cell.Remove(agent);
            }
        }
        
        private void UpdatePerformanceMetrics()
        {
            if (allAgents == null || allAgents.Count == 0) return;
            
            float totalNeighbors = 0;
            foreach (var agent in allAgents)
            {
                if (agent != null && agent.Neighbors != null)
                {
                    totalNeighbors += agent.Neighbors.Count;
                }
            }
            
            AverageNeighbors = totalNeighbors / allAgents.Count;
            MemoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(0) / (1024f * 1024f); // MB
            
            if (logMetrics)
            {
                Debug.Log($"Swarm Metrics - Agents: {TotalAgents}, Avg Neighbors: {AverageNeighbors:F1}, Update Time: {UpdateTime:F2}ms, Memory: {MemoryUsage:F1}MB");
            }
        }
        
        // Runtime configuration methods
        public void SetSpatialPartitioning(bool enabled, float newCellSize = 10f)
        {
            useSpatialPartitioning = enabled;
            cellSize = newCellSize;
            
            if (enabled)
            {
                RebuildSpatialGrid();
            }
        }
        
        public void SetPerformanceSettings(int maxAgents, float frequency, bool enableLODSystem)
        {
            maxAgentsPerFrame = maxAgents;
            updateFrequency = frequency;
            enableLOD = enableLODSystem;
        }
        
        private void RebuildSpatialGrid()
        {
            spatialGrid.Clear();
            foreach (var agent in allAgents)
            {
                if (agent != null)
                {
                    UpdateAgentCell(agent);
                }
            }
        }
        
        // Utility methods for editor
        public void ClearAllAgents()
        {
            var agentsToDestroy = allAgents.ToList();
            foreach (var agent in agentsToDestroy)
            {
                if (agent != null)
                {
                    DestroyImmediate(agent.gameObject);
                }
            }
            allAgents.Clear();
            spatialGrid.Clear();
        }
        
        public void SpawnTestAgents(int count, GameObject prefab, Bounds area)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(area.min.x, area.max.x),
                    area.center.y,
                    Random.Range(area.min.z, area.max.z)
                );
                
                GameObject newAgent = Instantiate(prefab, position, Quaternion.identity);
                newAgent.transform.SetParent(transform);
                newAgent.name = $"SwarmAgent_{i:000}";
            }
        }
        
        void OnDrawGizmosSelected()
        {
            if (showPerformanceMetrics && spatialGrid != null && useSpatialPartitioning)
            {
                Gizmos.color = Color.gray;
                
                // Draw spatial grid
                for (int x = -gridSize/2; x < gridSize/2; x++)
                {
                    for (int z = -gridSize/2; z < gridSize/2; z++)
                    {
                        Vector3 cellCenter = new Vector3(x * cellSize, 0, z * cellSize);
                        Gizmos.DrawWireCube(cellCenter, Vector3.one * cellSize);
                    }
                }
            }
        }
    }
}