using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI
{
    /// <summary>
    /// Prey behavior for ecosystem simulation - flocks with others, flees from predators
    /// </summary>
    [System.Serializable]
    public class PreyBehavior : BaseBehavior
    {
        [Header("Flocking")]
        public float flockingRadius = 3f;
        public float separationWeight = 2f;
        public float alignmentWeight = 1f;
        public float cohesionWeight = 1f;
        
        [Header("Predator Avoidance")]
        public float predatorDetectionRadius = 10f;
        public float fleeWeight = 8f;
        public float panicDistance = 5f;
        public float maxFleeTime = 10f;
        
        [Header("Grazing & Energy")]
        public float grazingRadius = 2f;
        public float energyRecoveryRate = 1f;
        public float fleeEnergyDrain = 2f;
        public float reproductionEnergyThreshold = 80f;
        
        [Header("Herd Protection")]
        public float herdProtectionRadius = 4f;
        public float herdProtectionWeight = 3f;
        public bool enableHerdBehavior = true;
        
        private PreyState currentState = PreyState.Grazing;
        private Agent detectedPredator;
        private Vector3 lastPredatorPosition;
        private float fleeStartTime;
        private float currentEnergy;
        private Vector3 grazingTarget;
        private float lastGrazingTime;
        
        public PreyBehavior()
        {
            behaviorName = "Prey";
            currentEnergy = 100f;
            SetNewGrazingTarget();
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            UpdateState(agent);
            UpdateEnergy(agent);
            
            Vector3 force = Vector3.zero;
            
            switch (currentState)
            {
                case PreyState.Grazing:
                    force = CalculateGrazingBehavior(agent);
                    break;
                    
                case PreyState.Fleeing:
                    force = CalculateFleeingBehavior(agent);
                    break;
                    
                case PreyState.Flocking:
                    force = CalculateFlockingBehavior(agent);
                    break;
                    
                case PreyState.Panicking:
                    force = CalculatePanickingBehavior(agent);
                    break;
                    
                case PreyState.Resting:
                    force = CalculateRestingBehavior(agent);
                    break;
            }
            
            // Always include basic flocking for cohesion
            force += CalculateBasicFlocking(agent) * 0.3f;
            
            return force;
        }
        
        void UpdateState(Agent agent)
        {
            List<Agent> nearbyPredators = FindNearbyPredators(agent);
            
            // Predator detection
            if (nearbyPredators.Count > 0)
            {
                detectedPredator = FindClosestPredator(agent, nearbyPredators);
                lastPredatorPosition = detectedPredator.position;
                
                float distanceToPredator = Vector3.Distance(agent.position, detectedPredator.position);
                
                if (distanceToPredator < panicDistance)
                {
                    currentState = PreyState.Panicking;
                    fleeStartTime = Time.time;
                }
                else if (distanceToPredator < predatorDetectionRadius)
                {
                    currentState = PreyState.Fleeing;
                    fleeStartTime = Time.time;
                }
            }
            else
            {
                // No predators detected
                if (currentState == PreyState.Fleeing || currentState == PreyState.Panicking)
                {
                    // Check if enough time has passed to stop fleeing
                    if (Time.time - fleeStartTime > maxFleeTime)
                    {
                        currentState = PreyState.Grazing;
                        detectedPredator = null;
                    }
                }
                else if (currentEnergy < 30f)
                {
                    currentState = PreyState.Resting;
                }
                else if (currentEnergy > 90f)
                {
                    // High energy, focus on flocking
                    currentState = PreyState.Flocking;
                }
                else
                {
                    currentState = PreyState.Grazing;
                }
            }
        }
        
        void UpdateEnergy(Agent agent)
        {
            switch (currentState)
            {
                case PreyState.Grazing:
                    if (Vector3.Distance(agent.position, grazingTarget) < 1f)
                    {
                        currentEnergy += energyRecoveryRate * Time.deltaTime * 2f; // Bonus for reaching target
                        SetNewGrazingTarget();
                    }
                    currentEnergy += energyRecoveryRate * Time.deltaTime * 0.5f;
                    break;
                    
                case PreyState.Fleeing:
                case PreyState.Panicking:
                    currentEnergy -= fleeEnergyDrain * Time.deltaTime;
                    break;
                    
                case PreyState.Resting:
                    currentEnergy += energyRecoveryRate * Time.deltaTime * 1.5f;
                    break;
                    
                case PreyState.Flocking:
                    currentEnergy += energyRecoveryRate * Time.deltaTime * 0.3f;
                    break;
            }
            
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);
            agent.energy = currentEnergy;
            
            // Check for reproduction possibility
            CheckForReproduction(agent);
        }
        
        Vector3 CalculateGrazingBehavior(Agent agent)
        {
            Vector3 grazingForce = Seek(agent, grazingTarget);
            Vector3 flockingForce = CalculateBasicFlocking(agent) * 0.5f;
            Vector3 wanderForce = CalculateGentleWander(agent) * 0.3f;
            
            return grazingForce + flockingForce + wanderForce;
        }
        
        Vector3 CalculateFleeingBehavior(Agent agent)
        {
            if (detectedPredator == null)
                return CalculateGrazingBehavior(agent);
            
            Vector3 fleeForce = Flee(agent, detectedPredator.position) * fleeWeight;
            Vector3 herdForce = CalculateHerdProtection(agent) * herdProtectionWeight;
            
            return fleeForce + herdForce;
        }
        
        Vector3 CalculatePanickingBehavior(Agent agent)
        {
            Vector3 panicFlee = Vector3.zero;
            
            if (detectedPredator != null)
            {
                panicFlee = Flee(agent, detectedPredator.position) * fleeWeight * 2f;
            }
            
            // Add erratic movement in panic
            Vector3 erraticMovement = Random.insideUnitSphere * agent.maxForce * 0.5f;
            erraticMovement.y = 0;
            
            Vector3 avoidance = CalculateObstacleAvoidance(agent) * 4f;
            
            return panicFlee + erraticMovement + avoidance;
        }
        
        Vector3 CalculateFlockingBehavior(Agent agent)
        {
            return CalculateBasicFlocking(agent);
        }
        
        Vector3 CalculateRestingBehavior(Agent agent)
        {
            // Slow movement, stay with herd
            Vector3 herdStay = CalculateHerdProtection(agent) * 2f;
            Vector3 slowWander = CalculateGentleWander(agent) * 0.1f;
            
            return herdStay + slowWander;
        }
        
        Vector3 CalculateBasicFlocking(Agent agent)
        {
            List<Agent> flockmates = FindNearbyPrey(agent);
            
            if (flockmates.Count == 0) return Vector3.zero;
            
            Vector3 separation = CalculateSeparation(agent, flockmates) * separationWeight;
            Vector3 alignment = CalculateAlignment(agent, flockmates) * alignmentWeight;
            Vector3 cohesion = CalculateCohesion(agent, flockmates) * cohesionWeight;
            
            return separation + alignment + cohesion;
        }
        
        Vector3 CalculateHerdProtection(Agent agent)
        {
            if (!enableHerdBehavior) return Vector3.zero;
            
            List<Agent> herdMembers = FindNearbyPrey(agent);
            
            if (herdMembers.Count == 0) return Vector3.zero;
            
            // Find center of herd
            Vector3 herdCenter = Vector3.zero;
            foreach (Agent member in herdMembers)
            {
                herdCenter += member.position;
            }
            herdCenter /= herdMembers.Count;
            
            // Move towards center when threatened
            Vector3 toCenter = Seek(agent, herdCenter);
            
            // Also try to put herd between self and predator
            if (detectedPredator != null)
            {
                Vector3 toPredator = (detectedPredator.position - agent.position).normalized;
                Vector3 behindHerd = herdCenter - toPredator * 3f;
                Vector3 shelterForce = Seek(agent, behindHerd) * 0.5f;
                
                return toCenter + shelterForce;
            }
            
            return toCenter;
        }
        
        Vector3 CalculateSeparation(Agent agent, List<Agent> neighbors)
        {
            Vector3 steer = Vector3.zero;
            int count = 0;
            
            foreach (Agent neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.position, neighbor.position);
                
                if (distance > 0 && distance < flockingRadius * 0.5f)
                {
                    Vector3 diff = agent.position - neighbor.position;
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
                steer *= agent.maxSpeed;
                steer -= agent.velocity;
                steer = Vector3.ClampMagnitude(steer, agent.maxForce);
            }
            
            return steer;
        }
        
        Vector3 CalculateAlignment(Agent agent, List<Agent> neighbors)
        {
            Vector3 averageVelocity = Vector3.zero;
            int count = 0;
            
            foreach (Agent neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.position, neighbor.position);
                
                if (distance > 0 && distance < flockingRadius)
                {
                    averageVelocity += neighbor.velocity;
                    count++;
                }
            }
            
            if (count > 0)
            {
                averageVelocity /= count;
                averageVelocity.Normalize();
                averageVelocity *= agent.maxSpeed;
                
                Vector3 steer = averageVelocity - agent.velocity;
                steer = Vector3.ClampMagnitude(steer, agent.maxForce);
                return steer;
            }
            
            return Vector3.zero;
        }
        
        Vector3 CalculateCohesion(Agent agent, List<Agent> neighbors)
        {
            Vector3 centerOfMass = Vector3.zero;
            int count = 0;
            
            foreach (Agent neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.position, neighbor.position);
                
                if (distance > 0 && distance < flockingRadius)
                {
                    centerOfMass += neighbor.position;
                    count++;
                }
            }
            
            if (count > 0)
            {
                centerOfMass /= count;
                return Seek(agent, centerOfMass);
            }
            
            return Vector3.zero;
        }
        
        Vector3 CalculateGentleWander(Agent agent)
        {
            Vector3 circleCenter = agent.velocity.normalized * 1.5f;
            Vector3 displacement = Random.insideUnitSphere * 0.5f;
            displacement.y = 0;
            
            Vector3 wanderTarget = agent.position + circleCenter + displacement;
            return Seek(agent, wanderTarget);
        }
        
        Vector3 CalculateObstacleAvoidance(Agent agent)
        {
            // Simple obstacle avoidance using raycasting
            Vector3 avoidanceForce = Vector3.zero;
            
            Vector3[] checkDirections = {
                agent.velocity.normalized,
                Quaternion.AngleAxis(30, Vector3.up) * agent.velocity.normalized,
                Quaternion.AngleAxis(-30, Vector3.up) * agent.velocity.normalized
            };
            
            foreach (Vector3 direction in checkDirections)
            {
                RaycastHit hit;
                if (Physics.Raycast(agent.position, direction, out hit, 3f))
                {
                    Vector3 avoidDirection = Vector3.Cross(direction, Vector3.up).normalized;
                    avoidanceForce += avoidDirection * agent.maxForce;
                }
            }
            
            return avoidanceForce;
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
        
        Vector3 Flee(Agent agent, Vector3 threat)
        {
            Vector3 desired = agent.position - threat;
            desired.Normalize();
            desired *= agent.maxSpeed;
            
            Vector3 steer = desired - agent.velocity;
            steer = Vector3.ClampMagnitude(steer, agent.maxForce);
            return steer;
        }
        
        List<Agent> FindNearbyPredators(Agent agent)
        {
            return agent.neighbors.Where(a => 
                a.agentType == AgentType.Predator &&
                Vector3.Distance(a.position, agent.position) <= predatorDetectionRadius
            ).ToList();
        }
        
        List<Agent> FindNearbyPrey(Agent agent)
        {
            return agent.neighbors.Where(a => 
                a.agentType == AgentType.Prey &&
                a != agent &&
                Vector3.Distance(a.position, agent.position) <= flockingRadius
            ).ToList();
        }
        
        Agent FindClosestPredator(Agent agent, List<Agent> predators)
        {
            if (predators.Count == 0) return null;
            
            Agent closest = predators[0];
            float closestDistance = Vector3.Distance(agent.position, closest.position);
            
            foreach (Agent predator in predators)
            {
                float distance = Vector3.Distance(agent.position, predator.position);
                if (distance < closestDistance)
                {
                    closest = predator;
                    closestDistance = distance;
                }
            }
            
            return closest;
        }
        
        void SetNewGrazingTarget()
        {
            grazingTarget = Random.insideUnitSphere * grazingRadius;
            grazingTarget.y = 0;
            lastGrazingTime = Time.time;
        }
        
        void CheckForReproduction(Agent agent)
        {
            // Simplified reproduction check
            if (currentEnergy > reproductionEnergyThreshold && 
                currentState == PreyState.Flocking &&
                Random.Range(0f, 1f) < 0.001f) // Very rare
            {
                // Could trigger reproduction event
                currentEnergy *= 0.7f; // Cost of reproduction
            }
        }
        
        public float GetEnergyPercentage()
        {
            return currentEnergy / 100f;
        }
        
        public PreyState GetCurrentState()
        {
            return currentState;
        }
        
        public Agent GetDetectedPredator()
        {
            return detectedPredator;
        }
    }
    
    public enum PreyState
    {
        Grazing,
        Fleeing,
        Flocking,
        Panicking,
        Resting
    }
}