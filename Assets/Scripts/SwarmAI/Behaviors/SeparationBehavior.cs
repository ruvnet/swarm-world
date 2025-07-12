using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Core;

namespace SwarmAI.Behaviors
{
    /// <summary>
    /// Separation behavior - agents avoid crowding neighbors
    /// </summary>
    [CreateAssetMenu(fileName = "SeparationBehavior", menuName = "SwarmAI/Behaviors/Separation", order = 1)]
    public class SeparationBehavior : SwarmBehavior
    {
        [Header("Separation Settings")]
        [SerializeField] private float separationRadius = 2f;
        [SerializeField] private AnimationCurve separationCurve = AnimationCurve.Linear(0, 1, 1, 0);
        [SerializeField] private bool useDistanceWeighting = true;
        
        public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (neighbors.Count == 0)
                return Vector3.zero;
            
            Vector3 steer = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                
                // Only consider neighbors within separation radius
                if (distance > 0 && distance < separationRadius)
                {
                    Vector3 diff = agent.Position - neighbor.Position;
                    
                    // Normalize and weight by distance
                    diff = diff.normalized;
                    if (useDistanceWeighting)
                    {
                        float normalizedDistance = distance / separationRadius;
                        float curveValue = separationCurve.Evaluate(normalizedDistance);
                        diff *= curveValue;
                    }
                    else
                    {
                        diff /= distance; // Simple inverse distance weighting
                    }
                    
                    steer += diff;
                    count++;
                }
            }
            
            if (count > 0)
            {
                steer /= count; // Average
                steer = steer.normalized * agent.MaxSpeed;
                steer -= agent.Velocity;
                steer = Vector3.ClampMagnitude(steer, agent.MaxForce);
            }
            
            return steer;
        }
        
        public override void DrawGizmos(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (!enabled) return;
            
            // Draw separation radius
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(agent.Position, separationRadius);
            
            // Draw separation forces to close neighbors
            Gizmos.color = Color.red;
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                if (distance > 0 && distance < separationRadius)
                {
                    Vector3 force = (agent.Position - neighbor.Position).normalized;
                    Gizmos.DrawRay(agent.Position, force * 2f);
                }
            }
        }
        
        private void OnValidate()
        {
            separationRadius = Mathf.Max(0.1f, separationRadius);
        }
    }
}