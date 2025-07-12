using UnityEngine;
using System.Collections.Generic;

namespace UnitySwarmAI
{
    /// <summary>
    /// Classic boids flocking behavior implementing separation, alignment, and cohesion.
    /// Based on Craig Reynolds' original boids algorithm.
    /// </summary>
    [CreateAssetMenu(fileName = "FlockingBehavior", menuName = "Unity Swarm AI/Behaviors/Flocking")]
    public class FlockingBehavior : SwarmBehavior
    {
        [Header("Flocking Weights")]
        [SerializeField, Range(0f, 5f)] private float separationWeight = 1.5f;
        [SerializeField, Range(0f, 5f)] private float alignmentWeight = 1f;
        [SerializeField, Range(0f, 5f)] private float cohesionWeight = 1f;
        
        [Header("Flocking Distances")]
        [SerializeField, Range(0.5f, 10f)] private float separationDistance = 2f;
        [SerializeField, Range(1f, 20f)] private float alignmentDistance = 5f;
        [SerializeField, Range(1f, 20f)] private float cohesionDistance = 5f;
        
        [Header("Advanced Settings")]
        [SerializeField, Range(0f, 2f)] private float separationExponent = 1f;
        [SerializeField] private bool useWeightedAlignment = true;
        [SerializeField] private bool useWeightedCohesion = true;
        
        public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (neighbors.Count == 0) return Vector3.zero;
            
            Vector3 separation = CalculateSeparation(agent, neighbors) * separationWeight;
            Vector3 alignment = CalculateAlignment(agent, neighbors) * alignmentWeight;
            Vector3 cohesion = CalculateCohesion(agent, neighbors) * cohesionWeight;
            
            Vector3 totalForce = separation + alignment + cohesion;
            
            if (showDebugInfo)
            {
                Debug.DrawRay(agent.Position, separation * 2f, Color.red, 0.1f);
                Debug.DrawRay(agent.Position, alignment * 2f, Color.blue, 0.1f);
                Debug.DrawRay(agent.Position, cohesion * 2f, Color.green, 0.1f);
                Debug.DrawRay(agent.Position, totalForce * 2f, Color.white, 0.1f);
            }
            
            return totalForce;
        }
        
        /// <summary>
        /// Calculate separation force - avoid crowding neighbors
        /// </summary>
        private Vector3 CalculateSeparation(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            Vector3 steering = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                
                if (distance > 0f && distance < separationDistance)
                {
                    Vector3 diff = agent.Position - neighbor.Position;
                    diff.Normalize();
                    
                    // Weight by distance - closer neighbors have stronger influence
                    float influence = 1f / Mathf.Pow(distance, separationExponent + 1f);
                    diff *= influence;
                    
                    steering += diff;
                    count++;
                }
            }
            
            if (count > 0)
            {
                steering /= count;
                steering.Normalize();
                steering *= agent.MaxSpeed;
                steering -= agent.Velocity;
                return LimitForce(steering, agent.MaxForce);
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Calculate alignment force - steer towards average heading of neighbors
        /// </summary>
        private Vector3 CalculateAlignment(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                
                if (distance > 0f && distance < alignmentDistance)
                {
                    if (useWeightedAlignment)
                    {
                        // Weight by distance - closer neighbors have stronger influence
                        float weight = 1f - (distance / alignmentDistance);
                        sum += neighbor.Velocity * weight;
                    }
                    else
                    {
                        sum += neighbor.Velocity;
                    }
                    count++;
                }
            }
            
            if (count > 0)
            {
                sum /= count;
                sum.Normalize();
                sum *= agent.MaxSpeed;
                
                Vector3 steering = sum - agent.Velocity;
                return LimitForce(steering, agent.MaxForce);
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Calculate cohesion force - steer towards average position of neighbors
        /// </summary>
        private Vector3 CalculateCohesion(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                
                if (distance > 0f && distance < cohesionDistance)
                {
                    if (useWeightedCohesion)
                    {
                        // Weight by distance - closer neighbors have stronger influence
                        float weight = 1f - (distance / cohesionDistance);
                        sum += neighbor.Position * weight;
                    }
                    else
                    {
                        sum += neighbor.Position;
                    }
                    count++;
                }
            }
            
            if (count > 0)
            {
                sum /= count;
                return Seek(agent, sum);
            }
            
            return Vector3.zero;
        }
        
        public override string GetDebugInfo(ISwarmAgent agent)
        {
            return $"Flocking: Sep={separationWeight:F1}, Align={alignmentWeight:F1}, Coh={cohesionWeight:F1}";
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            separationWeight = Mathf.Max(0f, separationWeight);
            alignmentWeight = Mathf.Max(0f, alignmentWeight);
            cohesionWeight = Mathf.Max(0f, cohesionWeight);
            
            separationDistance = Mathf.Max(0.5f, separationDistance);
            alignmentDistance = Mathf.Max(1f, alignmentDistance);
            cohesionDistance = Mathf.Max(1f, cohesionDistance);
            
            separationExponent = Mathf.Max(0f, separationExponent);
        }
    }
}