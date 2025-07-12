using UnityEngine;
using System.Collections.Generic;
using UnitySwarmAI.Behaviors;
using UnitySwarmAI.Communication;

namespace UnitySwarmAI.Core
{
    /// <summary>
    /// Base MonoBehaviour implementation of ISwarmAgent with configurable behaviors
    /// </summary>
    [System.Serializable]
    public class SwarmAgent : MonoBehaviour, ISwarmAgent
    {
        [Header("Agent Properties")]
        [SerializeField] private float perceptionRadius = 5f;
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float maxForce = 5f;
        [SerializeField] private float mass = 1f;
        
        [Header("Behaviors")]
        [SerializeField] private SwarmBehavior[] behaviors;
        [SerializeField] private float[] behaviorWeights;
        
        [Header("Debug")]
        [SerializeField] private bool showGizmos = false;
        [SerializeField] private Color gizmoColor = Color.white;
        
        // Runtime properties
        private Vector3 velocity;
        private Vector3 acceleration;
        private List<ISwarmAgent> neighbors = new List<ISwarmAgent>();
        private SwarmBehaviorState behaviorState = SwarmBehaviorState.Flocking;
        private int agentId;
        private ISwarmCoordinator coordinator;
        private SwarmMessageReceiver messageReceiver;
        
        // Properties
        public Vector3 Position => transform.position;
        public Vector3 Velocity => velocity;
        public float PerceptionRadius => perceptionRadius;
        public float MaxSpeed => maxSpeed;
        public float MaxForce => maxForce;
        public int AgentId => agentId;
        public SwarmBehaviorState BehaviorState 
        { 
            get => behaviorState; 
            set => behaviorState = value; 
        }
        
        private void Awake()
        {
            agentId = GetInstanceID();
            messageReceiver = GetComponent<SwarmMessageReceiver>();
            if (messageReceiver == null)
                messageReceiver = gameObject.AddComponent<SwarmMessageReceiver>();
                
            // Initialize behavior weights if not set
            if (behaviorWeights == null || behaviorWeights.Length != behaviors.Length)
            {
                behaviorWeights = new float[behaviors.Length];
                for (int i = 0; i < behaviorWeights.Length; i++)
                    behaviorWeights[i] = 1f;
            }
        }
        
        private void Start()
        {
            // Register with coordinator
            coordinator = FindObjectOfType<ISwarmCoordinator>() as MonoBehaviour;
            coordinator?.RegisterAgent(this);
        }
        
        private void Update()
        {
            UpdateAgent(Time.deltaTime);
        }
        
        public void UpdateAgent(float deltaTime)
        {
            // Get neighbors from coordinator for efficiency
            neighbors = coordinator?.GetNeighbors(this) ?? GetNeighborsDirectly();
            
            // Calculate steering forces
            Vector3 steering = CalculateSteering();
            
            // Apply physics
            ApplyForce(steering);
            UpdateMovement(deltaTime);
            
            // Update behavior state based on current conditions
            UpdateBehaviorState();
        }
        
        private Vector3 CalculateSteering()
        {
            Vector3 totalSteering = Vector3.zero;
            
            for (int i = 0; i < behaviors.Length; i++)
            {
                if (behaviors[i] != null)
                {
                    Vector3 force = behaviors[i].CalculateForce(this, neighbors);
                    totalSteering += force * behaviorWeights[i];
                }
            }
            
            return totalSteering;
        }
        
        public void ApplyForce(Vector3 force)
        {
            acceleration += force / mass;
        }
        
        private void UpdateMovement(float deltaTime)
        {
            // Update velocity
            velocity += acceleration * deltaTime;
            
            // Limit speed
            if (velocity.magnitude > maxSpeed)
                velocity = velocity.normalized * maxSpeed;
            
            // Update position
            transform.position += velocity * deltaTime;
            
            // Look in direction of movement
            if (velocity.magnitude > 0.1f)
                transform.forward = velocity.normalized;
            
            // Reset acceleration
            acceleration = Vector3.zero;
        }
        
        private void UpdateBehaviorState()
        {
            // Simple state determination based on velocity and neighbors
            if (velocity.magnitude < 0.1f)
                behaviorState = SwarmBehaviorState.Idle;
            else if (neighbors.Count > 0)
                behaviorState = SwarmBehaviorState.Flocking;
            else
                behaviorState = SwarmBehaviorState.Wandering;
        }
        
        public List<ISwarmAgent> GetNeighbors()
        {
            return neighbors;
        }
        
        private List<ISwarmAgent> GetNeighborsDirectly()
        {
            var result = new List<ISwarmAgent>();
            var colliders = Physics.OverlapSphere(Position, perceptionRadius);
            
            foreach (var collider in colliders)
            {
                var agent = collider.GetComponent<ISwarmAgent>();
                if (agent != null && agent != this)
                    result.Add(agent);
            }
            
            return result;
        }
        
        public void ReceiveMessage(ISwarmMessage message)
        {
            messageReceiver?.ProcessMessage(message);
        }
        
        private void OnDestroy()
        {
            coordinator?.UnregisterAgent(this);
        }
        
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // Draw perception radius
            Gizmos.color = gizmoColor * 0.3f;
            Gizmos.DrawWireSphere(transform.position, perceptionRadius);
            
            // Draw velocity
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(transform.position, transform.position + velocity);
            
            // Draw connections to neighbors
            if (neighbors != null)
            {
                Gizmos.color = Color.yellow * 0.5f;
                foreach (var neighbor in neighbors)
                {
                    Gizmos.DrawLine(transform.position, neighbor.Position);
                }
            }
        }
    }
}