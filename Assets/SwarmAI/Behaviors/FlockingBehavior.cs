using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI
{
    /// <summary>
    /// Classic flocking behavior implementing Craig Reynolds' boids algorithm
    /// </summary>
    [System.Serializable]
    public class FlockingBehavior : BaseBehavior
    {
        [Header("Flocking Weights")]
        public float separationWeight = 1.5f;
        public float alignmentWeight = 1.0f;
        public float cohesionWeight = 1.0f;
        
        [Header("Flocking Parameters")]
        public float separationRadius = 1f;
        public float alignmentRadius = 2f;
        public float cohesionRadius = 2f;
        
        public FlockingBehavior()
        {
            behaviorName = "Flocking";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            Vector3 separation = CalculateSeparation(agent) * separationWeight;
            Vector3 alignment = CalculateAlignment(agent) * alignmentWeight;
            Vector3 cohesion = CalculateCohesion(agent) * cohesionWeight;
            
            return separation + alignment + cohesion;
        }
        
        Vector3 CalculateSeparation(Agent agent)
        {
            Vector3 steer = Vector3.zero;
            int count = 0;
            
            foreach (Agent neighbor in agent.neighbors)
            {
                float distance = Vector3.Distance(agent.position, neighbor.position);
                
                if (distance > 0 && distance < separationRadius)
                {
                    Vector3 diff = agent.position - neighbor.position;
                    diff.Normalize();
                    diff /= distance; // Weight by distance
                    steer += diff;
                    count++;
                }
            }
            
            if (count > 0)
            {
                steer /= count;
                steer.Normalize();
                steer *= agent.maxSpeed;
                steer -= agent.velocity;
                steer = Vector3.ClampMagnitude(steer, agent.maxForce);
            }
            
            return steer;
        }
        
        Vector3 CalculateAlignment(Agent agent)
        {
            Vector3 averageVelocity = Vector3.zero;
            int count = 0;
            
            foreach (Agent neighbor in agent.neighbors)
            {
                float distance = Vector3.Distance(agent.position, neighbor.position);
                
                if (distance > 0 && distance < alignmentRadius)
                {
                    averageVelocity += neighbor.velocity;
                    count++;
                }
            }
            
            if (count > 0)
            {
                averageVelocity /= count;
                averageVelocity.Normalize();
                averageVelocity *= agent.maxSpeed;
                
                Vector3 steer = averageVelocity - agent.velocity;
                steer = Vector3.ClampMagnitude(steer, agent.maxForce);
                return steer;
            }
            
            return Vector3.zero;
        }
        
        Vector3 CalculateCohesion(Agent agent)
        {
            Vector3 centerOfMass = Vector3.zero;
            int count = 0;
            
            foreach (Agent neighbor in agent.neighbors)
            {
                float distance = Vector3.Distance(agent.position, neighbor.position);
                
                if (distance > 0 && distance < cohesionRadius)
                {
                    centerOfMass += neighbor.position;
                    count++;
                }
            }
            
            if (count > 0)
            {
                centerOfMass /= count;
                return Seek(agent, centerOfMass);
            }
            
            return Vector3.zero;
        }
        
        Vector3 Seek(Agent agent, Vector3 target)
        {
            Vector3 desired = target - agent.position;
            desired.Normalize();
            desired *= agent.maxSpeed;
            
            Vector3 steer = desired - agent.velocity;
            steer = Vector3.ClampMagnitude(steer, agent.maxForce);
            return steer;
        }
    }
}