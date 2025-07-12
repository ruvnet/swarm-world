using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SwarmAI.Utils;

namespace SwarmAI.Core
{
    /// <summary>
    /// Central manager for swarm systems with spatial partitioning and performance optimization
    /// Singleton pattern for easy access from agents
    /// </summary>
    public class SwarmManager : MonoBehaviour
    {
        public static SwarmManager Instance { get; private set; }
        
        [Header("Spawn Settings")]
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private int agentCount = 100;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private Transform spawnCenter;
        
        [Header("World Boundaries")]
        [SerializeField] private Vector3 boundarySize = new Vector3(50, 20, 50);
        [SerializeField] private bool showBoundaries = true;
        
        [Header("Spatial Partitioning")]
        [SerializeField] private SpatialPartitionType partitionType = SpatialPartitionType.UniformGrid;
        [SerializeField] private float cellSize = 5f;
        [SerializeField] private int maxAgentsPerOctreeNode = 10;
        [SerializeField] private int octreeMaxDepth = 6;
        [SerializeField] private bool enableSpatialPartitioning = true;
        
        [Header("Performance")]
        [SerializeField] private int maxNeighborsPerAgent = 20;
        [SerializeField] private bool updateSpatialPartitioningEveryFrame = true;
        [SerializeField] private float partitionUpdateInterval = 0.1f;
        
        [Header("Debug & Monitoring")]
        [SerializeField] private bool showPerformanceStats = false;
        [SerializeField] private bool showSpatialPartitioning = false;
        [SerializeField] private bool logPerformanceWarnings = true;
        
        public enum SpatialPartitionType
        {
            UniformGrid,
            Octree,
            None
        }
        
        // Private fields
        private List<SwarmAgent> agents = new List<SwarmAgent>();
        private SpatialPartition spatialGrid;
        private SwarmOctree octree;
        private float lastPartitionUpdate;
        
        // Performance tracking
        private float lastPerformanceCheck;
        private float totalUpdateTime;
        private int updateFrameCount;
        private const float PERFORMANCE_CHECK_INTERVAL = 1f;
        
        // Properties
        public int AgentCount => agents.Count;
        public Bounds WorldBounds => new Bounds(transform.position, boundarySize);
        public float AverageNeighborCount { get; private set; }
        public float AverageUpdateTime { get; private set; }
        public Vector3 BoundarySize => boundarySize;
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                InitializeSpatialPartitioning();
            }
            else
            {
                Debug.LogWarning($"Multiple SwarmManagers detected! Destroying {name}");
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (spawnOnStart && agentPrefab != null)
            {
                SpawnAgents();
            }
        }
        
        private void Update()
        {
            float startTime = Time.realtimeSinceStartup;
            
            // Update spatial partitioning
            if (enableSpatialPartitioning)
            {
                if (updateSpatialPartitioningEveryFrame || 
                    Time.time - lastPartitionUpdate > partitionUpdateInterval)
                {
                    UpdateSpatialPartitioning();
                    lastPartitionUpdate = Time.time;
                }
            }
            
            // Performance monitoring
            if (Time.time - lastPerformanceCheck > PERFORMANCE_CHECK_INTERVAL)
            {
                UpdatePerformanceStats();
                lastPerformanceCheck = Time.time;
            }
            
            totalUpdateTime += Time.realtimeSinceStartup - startTime;
            updateFrameCount++;
        }
        
        private void InitializeSpatialPartitioning()
        {
            Bounds bounds = WorldBounds;
            
            switch (partitionType)
            {
                case SpatialPartitionType.UniformGrid:
                    spatialGrid = new SpatialPartition(bounds, cellSize);
                    break;
                case SpatialPartitionType.Octree:
                    octree = new SwarmOctree(bounds, maxAgentsPerOctreeNode, octreeMaxDepth);
                    break;
            }
        }
        
