using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI
{
    public class SwarmAgent : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float maxForce = 10f;
        
        [Header("Perception")]
        [SerializeField] private float perceptionRadius = 5f;
        [SerializeField] private LayerMask agentLayer;
        
        [Header("Behavior Weights")]
        [SerializeField] private float separationWeight = 1.5f;
        [SerializeField] private float alignmentWeight = 1.0f;
        [SerializeField] private float cohesionWeight = 1.0f;
        
        [Header("Debugging")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private Color gizmoColor = Color.yellow;
        
        private Vector3 velocity;
        private List<SwarmAgent> neighbors;
        
        // Public properties for Inspector access
        public float MaxSpeed => maxSpeed;
        public float MaxForce => maxForce;
        public float PerceptionRadius => perceptionRadius;
        public Vector3 Velocity => velocity;
        public List<SwarmAgent> Neighbors => neighbors;
        public float SeparationWeight => separationWeight;
        public float AlignmentWeight => alignmentWeight;
        public float CohesionWeight => cohesionWeight;
        
        void Start()
        {
            velocity = Random.insideUnitSphere * maxSpeed;
            velocity.y = 0; // Keep movement on XZ plane
            SwarmManager.Instance?.RegisterAgent(this);
        }
        
        void Update()
        {
            UpdateNeighbors();
            Vector3 acceleration = CalculateAcceleration();
            
            velocity += acceleration * Time.deltaTime;
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            
            transform.position += velocity * Time.deltaTime;
            if (velocity.magnitude > 0.1f)
            {
                transform.forward = velocity.normalized;
            }
        }
        
        private void UpdateNeighbors()
        {
            neighbors = Physics.OverlapSphere(transform.position, perceptionRadius, agentLayer)
                .Select(c => c.GetComponent<SwarmAgent>())
                .Where(a => a != null && a != this)
                .ToList();
        }
        
        private Vector3 CalculateAcceleration()
        {
            if (neighbors.Count == 0) return Vector3.zero;
            
            Vector3 separation = CalculateSeparation() * separationWeight;
            Vector3 alignment = CalculateAlignment() * alignmentWeight;
            Vector3 cohesion = CalculateCohesion() * cohesionWeight;
            
            return separation + alignment + cohesion;
        }
        
        private Vector3 CalculateSeparation()
        {
            Vector3 steer = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(transform.position, neighbor.transform.position);
                if (distance > 0 && distance < 2f)
                {
                    Vector3 diff = transform.position - neighbor.transform.position;
                    diff.Normalize();
                    diff /= distance; // Weight by distance
                    steer += diff;
                    count++;
                }
            }
            
            if (count > 0)
            {
                steer /= count;
                steer.Normalize();
                steer *= maxSpeed;
                steer -= velocity;
                steer = Vector3.ClampMagnitude(steer, maxForce);
            }
            
            return steer;
        }
        
        private Vector3 CalculateAlignment()
        {
            Vector3 sum = Vector3.zero;
            
            foreach (var neighbor in neighbors)
            {
                sum += neighbor.velocity;
            }
            
            sum /= neighbors.Count;
            sum.Normalize();
            sum *= maxSpeed;
            
            Vector3 steer = sum - velocity;
            return Vector3.ClampMagnitude(steer, maxForce);
        }
        
        private Vector3 CalculateCohesion()
        {
            Vector3 sum = Vector3.zero;
            
            foreach (var neighbor in neighbors)
            {
                sum += neighbor.transform.position;
            }
            
            sum /= neighbors.Count;
            return Seek(sum);
        }
        
        private Vector3 Seek(Vector3 target)
        {
            Vector3 desired = target - transform.position;
            desired.Normalize();
            desired *= maxSpeed;
            
            Vector3 steer = desired - velocity;
            return Vector3.ClampMagnitude(steer, maxForce);
        }
        
        void OnDestroy()
        {
            SwarmManager.Instance?.UnregisterAgent(this);
        }
        
        void OnDrawGizmosSelected()
        {
            if (showDebugInfo)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(transform.position, perceptionRadius);
                
                // Draw velocity vector
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, velocity);
                
                // Draw neighbor connections
                if (neighbors != null)
                {
                    Gizmos.color = Color.green;
                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor != null)
                        {
                            Gizmos.DrawLine(transform.position, neighbor.transform.position);
                        }
                    }
                }
            }
        }
        
        // Runtime parameter adjustment methods
        public void SetBehaviorWeights(float separation, float alignment, float cohesion)
        {
            separationWeight = separation;
            alignmentWeight = alignment;
            cohesionWeight = cohesion;
        }
        
        public void SetMovementParameters(float speed, float force)
        {
            maxSpeed = speed;
            maxForce = force;
        }
        
        public void SetPerceptionRadius(float radius)
        {
            perceptionRadius = radius;
        }
    }
}