using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI
{
    /// <summary>
    /// Formation flying behavior for military/aerospace demonstrations
    /// Supports various formations: V-formation, line, diamond, box, etc.
    /// </summary>
    [System.Serializable]
    public class FormationBehavior : BaseBehavior
    {
        [Header("Formation Configuration")]
        public FormationType formation = FormationType.VFormation;
        public int formationIndex = 0; // Position in formation
        public Agent leader;
        public float formationSpacing = 3f;
        public float formationTightness = 2f;
        
        [Header("Formation Dynamics")]
        public float leaderFollowWeight = 4f;
        public float positionHoldWeight = 3f;
        public float collisionAvoidanceWeight = 5f;
        public float speedMatchingWeight = 2f;
        
        [Header("Formation Flexibility")]
        public bool allowFormationBreaking = true;
        public float breakFormationDistance = 15f;
        public float reformFormationDistance = 8f;
        public float turbulenceResistance = 0.5f;
        
        private Vector3 targetFormationPosition;
        private FormationState currentState = FormationState.Forming;
        private float lastFormationUpdate = 0f;
        private Vector3 formationOffset;
        
        public FormationBehavior()
        {
            behaviorName = "Formation Flying";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            if (leader == null)
            {
                return FindAndAssignLeader(agent);
            }
            
            UpdateFormationState(agent);
            CalculateFormationPosition();
            
            Vector3 force = Vector3.zero;
            
            switch (currentState)
            {
                case FormationState.Forming:
                    force = CalculateFormingBehavior(agent);
                    break;
                    
                case FormationState.InFormation:
                    force = CalculateFormationMaintenance(agent);
                    break;
                    
                case FormationState.Breaking:
                    force = CalculateBreakingBehavior(agent);
                    break;
                    
                case FormationState.Reforming:
                    force = CalculateReformingBehavior(agent);
                    break;
            }
            
            return force;
        }
        
        void UpdateFormationState(Agent agent)
        {
            if (leader == null || !leader.isAlive)
            {
                currentState = FormationState.Breaking;
                return;
            }
            
            float distanceToLeader = Vector3.Distance(agent.position, leader.position);
            float distanceToFormationPos = Vector3.Distance(agent.position, targetFormationPosition);
            
            switch (currentState)
            {
                case FormationState.Forming:
                    if (distanceToFormationPos < formationSpacing * 0.5f)
                        currentState = FormationState.InFormation;
                    break;
                    
                case FormationState.InFormation:
                    if (allowFormationBreaking && distanceToLeader > breakFormationDistance)
                        currentState = FormationState.Breaking;
                    else if (distanceToFormationPos > formationSpacing * 2f)
                        currentState = FormationState.Reforming;
                    break;
                    
                case FormationState.Breaking:
                    if (distanceToLeader < reformFormationDistance)
                        currentState = FormationState.Reforming;
                    break;
                    
                case FormationState.Reforming:
                    if (distanceToFormationPos < formationSpacing * 0.7f)
                        currentState = FormationState.InFormation;
                    break;
            }
        }
        
        void CalculateFormationPosition()
        {
            if (leader == null) return;
            
            Vector3 leaderPosition = leader.position;
            Vector3 leaderDirection = leader.velocity.normalized;
            
            // Calculate offset based on formation type and index
            formationOffset = GetFormationOffset(formation, formationIndex, formationSpacing);
            
            // Transform offset relative to leader's orientation
            Vector3 right = Vector3.Cross(leaderDirection, Vector3.up).normalized;
            Vector3 forward = leaderDirection;
            Vector3 up = Vector3.up;
            
            targetFormationPosition = leaderPosition + 
                right * formationOffset.x + 
                up * formationOffset.y + 
                forward * formationOffset.z;
        }
        
        Vector3 CalculateFormingBehavior(Agent agent)
        {
            Vector3 moveToPosition = Seek(agent, targetFormationPosition) * positionHoldWeight;
            Vector3 matchSpeed = MatchLeaderSpeed(agent) * speedMatchingWeight;
            Vector3 avoidCollisions = CalculateCollisionAvoidance(agent) * collisionAvoidanceWeight;
            
            return moveToPosition + matchSpeed + avoidCollisions;
        }
        
        Vector3 CalculateFormationMaintenance(Agent agent)
        {
            Vector3 holdPosition = Seek(agent, targetFormationPosition) * positionHoldWeight;
            Vector3 followLeader = FollowLeader(agent) * leaderFollowWeight * 0.3f;
            Vector3 matchSpeed = MatchLeaderSpeed(agent) * speedMatchingWeight;
            Vector3 avoidCollisions = CalculateCollisionAvoidance(agent) * collisionAvoidanceWeight;
            
            // Add some turbulence resistance for realistic flight
            Vector3 stabilization = -agent.acceleration * turbulenceResistance;
            
            return holdPosition + followLeader + matchSpeed + avoidCollisions + stabilization;
        }
        
        Vector3 CalculateBreakingBehavior(Agent agent)
        {
            // Emergency avoidance and independent flight
            Vector3 avoidCollisions = CalculateCollisionAvoidance(agent) * collisionAvoidanceWeight * 2f;
            Vector3 independentMovement = CalculateWander(agent) * 0.5f;
            Vector3 returnToLeader = Seek(agent, leader.position) * 0.2f;
            
            return avoidCollisions + independentMovement + returnToLeader;
        }
        
        Vector3 CalculateReformingBehavior(Agent agent)
        {
            Vector3 returnToFormation = Seek(agent, targetFormationPosition) * positionHoldWeight * 1.5f;
            Vector3 matchSpeed = MatchLeaderSpeed(agent) * speedMatchingWeight;
            Vector3 avoidCollisions = CalculateCollisionAvoidance(agent) * collisionAvoidanceWeight;
            
            return returnToFormation + matchSpeed + avoidCollisions;
        }
        
        Vector3 FollowLeader(Agent agent)
        {
            if (leader == null) return Vector3.zero;
            
            // Predict where leader will be
            float predictionTime = Vector3.Distance(agent.position, leader.position) / agent.maxSpeed;
            Vector3 predictedPosition = leader.position + leader.velocity * predictionTime;
            
            return Seek(agent, predictedPosition);
        }
        
        Vector3 MatchLeaderSpeed(Agent agent)
        {
            if (leader == null) return Vector3.zero;
            
            Vector3 desiredVelocity = leader.velocity;
            Vector3 speedAdjustment = desiredVelocity - agent.velocity;
            
            return Vector3.ClampMagnitude(speedAdjustment, agent.maxForce);
        }
        
        Vector3 CalculateCollisionAvoidance(Agent agent)
        {
            Vector3 avoidanceForce = Vector3.zero;
            
            // Avoid other formation members
            foreach (Agent neighbor in agent.neighbors)
            {
                if (neighbor == leader) continue;
                
                float distance = Vector3.Distance(agent.position, neighbor.position);
                float minDistance = formationSpacing * 0.3f;
                
                if (distance < minDistance && distance > 0)
                {
                    Vector3 diff = agent.position - neighbor.position;
                    diff.Normalize();
                    diff /= distance; // Weight by inverse distance
                    avoidanceForce += diff;
                }
            }
            
            // Obstacle avoidance
            avoidanceForce += CalculateObstacleAvoidance(agent);
            
            return avoidanceForce;
        }
        
        Vector3 CalculateObstacleAvoidance(Agent agent)
        {
            Vector3 avoidanceForce = Vector3.zero;
            float lookAheadDistance = agent.velocity.magnitude + 2f;
            
            // Cast rays in multiple directions
            Vector3[] checkDirections = {
                agent.velocity.normalized,
                Quaternion.AngleAxis(20, Vector3.up) * agent.velocity.normalized,
                Quaternion.AngleAxis(-20, Vector3.up) * agent.velocity.normalized,
                Quaternion.AngleAxis(45, Vector3.up) * agent.velocity.normalized,
                Quaternion.AngleAxis(-45, Vector3.up) * agent.velocity.normalized
            };
            
            foreach (Vector3 direction in checkDirections)
            {
                RaycastHit hit;
                if (Physics.Raycast(agent.position, direction, out hit, lookAheadDistance))
                {
                    // Calculate avoidance direction
                    Vector3 avoidDirection = Vector3.Cross(direction, Vector3.up).normalized;
                    float proximity = 1f - (hit.distance / lookAheadDistance);
                    avoidanceForce += avoidDirection * proximity * agent.maxForce;
                }
            }
            
            return avoidanceForce;
        }
        
        Vector3 CalculateWander(Agent agent)
        {
            Vector3 circleCenter = agent.velocity.normalized * 2f;
            Vector3 displacement = Random.insideUnitSphere;
            displacement.y *= 0.3f; // Reduce vertical wandering for flight
            displacement.Normalize();
            displacement *= 1.5f;
            
            Vector3 wanderTarget = agent.position + circleCenter + displacement;
            return Seek(agent, wanderTarget);
        }
        
        Vector3 Seek(Agent agent, Vector3 target)
        {
            Vector3 desired = target - agent.position;
            desired.Normalize();
            desired *= agent.maxSpeed;
            
            Vector3 steer = desired - agent.velocity;
            steer = Vector3.ClampMagnitude(steer, agent.maxForce);
            return steer;
        }
        
        Vector3 FindAndAssignLeader(Agent agent)
        {
            // Look for a designated leader or become one
            FormationManager formationManager = Object.FindObjectOfType<FormationManager>();
            if (formationManager != null)
            {
                leader = formationManager.AssignToFormation(agent);
                if (leader != null)
                {
                    currentState = FormationState.Forming;
                }
            }
            
            // Default behavior while searching for leader
            return CalculateWander(agent) * 0.5f;
        }
        
        Vector3 GetFormationOffset(FormationType type, int index, float spacing)
        {
            switch (type)
            {
                case FormationType.VFormation:
                    return GetVFormationOffset(index, spacing);
                    
                case FormationType.LineAbreast:
                    return new Vector3((index - 2) * spacing, 0, 0);
                    
                case FormationType.LineAstern:
                    return new Vector3(0, 0, -index * spacing);
                    
                case FormationType.Diamond:
                    return GetDiamondFormationOffset(index, spacing);
                    
                case FormationType.Box:
                    return GetBoxFormationOffset(index, spacing);
                    
                case FormationType.Echelon:
                    return new Vector3(index * spacing, 0, -index * spacing * 0.5f);
                    
                case FormationType.Trail:
                    return new Vector3(
                        Mathf.Sin(index * 0.5f) * spacing * 0.5f, 
                        0, 
                        -index * spacing
                    );
                    
                default:
                    return Vector3.zero;
            }
        }
        
        Vector3 GetVFormationOffset(int index, float spacing)
        {
            if (index == 0) return Vector3.zero; // Leader
            
            int side = (index % 2 == 1) ? 1 : -1; // Alternate sides
            int row = (index + 1) / 2;
            
            return new Vector3(
                side * row * spacing, 
                0, 
                -row * spacing * 0.7f
            );
        }
        
        Vector3 GetDiamondFormationOffset(int index, float spacing)
        {
            switch (index)
            {
                case 0: return Vector3.zero; // Leader (front)
                case 1: return new Vector3(-spacing, 0, -spacing);
                case 2: return new Vector3(spacing, 0, -spacing);
                case 3: return new Vector3(0, 0, -spacing * 2);
                default: 
                    // Additional aircraft in expanded diamond
                    int extraIndex = index - 4;
                    float angle = (extraIndex * 60f) * Mathf.Deg2Rad;
                    return new Vector3(
                        Mathf.Sin(angle) * spacing * 1.5f,
                        0,
                        -spacing - Mathf.Cos(angle) * spacing * 0.5f
                    );
            }
        }
        
        Vector3 GetBoxFormationOffset(int index, float spacing)
        {
            switch (index)
            {
                case 0: return new Vector3(-spacing * 0.5f, 0, 0);
                case 1: return new Vector3(spacing * 0.5f, 0, 0);
                case 2: return new Vector3(-spacing * 0.5f, 0, -spacing);
                case 3: return new Vector3(spacing * 0.5f, 0, -spacing);
                default:
                    // Stack additional layers
                    int layer = (index - 4) / 4 + 1;
                    int posInLayer = (index - 4) % 4;
                    Vector3 baseOffset = GetBoxFormationOffset(posInLayer, spacing);
                    return baseOffset + Vector3.back * spacing * layer;
            }
        }
        
        public void SetLeader(Agent newLeader)
        {
            leader = newLeader;
            currentState = FormationState.Forming;
        }
        
        public void SetFormationIndex(int index)
        {
            formationIndex = index;
            currentState = FormationState.Forming;
        }
        
        public void SetFormationType(FormationType newType)
        {
            formation = newType;
            currentState = FormationState.Forming;
        }
        
        public FormationState GetCurrentState()
        {
            return currentState;
        }
        
        public Vector3 GetTargetPosition()
        {
            return targetFormationPosition;
        }
        
        public float GetFormationDistance()
        {
            if (leader == null) return float.MaxValue;
            return Vector3.Distance(leader.position, targetFormationPosition);
        }
    }
    
    public enum FormationType
    {
        VFormation,
        LineAbreast,
        LineAstern,
        Diamond,
        Box,
        Echelon,
        Trail
    }
    
    public enum FormationState
    {
        Forming,
        InFormation,
        Breaking,
        Reforming
    }
}