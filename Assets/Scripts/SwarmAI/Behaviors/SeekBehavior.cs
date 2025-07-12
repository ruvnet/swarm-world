using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Core;

namespace SwarmAI.Behaviors
{
    /// <summary>
    /// Seek behavior - agents move towards a target position
    /// </summary>
    [CreateAssetMenu(fileName = "SeekBehavior", menuName = "SwarmAI/Behaviors/Seek", order = 5)]
    public class SeekBehavior : SwarmBehavior
    {
        [Header("Seek Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private bool useTransformTarget = true;
        [SerializeField] private float arrivalRadius = 2f;
        [SerializeField] private bool enableArrival = true;
        [SerializeField] private AnimationCurve arrivalCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        public Transform Target
        {
            get => target;
            set => target = value;
        }
        
        public Vector3 TargetPosition
        {
            get => useTransformTarget && target != null ? target.position : targetPosition;
            set => targetPosition = value;
        }
        
        public override Vector3 CalculateForce(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            Vector3 seekTarget = TargetPosition;
            Vector3 desired = seekTarget - agent.Position;
            float distance = desired.magnitude;
            
            if (distance < 0.1f)
                return Vector3.zero;
            
            desired = desired.normalized;
            
            // Apply arrival behavior if enabled
            if (enableArrival && distance < arrivalRadius)
            {
                float normalizedDistance = distance / arrivalRadius;
                float speedMultiplier = arrivalCurve.Evaluate(normalizedDistance);
                desired *= agent.MaxSpeed * speedMultiplier;
            }
            else
            {
                desired *= agent.MaxSpeed;
            }
            
            Vector3 steer = desired - agent.Velocity;
            return Vector3.ClampMagnitude(steer, agent.MaxForce);
        }
        
        public override void DrawGizmos(ISwarmAgent agent, List<ISwarmAgent> neighbors)
        {
            if (!enabled) return;
            
            Vector3 seekTarget = TargetPosition;
            
            // Draw target
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(seekTarget, 0.5f);
            
            // Draw arrival radius
            if (enableArrival)
            {
                Gizmos.color = new Color(1, 1, 0, 0.2f);
                Gizmos.DrawWireSphere(seekTarget, arrivalRadius);
            }
            
            // Draw seek line
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(agent.Position, seekTarget);
        }
        
        private void OnValidate()
        {
            arrivalRadius = Mathf.Max(0.1f, arrivalRadius);
        }
    }
}