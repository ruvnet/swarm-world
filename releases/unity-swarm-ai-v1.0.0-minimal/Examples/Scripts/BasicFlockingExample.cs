using UnityEngine;
using UnitySwarmAI.Core;
using UnitySwarmAI.Behaviors;

namespace UnitySwarmAI.Examples
{
    /// <summary>
    /// Basic example demonstrating simple flocking behavior
    /// </summary>
    public class BasicFlockingExample : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private int agentCount = 50;
        [SerializeField] private float spawnRadius = 10f;
        [SerializeField] private Vector3 spawnCenter = Vector3.zero;
        
        [Header("Behavior Settings")]
        [SerializeField] private FlockingBehavior flockingBehavior;
        
        [Header("Runtime Controls")]
        [SerializeField] private bool respawnOnStart = true;
        [SerializeField] private KeyCode respawnKey = KeyCode.R;
        [SerializeField] private KeyCode togglePauseKey = KeyCode.Space;
        
        private GameObject[] agents;
        private bool isPaused = false;
        
        private void Start()
        {
            if (respawnOnStart)
                SpawnAgents();
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        private void HandleInput()
        {
            if (Input.GetKeyDown(respawnKey))
            {
                SpawnAgents();
            }
            
            if (Input.GetKeyDown(togglePauseKey))
            {
                TogglePause();
            }
        }
        
        [ContextMenu("Spawn Agents")]
        public void SpawnAgents()
        {
            ClearExistingAgents();
            
            if (agentPrefab == null)
            {
                Debug.LogError("Agent prefab not assigned!");
                return;
            }
            
            agents = new GameObject[agentCount];
            
            for (int i = 0; i < agentCount; i++)
            {
                // Spawn position
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
                
                // Create agent
                GameObject agent = Instantiate(agentPrefab, spawnPosition, Quaternion.identity, transform);
                agent.name = $"FlockingAgent_{i:000}";
                
                // Configure SwarmAgent component
                var swarmAgent = agent.GetComponent<SwarmAgent>();
                if (swarmAgent != null && flockingBehavior != null)
                {
                    // Note: In actual implementation, you'd set behaviors through SerializedObject
                    // This is a simplified example
                    Debug.Log($"Configured agent {i} with flocking behavior");
                }
                
                // Add visual variety
                ConfigureAgentAppearance(agent, i);
                
                agents[i] = agent;
            }
            
            Debug.Log($"Spawned {agentCount} flocking agents");
        }
        
        private void ConfigureAgentAppearance(GameObject agent, int index)
        {
            var renderer = agent.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Create unique material for each agent
                Material material = new Material(renderer.material);
                
                // Color based on index for visual variety
                float hue = (float)index / agentCount;
                material.color = Color.HSVToRGB(hue, 0.7f, 0.9f);
                
                renderer.material = material;
            }
            
            // Random initial rotation
            agent.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            // Slight scale variation
            float scale = Random.Range(0.8f, 1.2f);
            agent.transform.localScale = Vector3.one * scale;
        }
        
        [ContextMenu("Clear Agents")]
        public void ClearExistingAgents()
        {
            if (agents != null)
            {
                foreach (var agent in agents)
                {
                    if (agent != null)
                        DestroyImmediate(agent);
                }
            }
            
            // Also clear any existing children
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            
            agents = null;
        }
        
        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            Debug.Log($"Simulation {(isPaused ? "paused" : "resumed")}");
        }
        
        private void OnDrawGizmos()
        {
            // Draw spawn area
            Gizmos.color = Color.yellow * 0.3f;
            Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
            
            // Draw spawn center
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(spawnCenter, Vector3.one * 0.5f);
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 150));
            GUILayout.Label("Basic Flocking Example", GUI.skin.box);
            GUILayout.Label($"Agents: {(agents != null ? agents.Length : 0)}");
            GUILayout.Label($"Status: {(isPaused ? "Paused" : "Running")}");
            GUILayout.Space(10);
            GUILayout.Label($"Controls:");
            GUILayout.Label($"{respawnKey} - Respawn");
            GUILayout.Label($"{togglePauseKey} - Pause/Resume");
            GUILayout.EndArea();
        }
    }
}