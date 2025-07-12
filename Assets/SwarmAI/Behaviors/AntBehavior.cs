using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI
{
    /// <summary>
    /// Ant behavior implementing pheromone-based pathfinding and colony simulation
    /// </summary>
    [System.Serializable]
    public class AntBehavior : BaseBehavior
    {
        [Header("Ant States")]
        public AntState currentState = AntState.SearchingFood;
        
        [Header("Movement Parameters")]
        public float wanderStrength = 2f;
        public float pheromoneFollowStrength = 3f;
        public float returnToColonyStrength = 5f;
        
        [Header("Pheromone Settings")]
        public float pheromoneDropRate = 1f;
        public float pheromoneDecayRate = 0.98f;
        public float pheromoneDetectionRadius = 2f;
        
        [Header("Targets")]
        public Transform colony;
        public Transform targetFood;
        
        private Vector3 wanderTarget;
        private float wanderRadius = 5f;
        private float wanderDistance = 10f;
        private float wanderJitter = 1f;
        
        public AntBehavior()
        {
            behaviorName = "Ant Colony";
            wanderTarget = Random.insideUnitSphere * wanderRadius;
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            Vector3 force = Vector3.zero;
            
            switch (currentState)
            {
                case AntState.SearchingFood:
                    force = CalculateSearchBehavior(agent);
                    break;
                    
                case AntState.ReturningToColony:
                    force = CalculateReturnBehavior(agent);
                    break;
                    
                case AntState.FollowingTrail:
                    force = CalculateTrailFollowBehavior(agent);
                    break;
            }
            
            // Always drop pheromones
            DropPheromone(agent);
            
            return force;
        }
        
        Vector3 CalculateSearchBehavior(Agent agent)
        {
            Vector3 wander = CalculateWander(agent);
            Vector3 pheromoneFollow = FollowPheromones(agent, PheromoneType.FoodTrail);
            
            // Check for food
            CheckForFood(agent);
            
            return wander * wanderStrength + pheromoneFollow * pheromoneFollowStrength;
        }
        
        Vector3 CalculateReturnBehavior(Agent agent)
        {
            if (colony == null) return Vector3.zero;
            
            Vector3 returnForce = Seek(agent, colony.position) * returnToColonyStrength;
            Vector3 pheromoneFollow = FollowPheromones(agent, PheromoneType.ColonyTrail);
            
            // Check if reached colony
            if (Vector3.Distance(agent.position, colony.position) < 1f)
            {
                currentState = AntState.SearchingFood;
                targetFood = null;
            }
            
            return returnForce + pheromoneFollow * pheromoneFollowStrength;
        }
        
        Vector3 CalculateTrailFollowBehavior(Agent agent)
        {
            Vector3 trailForce = FollowPheromones(agent, PheromoneType.FoodTrail);
            
            if (trailForce.magnitude < 0.1f)
            {
                // Lost trail, return to searching
                currentState = AntState.SearchingFood;
                return CalculateSearchBehavior(agent);
            }
            
            return trailForce * pheromoneFollowStrength;
        }
        
        Vector3 CalculateWander(Agent agent)
        {
            // Update wander target
            wanderTarget += new Vector3(
                Random.Range(-1f, 1f) * wanderJitter,
                Random.Range(-1f, 1f) * wanderJitter,
                Random.Range(-1f, 1f) * wanderJitter
            );
            wanderTarget.Normalize();
            wanderTarget *= wanderRadius;
            
            Vector3 targetInWorldSpace = agent.position + agent.velocity.normalized * wanderDistance + wanderTarget;
            
            return Seek(agent, targetInWorldSpace);
        }
        
        Vector3 FollowPheromones(Agent agent, PheromoneType type)
        {
            PheromoneManager pheromoneManager = Object.FindObjectOfType<PheromoneManager>();
            if (pheromoneManager == null) return Vector3.zero;
            
            Vector3 pheromoneForce = Vector3.zero;
            float totalStrength = 0f;
            
            List<Pheromone> nearbyPheromones = pheromoneManager.GetPheromonesInRadius(
                agent.position, pheromoneDetectionRadius, type);
            
            foreach (Pheromone pheromone in nearbyPheromones)
            {
                Vector3 direction = (pheromone.position - agent.position).normalized;
                float strength = pheromone.strength;
                
                pheromoneForce += direction * strength;
                totalStrength += strength;
            }
            
            if (totalStrength > 0)
            {
                pheromoneForce /= totalStrength;
                pheromoneForce.Normalize();
                pheromoneForce *= agent.maxSpeed;
                
                Vector3 steer = pheromoneForce - agent.velocity;
                return Vector3.ClampMagnitude(steer, agent.maxForce);
            }
            
            return Vector3.zero;
        }
        
        void DropPheromone(Agent agent)
        {
            PheromoneManager pheromoneManager = Object.FindObjectOfType<PheromoneManager>();
            if (pheromoneManager == null) return;
            
            PheromoneType typeToDropa = currentState == AntState.ReturningToColony ? 
                PheromoneType.FoodTrail : PheromoneType.ColonyTrail;
            
            pheromoneManager.DropPheromone(agent.position, typeToDropa, pheromoneDropRate);
        }
        
        void CheckForFood(Agent agent)
        {
            FoodSource[] foodSources = Object.FindObjectsOfType<FoodSource>();
            
            foreach (FoodSource food in foodSources)
            {
                if (Vector3.Distance(agent.position, food.transform.position) < food.detectionRadius)
                {
                    if (food.ConsumeFood())
                    {
                        targetFood = food.transform;
                        currentState = AntState.ReturningToColony;
                        break;
                    }
                }
            }
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
        
        public void SetColony(Transform colonyTransform)
        {
            colony = colonyTransform;
        }
        
        public void SetState(AntState newState)
        {
            currentState = newState;
        }
    }
    
    public enum AntState
    {
        SearchingFood,
        ReturningToColony,
        FollowingTrail,
        AtColony
    }
}