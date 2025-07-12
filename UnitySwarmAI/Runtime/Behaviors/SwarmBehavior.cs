using UnityEngine;
using System.Collections.Generic;

namespace UnitySwarmAI
{
    /// <summary>
    /// Base class for all swarm behaviors. Inherit from this to create custom behaviors.
    /// ScriptableObject-based for easy configuration and sharing.
    /// </summary>
    public abstract class SwarmBehavior : ScriptableObject
    {
        [Header("Behavior Settings")]
        [SerializeField, Range(0f, 10f)] protected float weight = 1f;
        [SerializeField] protected bool enabled = true;
        
        [Header("Debug")]
        [SerializeField] protected bool showDebugInfo = false;
        
        /// <summary>Weight multiplier for this behavior</summary>
        public float Weight => weight;
        
        /// <summary>Whether this behavior is currently enabled</summary>
        public bool IsEnabled => enabled;
        
        /// <summary>Whether to show debug information for this behavior</summary>
        public bool ShowDebugInfo => showDebugInfo;
        
        /// <summary>
        /// Calculate the steering force for this behavior
        /// </summary>
        /// <param name="agent">The agent this behavior is applied to</param>
        /// <param name="neighbors">List of neighboring agents</param>
        /// <returns>Steering force vector</returns>
        public abstract Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors);
        
        /// <summary>
        /// Get debug information about this behavior
        /// </summary>
        /// <param name="agent">The agent to get debug info for</param>
        /// <returns>Debug information string</returns>
        public virtual string GetDebugInfo(ISwarmAgent agent)
        {
            return $"{GetType().Name}: Weight={weight:F2}, Enabled={enabled}";
        }
        
        /// <summary>
        /// Validate behavior parameters
        /// </summary>
        protected virtual void OnValidate()
        {
            weight = Mathf.Max(0f, weight);
        }
        
        /// <summary>
        /// Utility method to calculate steering force towards a target
        /// </summary>
        /// <param name="agent">The agent</param>
        /// <param name="target">Target position</param>
        /// <param name="slowingRadius">Distance at which to start slowing down</param>
        /// <returns>Steering force</returns>
        protected Vector3 Seek(ISwarmAgent agent, Vector3 target, float slowingRadius = 0f)
        {
            Vector3 desired = target - agent.Position;
            float distance = desired.magnitude;
            
            if (distance < 0.001f) return Vector3.zero;
            
            desired.Normalize();
            
            // Slow down if within slowing radius
            if (slowingRadius > 0f && distance < slowingRadius)
            {
                desired *= agent.MaxSpeed * (distance / slowingRadius);
            }
            else
            {
                desired *= agent.MaxSpeed;
            }
            
            Vector3 steering = desired - agent.Velocity;
            return Vector3.ClampMagnitude(steering, agent.MaxForce);
        }
        
        /// <summary>
        /// Utility method to calculate steering force away from a target
        /// </summary>
        /// <param name="agent">The agent</param>
        /// <param name="target">Target position to flee from</param>
        /// <param name="panicDistance">Distance at which panic begins</param>
        /// <returns>Steering force</returns>
        protected Vector3 Flee(ISwarmAgent agent, Vector3 target, float panicDistance = 10f)
        {
            Vector3 desired = agent.Position - target;
            float distance = desired.magnitude;
            
            if (distance > panicDistance || distance < 0.001f) return Vector3.zero;
            
            desired.Normalize();
            desired *= agent.MaxSpeed;
            
            Vector3 steering = desired - agent.Velocity;
            
            // Stronger force when closer
            float panicMultiplier = 1f - (distance / panicDistance);
            steering *= panicMultiplier;
            
            return Vector3.ClampMagnitude(steering, agent.MaxForce);
        }
        
        /// <summary>
        /// Utility method to limit steering force
        /// </summary>
        /// <param name="force">Original force</param>
        /// <param name="maxForce">Maximum allowed force</param>
        /// <returns>Limited force</returns>
        protected Vector3 LimitForce(Vector3 force, float maxForce)
        {
            return Vector3.ClampMagnitude(force, maxForce);
        }
        
        /// <summary>
        /// Utility method to get neighbors within a specific distance
        /// </summary>
        /// <param name="agent">The agent</param>
        /// <param name="neighbors">Full neighbor list</param>
        /// <param name="maxDistance">Maximum distance</param>
        /// <returns>Filtered neighbor list</returns>
        protected List<ISwarmAgent> GetNeighborsInRange(ISwarmAgent agent, List<ISwarmAgent> neighbors, float maxDistance)
        {
            var result = new List<ISwarmAgent>();
            foreach (var neighbor in neighbors)
            {
                if (Vector3.Distance(agent.Position, neighbor.Position) <= maxDistance)
                {
                    result.Add(neighbor);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Utility method to calculate center of mass for a group of agents
        /// </summary>
        /// <param name="agents">List of agents</param>
        /// <returns>Center of mass position</returns>
        protected Vector3 GetCenterOfMass(List<ISwarmAgent> agents)
        {
            if (agents.Count == 0) return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            foreach (var agent in agents)
            {
                sum += agent.Position;
            }
            return sum / agents.Count;
        }
        
        /// <summary>
        /// Utility method to calculate average velocity for a group of agents
        /// </summary>
        /// <param name="agents">List of agents</param>
        /// <returns>Average velocity</returns>
        protected Vector3 GetAverageVelocity(List<ISwarmAgent> agents)
        {
            if (agents.Count == 0) return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            foreach (var agent in agents)
            {
                sum += agent.Velocity;
            }
            return sum / agents.Count;
        }
    }
}