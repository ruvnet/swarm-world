using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Behaviors;

namespace SwarmAI.Core
{
    /// <summary>
    /// Core SwarmAgent MonoBehaviour with configurable behaviors
    /// Follows Unity best practices and provides Inspector-friendly configuration
    /// </summary>
    public class SwarmAgent : MonoBehaviour, ISwarmAgent
    {
        [Header("Movement Settings")]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float maxForce = 10f;
        [SerializeField] private float mass = 1f;
        
        [Header("Perception Settings")]
        [SerializeField] private float perceptionRadius = 5f;
        [SerializeField] private LayerMask agentLayer = -1;
        [SerializeField] private LayerMask obstacleLayer = -1;
        
        [Header("Behaviors")]
        [SerializeField] private SwarmBehavior[] behaviors = new SwarmBehavior[0];
        
        [Header("Boundary Settings")]
        [SerializeField] private bool enforceBoundaries = true;
        [SerializeField] private Bounds boundaries = new Bounds(Vector3.zero, Vector3.one * 50f);
        [SerializeField] private float boundaryForce = 15f;
        
        [Header("Performance")]
        [SerializeField] private bool useOptimizedNeighborSearch = true;
        [SerializeField] private int maxNeighbors = 20;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showPerceptionRadius = false;
        [SerializeField] private bool showVelocity = false;
        [SerializeField] private bool showNeighborConnections = false;
        
        // Private fields
        private Vector3 velocity;
        private Vector3 acceleration;
        private List<ISwarmAgent> neighbors = new List<ISwarmAgent>();
        private SwarmManager swarmManager;
        private float lastNeighborUpdate;
        private const float NEIGHBOR_UPDATE_INTERVAL = 0.1f; // Update neighbors 10 times per second
        
        // Performance tracking
        private int framesSinceLastUpdate = 0;
        private float totalUpdateTime = 0f;
        
        // Properties implementing ISwarmAgent
        public Vector3 Position => transform.position;
        public Vector3 Velocity => velocity;
        public float PerceptionRadius => perceptionRadius;
        public float MaxSpeed => maxSpeed;
        public float MaxForce => maxForce;
        
        // Additional properties
        public Vector3 Acceleration => acceleration;
        public float Mass => mass;
        public int NeighborCount => neighbors.Count;
        public SwarmBehavior[] Behaviors => behaviors;
        
        private void Start()
        {
            // Initialize with random velocity
            velocity = Random.insideUnitSphere * maxSpeed;
            velocity.y = 0f; // Keep movement on horizontal plane initially
            
            // Register with SwarmManager
            swarmManager = SwarmManager.Instance;
            if (swarmManager != null)
            {
                swarmManager.RegisterAgent(this);
            }
            else
            {
                Debug.LogWarning($"SwarmAgent {name}: No SwarmManager found in scene!");
            }
            
            // Validate behaviors
            ValidateBehaviors();
        }
        
        private void Update()
        {
            float startTime = Time.realtimeSinceStartup;
            
            // Update neighbors periodically for performance
            if (Time.time - lastNeighborUpdate > NEIGHBOR_UPDATE_INTERVAL)
            {
                UpdateNeighbors();
                lastNeighborUpdate = Time.time;
            }
            
            // Calculate steering forces
            acceleration = CalculateSteering();
            
            // Apply boundary enforcement
            if (enforceBoundaries)
            {
                acceleration += EnforceBoundaries();
            }
            
            // Apply physics
            velocity += acceleration * Time.deltaTime;
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            
            // Move the agent
            transform.position += velocity * Time.deltaTime;
            
            // Face movement direction
            if (velocity.magnitude > 0.1f)
            {
                transform.forward = velocity.normalized;
            }
            
            // Reset acceleration for next frame
            acceleration = Vector3.zero;
            
            // Performance tracking
            totalUpdateTime += Time.realtimeSinceStartup - startTime;
            framesSinceLastUpdate++;
        }
        
        private void ValidateBehaviors()
        {
            for (int i = 0; i < behaviors.Length; i++)
            {
                if (behaviors[i] == null)
                {
                    Debug.LogWarning($"SwarmAgent {name}: Behavior at index {i} is null!");
                }
            }
        }
        
