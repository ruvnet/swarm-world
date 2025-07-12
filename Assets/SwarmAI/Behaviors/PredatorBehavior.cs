using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI
{
    /// <summary>
    /// Predator behavior for ecosystem simulation - hunts prey, avoids other predators
    /// </summary>
    [System.Serializable]
    public class PredatorBehavior : BaseBehavior
    {
        [Header("Hunting Parameters")]
        public float huntingRadius = 8f;
        public float attackRange = 1.5f;
        public float huntingSpeed = 6f;
        
        [Header("Pack Behavior")]
        public float packRadius = 5f;
        public float packCohesionWeight = 0.5f;
        public bool enablePackHunting = true;
        
        [Header("Energy System")]
        public float maxEnergy = 100f;
        public float energyDecayRate = 0.5f;
        public float energyFromKill = 40f;
        public float huntingEnergyDrain = 1f;
        
        [Header("Territorial")]
        public float territoryRadius = 12f;
        public float territorialAggressionWeight = 2f;
        
        private Agent targetPrey;
        private Vector3 lastKnownPreyPosition;
        private float lastHuntTime;
        private float currentEnergy;
        private PredatorState currentState = PredatorState.Patrolling;
        
        public PredatorBehavior()
        {
            behaviorName = "Predator";
            currentEnergy = maxEnergy;
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            UpdateState(agent);
            UpdateEnergy(agent);
            
            Vector3 force = Vector3.zero;
            
            switch (currentState)
            {
                case PredatorState.Hunting:
                    force = CalculateHuntingBehavior(agent);
                    break;
                    
                case PredatorState.Patrolling:
                    force = CalculatePatrolBehavior(agent);
                    break;
                    
                case PredatorState.Resting:
                    force = CalculateRestingBehavior(agent);
                    break;
                    
                case PredatorState.PackHunting:
                    force = CalculatePackHuntingBehavior(agent);
                    break;
            }
            
            // Add territorial behavior
            force += CalculateTerritorialBehavior(agent) * territorialAggressionWeight;
            
            return force;
        }
        
        void UpdateState(Agent agent)
        {
            List<Agent> nearbyPrey = FindNearbyPrey(agent);
            List<Agent> nearbyPredators = FindNearbyPredators(agent);
            
            // Energy-based state transitions
            if (currentEnergy < 20f)
            {
                if (nearbyPrey.Count > 0)
                    currentState = PredatorState.Hunting;
                else
                    currentState = PredatorState.Resting;
            }
            else if (currentEnergy > 80f)
            {
                if (nearbyPrey.Count > 0 && nearbyPredators.Count > 1 && enablePackHunting)
                    currentState = PredatorState.PackHunting;
                else if (nearbyPrey.Count > 0)
                    currentState = PredatorState.Hunting;
                else
                    currentState = PredatorState.Patrolling;
            }
            else
            {
                if (nearbyPrey.Count > 0)
                    currentState = PredatorState.Hunting;
                else
                    currentState = PredatorState.Patrolling;
            }
            
            // Update target prey
            if (nearbyPrey.Count > 0)
            {
                targetPrey = FindBestTarget(agent, nearbyPrey);
                if (targetPrey != null)
                    lastKnownPreyPosition = targetPrey.position;
            }
            else if (Vector3.Distance(agent.position, lastKnownPreyPosition) < 2f)
            {
                targetPrey = null; // Lost target
            }
        }
        
        void UpdateEnergy(Agent agent)
        {
            // Decay energy over time
            currentEnergy -= energyDecayRate * Time.deltaTime;
            
            // Extra drain when hunting
            if (currentState == PredatorState.Hunting || currentState == PredatorState.PackHunting)
            {
                currentEnergy -= huntingEnergyDrain * Time.deltaTime;
            }
            
            // Clamp energy
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
            
            // Update agent's energy for external systems
            agent.energy = currentEnergy;
            
            // Check for prey kills
            CheckForKill(agent);
        }
        
        Vector3 CalculateHuntingBehavior(Agent agent)
        {
            if (targetPrey == null)
                return CalculatePatrolBehavior(agent);
            
            // Pursue prey with prediction
            Vector3 pursuitForce = CalculatePursuit(agent, targetPrey);
            
            // Add some pack coordination if other predators nearby
            Vector3 packForce = Vector3.zero;
            if (enablePackHunting)
            {
                List<Agent> nearbyPredators = FindNearbyPredators(agent);
                if (nearbyPredators.Count > 0)
                {
                    packForce = CalculatePackCoordination(agent, nearbyPredators) * packCohesionWeight;
                }
            }
            
            return pursuitForce + packForce;
        }
        
        Vector3 CalculatePatrolBehavior(Agent agent)
        {
            // Wander behavior with territory awareness
            Vector3 wanderForce = CalculateWander(agent);
            
            // Stay near territory center
            Vector3 territoryForce = CalculateTerritoryAttraction(agent);
            
            return wanderForce + territoryForce * 0.3f;
        }
        
        Vector3 CalculateRestingBehavior(Agent agent)
        {
            // Move slowly, recover energy
            currentEnergy += 0.2f * Time.deltaTime; // Slow recovery
            
            Vector3 avoidanceForce = CalculateAvoidance(agent);
            Vector3 slowWander = CalculateWander(agent) * 0.3f;
            
            return avoidanceForce + slowWander;
        }
        
        Vector3 CalculatePackHuntingBehavior(Agent agent)
        {
            if (targetPrey == null)
                return CalculatePatrolBehavior(agent);
            
            List<Agent> packMembers = FindNearbyPredators(agent);
            
            // Coordinate attack patterns
            Vector3 huntForce = CalculatePursuit(agent, targetPrey);
            Vector3 packCoordination = CalculatePackCoordination(agent, packMembers);
            Vector3 flankingForce = CalculateFlankingPosition(agent, targetPrey, packMembers);
            
            return huntForce + packCoordination * packCohesionWeight + flankingForce;
        }
        
        Vector3 CalculateTerritorialBehavior(Agent agent)
        {
            Vector3 aggressionForce = Vector3.zero;
            
            List<Agent> nearbyPredators = FindNearbyPredators(agent);
            
            foreach (Agent otherPredator in nearbyPredators)
            {
                float distance = Vector3.Distance(agent.position, otherPredator.position);
                if (distance < territoryRadius && distance > 0)
                {
                    // Repel other predators
                    Vector3 repelDirection = (agent.position - otherPredator.position).normalized;
                    float strength = (territoryRadius - distance) / territoryRadius;
                    aggressionForce += repelDirection * strength;
                }
            }
            
            return aggressionForce;
        }
        
        Vector3 CalculatePursuit(Agent agent, Agent target)
        {
            if (target == null) return Vector3.zero;
            
            // Predict where prey will be
            float predictionTime = Vector3.Distance(agent.position, target.position) / agent.maxSpeed;
            Vector3 predictedPosition = target.position + target.velocity * predictionTime;
            
            return Seek(agent, predictedPosition);
        }
        
        Vector3 CalculatePackCoordination(Agent agent, List<Agent> packMembers)
        {
            if (packMembers.Count == 0) return Vector3.zero;
            
            Vector3 averagePosition = Vector3.zero;
            Vector3 averageVelocity = Vector3.zero;
            
            foreach (Agent member in packMembers)
            {
                averagePosition += member.position;
                averageVelocity += member.velocity;
            }
            
            averagePosition /= packMembers.Count;
            averageVelocity /= packMembers.Count;
            
            Vector3 cohesion = Seek(agent, averagePosition) * 0.3f;
            Vector3 alignment = (averageVelocity.normalized * agent.maxSpeed - agent.velocity) * 0.5f;
            
            return cohesion + alignment;
        }
        
        Vector3 CalculateFlankingPosition(Agent agent, Agent prey, List<Agent> packMembers)
        {
            if (prey == null || packMembers.Count == 0) return Vector3.zero;
            
            // Try to position for flanking attack
            Vector3 preyDirection = prey.velocity.normalized;
            Vector3 flankingDirection = Vector3.Cross(preyDirection, Vector3.up).normalized;
            
            // Alternate flanking sides based on agent ID
            int agentIndex = System.Array.IndexOf(packMembers.ToArray(), agent);
            if (agentIndex % 2 == 1)
                flankingDirection = -flankingDirection;
            
            Vector3 flankingPosition = prey.position + flankingDirection * 3f - preyDirection * 2f;
            
            return Seek(agent, flankingPosition) * 0.8f;
        }
        
        Vector3 CalculateWander(Agent agent)
        {
            Vector3 circleCenter = agent.velocity.normalized * 2f;
            Vector3 displacement = Random.insideUnitSphere;
            displacement.y = 0; // Keep on horizontal plane
            displacement.Normalize();
            displacement *= 1f;
            
            Vector3 wanderTarget = agent.position + circleCenter + displacement;
            return Seek(agent, wanderTarget);
        }
        
        Vector3 CalculateTerritoryAttraction(Agent agent)
        {
            // Assume territory center is at origin or can be set
            Vector3 territoryCenter = Vector3.zero; // This could be configurable
            float distanceFromCenter = Vector3.Distance(agent.position, territoryCenter);
            
            if (distanceFromCenter > territoryRadius * 0.7f)
            {
                return Seek(agent, territoryCenter) * 0.5f;
            }
            
            return Vector3.zero;
        }
        
        Vector3 CalculateAvoidance(Agent agent)
        {
            Vector3 avoidanceForce = Vector3.zero;
            
            foreach (Agent neighbor in agent.neighbors)
            {
                if (neighbor.agentType == AgentType.Predator)
                {
                    float distance = Vector3.Distance(agent.position, neighbor.position);
                    if (distance < 2f && distance > 0)
                    {
                        Vector3 diff = agent.position - neighbor.position;
                        diff.Normalize();
                        diff /= distance; // Weight by distance
                        avoidanceForce += diff;
                    }
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
        
        List<Agent> FindNearbyPrey(Agent agent)
        {
            return agent.neighbors.Where(a => 
                a.agentType == AgentType.Prey && 
                Vector3.Distance(a.position, agent.position) <= huntingRadius
            ).ToList();
        }
        
        List<Agent> FindNearbyPredators(Agent agent)
        {
            return agent.neighbors.Where(a => 
                a.agentType == AgentType.Predator && 
                a != agent &&
                Vector3.Distance(a.position, agent.position) <= packRadius
            ).ToList();
        }
        
        Agent FindBestTarget(Agent agent, List<Agent> preyList)
        {
            if (preyList.Count == 0) return null;
            
            // Prefer closest, weakest, or isolated prey
            Agent bestTarget = null;
            float bestScore = float.MaxValue;
            
            foreach (Agent prey in preyList)
            {
                float distance = Vector3.Distance(agent.position, prey.position);
                float healthFactor = prey.health / 100f; // Lower health = better target
                float isolationFactor = prey.neighbors.Count(n => n.agentType == AgentType.Prey); // Isolated = better
                
                float score = distance + healthFactor * 5f + isolationFactor * 2f;
                
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTarget = prey;
                }
            }
            
            return bestTarget;
        }
        
        void CheckForKill(Agent agent)
        {
            if (targetPrey == null) return;
            
            float distanceToTarget = Vector3.Distance(agent.position, targetPrey.position);
            
            if (distanceToTarget < attackRange)
            {
                // Attack the prey
                float damage = 25f + Random.Range(0f, 15f);
                targetPrey.health -= damage;
                
                if (targetPrey.health <= 0)
                {
                    // Prey killed
                    currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyFromKill);
                    targetPrey.Die();
                    targetPrey = null;
                    lastHuntTime = Time.time;
                }
            }
        }
        
        public float GetEnergyPercentage()
        {
            return currentEnergy / maxEnergy;
        }
        
        public PredatorState GetCurrentState()
        {
            return currentState;
        }
        
        public Agent GetCurrentTarget()
        {
            return targetPrey;
        }
    }
    
    public enum PredatorState
    {
        Hunting,
        Patrolling,
        Resting,
        PackHunting
    }
}