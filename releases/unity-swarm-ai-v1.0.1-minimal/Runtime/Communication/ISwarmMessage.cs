using UnityEngine;

namespace UnitySwarmAI.Communication
{
    /// <summary>
    /// Interface for messages passed between swarm agents
    /// </summary>
    public interface ISwarmMessage
    {
        /// <summary>
        /// Type identifier for the message
        /// </summary>
        string MessageType { get; }
        
        /// <summary>
        /// The agent that sent this message
        /// </summary>
        int SenderId { get; }
        
        /// <summary>
        /// Timestamp when the message was created
        /// </summary>
        float Timestamp { get; }
        
        /// <summary>
        /// Priority level of the message
        /// </summary>
        MessagePriority Priority { get; }
        
        /// <summary>
        /// The payload data of the message
        /// </summary>
        object Data { get; }
        
        /// <summary>
        /// Position where the message originated
        /// </summary>
        Vector3 Origin { get; }
        
        /// <summary>
        /// Maximum distance this message should travel
        /// </summary>
        float Range { get; }
        
        /// <summary>
        /// Check if this message is still valid (not expired)
        /// </summary>
        /// <param name="currentTime">Current timestamp</param>
        /// <returns>True if message is still valid</returns>
        bool IsValid(float currentTime);
    }
    
    /// <summary>
    /// Priority levels for swarm messages
    /// </summary>
    public enum MessagePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
    
    /// <summary>
    /// Base implementation of ISwarmMessage
    /// </summary>
    [System.Serializable]
    public class SwarmMessage : ISwarmMessage
    {
        [SerializeField] private string messageType;
        [SerializeField] private int senderId;
        [SerializeField] private float timestamp;
        [SerializeField] private MessagePriority priority;
        [SerializeField] private object data;
        [SerializeField] private Vector3 origin;
        [SerializeField] private float range;
        [SerializeField] private float timeToLive;
        
        public string MessageType => messageType;
        public int SenderId => senderId;
        public float Timestamp => timestamp;
        public MessagePriority Priority => priority;
        public object Data => data;
        public Vector3 Origin => origin;
        public float Range => range;
        
        public SwarmMessage(string type, int sender, object messageData, Vector3 messageOrigin, 
                           float messageRange = 10f, MessagePriority messagePriority = MessagePriority.Normal, 
                           float ttl = 5f)
        {
            messageType = type;
            senderId = sender;
            timestamp = Time.time;
            priority = messagePriority;
            data = messageData;
            origin = messageOrigin;
            range = messageRange;
            timeToLive = ttl;
        }
        
        public bool IsValid(float currentTime)
        {
            return (currentTime - timestamp) < timeToLive;
        }
    }
}