using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI
{
    /// <summary>
    /// Central manager for coordinating swarm behavior and performance optimization
    /// </summary>
    public class SwarmManager : MonoBehaviour
    {
        [Header("Swarm Configuration")]
        public GameObject agentPrefab;
        public int initialAgentCount = 50;
        public Vector3 spawnArea = new Vector3(10, 5, 10);
        public Transform boundaryCenter;
        public Vector3 boundarySize = new Vector3(20, 10, 20);
        
        [Header("Performance")]
        public bool useSpatialHashing = true;
        public float spatialHashCellSize = 2f;
        public int maxAgentsPerFrame = 100;
        public bool enableLOD = true;
        public float lodDistance = 50f;
        
        [Header("Debugging")]
        public bool showBoundaries = true;
        public bool showPerformanceStats = true;
        
        // Agent management
        public List<Agent> agents = new List<Agent>();
        private Queue<Agent> agentPool = new Queue<Agent>();
        private SpatialHash spatialHash;
        
        // Performance tracking
        private int frameCounter = 0;
        private float lastFPSUpdate = 0f;
        private float currentFPS = 0f;
        
        // Events
        public System.Action<Agent> OnAgentSpawned;
        public System.Action<Agent> OnAgentDestroyed;
        public System.Action<int> OnAgentCountChanged;
        
        void Start()
        {
            InitializeSwarm();
        }
        
        void InitializeSwarm()
        {
            if (boundaryCenter == null)
                boundaryCenter = transform;
                
            if (useSpatialHashing)
                spatialHash = new SpatialHash(spatialHashCellSize);
                
            SpawnInitialAgents();
        }
        
        void SpawnInitialAgents()
        {
            for (int i = 0; i < initialAgentCount; i++)
            {
                SpawnAgent();
            }
        }
        
        public Agent SpawnAgent(Vector3? position = null, AgentType agentType = AgentType.Basic)
        {
            GameObject agentObj;
            
            // Use object pooling
            if (agentPool.Count > 0)
            {
                Agent pooledAgent = agentPool.Dequeue();
                agentObj = pooledAgent.gameObject;
                agentObj.SetActive(true);
            }
            else
            {
                agentObj = Instantiate(agentPrefab);
            }
            
            Agent agent = agentObj.GetComponent<Agent>();
            if (agent == null)
            {
                agent = agentObj.AddComponent<Agent>();
            }
            
            // Set position
            if (position.HasValue)
            {
                agent.transform.position = position.Value;
            }
            else
            {
                Vector3 spawnPos = boundaryCenter.position + new Vector3(
                    Random.Range(-spawnArea.x/2, spawnArea.x/2),
                    Random.Range(-spawnArea.y/2, spawnArea.y/2),
                    Random.Range(-spawnArea.z/2, spawnArea.z/2)
                );
                agent.transform.position = spawnPos;
            }
            
            // Configure agent
            agent.swarmManager = this;
            agent.agentType = agentType;
            agent.OnAgentDied += OnAgentDied;
            
            agents.Add(agent);
            OnAgentSpawned?.Invoke(agent);
            OnAgentCountChanged?.Invoke(agents.Count);
            
            return agent;
        }
        
        void OnAgentDied(Agent agent)
        {
            agents.Remove(agent);
            agentPool.Enqueue(agent);
            OnAgentDestroyed?.Invoke(agent);
            OnAgentCountChanged?.Invoke(agents.Count);
        }
        
        void Update()
        {
            UpdateSpatialHash();
            UpdatePerformanceStats();
            EnforceBoundaries();
            
            if (enableLOD)
                UpdateLOD();
        }
        
        void UpdateSpatialHash()
        {
            if (!useSpatialHashing || spatialHash == null) return;
            
            spatialHash.Clear();
            
            int agentsToProcess = Mathf.Min(maxAgentsPerFrame, agents.Count);
            int startIndex = frameCounter % agents.Count;
            
            for (int i = 0; i < agentsToProcess; i++)
            {
                int index = (startIndex + i) % agents.Count;
                if (index < agents.Count && agents[index].isAlive)
                {
                    spatialHash.Insert(agents[index]);
                }
            }
            
            frameCounter++;
        }
        
        void UpdatePerformanceStats()
        {
            if (!showPerformanceStats) return;
            
            if (Time.time - lastFPSUpdate > 0.5f)
            {
                currentFPS = 1f / Time.deltaTime;
                lastFPSUpdate = Time.time;
            }
        }
        
        void UpdateLOD()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            foreach (Agent agent in agents)
            {
                if (!agent.isAlive) continue;
                
                float distance = Vector3.Distance(agent.transform.position, mainCamera.transform.position);
                
                // Adjust agent update rate based on distance
                if (distance > lodDistance)
                {
                    // Reduce update frequency for distant agents
                    if (frameCounter % 3 != 0)
                        agent.enabled = false;
                    else
                        agent.enabled = true;
                }
                else
                {
                    agent.enabled = true;
                }
            }
        }
        
        void EnforceBoundaries()
        {
            Vector3 center = boundaryCenter.position;
            Vector3 halfSize = boundarySize * 0.5f;
            
            foreach (Agent agent in agents)
            {
                if (!agent.isAlive) continue;
                
                Vector3 pos = agent.transform.position;
                Vector3 localPos = pos - center;
                
                // Wrap around boundaries
                if (Mathf.Abs(localPos.x) > halfSize.x)
                    pos.x = center.x - Mathf.Sign(localPos.x) * halfSize.x;
                if (Mathf.Abs(localPos.y) > halfSize.y)
                    pos.y = center.y - Mathf.Sign(localPos.y) * halfSize.y;
                if (Mathf.Abs(localPos.z) > halfSize.z)
                    pos.z = center.z - Mathf.Sign(localPos.z) * halfSize.z;
                
                agent.transform.position = pos;
                agent.position = pos;
            }
        }
        
        public List<Agent> GetAgentsInRadius(Agent center, float radius)
        {
            if (useSpatialHashing && spatialHash != null)
            {
                return spatialHash.GetAgentsInRadius(center.position, radius);
            }
            else
            {
                // Fallback to brute force
                return agents.Where(agent => 
                    agent.isAlive && 
                    Vector3.Distance(agent.position, center.position) <= radius
                ).ToList();
            }
        }
        
        public void AddAgent(Agent agent)
        {
            if (!agents.Contains(agent))
            {
                agents.Add(agent);
                agent.swarmManager = this;
                OnAgentSpawned?.Invoke(agent);
                OnAgentCountChanged?.Invoke(agents.Count);
            }
        }
        
        public void RemoveAgent(Agent agent)
        {
            if (agents.Remove(agent))
            {
                OnAgentDestroyed?.Invoke(agent);
                OnAgentCountChanged?.Invoke(agents.Count);
            }
        }
        
        public void ClearSwarm()
        {
            foreach (Agent agent in agents.ToList())
            {
                if (agent != null)
                    DestroyImmediate(agent.gameObject);
            }
            agents.Clear();
            agentPool.Clear();
        }
        
        void OnDrawGizmos()
        {
            if (!showBoundaries || boundaryCenter == null) return;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(boundaryCenter.position, boundarySize);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(boundaryCenter.position, spawnArea);
        }
        
        void OnGUI()
        {
            if (!showPerformanceStats) return;
            
            GUI.Box(new Rect(10, 10, 200, 100), "");
            GUI.Label(new Rect(15, 15, 190, 20), $"Agents: {agents.Count}");
            GUI.Label(new Rect(15, 35, 190, 20), $"FPS: {currentFPS:F1}");
            GUI.Label(new Rect(15, 55, 190, 20), $"Spatial Hash: {useSpatialHashing}");
            GUI.Label(new Rect(15, 75, 190, 20), $"LOD: {enableLOD}");
        }
    }
    
    /// <summary>
    /// Spatial hashing for efficient neighbor finding
    /// </summary>
    public class SpatialHash
    {
        private Dictionary<Vector3Int, List<Agent>> grid;
        private float cellSize;
        
        public SpatialHash(float cellSize)
        {
            this.cellSize = cellSize;
            this.grid = new Dictionary<Vector3Int, List<Agent>>();
        }
        
        public void Clear()
        {
            foreach (var cell in grid.Values)
            {
                cell.Clear();
            }
        }
        
        public void Insert(Agent agent)
        {
            Vector3Int cell = GetCell(agent.position);
            
            if (!grid.ContainsKey(cell))
                grid[cell] = new List<Agent>();
                
            grid[cell].Add(agent);
        }
        
        public List<Agent> GetAgentsInRadius(Vector3 position, float radius)
        {
            List<Agent> result = new List<Agent>();
            Vector3Int centerCell = GetCell(position);
            
            int cellRadius = Mathf.CeilToInt(radius / cellSize);
            
            for (int x = -cellRadius; x <= cellRadius; x++)
            {
                for (int y = -cellRadius; y <= cellRadius; y++)
                {
                    for (int z = -cellRadius; z <= cellRadius; z++)
                    {
                        Vector3Int cell = centerCell + new Vector3Int(x, y, z);
                        
                        if (grid.ContainsKey(cell))
                        {
                            foreach (Agent agent in grid[cell])
                            {
                                if (Vector3.Distance(agent.position, position) <= radius)
                                {
                                    result.Add(agent);
                                }
                            }
                        }
                    }
                }
            }
            
            return result;
        }
        
        private Vector3Int GetCell(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize),
                Mathf.FloorToInt(position.z / cellSize)
            );
        }
    }
}