        private void UpdateSpatialPartitioning()
        {
            if (!enableSpatialPartitioning) return;
            
            switch (partitionType)
            {
                case SpatialPartitionType.UniformGrid:
                    if (spatialGrid != null)
                    {
                        spatialGrid.Clear();
                        foreach (var agent in agents)
                        {
                            if (agent != null)
                                spatialGrid.Add(agent);
                        }
                    }
                    break;
                    
                case SpatialPartitionType.Octree:
                    if (octree != null)
                    {
                        octree.Clear();
                        foreach (var agent in agents)
                        {
                            if (agent != null)
                                octree.Insert(agent);
                        }
                    }
                    break;
            }
        }
        
        public void SpawnAgents()
        {
            Vector3 spawnPosition = spawnCenter != null ? spawnCenter.position : transform.position;
            
            for (int i = 0; i < agentCount; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
                randomOffset.y = Mathf.Abs(randomOffset.y); // Keep above ground
                
                Vector3 position = spawnPosition + randomOffset;
                GameObject agentObject = Instantiate(agentPrefab, position, Random.rotation);
                agentObject.name = $"SwarmAgent_{i:000}";
                
                // Ensure it has a SwarmAgent component
                SwarmAgent agent = agentObject.GetComponent<SwarmAgent>();
                if (agent == null)
                {
                    Debug.LogError($"Agent prefab must have a SwarmAgent component! {agentObject.name}");
                    DestroyImmediate(agentObject);
                }
            }
            
            Debug.Log($"Spawned {agentCount} agents in swarm");
        }
        
        public void RegisterAgent(SwarmAgent agent)
        {
            if (!agents.Contains(agent))
            {
                agents.Add(agent);
                Debug.Log($"Registered agent: {agent.name}. Total agents: {agents.Count}");
            }
        }
        
        public void UnregisterAgent(SwarmAgent agent)
        {
            if (agents.Contains(agent))
            {
                agents.Remove(agent);
                Debug.Log($"Unregistered agent: {agent.name}. Total agents: {agents.Count}");
            }
        }
        
        public List<ISwarmAgent> GetNeighbors(ISwarmAgent agent, float radius)
        {
            if (!enableSpatialPartitioning || partitionType == SpatialPartitionType.None)
            {
                return GetNeighborsBruteForce(agent, radius);
            }
            
            List<ISwarmAgent> neighbors = new List<ISwarmAgent>();
            
            switch (partitionType)
            {
                case SpatialPartitionType.UniformGrid:
                    if (spatialGrid != null)
                    {
                        neighbors = spatialGrid.GetNeighborsExcluding(agent, radius);
                    }
                    break;
                    
                case SpatialPartitionType.Octree:
                    if (octree != null)
                    {
                        neighbors = octree.Query(agent.Position, radius);
                        neighbors.Remove(agent);
                    }
                    break;
            }
            
            // Limit number of neighbors for performance
            if (neighbors.Count > maxNeighborsPerAgent)
            {
                // Sort by distance and take closest
                neighbors = neighbors
                    .OrderBy(n => Vector3.Distance(agent.Position, n.Position))
                    .Take(maxNeighborsPerAgent)
                    .ToList();
            }
            
            return neighbors;
        }
        
        private List<ISwarmAgent> GetNeighborsBruteForce(ISwarmAgent agent, float radius)
        {
            List<ISwarmAgent> neighbors = new List<ISwarmAgent>();
            
            foreach (var otherAgent in agents)
            {
                if (otherAgent != agent && otherAgent != null)
                {
                    float distance = Vector3.Distance(agent.Position, otherAgent.Position);
                    if (distance <= radius)
                    {
                        neighbors.Add(otherAgent);
                        if (neighbors.Count >= maxNeighborsPerAgent)
                            break;
                    }
                }
            }
            
            return neighbors;
        }
        
