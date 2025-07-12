using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Core;

namespace SwarmAI.Behaviors
{
    /// <summary>
    /// Alignment behavior - agents steer towards the average heading of neighbors
    /// </summary>
    [CreateAssetMenu(fileName = "AlignmentBehavior", menuName = "SwarmAI/Behaviors/Alignment", order = 2)]
    public class AlignmentBehavior : SwarmBehavior
    {
        [Header("Alignment Settings")]
        [SerializeField] private float alignmentRadius = 5f;
        [SerializeField] private bool normalizeByDistance = false;
        [SerializeField] private float minVelocityThreshold = 0.1f;
        [SerializeField] private bool useWeightedAverage = true;
        
        public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (neighbors.Count == 0)
                return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            float totalWeight = 0f;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                
                // Only consider neighbors within alignment radius
                if (distance <= alignmentRadius && neighbor.Velocity.magnitude > minVelocityThreshold)
                {
                    Vector3 neighborVelocity = neighbor.Velocity;
                    
                    if (useWeightedAverage)
                    {
                        // Weight by inverse distance (closer neighbors have more influence)
                        float weight = normalizeByDistance ? (1f / Mathf.Max(distance, 0.1f)) : 1f;
                        sum += neighborVelocity * weight;
                        totalWeight += weight;
                    }
                    else
                    {
                        sum += neighborVelocity;
                    }
                    count++;
                }
            }
            
            if (count > 0)
            {
                Vector3 average;
                if (useWeightedAverage && totalWeight > 0)
                {
                    average = sum / totalWeight;
                }
                else
                {
                    average = sum / count;
                }
                
                // Normalize and scale to max speed
                average = average.normalized * agent.MaxSpeed;
                
                // Calculate steering force
                Vector3 steer = average - agent.Velocity;
                return Vector3.ClampMagnitude(steer, agent.MaxForce);
            }
            
            return Vector3.zero;
        }
        
        public override void DrawGizmos(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (!enabled) return;
            
            // Draw alignment radius
            Gizmos.color = new Color(0, 0, 1, 0.2f);
            Gizmos.DrawWireSphere(agent.Position, alignmentRadius);
            
            // Draw average velocity direction
            if (neighbors.Count > 0)
            {
                Vector3 averageVel = Vector3.zero;
                int count = 0;
                
                foreach (var neighbor in neighbors)
                {
                    float distance = Vector3.Distance(agent.Position, neighbor.Position);
                    if (distance <= alignmentRadius)
                    {
                        averageVel += neighbor.Velocity;
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    averageVel /= count;
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(agent.Position, averageVel.normalized * 3f);
                }
            }
        }
        
        private void OnValidate()
        {
            alignmentRadius = Mathf.Max(0.1f, alignmentRadius);
            minVelocityThreshold = Mathf.Max(0f, minVelocityThreshold);
        }
    }
}