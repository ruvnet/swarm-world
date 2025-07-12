using UnityEngine;
using System.Collections.Generic;

namespace UnitySwarmAI
{
    /// <summary>
    /// Core interface for all swarm agents. Provides essential properties and methods
    /// for agent coordination and behavior.
    /// </summary>
    public interface ISwarmAgent
    {
        /// <summary>Current world position of the agent</summary>
        Vector3 Position { get; }
        
        /// <summary>Current velocity vector of the agent</summary>
        Vector3 Velocity { get; }
        
        /// <summary>Current forward direction of the agent</summary>
        Vector3 Forward { get; }
        
        /// <summary>Maximum movement speed of the agent</summary>
        float MaxSpeed { get; }
        
        /// <summary>Maximum steering force that can be applied</summary>
        float MaxForce { get; }
        
        /// <summary>Radius within which the agent can perceive neighbors</summary>
        float PerceptionRadius { get; }
        
        /// <summary>Mass of the agent for physics calculations</summary>
        float Mass { get; }
        
        /// <summary>Unique identifier for the agent</summary>
        int AgentId { get; }
        
        /// <summary>Current neighbors within perception radius</summary>
        List<ISwarmAgent> Neighbors { get; }
        
        /// <summary>Whether the agent is currently active and processing</summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Apply a steering force to the agent
        /// </summary>
        /// <param name="force">Force vector to apply</param>
        void ApplyForce(Vector3 force);
        
        /// <summary>
        /// Get neighbors within a specific radius
        /// </summary>
        /// <param name="radius">Search radius</param>
        /// <returns>List of neighbors within radius</returns>
        List<ISwarmAgent> GetNeighborsInRadius(float radius);
        
        /// <summary>
        /// Send a message to this agent
        /// </summary>
        /// <param name="message">Message to receive</param>
        void ReceiveMessage(ISwarmMessage message);
        
        /// <summary>
        /// Get the agent's current behavior priorities
        /// </summary>
        /// <returns>Dictionary of behavior names and weights</returns>
        Dictionary<string, float> GetBehaviorWeights();
    }
}