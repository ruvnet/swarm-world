using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI
{
    /// <summary>
    /// Core Agent class representing individual entities in a swarm
    /// </summary>
    public class Agent : MonoBehaviour
    {
        [Header("Agent Properties")]
        public float maxSpeed = 5f;
        public float maxForce = 3f;
        public float neighborRadius = 2f;
        public float separationRadius = 1f;
        
        [Header("Visual")]
        public Renderer agentRenderer;
        public TrailRenderer trail;
        
        [Header("Debug")]
        public bool showDebugGizmos = false;
        
        // Core properties
        public Vector3 velocity;
        public Vector3 acceleration;
        public Vector3 position;
        
        // Swarm management
        public SwarmManager swarmManager;
        public List<Agent> neighbors = new List<Agent>();
        public List<IBehavior> behaviors = new List<IBehavior>();
        
        // Agent state
        public AgentType agentType = AgentType.Basic;
        public float health = 100f;
        public float energy = 100f;
        public bool isAlive = true;
        
        // Events
        public System.Action<Agent> OnAgentDied;
        public System.Action<Agent, Agent> OnAgentCollision;
        
        void Start()
        {
            position = transform.position;
            velocity = Random.insideUnitSphere * maxSpeed * 0.1f;
            
            if (agentRenderer == null)
                agentRenderer = GetComponent<Renderer>();
                
            if (trail == null)
                trail = GetComponent<TrailRenderer>();
        }
        
        void Update()
        {
            if (!isAlive) return;
            
            UpdateNeighbors();
            ApplyBehaviors();
            UpdateMovement();
            UpdateVisuals();
        }
        
        void UpdateNeighbors()
        {
            neighbors.Clear();
            if (swarmManager != null)
            {
                foreach (Agent other in swarmManager.GetAgentsInRadius(this, neighborRadius))
                {
                    if (other != this)
                        neighbors.Add(other);
                }
            }
        }
        
        void ApplyBehaviors()
        {
            acceleration = Vector3.zero;
            
            foreach (IBehavior behavior in behaviors)
            {
                if (behavior.IsEnabled())
                {
                    Vector3 force = behavior.Calculate(this);
                    acceleration += force * behavior.GetWeight();
                }
            }
            
            acceleration = Vector3.ClampMagnitude(acceleration, maxForce);
        }
        
        void UpdateMovement()
        {
            velocity += acceleration * Time.deltaTime;
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            
            position += velocity * Time.deltaTime;
            transform.position = position;
            
            if (velocity.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(velocity.normalized);
            }
        }
        
        void UpdateVisuals()
        {
            // Update trail based on speed
            if (trail != null)
            {
                trail.enabled = velocity.magnitude > 0.1f;
                trail.time = Mathf.Lerp(0.5f, 2f, velocity.magnitude / maxSpeed);
            }
            
            // Update color based on agent state
            if (agentRenderer != null)
            {
                Color baseColor = GetAgentTypeColor();
                float healthPercent = health / 100f;
                agentRenderer.material.color = Color.Lerp(Color.red, baseColor, healthPercent);
            }
        }
        
        Color GetAgentTypeColor()
        {
            switch (agentType)
            {
                case AgentType.Predator: return Color.red;
                case AgentType.Prey: return Color.blue;
                case AgentType.Leader: return Color.yellow;
                case AgentType.Scout: return Color.green;
                case AgentType.Worker: return Color.cyan;
                default: return Color.white;
            }
        }
        
        public void AddBehavior(IBehavior behavior)
        {
            behaviors.Add(behavior);
        }
        
        public void RemoveBehavior(IBehavior behavior)
        {
            behaviors.Remove(behavior);
        }
        
        public void Die()
        {
            isAlive = false;
            OnAgentDied?.Invoke(this);
            
            if (gameObject != null)
                gameObject.SetActive(false);
        }
        
        void OnTriggerEnter(Collider other)
        {
            Agent otherAgent = other.GetComponent<Agent>();
            if (otherAgent != null)
            {
                OnAgentCollision?.Invoke(this, otherAgent);
            }
        }
        
        void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // Neighbor radius
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, neighborRadius);
            
            // Separation radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, separationRadius);
            
            // Velocity vector
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, velocity);
            
            // Acceleration vector
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, acceleration * 2f);
        }
    }
    
    public enum AgentType
    {
        Basic,
        Predator,
        Prey,
        Leader,
        Scout,
        Worker
    }
}