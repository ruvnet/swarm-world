using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Core;

namespace SwarmAI.Behaviors
{
    /// <summary>
    /// Base class for all swarm behaviors. Use ScriptableObjects for easy configuration.
    /// </summary>
    public abstract class SwarmBehavior : ScriptableObject
    {
        [Header("Behavior Settings")]
        [SerializeField] protected float weight = 1.0f;
        [SerializeField] protected bool enabled = true;
        
        public float Weight => weight;
        public bool Enabled => enabled;
        
        /// <summary>
        /// Calculate the steering force for this behavior
        /// </summary>
        public abstract Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors);
        
        /// <summary>
        /// Optional visualization for debugging
        /// </summary>
        public virtual void DrawGizmos(ISwarmAgent agent, List<ISwarmAgent> neighbors) { }
        
        /// <summary>
        /// Utility method for seeking a target position
        /// </summary>
        protected Vector3 Seek(Vector3 currentPos, Vector3 currentVel, Vector3 target, float maxSpeed, float maxForce)
        {
            Vector3 desired = (target - currentPos).normalized * maxSpeed;
            Vector3 steer = desired - currentVel;
            return Vector3.ClampMagnitude(steer, maxForce);
        }
        
        /// <summary>
        /// Utility method for fleeing from a position
        /// </summary>
        protected Vector3 Flee(Vector3 currentPos, Vector3 currentVel, Vector3 target, float maxSpeed, float maxForce)
        {
            Vector3 desired = (currentPos - target).normalized * maxSpeed;
            Vector3 steer = desired - currentVel;
            return Vector3.ClampMagnitude(steer, maxForce);
        }
    }
}