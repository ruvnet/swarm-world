using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnitySwarmAI
{
    /// <summary>
    /// Main MonoBehaviour implementation of ISwarmAgent.
    /// High-performance swarm agent with configurable behaviors and spatial optimization.
    /// </summary>
    [AddComponentMenu("Unity Swarm AI/Swarm Agent")]
    public class SwarmAgent : MonoBehaviour, ISwarmAgent
    {
        [Header("Movement Settings")]
        [SerializeField, Range(0.1f, 50f)] private float maxSpeed = 10f;
        [SerializeField, Range(0.1f, 100f)] private float maxForce = 5f;
        [SerializeField, Range(0.1f, 10f)] private float mass = 1f;
        
        [Header("Perception")]
        [SerializeField, Range(0.5f, 20f)] private float perceptionRadius = 5f;
        [SerializeField] private LayerMask agentLayer = -1;
        
        [Header("Behaviors")]
        [SerializeField] private SwarmBehavior[] behaviors = new SwarmBehavior[0];
        [SerializeField, Range(0f, 5f)] private float behaviorUpdateRate = 1f;
        
        [Header("Boundaries")]
        [SerializeField] private bool enforceBoundaries = true;
        [SerializeField] private Bounds boundaries = new Bounds(Vector3.zero, Vector3.one * 100f);
        [SerializeField, Range(1f, 50f)] private float boundaryForce = 10f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;
        [SerializeField] private Color gizmoColor = Color.green;
        
        // Core properties
        private Vector3 velocity;
        private Vector3 acceleration;
        private List<ISwarmAgent> neighbors = new List<ISwarmAgent>();
        private List<ISwarmAgent> cachedNeighbors = new List<ISwarmAgent>();
        private float nextBehaviorUpdate;
        private int agentId;
        
        // Performance tracking
        private int neighborCount;
        private float lastUpdateTime;
        private static int nextId = 0;
        
        // ISwarmAgent Implementation
        public Vector3 Position => transform.position;
        public Vector3 Velocity => velocity;
        public Vector3 Forward => transform.forward;
        public float MaxSpeed => maxSpeed;
        public float MaxForce => maxForce;
        public float PerceptionRadius => perceptionRadius;
        public float Mass => mass;
        public int AgentId => agentId;
        public List<ISwarmAgent> Neighbors => neighbors;
        public bool IsActive { get; private set; } = true;
        
        private void Awake()
        {
            agentId = nextId++;
            velocity = Random.insideUnitSphere * maxSpeed * 0.5f;
            velocity.y = 0; // Keep movement horizontal by default
        }
        
        private void Start()
        {
            // Register with SwarmManager if available
            var manager = SwarmManager.Instance;
            if (manager != null)
            {
                manager.RegisterAgent(this);
            }
            
            // Initialize random velocity
            if (velocity.magnitude < 0.1f)
            {
                velocity = Random.insideUnitSphere * maxSpeed * 0.5f;
                velocity.y = 0;
            }
        }
        
        private void Update()
        {
            if (!IsActive) return;
            
            lastUpdateTime = Time.time;
            
            // Update behaviors at specified rate
            if (Time.time >= nextBehaviorUpdate)
            {
                UpdateBehaviors();
                nextBehaviorUpdate = Time.time + (1f / Mathf.Max(0.1f, behaviorUpdateRate));
            }
            
            // Apply physics
            UpdateMovement();
            
            // Enforce boundaries
            if (enforceBoundaries)
            {
                EnforceBoundaries();
            }
            
            // Update transform
            UpdateTransform();
        }
        
        private void UpdateBehaviors()
        {
            // Clear previous acceleration
            acceleration = Vector3.zero;
            
            // Update neighbors efficiently
            UpdateNeighbors();
            
            // Calculate steering forces from behaviors
            foreach (var behavior in behaviors)
            {
                if (behavior != null && behavior.IsEnabled)
                {
                    Vector3 force = behavior.CalculateForce(this, neighbors);
                    ApplyForce(force * behavior.Weight);
                }
            }
        }
        
        private void UpdateNeighbors()
        {
            neighbors.Clear();
            neighborCount = 0;
            
            // Use SwarmManager's spatial partitioning if available
            var manager = SwarmManager.Instance;
            if (manager != null && manager.UseSpatialPartitioning)
            {
                neighbors = manager.GetNeighbors(Position, perceptionRadius);
                neighbors.Remove(this); // Remove self
            }
            else
            {
                // Fallback to Physics.OverlapSphere
                Collider[] nearbyColliders = Physics.OverlapSphere(Position, perceptionRadius, agentLayer);
                
                foreach (var collider in nearbyColliders)
                {
                    if (collider.gameObject != gameObject)
                    {
                        var agent = collider.GetComponent<ISwarmAgent>();
                        if (agent != null && agent.IsActive)
                        {
                            neighbors.Add(agent);
                        }
                    }
                }
            }
            
            neighborCount = neighbors.Count;
        }
        
        private void UpdateMovement()
        {
            // Apply acceleration with mass
            velocity += acceleration * Time.deltaTime / mass;
            
            // Limit speed
            if (velocity.magnitude > maxSpeed)
            {
                velocity = velocity.normalized * maxSpeed;
            }
            
            // Minimum speed to prevent stopping
            if (velocity.magnitude < maxSpeed * 0.1f)
            {
                velocity = velocity.normalized * maxSpeed * 0.1f;
            }
        }
        
        private void EnforceBoundaries()
        {
            Vector3 center = boundaries.center;
            Vector3 extents = boundaries.extents;
            Vector3 pos = Position;
            
            Vector3 boundaryForceVector = Vector3.zero;
            
            // Check each axis
            if (pos.x < center.x - extents.x) boundaryForceVector.x = boundaryForce;
            if (pos.x > center.x + extents.x) boundaryForceVector.x = -boundaryForce;
            if (pos.y < center.y - extents.y) boundaryForceVector.y = boundaryForce;
            if (pos.y > center.y + extents.y) boundaryForceVector.y = -boundaryForce;
            if (pos.z < center.z - extents.z) boundaryForceVector.z = boundaryForce;
            if (pos.z > center.z + extents.z) boundaryForceVector.z = -boundaryForce;
            
            if (boundaryForceVector.magnitude > 0)
            {
                ApplyForce(boundaryForceVector);
            }
        }
        
        private void UpdateTransform()
        {
            // Update position
            transform.position += velocity * Time.deltaTime;
            
            // Update rotation to face movement direction
            if (velocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
        
        // ISwarmAgent Methods
        public void ApplyForce(Vector3 force)
        {
            // Limit force magnitude
            if (force.magnitude > maxForce)
            {
                force = force.normalized * maxForce;
            }
            
            acceleration += force;
        }
        
        public List<ISwarmAgent> GetNeighborsInRadius(float radius)
        {
            return neighbors.Where(n => Vector3.Distance(Position, n.Position) <= radius).ToList();
        }
        
        public void ReceiveMessage(ISwarmMessage message)
        {
            // Handle incoming messages
            switch (message.Type)
            {
                case "Danger":
                    // Increase flee behavior temporarily
                    break;
                case "Food":
                    // Move towards food source
                    break;
                default:
                    Debug.Log($"Agent {AgentId} received message: {message.Type}");
                    break;
            }
        }
        
        public Dictionary<string, float> GetBehaviorWeights()
        {
            var weights = new Dictionary<string, float>();
            foreach (var behavior in behaviors)
            {
                if (behavior != null)
                {
                    weights[behavior.GetType().Name] = behavior.Weight;
                }
            }
            return weights;
        }
        
        // Public utility methods
        public void SetBehaviors(SwarmBehavior[] newBehaviors)
        {
            behaviors = newBehaviors ?? new SwarmBehavior[0];
        }
        
        public void AddBehavior(SwarmBehavior behavior)
        {
            if (behavior != null)
            {
                var behaviorList = new List<SwarmBehavior>(behaviors) { behavior };
                behaviors = behaviorList.ToArray();
            }
        }
        
        public void RemoveBehavior(SwarmBehavior behavior)
        {
            if (behavior != null)
            {
                var behaviorList = new List<SwarmBehavior>(behaviors);
                behaviorList.Remove(behavior);
                behaviors = behaviorList.ToArray();
            }
        }
        
        public void SetActive(bool active)
        {
            IsActive = active;
        }
        
        // Performance and debugging
        public float GetPerformanceMetric()
        {
            return neighborCount / Mathf.Max(1f, Time.time - lastUpdateTime);
        }
        
        private void OnValidate()
        {
            // Ensure reasonable values
            maxSpeed = Mathf.Max(0.1f, maxSpeed);
            maxForce = Mathf.Max(0.1f, maxForce);
            mass = Mathf.Max(0.1f, mass);
            perceptionRadius = Mathf.Max(0.5f, perceptionRadius);
            behaviorUpdateRate = Mathf.Max(0.1f, behaviorUpdateRate);
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            
            // Perception radius
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, perceptionRadius);
            
            // Velocity vector
            Gizmos.color = gizmoColor;
            if (velocity.magnitude > 0.1f)
            {
                Gizmos.DrawRay(transform.position, velocity.normalized * 2f);
            }
            
            // Neighbor connections
            if (Application.isPlaying && neighbors != null)
            {
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.5f);
                foreach (var neighbor in neighbors)
                {
                    if (neighbor != null)
                    {
                        Gizmos.DrawLine(transform.position, neighbor.Position);
                    }
                }
            }
            
            // Boundaries
            if (enforceBoundaries)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                Gizmos.DrawWireCube(boundaries.center, boundaries.size);
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from SwarmManager
            var manager = SwarmManager.Instance;
            if (manager != null)
            {
                manager.UnregisterAgent(this);
            }
        }
    }
}