        private void UpdatePerformanceStats()
        {
            if (updateFrameCount == 0) return;
            
            AverageUpdateTime = (totalUpdateTime / updateFrameCount) * 1000f; // Convert to milliseconds
            
            // Calculate average neighbor count
            float totalNeighbors = 0f;
            int activeAgents = 0;
            
            foreach (var agent in agents)
            {
                if (agent != null)
                {
                    totalNeighbors += agent.NeighborCount;
                    activeAgents++;
                }
            }
            
            AverageNeighborCount = activeAgents > 0 ? totalNeighbors / activeAgents : 0f;
            
            // Performance warnings
            if (logPerformanceWarnings)
            {
                if (AverageUpdateTime > 5f) // 5ms threshold
                {
                    Debug.LogWarning($"SwarmManager performance warning: Average update time {AverageUpdateTime:F2}ms");
                }
                
                if (agents.Count > 500 && !enableSpatialPartitioning)
                {
                    Debug.LogWarning("Consider enabling spatial partitioning for better performance with many agents");
                }
            }
            
            // Reset counters
            totalUpdateTime = 0f;
            updateFrameCount = 0;
        }
        
        public void ClearAllAgents()
        {
            foreach (var agent in agents.ToList())
            {
                if (agent != null)
                {
                    DestroyImmediate(agent.gameObject);
                }
            }
            agents.Clear();
        }
        
        public SwarmPerformanceStats GetPerformanceStats()
        {
            return new SwarmPerformanceStats
            {
                AgentCount = AgentCount,
                AverageNeighborCount = AverageNeighborCount,
                AverageUpdateTime = AverageUpdateTime,
                SpatialPartitionType = partitionType.ToString(),
                SpatialPartitioningEnabled = enableSpatialPartitioning,
                MemoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024) // MB
            };
        }
        
        private void OnDrawGizmos()
        {
            // Draw world boundaries
            if (showBoundaries)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(transform.position, boundarySize);
            }
            
            // Draw spatial partitioning visualization
            if (showSpatialPartitioning && enableSpatialPartitioning)
            {
                switch (partitionType)
                {
                    case SpatialPartitionType.UniformGrid:
                        spatialGrid?.DrawGizmos();
                        break;
                    case SpatialPartitionType.Octree:
                        octree?.DrawGizmos();
                        break;
                }
            }
            
            // Draw spawn area
            if (spawnOnStart)
            {
                Vector3 spawnPosition = spawnCenter != null ? spawnCenter.position : transform.position;
                Gizmos.color = new Color(0, 0, 1, 0.2f);
                Gizmos.DrawWireSphere(spawnPosition, spawnRadius);
            }
        }
        
        private void OnGUI()
        {
            if (!showPerformanceStats) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Swarm Performance Stats", EditorGUIUtility.boldLabel);
            GUILayout.Label($"Agents: {AgentCount}");
            GUILayout.Label($"Avg Neighbors: {AverageNeighborCount:F1}");
            GUILayout.Label($"Update Time: {AverageUpdateTime:F2}ms");
            GUILayout.Label($"Spatial Partitioning: {partitionType}");
            
            if (spatialGrid != null)
            {
                GUILayout.Label($"Grid Cells: {spatialGrid.GetCellCount()}");
                GUILayout.Label($"Load Factor: {spatialGrid.GetLoadFactor():F2}");
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void OnValidate()
        {
            agentCount = Mathf.Max(1, agentCount);
            spawnRadius = Mathf.Max(0.1f, spawnRadius);
            cellSize = Mathf.Max(0.1f, cellSize);
            maxAgentsPerOctreeNode = Mathf.Max(1, maxAgentsPerOctreeNode);
            octreeMaxDepth = Mathf.Clamp(octreeMaxDepth, 1, 10);
            maxNeighborsPerAgent = Mathf.Max(1, maxNeighborsPerAgent);
            partitionUpdateInterval = Mathf.Max(0.01f, partitionUpdateInterval);
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
    
    [System.Serializable]
    public struct SwarmPerformanceStats
    {
        public int AgentCount;
        public float AverageNeighborCount;
        public float AverageUpdateTime;
        public string SpatialPartitionType;
        public bool SpatialPartitioningEnabled;
        public long MemoryUsage;
    }
}