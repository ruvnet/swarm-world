using UnityEngine;
using System.Collections.Generic;
using UnitySwarmAI.Core;

namespace UnitySwarmAI.Behaviors
{
    /// <summary>
    /// Base ScriptableObject for all swarm behaviors. Provides a modular
    /// system for defining agent behaviors that can be mixed and matched.
    /// </summary>
    public abstract class SwarmBehavior : ScriptableObject
    {
        [Header("Behavior Settings")]
        [SerializeField] protected float weight = 1f;
        [SerializeField] protected bool enabled = true;
        [SerializeField] protected float minDistance = 0.5f;
        [SerializeField] protected float maxDistance = 5f;
        
        [Header("Debug")]
        [SerializeField] protected bool showDebugGizmos = false;
        [SerializeField] protected Color debugColor = Color.white;
        
        /// <summary>
        /// Weight multiplier for this behavior
        /// </summary>
        public float Weight => weight;
        
        /// <summary>
        /// Whether this behavior is currently enabled
        /// </summary>
        public bool Enabled => enabled;
        
        /// <summary>
        /// Calculate the steering force for this behavior
        /// </summary>
        /// <param name="agent">The agent requesting the force</param>
        /// <param name="neighbors">List of neighboring agents</param>
        /// <returns>Steering force vector</returns>
        public abstract Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors);
        
        /// <summary>
        /// Optional validation method called before force calculation
        /// </summary>
        /// <param name="agent">The agent to validate</param>
        /// <returns>True if behavior should execute</returns>
        public virtual bool ShouldExecute(ISwarmAgent agent)
        {
            return enabled;
        }
        
        /// <summary>
        /// Helper method to limit a steering force to maximum force
        /// </summary>
        /// <param name="force">The force to limit</param>
        /// <param name="maxForce">Maximum allowed force magnitude</param>
        /// <returns>Limited force vector</returns>
        protected Vector3 LimitForce(Vector3 force, float maxForce)
        {
            if (force.magnitude > maxForce)
                return force.normalized * maxForce;
            return force;
        }
        
        /// <summary>
        /// Calculate the desired velocity towards a target
        /// </summary>
        /// <param name="from">Starting position</param>
        /// <param name="to">Target position</param>
        /// <param name="maxSpeed">Maximum speed</param>
        /// <returns>Desired velocity vector</returns>
        protected Vector3 Seek(Vector3 from, Vector3 to, float maxSpeed)
        {
            Vector3 desired = (to - from).normalized * maxSpeed;
            return desired;
        }
        
        /// <summary>
        /// Calculate the desired velocity away from a target
        /// </summary>
        /// <param name="from">Starting position</param>
        /// <param name="away">Position to flee from</param>
        /// <param name="maxSpeed">Maximum speed</param>
        /// <returns>Desired velocity vector</returns>
        protected Vector3 Flee(Vector3 from, Vector3 away, float maxSpeed)
        {
            Vector3 desired = (from - away).normalized * maxSpeed;
            return desired;
        }
        
        /// <summary>
        /// Filter neighbors by distance
        /// </summary>
        /// <param name="agent">The central agent</param>
        /// <param name="neighbors">All neighbors</param>
        /// <param name="minDist">Minimum distance</param>
        /// <param name="maxDist">Maximum distance</param>
        /// <returns>Filtered neighbors list</returns>
        protected List<ISwarmAgent> FilterNeighborsByDistance(ISwarmAgent agent, List<ISwarmAgent> neighbors, float minDist, float maxDist)
        {
            var filtered = new List<ISwarmAgent>();
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                if (distance >= minDist && distance <= maxDist)
                    filtered.Add(neighbor);
            }
            return filtered;
        }
        
        /// <summary>
        /// Draw debug gizmos for this behavior
        /// </summary>
        /// <param name="agent">The agent to draw gizmos for</param>
        /// <param name="force">The calculated force</param>
        public virtual void DrawGizmos(ISwarmAgent agent, Vector3 force)
        {
            if (!showDebugGizmos) return;
            
            Gizmos.color = debugColor;
            Gizmos.DrawLine(agent.Position, agent.Position + force);
        }
    }
}