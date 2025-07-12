using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Core;

namespace SwarmAI.Behaviors
{
    /// <summary>
    /// Flee behavior - agents move away from a threat position
    /// </summary>
    [CreateAssetMenu(fileName = "FleeBehavior", menuName = "SwarmAI/Behaviors/Flee", order = 6)]
    public class FleeBehavior : SwarmBehavior
    {
        [Header("Flee Settings")]
        [SerializeField] private Transform threat;
        [SerializeField] private Vector3 threatPosition;
        [SerializeField] private bool useTransformThreat = true;
        [SerializeField] private float fleeRadius = 10f;
        [SerializeField] private float panicRadius = 5f;
        [SerializeField] private float panicMultiplier = 2f;
        [SerializeField] private AnimationCurve fleeIntensityCurve = AnimationCurve.Linear(0, 1, 1, 0);
        
        public Transform Threat
        {
            get => threat;
            set => threat = value;
        }
        
        public Vector3 ThreatPosition
        {
            get => useTransformThreat && threat != null ? threat.position : threatPosition;
            set => threatPosition = value;
        }
        
        public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            Vector3 threatPos = ThreatPosition;
            Vector3 diff = agent.Position - threatPos;
            float distance = diff.magnitude;
            
            // Only flee if within flee radius
            if (distance > fleeRadius || distance < 0.1f)
                return Vector3.zero;
            
            // Calculate flee intensity based on distance
            float normalizedDistance = distance / fleeRadius;
            float intensity = fleeIntensityCurve.Evaluate(normalizedDistance);
            
            // Apply panic multiplier if very close
            if (distance < panicRadius)
            {
                intensity *= panicMultiplier;
            }
            
            Vector3 desired = diff.normalized * agent.MaxSpeed * intensity;
            Vector3 steer = desired - agent.Velocity;
            
            return Vector3.ClampMagnitude(steer, agent.MaxForce * intensity);
        }
        
        public override void DrawGizmos(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (!enabled) return;
            
            Vector3 threatPos = ThreatPosition;
            
            // Draw threat position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(threatPos, 1f);
            
            // Draw flee radius
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(threatPos, fleeRadius);
            
            // Draw panic radius
            Gizmos.color = new Color(1, 0, 0, 0.4f);
            Gizmos.DrawWireSphere(threatPos, panicRadius);
            
            // Draw flee direction if agent is within range
            float distance = Vector3.Distance(agent.Position, threatPos);
            if (distance <= fleeRadius)
            {
                Vector3 fleeDirection = (agent.Position - threatPos).normalized;
                Gizmos.color = Color.red;
                Gizmos.DrawRay(agent.Position, fleeDirection * 3f);
            }
        }
        
        private void OnValidate()
        {
            fleeRadius = Mathf.Max(0.1f, fleeRadius);
            panicRadius = Mathf.Max(0.1f, Mathf.Min(panicRadius, fleeRadius));
            panicMultiplier = Mathf.Max(1f, panicMultiplier);
        }
    }
}