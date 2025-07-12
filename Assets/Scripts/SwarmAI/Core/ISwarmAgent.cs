using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI.Core
{
    /// <summary>
    /// Core interface defining swarm agent capabilities
    /// </summary>
    public interface ISwarmAgent
    {
        Vector3 Position { get; }
        Vector3 Velocity { get; }
        float PerceptionRadius { get; }
        float MaxSpeed { get; }
        float MaxForce { get; }
        
        void ApplyForce(Vector3 force);
        List<ISwarmAgent> GetNeighbors();
        void RegisterNeighbor(ISwarmAgent neighbor);
        void ClearNeighbors();
    }
}