using UnityEngine;
using SwarmAI.Core;
using SwarmAI.Behaviors;

namespace SwarmAI.Utils
{
    /// <summary>
    /// Comprehensive debugging and visualization tools for swarm agents
    /// </summary>
    [RequireComponent(typeof(SwarmAgent))]
    public class SwarmDebugger : MonoBehaviour
    {
        [Header("Visualization Options")]
        [SerializeField] private bool showPerceptionRadius = true;
        [SerializeField] private bool showVelocity = true;
        [SerializeField] private bool showAcceleration = false;
        [SerializeField] private bool showNeighborConnections = true;
        [SerializeField] private bool showForceVectors = false;
        [SerializeField] private bool showBehaviorInfo = false;
        
        [Header("Visual Settings")]
        [SerializeField] private Color perceptionColor = new Color(0, 1, 0, 0.1f);
        [SerializeField] private Color velocityColor = Color.blue;
        [SerializeField] private Color accelerationColor = Color.red;
        [SerializeField] private Color connectionColor = new Color(1, 1, 0, 0.3f);
        [SerializeField] private Color forceColor = Color.magenta;
        
        [Header("Performance Monitoring")]
        [SerializeField] private bool showPerformanceInfo = false;
        [SerializeField] private bool logPerformanceWarnings = false;
        [SerializeField] private float performanceWarningThreshold = 5f; // milliseconds
        
        private SwarmAgent agent;
        private Vector3 lastPosition;
        private float lastUpdateTime;
        
        private void Start()
        {
            agent = GetComponent<SwarmAgent>();
            lastPosition = transform.position;
        }
        
        private void Update()
        {
            if (logPerformanceWarnings)
            {
                MonitorPerformance();
            }
        }
        
        private void MonitorPerformance()
        {
            float updateTime = agent.GetAverageUpdateTime();
            if (updateTime > performanceWarningThreshold)
            {
                Debug.LogWarning($"Agent {name} performance issue: {updateTime:F2}ms update time", this);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (agent == null) return;
            
            // Perception radius
            if (showPerceptionRadius)
            {
                Gizmos.color = perceptionColor;
                Gizmos.DrawWireSphere(transform.position, agent.PerceptionRadius);
            }
            
            // Velocity vector
            if (showVelocity && agent.Velocity.magnitude > 0.1f)
            {
                Gizmos.color = velocityColor;
                Gizmos.DrawRay(transform.position, agent.Velocity.normalized * 2f);
                Gizmos.DrawWireSphere(transform.position + agent.Velocity.normalized * 2f, 0.1f);
            }
            
            // Acceleration vector
            if (showAcceleration && agent.Acceleration.magnitude > 0.1f)
            {
                Gizmos.color = accelerationColor;
                Gizmos.DrawRay(transform.position, agent.Acceleration.normalized * 1.5f);
            }
            
            // Neighbor connections
            if (showNeighborConnections)
            {
                Gizmos.color = connectionColor;
                var neighbors = agent.GetNeighbors();
                foreach (var neighbor in neighbors)
                {
                    if (neighbor != null)
                        Gizmos.DrawLine(transform.position, neighbor.Position);
                }
            }
            
            // Force vectors from behaviors
            if (showForceVectors)
            {
                DrawBehaviorForces();
            }
        }
        
        private void DrawBehaviorForces()
        {
            var neighbors = agent.GetNeighbors();
            Vector3 offset = Vector3.zero;
            
            foreach (var behavior in agent.Behaviors)
            {
                if (behavior != null && behavior.Enabled)
                {
                    Vector3 force = behavior.CalculateForce(agent, neighbors);
                    if (force.magnitude > 0.1f)
                    {
                        Gizmos.color = GetBehaviorColor(behavior);
                        Vector3 startPos = transform.position + offset;
                        Gizmos.DrawRay(startPos, force.normalized * 1f);
                        Gizmos.DrawWireCube(startPos + force.normalized * 1f, Vector3.one * 0.1f);
                        
                        offset += Vector3.up * 0.2f; // Stack force vectors vertically
                    }
                }
            }
        }
        
        private Color GetBehaviorColor(SwarmBehavior behavior)
        {
            switch (behavior.GetType().Name)
            {
                case nameof(SeparationBehavior): return Color.red;
                case nameof(AlignmentBehavior): return Color.blue;
                case nameof(CohesionBehavior): return Color.green;
                case nameof(ObstacleAvoidanceBehavior): return Color.orange;
                default: return Color.white;
            }
        }
        
        private void OnGUI()
        {
            if (!showBehaviorInfo && !showPerformanceInfo) return;
            
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
            if (screenPos.z > 0 && Vector3.Distance(Camera.main.transform.position, transform.position) < 20f)
            {
                Rect rect = new Rect(screenPos.x - 100, Screen.height - screenPos.y - 50, 200, 100);
                GUILayout.BeginArea(rect);
                GUILayout.BeginVertical("box");
                
                GUILayout.Label($"Agent: {name}", EditorGUIUtility.boldLabel);
                
                if (showBehaviorInfo)
                {
                    GUILayout.Label($"Neighbors: {agent.GetNeighbors().Count}");
                    GUILayout.Label($"Speed: {agent.Velocity.magnitude:F1}/{agent.MaxSpeed:F1}");
                    
                    foreach (var behavior in agent.Behaviors)
                    {
                        if (behavior != null)
                        {
                            string status = behavior.Enabled ? "ON" : "OFF";
                            GUILayout.Label($"{behavior.GetType().Name}: {status} (W:{behavior.Weight:F1})");
                        }
                    }
                }
                
                if (showPerformanceInfo)
                {
                    GUILayout.Label($"Update Time: {agent.GetAverageUpdateTime():F2}ms");
                    float distance = Vector3.Distance(transform.position, lastPosition);
                    GUILayout.Label($"Distance Moved: {distance:F2}");
                }
                
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }
        
        private void LateUpdate()
        {
            lastPosition = transform.position;
        }
    }
}