        private void UpdateNeighbors()
        {
            neighbors.Clear();
            
            if (useOptimizedNeighborSearch && swarmManager != null)
            {
                // Use SwarmManager's spatial partitioning
                neighbors.AddRange(swarmManager.GetNeighbors(this, perceptionRadius));
            }
            else
            {
                // Fallback to Physics.OverlapSphere
                Collider[] nearbyColliders = Physics.OverlapSphere(
                    transform.position, 
                    perceptionRadius, 
                    agentLayer);
                
                foreach (var collider in nearbyColliders)
                {
                    if (collider.gameObject != gameObject)
                    {
                        ISwarmAgent agent = collider.GetComponent<ISwarmAgent>();
                        if (agent != null)
                        {
                            neighbors.Add(agent);
                            if (neighbors.Count >= maxNeighbors)
                                break;
                        }
                    }
                }
            }
        }
        
        private Vector3 CalculateSteering()
        {
            Vector3 totalForce = Vector3.zero;
            
            foreach (var behavior in behaviors)
            {
                if (behavior != null && behavior.Enabled)
                {
                    Vector3 force = behavior.CalculateForce(this, neighbors);
                    totalForce += force * behavior.Weight;
                }
            }
            
            return Vector3.ClampMagnitude(totalForce, maxForce);
        }
        
        private Vector3 EnforceBoundaries()
        {
            Vector3 force = Vector3.zero;
            Vector3 center = boundaries.center;
            Vector3 size = boundaries.size;
            
            // Check each axis
            if (transform.position.x < center.x - size.x / 2)
                force.x += boundaryForce;
            else if (transform.position.x > center.x + size.x / 2)
                force.x -= boundaryForce;
                
            if (transform.position.y < center.y - size.y / 2)
                force.y += boundaryForce;
            else if (transform.position.y > center.y + size.y / 2)
                force.y -= boundaryForce;
                
            if (transform.position.z < center.z - size.z / 2)
                force.z += boundaryForce;
            else if (transform.position.z > center.z + size.z / 2)
                force.z -= boundaryForce;
            
            return force;
        }
        
        // ISwarmAgent implementation
        public void ApplyForce(Vector3 force)
        {
            acceleration += force / mass;
        }
        
        public List<ISwarmAgent> GetNeighbors()
        {
            return new List<ISwarmAgent>(neighbors);
        }
        
        public void RegisterNeighbor(ISwarmAgent neighbor)
        {
            if (!neighbors.Contains(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }
        
        public void ClearNeighbors()
        {
            neighbors.Clear();
        }
        
        // Performance monitoring
        public float GetAverageUpdateTime()
        {
            if (framesSinceLastUpdate == 0) return 0f;
            return (totalUpdateTime / framesSinceLastUpdate) * 1000f; // Convert to milliseconds
        }
        
        public void ResetPerformanceStats()
        {
            framesSinceLastUpdate = 0;
            totalUpdateTime = 0f;
        }
        
        // Gizmos for debugging
        private void OnDrawGizmos()
        {
            if (!showDebugInfo) return;
            
            // Perception radius
            if (showPerceptionRadius)
            {
                Gizmos.color = new Color(0, 1, 0, 0.1f);
                Gizmos.DrawWireSphere(transform.position, perceptionRadius);
            }
            
            // Velocity vector
            if (showVelocity && velocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, velocity.normalized * 2f);
            }
            
            // Neighbor connections
            if (showNeighborConnections)
            {
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                foreach (var neighbor in neighbors)
                {
                    if (neighbor != null)
                        Gizmos.DrawLine(transform.position, neighbor.Position);
                }
            }
            
            // Boundaries
            if (enforceBoundaries)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(boundaries.center, boundaries.size);
            }
            
            // Draw behavior-specific gizmos
            foreach (var behavior in behaviors)
            {
                if (behavior != null && behavior.Enabled)
                {
                    behavior.DrawGizmos(this, neighbors);
                }
            }
        }
        
        private void OnValidate()
        {
            // Clamp values to reasonable ranges
            maxSpeed = Mathf.Max(0.1f, maxSpeed);
            maxForce = Mathf.Max(0.1f, maxForce);
            perceptionRadius = Mathf.Max(0.1f, perceptionRadius);
            mass = Mathf.Max(0.1f, mass);
            maxNeighbors = Mathf.Max(1, maxNeighbors);
        }
        
        private void OnDestroy()
        {
            // Unregister from SwarmManager
            if (swarmManager != null)
            {
                swarmManager.UnregisterAgent(this);
            }
        }
    }
}