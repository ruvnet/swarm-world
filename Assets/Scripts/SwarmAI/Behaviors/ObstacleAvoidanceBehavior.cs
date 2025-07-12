using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Core;

namespace SwarmAI.Behaviors
{
    /// <summary>
    /// Obstacle avoidance behavior using raycast prediction
    /// </summary>
    [CreateAssetMenu(fileName = "ObstacleAvoidanceBehavior", menuName = "SwarmAI/Behaviors/ObstacleAvoidance", order = 4)]
    public class ObstacleAvoidanceBehavior : SwarmBehavior
    {
        [Header("Avoidance Settings")]
        [SerializeField] private float avoidDistance = 3f;
        [SerializeField] private float avoidForceMultiplier = 2f;
        [SerializeField] private LayerMask obstacleLayer = -1;
        [SerializeField] private bool useMultipleRays = true;
        [SerializeField] private int rayCount = 5;
        [SerializeField] private float raySpread = 45f;
        [SerializeField] private bool dynamicAvoidDistance = true;
        
        public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            Vector3 velocity = agent.Velocity;
            if (velocity.magnitude < 0.1f)
                return Vector3.zero;
            
            float currentAvoidDistance = dynamicAvoidDistance ? 
                avoidDistance * (velocity.magnitude / agent.MaxSpeed) : avoidDistance;
            
            Vector3 avoidForce = Vector3.zero;
            
            if (useMultipleRays)
            {
                avoidForce = CalculateMultiRayAvoidance(agent, currentAvoidDistance);
            }
            else
            {
                avoidForce = CalculateSingleRayAvoidance(agent, currentAvoidDistance);
            }
            
            return avoidForce * avoidForceMultiplier;
        }
        
        private Vector3 CalculateSingleRayAvoidance(ISwarmAgent agent, float distance)
        {
            Vector3 forward = agent.Velocity.normalized;
            
            if (Physics.Raycast(agent.Position, forward, out RaycastHit hit, distance, obstacleLayer))
            {
                // Calculate avoidance direction (perpendicular to hit normal)
                Vector3 avoidDirection = Vector3.Cross(hit.normal, Vector3.up);
                
                // Choose the direction that aligns better with current velocity
                if (Vector3.Dot(avoidDirection, agent.Velocity) < 0)
                    avoidDirection = -avoidDirection;
                
                // Scale force based on distance to obstacle
                float forceScale = 1f - (hit.distance / distance);
                return avoidDirection * agent.MaxForce * forceScale;
            }
            
            return Vector3.zero;
        }
        
        private Vector3 CalculateMultiRayAvoidance(ISwarmAgent agent, float distance)
        {
            Vector3 velocity = agent.Velocity.normalized;
            Vector3 totalAvoidForce = Vector3.zero;
            int hitCount = 0;
            
            for (int i = 0; i < rayCount; i++)
            {
                // Calculate ray direction
                float angle = (i - rayCount / 2f) * (raySpread / rayCount);
                Vector3 rayDirection = Quaternion.AngleAxis(angle, Vector3.up) * velocity;
                
                if (Physics.Raycast(agent.Position, rayDirection, out RaycastHit hit, distance, obstacleLayer))
                {
                    // Calculate avoidance force perpendicular to obstacle
                    Vector3 avoidDirection = Vector3.Cross(hit.normal, Vector3.up);
                    
                    // Choose direction that moves away from obstacle
                    if (Vector3.Dot(avoidDirection, agent.Position - hit.point) < 0)
                        avoidDirection = -avoidDirection;
                    
                    // Weight by distance and ray position
                    float distanceWeight = 1f - (hit.distance / distance);
                    float centerWeight = 1f - Mathf.Abs(angle) / (raySpread / 2f);
                    float weight = distanceWeight * centerWeight;
                    
                    totalAvoidForce += avoidDirection * weight;
                    hitCount++;
                }
            }
            
            if (hitCount > 0)
            {
                totalAvoidForce /= hitCount;
                return Vector3.ClampMagnitude(totalAvoidForce, agent.MaxForce);
            }
            
            return Vector3.zero;
        }
        
        public override void DrawGizmos(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (!enabled) return;
            
            Vector3 velocity = agent.Velocity;
            if (velocity.magnitude < 0.1f) return;
            
            float currentAvoidDistance = dynamicAvoidDistance ? 
                avoidDistance * (velocity.magnitude / agent.MaxSpeed) : avoidDistance;
            
            if (useMultipleRays)
            {
                DrawMultiRayGizmos(agent, currentAvoidDistance);
            }
            else
            {
                DrawSingleRayGizmos(agent, currentAvoidDistance);
            }
        }
        
        private void DrawSingleRayGizmos(ISwarmAgent agent, float distance)
        {
            Vector3 forward = agent.Velocity.normalized;
            
            if (Physics.Raycast(agent.Position, forward, out RaycastHit hit, distance, obstacleLayer))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(agent.Position, hit.point);
                Gizmos.DrawWireSphere(hit.point, 0.2f);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(agent.Position, forward * distance);
            }
        }
        
        private void DrawMultiRayGizmos(ISwarmAgent agent, float distance)
        {
            Vector3 velocity = agent.Velocity.normalized;
            
            for (int i = 0; i < rayCount; i++)
            {
                float angle = (i - rayCount / 2f) * (raySpread / rayCount);
                Vector3 rayDirection = Quaternion.AngleAxis(angle, Vector3.up) * velocity;
                
                if (Physics.Raycast(agent.Position, rayDirection, out RaycastHit hit, distance, obstacleLayer))
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(agent.Position, hit.point);
                }
                else
                {
                    Gizmos.color = new Color(0, 1, 0, 0.3f);
                    Gizmos.DrawRay(agent.Position, rayDirection * distance);
                }
            }
        }
        
        private void OnValidate()
        {
            avoidDistance = Mathf.Max(0.1f, avoidDistance);
            rayCount = Mathf.Max(1, rayCount);
            raySpread = Mathf.Clamp(raySpread, 0f, 180f);
            avoidForceMultiplier = Mathf.Max(0.1f, avoidForceMultiplier);
        }
    }
}