using UnityEngine;
using System.Collections.Generic;
using System;
using UnitySwarmAI.Core;

namespace UnitySwarmAI.Communication
{
    /// <summary>
    /// Component for receiving and processing swarm messages
    /// </summary>
    public class SwarmMessageReceiver : MonoBehaviour
    {
        [Header("Message Processing")]
        [SerializeField] private int maxMessagesPerFrame = 10;
        [SerializeField] private bool logReceivedMessages = false;
        
        private Dictionary<string, List<Action<ISwarmMessage>>> messageHandlers;
        private Queue<ISwarmMessage> messageQueue;
        private ISwarmAgent agent;
        
        private void Awake()
        {
            messageHandlers = new Dictionary<string, List<Action<ISwarmMessage>>>();
            messageQueue = new Queue<ISwarmMessage>();
            agent = GetComponent<ISwarmAgent>();
        }
        
        private void Update()
        {
            ProcessMessages();
        }
        
        /// <summary>
        /// Register a handler for a specific message type
        /// </summary>
        /// <param name="messageType">Type of message to handle</param>
        /// <param name="handler">Handler function</param>
        public void RegisterMessageHandler(string messageType, Action<ISwarmMessage> handler)
        {
            if (!messageHandlers.ContainsKey(messageType))
                messageHandlers[messageType] = new List<Action<ISwarmMessage>>();
            
            messageHandlers[messageType].Add(handler);
        }
        
        /// <summary>
        /// Unregister a handler for a specific message type
        /// </summary>
        /// <param name="messageType">Type of message</param>
        /// <param name="handler">Handler function to remove</param>
        public void UnregisterMessageHandler(string messageType, Action<ISwarmMessage> handler)
        {
            if (messageHandlers.ContainsKey(messageType))
            {
                messageHandlers[messageType].Remove(handler);
                if (messageHandlers[messageType].Count == 0)
                    messageHandlers.Remove(messageType);
            }
        }
        
        /// <summary>
        /// Process an incoming message
        /// </summary>
        /// <param name="message">The message to process</param>
        public void ProcessMessage(ISwarmMessage message)
        {
            if (message == null || !message.IsValid(Time.time))
                return;
            
            // Check if message is in range
            if (agent != null)
            {
                float distance = Vector3.Distance(agent.Position, message.Origin);
                if (distance > message.Range)
                    return;
            }
            
            // Add to queue for processing
            messageQueue.Enqueue(message);
            
            if (logReceivedMessages)
                Debug.Log($"Agent {agent?.AgentId} received message: {message.MessageType}");
        }
        
        private void ProcessMessages()
        {
            int processedCount = 0;
            
            while (messageQueue.Count > 0 && processedCount < maxMessagesPerFrame)
            {
                var message = messageQueue.Dequeue();
                
                if (messageHandlers.ContainsKey(message.MessageType))
                {
                    foreach (var handler in messageHandlers[message.MessageType])
                    {
                        try
                        {
                            handler.Invoke(message);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error processing message {message.MessageType}: {e.Message}");
                        }
                    }
                }
                
                processedCount++;
            }
        }
        
        /// <summary>
        /// Clear all pending messages
        /// </summary>
        public void ClearMessages()
        {
            messageQueue.Clear();
        }
        
        /// <summary>
        /// Get the number of pending messages
        /// </summary>
        /// <returns>Number of messages in queue</returns>
        public int GetPendingMessageCount()
        {
            return messageQueue.Count;
        }
    }
}