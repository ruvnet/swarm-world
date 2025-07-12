using UnityEngine;
using System.Collections.Generic;

namespace UnitySwarmAI.Core
{
    /// <summary>
    /// Core interface for all swarm agents, defining the basic contract
    /// for position, velocity, and force application.
    /// </summary>
    public interface ISwarmAgent
    {
        /// <summary>
        /// Current world position of the agent
        /// </summary>
        Vector3 Position { get; }
        
        /// <summary>
        /// Current velocity vector of the agent
        /// </summary>
        Vector3 Velocity { get; }
        
        /// <summary>
        /// Radius within which this agent can perceive other agents
        /// </summary>
        float PerceptionRadius { get; }
        
        /// <summary>
        /// Maximum speed the agent can achieve
        /// </summary>
        float MaxSpeed { get; }
        
        /// <summary>
        /// Maximum force that can be applied to the agent
        /// </summary>
        float MaxForce { get; }
        
        /// <summary>
        /// Unique identifier for this agent
        /// </summary>
        int AgentId { get; }
        
        /// <summary>
        /// Current behavior state of the agent
        /// </summary>
        SwarmBehaviorState BehaviorState { get; set; }
        
        /// <summary>
        /// Apply a force to this agent's movement
        /// </summary>
        /// <param name="force">Force vector to apply</param>
        void ApplyForce(Vector3 force);
        
        /// <summary>
        /// Get all neighbors within perception radius
        /// </summary>
        /// <returns>List of neighboring agents</returns>
        List<ISwarmAgent> GetNeighbors();
        
        /// <summary>
        /// Receive a message from another agent or system
        /// </summary>
        /// <param name="message">The message to process</param>
        void ReceiveMessage(ISwarmMessage message);
        
        /// <summary>
        /// Update the agent's behavior and position
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        void UpdateAgent(float deltaTime);
    }
    
    /// <summary>
    /// Enumeration of possible behavior states for swarm agents
    /// </summary>
    public enum SwarmBehaviorState
    {
        Idle,
        Flocking,
        Seeking,
        Fleeing,
        Wandering,
        Foraging,
        Returning,
        Custom
    }
}