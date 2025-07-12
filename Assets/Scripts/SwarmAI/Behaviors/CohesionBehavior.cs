using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Core;

namespace SwarmAI.Behaviors
{
    /// <summary>
    /// Cohesion behavior - agents steer towards the average position of neighbors
    /// </summary>
    [CreateAssetMenu(fileName = "CohesionBehavior", menuName = "SwarmAI/Behaviors/Cohesion", order = 3)]
    public class CohesionBehavior : SwarmBehavior
    {
        [Header("Cohesion Settings")]
        [SerializeField] private float cohesionRadius = 8f;
        [SerializeField] private float arrivalRadius = 2f;
        [SerializeField] private bool useWeightedCenter = true;
        [SerializeField] private bool enableArrivalBehavior = true;
        [SerializeField] private AnimationCurve arrivalCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (neighbors.Count == 0)
                return Vector3.zero;
            
            Vector3 centerOfMass = Vector3.zero;
            float totalWeight = 0f;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                
                // Only consider neighbors within cohesion radius
                if (distance <= cohesionRadius)
                {
                    if (useWeightedCenter)
                    {
                        // Weight by inverse distance (closer neighbors have more influence)
                        float weight = 1f / Mathf.Max(distance, 0.1f);
                        centerOfMass += neighbor.Position * weight;
                        totalWeight += weight;
                    }
                    else
                    {
                        centerOfMass += neighbor.Position;
                    }
                    count++;
                }
            }
            
            if (count > 0)
            {
                // Calculate center of mass
                if (useWeightedCenter && totalWeight > 0)
                {
                    centerOfMass /= totalWeight;
                }
                else
                {
                    centerOfMass /= count;
                }
                
                // Use seek behavior to move towards center
                return SeekWithArrival(agent.Position, agent.Velocity, centerOfMass, agent.MaxSpeed, agent.MaxForce);
            }
            
            return Vector3.zero;
        }
        
        private Vector3 SeekWithArrival(Vector3 currentPos, Vector3 currentVel, Vector3 target, float maxSpeed, float maxForce)
        {
            Vector3 desired = target - currentPos;
            float distance = desired.magnitude;
            
            if (distance > 0)
            {
                desired = desired.normalized;
                
                // Implement arrival behavior - slow down as we approach
                if (enableArrivalBehavior && distance < arrivalRadius)
                {
                    float normalizedDistance = distance / arrivalRadius;
                    float speedMultiplier = arrivalCurve.Evaluate(normalizedDistance);
                    desired *= maxSpeed * speedMultiplier;
                }
                else
                {
                    desired *= maxSpeed;
                }
                
                Vector3 steer = desired - currentVel;
                return Vector3.ClampMagnitude(steer, maxForce);
            }
            
            return Vector3.zero;
        }
        
        public override void DrawGizmos(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (!enabled) return;
            
            // Draw cohesion radius
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(agent.Position, cohesionRadius);
            
            // Draw arrival radius
            if (enableArrivalBehavior)
            {
                Gizmos.color = new Color(0, 1, 1, 0.3f);
                Gizmos.DrawWireSphere(agent.Position, arrivalRadius);
            }
            
            // Calculate and draw center of mass
            if (neighbors.Count > 0)
            {
                Vector3 centerOfMass = Vector3.zero;
                float totalWeight = 0f;
                int count = 0;
                
                foreach (var neighbor in neighbors)
                {
                    float distance = Vector3.Distance(agent.Position, neighbor.Position);
                    if (distance <= cohesionRadius)
                    {
                        if (useWeightedCenter)
                        {
                            float weight = 1f / Mathf.Max(distance, 0.1f);
                            centerOfMass += neighbor.Position * weight;
                            totalWeight += weight;
                        }
                        else
                        {
                            centerOfMass += neighbor.Position;
                        }
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    if (useWeightedCenter && totalWeight > 0)
                        centerOfMass /= totalWeight;
                    else
                        centerOfMass /= count;
                    
                    // Draw center of mass and force vector
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(centerOfMass, 0.5f);
                    Gizmos.DrawLine(agent.Position, centerOfMass);
                }
            }
        }
        
        private void OnValidate()
        {
            cohesionRadius = Mathf.Max(0.1f, cohesionRadius);
            arrivalRadius = Mathf.Max(0.1f, arrivalRadius);
            arrivalRadius = Mathf.Min(arrivalRadius, cohesionRadius);
        }
    }
}