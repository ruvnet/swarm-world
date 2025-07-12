using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnitySwarmAI
{
    /// <summary>
    /// Central manager for coordinating swarm agents with spatial optimization
    /// </summary>
    [AddComponentMenu("Unity Swarm AI/Swarm Manager")]
    public class SwarmManager : MonoBehaviour
    {
        [Header("Spatial Partitioning")]
        [SerializeField] private bool useSpatialPartitioning = true;
        [SerializeField, Range(1f, 20f)] private float cellSize = 5f;
        [SerializeField] private Bounds worldBounds = new Bounds(Vector3.zero, Vector3.one * 100f);
        
        [Header("Performance")]
        [SerializeField, Range(1, 100)] private int maxAgentsPerFrame = 50;
        [SerializeField, Range(0.1f, 10f)] private float updateRate = 1f;
        [SerializeField] private bool enableLOD = true;
        
        [Header("Auto Spawning")]
        [SerializeField] private bool autoSpawn = false;
        [SerializeField] private GameObject agentPrefab;
        [SerializeField, Range(1, 1000)] private int targetAgentCount = 50;
        [SerializeField, Range(1f, 50f)] private float spawnRadius = 20f;
        
        // Core data
        private List<ISwarmAgent> allAgents = new List<ISwarmAgent>();
        private UniformSpatialGrid spatialGrid;
        private float nextUpdate;
        private int updateIndex;
        
        // Performance tracking
        private float averageFPS;
        private int activeAgentCount;
        private float lastFrameTime;
        
        // Singleton instance
        public static SwarmManager Instance { get; private set; }
        
        // Public properties
        public bool UseSpatialPartitioning => useSpatialPartitioning && spatialGrid != null;
        public int AgentCount => allAgents.Count;
        public int ActiveAgentCount => activeAgentCount;
        public float AverageFPS => averageFPS;
        public Bounds WorldBounds => worldBounds;
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Debug.LogWarning("Multiple SwarmManagers detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            
            // Initialize spatial partitioning
            if (useSpatialPartitioning)
            {
                spatialGrid = new UniformSpatialGrid(worldBounds, cellSize);
            }
        }
        
        private void Start()
        {
            if (autoSpawn && agentPrefab != null)
            {
                SpawnAgents(targetAgentCount);
            }
        }
        
        private void Update()
        {
            // Performance tracking
            TrackPerformance();
            
            // Update spatial partitioning
            if (Time.time >= nextUpdate)
            {
                UpdateSpatialPartitioning();
                nextUpdate = Time.time + (1f / updateRate);
            }
            
            // Update agents in batches
            if (enableLOD)
            {
                UpdateAgentsWithLOD();
            }
        }
        
        private void TrackPerformance()
        {
            float deltaTime = Time.unscaledDeltaTime;
            averageFPS = Mathf.Lerp(averageFPS, 1f / deltaTime, Time.deltaTime * 2f);
            activeAgentCount = allAgents.Count(a => a != null && a.IsActive);
            lastFrameTime = deltaTime;
        }
        
        private void UpdateSpatialPartitioning()
        {
            if (!useSpatialPartitioning || spatialGrid == null) return;
            
            spatialGrid.Clear();
            
            foreach (var agent in allAgents)
            {
                if (agent != null && agent.IsActive)
                {
                    spatialGrid.Insert(agent);
                }
            }
        }
        
        private void UpdateAgentsWithLOD()
        {
            // Batch update agents to spread work across frames
            int agentsPerFrame = Mathf.Min(maxAgentsPerFrame, allAgents.Count);
            int endIndex = Mathf.Min(updateIndex + agentsPerFrame, allAgents.Count);
            
            for (int i = updateIndex; i < endIndex; i++)
            {
                var agent = allAgents[i];
                if (agent != null && agent.IsActive)
                {
                    // LOD based on distance to camera
                    float distance = GetDistanceToCamera(agent.Position);
                    ApplyLOD(agent, distance);
                }
            }
            
            updateIndex = endIndex;
            if (updateIndex >= allAgents.Count)
            {
                updateIndex = 0;
            }
        }
        
        private float GetDistanceToCamera(Vector3 position)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                return Vector3.Distance(position, mainCamera.transform.position);
            }
            return 0f;
        }
        
        private void ApplyLOD(ISwarmAgent agent, float distance)
        {
            // Simple LOD system - can be extended
            if (distance > 50f)
            {
                // Distant agents - reduce update rate
                if (agent is SwarmAgent swarmAgent)
                {
                    // Reduce behavior update frequency for distant agents
                    // This would require additional API in SwarmAgent
                }
            }
        }
        
        // Agent management
        public void RegisterAgent(ISwarmAgent agent)
        {
            if (agent != null && !allAgents.Contains(agent))
            {
                allAgents.Add(agent);
            }
        }
        
        public void UnregisterAgent(ISwarmAgent agent)
        {
            if (agent != null)
            {
                allAgents.Remove(agent);
            }
        }
        
        public List<ISwarmAgent> GetNeighbors(Vector3 position, float radius)
        {
            if (useSpatialPartitioning && spatialGrid != null)
            {
                return spatialGrid.Query(position, radius);
            }
            else
            {
                // Fallback to brute force search
                var neighbors = new List<ISwarmAgent>();
                foreach (var agent in allAgents)
                {
                    if (agent != null && agent.IsActive)
                    {
                        float distance = Vector3.Distance(position, agent.Position);
                        if (distance <= radius)
                        {
                            neighbors.Add(agent);
                        }
                    }
                }
                return neighbors;
            }
        }
        
        public void SpawnAgents(int count)
        {
            if (agentPrefab == null)
            {
                Debug.LogError("No agent prefab assigned to SwarmManager");
                return;
            }
            
            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition = Random.insideUnitSphere * spawnRadius;
                spawnPosition += transform.position;
                
                GameObject agentGO = Instantiate(agentPrefab, spawnPosition, Random.rotation);
                agentGO.name = $"Agent_{i}";
            }
        }
        
        public void DestroyAllAgents()
        {
            var agentsToDestroy = new List<ISwarmAgent>(allAgents);
            foreach (var agent in agentsToDestroy)
            {
                if (agent != null && agent is MonoBehaviour mb)
                {
                    if (Application.isPlaying)
                        Destroy(mb.gameObject);
                    else
                        DestroyImmediate(mb.gameObject);
                }
            }
            allAgents.Clear();
        }
        
        // Performance and debugging
        public SwarmPerformanceData GetPerformanceData()
        {
            return new SwarmPerformanceData
            {
                TotalAgents = allAgents.Count,
                ActiveAgents = activeAgentCount,
                AverageFPS = averageFPS,
                LastFrameTime = lastFrameTime,
                UsingSpatialPartitioning = useSpatialPartitioning,
                SpatialCellSize = cellSize,
                UpdateRate = updateRate
            };
        }
        
        private void OnValidate()
        {
            cellSize = Mathf.Max(1f, cellSize);
            maxAgentsPerFrame = Mathf.Max(1, maxAgentsPerFrame);
            updateRate = Mathf.Max(0.1f, updateRate);
            targetAgentCount = Mathf.Max(1, targetAgentCount);
            spawnRadius = Mathf.Max(1f, spawnRadius);
        }
        
        private void OnDrawGizmos()
        {
            // Draw world bounds
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
            
            // Draw spawn radius
            if (autoSpawn)
            {
                Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
            }
            
            // Draw spatial grid (if enabled and in scene view)
            if (useSpatialPartitioning && spatialGrid != null)
            {
                spatialGrid.DrawGizmos();
            }
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
    public struct SwarmPerformanceData
    {
        public int TotalAgents;
        public int ActiveAgents;
        public float AverageFPS;
        public float LastFrameTime;
        public bool UsingSpatialPartitioning;
        public float SpatialCellSize;
        public float UpdateRate;
    }
}