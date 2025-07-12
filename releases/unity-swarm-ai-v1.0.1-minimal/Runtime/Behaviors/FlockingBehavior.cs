using UnityEngine;
using System.Collections.Generic;
using UnitySwarmAI.Core;

namespace UnitySwarmAI.Behaviors
{
    /// <summary>
    /// Classic flocking behavior implementing separation, alignment, and cohesion
    /// </summary>
    [CreateAssetMenu(fileName = "FlockingBehavior", menuName = "Unity Swarm AI/Behaviors/Flocking")]
    public class FlockingBehavior : SwarmBehavior
    {
        [Header("Flocking Parameters")]
        [SerializeField] private float separationWeight = 2f;
        [SerializeField] private float alignmentWeight = 1f;
        [SerializeField] private float cohesionWeight = 1f;
        
        [Header("Distances")]
        [SerializeField] private float separationRadius = 2f;
        [SerializeField] private float alignmentRadius = 5f;
        [SerializeField] private float cohesionRadius = 5f;
        
        public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (neighbors.Count == 0)
                return Vector3.zero;
            
            Vector3 separation = CalculateSeparation(agent, neighbors);
            Vector3 alignment = CalculateAlignment(agent, neighbors);
            Vector3 cohesion = CalculateCohesion(agent, neighbors);
            
            Vector3 totalForce = separation * separationWeight + 
                               alignment * alignmentWeight + 
                               cohesion * cohesionWeight;
            
            return LimitForce(totalForce, agent.MaxForce);
        }
        
        private Vector3 CalculateSeparation(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            Vector3 steering = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                if (distance > 0 && distance < separationRadius)
                {
                    Vector3 diff = agent.Position - neighbor.Position;
                    diff.Normalize();
                    diff /= distance; // Weight by distance
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
            }
            
            return steering;
        }
        
        private Vector3 CalculateAlignment(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                if (distance > 0 && distance < alignmentRadius)
                {
                    sum += neighbor.Velocity;
                    count++;
                }
            }
            
            if (count > 0)
            {
                sum /= count;
                sum.Normalize();
                sum *= agent.MaxSpeed;
                Vector3 steering = sum - agent.Velocity;
                return steering;
            }
            
            return Vector3.zero;
        }
        
        private Vector3 CalculateCohesion(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                if (distance > 0 && distance < cohesionRadius)
                {
                    sum += neighbor.Position;
                    count++;
                }
            }
            
            if (count > 0)
            {
                sum /= count;
                Vector3 desired = Seek(agent.Position, sum, agent.MaxSpeed);
                Vector3 steering = desired - agent.Velocity;
                return steering;
            }
            
            return Vector3.zero;
        }
        
        public override void DrawGizmos(ISwarmAgent agent, Vector3 force)
        {
            if (!showDebugGizmos) return;
            
            // Draw separation radius
            Gizmos.color = Color.red * 0.3f;
            Gizmos.DrawWireSphere(agent.Position, separationRadius);
            
            // Draw alignment radius
            Gizmos.color = Color.green * 0.3f;
            Gizmos.DrawWireSphere(agent.Position, alignmentRadius);
            
            // Draw cohesion radius
            Gizmos.color = Color.blue * 0.3f;
            Gizmos.DrawWireSphere(agent.Position, cohesionRadius);
            
            // Draw resulting force
            Gizmos.color = debugColor;
            Gizmos.DrawLine(agent.Position, agent.Position + force);
        }
    }
}