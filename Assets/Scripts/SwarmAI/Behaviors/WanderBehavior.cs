using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Core;

namespace SwarmAI.Behaviors
{
    /// <summary>
    /// Wander behavior - agents move in random directions with smooth changes
    /// </summary>
    [CreateAssetMenu(fileName = "WanderBehavior", menuName = "SwarmAI/Behaviors/Wander", order = 7)]
    public class WanderBehavior : SwarmBehavior
    {
        [Header("Wander Settings")]
        [SerializeField] private float wanderRadius = 2f;
        [SerializeField] private float wanderDistance = 3f;
        [SerializeField] private float wanderJitter = 1f;
        [SerializeField] private bool constrainToXZ = true;
        
        // Per-agent wander targets (stored by instance ID)
        private static Dictionary<int, Vector3> wanderTargets = new Dictionary<int, Vector3>();
        
        public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            int agentID = agent.GetHashCode();
            
            // Initialize wander target if not exists
            if (!wanderTargets.ContainsKey(agentID))
            {
                wanderTargets[agentID] = Random.insideUnitSphere * wanderRadius;
                if (constrainToXZ)
                    wanderTargets[agentID] = new Vector3(wanderTargets[agentID].x, 0, wanderTargets[agentID].z);
            }
            
            Vector3 wanderTarget = wanderTargets[agentID];
            
            // Add random jitter to the wander target
            Vector3 jitter = Random.insideUnitSphere * wanderJitter * Time.deltaTime;
            if (constrainToXZ)
                jitter = new Vector3(jitter.x, 0, jitter.z);
            
            wanderTarget += jitter;
            
            // Normalize and scale to wander radius
            wanderTarget = wanderTarget.normalized * wanderRadius;
            
            // Project the target in front of the agent
            Vector3 velocity = agent.Velocity;
            if (velocity.magnitude < 0.1f)
            {
                velocity = Random.insideUnitSphere;
                if (constrainToXZ)
                    velocity = new Vector3(velocity.x, 0, velocity.z);
            }
            
            Vector3 forward = velocity.normalized;
            Vector3 circleCenter = agent.Position + forward * wanderDistance;
            Vector3 targetPosition = circleCenter + wanderTarget;
            
            // Store the updated wander target
            wanderTargets[agentID] = wanderTarget;
            
            // Seek towards the wander target
            return Seek(agent.Position, agent.Velocity, targetPosition, agent.MaxSpeed, agent.MaxForce);
        }
        
        public override void DrawGizmos(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (!enabled) return;
            
            int agentID = agent.GetHashCode();
            Vector3 velocity = agent.Velocity;
            
            if (velocity.magnitude < 0.1f) return;
            
            Vector3 forward = velocity.normalized;
            Vector3 circleCenter = agent.Position + forward * wanderDistance;
            
            // Draw wander circle
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(circleCenter, wanderRadius);
            
            // Draw wander target if exists
            if (wanderTargets.ContainsKey(agentID))
            {
                Vector3 wanderTarget = wanderTargets[agentID];
                Vector3 targetPosition = circleCenter + wanderTarget;
                
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(targetPosition, 0.2f);
                Gizmos.DrawLine(agent.Position, targetPosition);
            }
            
            // Draw forward direction
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(agent.Position, forward * wanderDistance);
        }
        
        // Clean up wander targets for destroyed agents
        public static void CleanupWanderTarget(int agentID)
        {
            if (wanderTargets.ContainsKey(agentID))
            {
                wanderTargets.Remove(agentID);
            }
        }
        
        // Clear all wander targets (useful for reset scenarios)
        public static void ClearAllWanderTargets()
        {
            wanderTargets.Clear();
        }
        
        private void OnValidate()
        {
            wanderRadius = Mathf.Max(0.1f, wanderRadius);
            wanderDistance = Mathf.Max(0.1f, wanderDistance);
            wanderJitter = Mathf.Max(0.1f, wanderJitter);
        }
    }
}