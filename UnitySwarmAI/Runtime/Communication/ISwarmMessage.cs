using UnityEngine;

namespace UnitySwarmAI
{
    /// <summary>
    /// Interface for messages passed between swarm agents
    /// </summary>
    public interface ISwarmMessage
    {
        /// <summary>Type of message (e.g., "Danger", "Food", "Formation")</summary>
        string Type { get; }
        
        /// <summary>Message data/payload</summary>
        object Data { get; }
        
        /// <summary>Position where message originated</summary>
        Vector3 Origin { get; }
        
        /// <summary>Time when message was sent</summary>
        float Timestamp { get; }
        
        /// <summary>Priority level of the message</summary>
        int Priority { get; }
    }
    
    /// <summary>
    /// Basic implementation of ISwarmMessage
    /// </summary>
    [System.Serializable]
    public class SwarmMessage : ISwarmMessage
    {
        public string Type { get; private set; }
        public object Data { get; private set; }
        public Vector3 Origin { get; private set; }
        public float Timestamp { get; private set; }
        public int Priority { get; private set; }
        
        public SwarmMessage(string type, object data = null, Vector3 origin = default, int priority = 0)
        {
            Type = type;
            Data = data;
            Origin = origin;
            Timestamp = Time.time;
            Priority = priority;
        }
    }
}