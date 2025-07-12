using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI
{
    /// <summary>
    /// Manages formation assignments and coordination for formation flying demo
    /// </summary>
    public class FormationManager : MonoBehaviour
    {
        [Header("Formation Configuration")]
        public int maxFormationSize = 8;
        public float leaderSelectionRadius = 20f;
        public bool autoAssignFormations = true;
        
        [Header("Formation Types")]
        public FormationType defaultFormation = FormationType.VFormation;
        public float formationSpacing = 3f;
        
        private List<Formation> activeFormations = new List<Formation>();
        private Dictionary<Agent, Formation> agentFormationMap = new Dictionary<Agent, Formation>();
        
        void Start()
        {
            // Find existing agents and organize them
            if (autoAssignFormations)
            {
                OrganizeExistingAgents();
            }
        }
        
        void Update()
        {
            UpdateFormations();
            
            if (autoAssignFormations)
            {
                CheckForUnassignedAgents();
            }
        }
        
        void OrganizeExistingAgents()
        {
            SwarmManager swarmManager = FindObjectOfType<SwarmManager>();
            if (swarmManager == null) return;
            
            List<Agent> availableAgents = swarmManager.agents.Where(a => a.isAlive).ToList();
            
            while (availableAgents.Count > 0)
            {
                CreateFormationFromAgents(availableAgents);
            }
        }
        
        void CreateFormationFromAgents(List<Agent> availableAgents)
        {
            if (availableAgents.Count == 0) return;
            
            // Select leader (prefer agents with leader type or highest energy)
            Agent leader = SelectBestLeader(availableAgents);
            availableAgents.Remove(leader);
            
            // Create formation
            Formation newFormation = new Formation(leader, defaultFormation, formationSpacing);
            
            // Assign nearby agents to formation
            List<Agent> nearbyAgents = availableAgents
                .Where(a => Vector3.Distance(a.position, leader.position) < leaderSelectionRadius)
                .OrderBy(a => Vector3.Distance(a.position, leader.position))
                .Take(maxFormationSize - 1)
                .ToList();
            
            foreach (Agent agent in nearbyAgents)
            {
                newFormation.AddMember(agent);
                availableAgents.Remove(agent);
            }
            
            activeFormations.Add(newFormation);
            
            // Set up formation behaviors
            SetupFormationBehaviors(newFormation);
        }
        
        Agent SelectBestLeader(List<Agent> candidates)
        {
            if (candidates.Count == 0) return null;
            
            // Prefer designated leaders
            Agent designatedLeader = candidates.FirstOrDefault(a => a.agentType == AgentType.Leader);
            if (designatedLeader != null) return designatedLeader;
            
            // Otherwise select based on energy and position
            return candidates.OrderByDescending(a => a.energy)
                           .ThenBy(a => a.neighbors.Count) // Prefer central agents
                           .First();
        }
        
        void SetupFormationBehaviors(Formation formation)
        {
            // Setup leader behavior (different from followers)
            if (formation.leader != null)
            {
                formation.leader.agentType = AgentType.Leader;
                agentFormationMap[formation.leader] = formation;
            }
            
            // Setup follower behaviors
            for (int i = 0; i < formation.members.Count; i++)
            {
                Agent member = formation.members[i];
                
                // Remove existing formation behaviors
                var existingFormationBehaviors = member.behaviors.OfType<FormationBehavior>().ToList();
                foreach (var behavior in existingFormationBehaviors)
                {
                    member.RemoveBehavior(behavior);
                }
                
                // Add new formation behavior
                FormationBehavior formationBehavior = new FormationBehavior();
                formationBehavior.SetLeader(formation.leader);
                formationBehavior.SetFormationIndex(i + 1); // +1 because leader is index 0
                formationBehavior.SetFormationType(formation.formationType);
                formationBehavior.formationSpacing = formation.spacing;
                
                member.AddBehavior(formationBehavior);
                agentFormationMap[member] = formation;
            }
        }
        
        void UpdateFormations()
        {
            for (int i = activeFormations.Count - 1; i >= 0; i--)
            {
                Formation formation = activeFormations[i];
                
                // Check if leader is still alive
                if (formation.leader == null || !formation.leader.isAlive)
                {
                    HandleLeaderLoss(formation);
                    continue;
                }
                
                // Remove dead members
                formation.members.RemoveAll(m => m == null || !m.isAlive);
                
                // Check formation integrity
                CheckFormationIntegrity(formation);
                
                // Update formation statistics
                formation.UpdateStatistics();
            }
        }
        
        void HandleLeaderLoss(Formation formation)
        {
            if (formation.members.Count > 0)
            {
                // Promote a new leader
                Agent newLeader = SelectBestLeader(formation.members);
                formation.members.Remove(newLeader);
                formation.leader = newLeader;
                
                // Reconfigure formation
                SetupFormationBehaviors(formation);
            }
            else
            {
                // Disband formation
                activeFormations.Remove(formation);
            }
        }
        
        void CheckFormationIntegrity(Formation formation)
        {
            // Check if members are too far from leader
            float maxDistance = formation.spacing * 8f; // Generous threshold
            
            for (int i = formation.members.Count - 1; i >= 0; i--)
            {
                Agent member = formation.members[i];
                float distance = Vector3.Distance(member.position, formation.leader.position);
                
                if (distance > maxDistance)
                {
                    // Remove from formation
                    formation.members.RemoveAt(i);
                    agentFormationMap.Remove(member);
                    
                    // Remove formation behavior
                    var formationBehaviors = member.behaviors.OfType<FormationBehavior>().ToList();
                    foreach (var behavior in formationBehaviors)
                    {
                        member.RemoveBehavior(behavior);
                    }
                }
            }
            
            // If formation becomes too small, consider disbanding
            if (formation.members.Count < 2)
            {
                DisbandFormation(formation);
            }
        }
        
        void CheckForUnassignedAgents()
        {
            SwarmManager swarmManager = FindObjectOfType<SwarmManager>();
            if (swarmManager == null) return;
            
            List<Agent> unassignedAgents = swarmManager.agents
                .Where(a => a.isAlive && !agentFormationMap.ContainsKey(a))
                .ToList();
            
            foreach (Agent unassignedAgent in unassignedAgents)
            {
                // Try to join an existing formation
                Formation suitableFormation = FindSuitableFormation(unassignedAgent);
                
                if (suitableFormation != null && suitableFormation.members.Count < maxFormationSize - 1)
                {
                    JoinFormation(unassignedAgent, suitableFormation);
                }
                else if (unassignedAgents.Count >= 3) // Minimum for new formation
                {
                    // Create new formation with nearby unassigned agents
                    List<Agent> nearbyUnassigned = unassignedAgents
                        .Where(a => Vector3.Distance(a.position, unassignedAgent.position) < leaderSelectionRadius)
                        .ToList();
                    
                    if (nearbyUnassigned.Count >= 3)
                    {
                        CreateFormationFromAgents(nearbyUnassigned);
                        break; // Only create one formation per update
                    }
                }
            }
        }
        
        Formation FindSuitableFormation(Agent agent)
        {
            float closestDistance = float.MaxValue;
            Formation closestFormation = null;
            
            foreach (Formation formation in activeFormations)
            {
                if (formation.members.Count >= maxFormationSize - 1) continue;
                
                float distance = Vector3.Distance(agent.position, formation.leader.position);
                if (distance < leaderSelectionRadius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestFormation = formation;
                }
            }
            
            return closestFormation;
        }
        
        void JoinFormation(Agent agent, Formation formation)
        {
            formation.AddMember(agent);
            agentFormationMap[agent] = formation;
            
            // Setup formation behavior
            FormationBehavior formationBehavior = new FormationBehavior();
            formationBehavior.SetLeader(formation.leader);
            formationBehavior.SetFormationIndex(formation.members.Count);
            formationBehavior.SetFormationType(formation.formationType);
            formationBehavior.formationSpacing = formation.spacing;
            
            agent.AddBehavior(formationBehavior);
        }
        
        void DisbandFormation(Formation formation)
        {
            // Remove formation behaviors from all members
            foreach (Agent member in formation.members)
            {
                var formationBehaviors = member.behaviors.OfType<FormationBehavior>().ToList();
                foreach (var behavior in formationBehaviors)
                {
                    member.RemoveBehavior(behavior);
                }
                agentFormationMap.Remove(member);
            }
            
            // Remove leader mapping
            if (formation.leader != null)
            {
                agentFormationMap.Remove(formation.leader);
            }
            
            activeFormations.Remove(formation);
        }
        
        public Agent AssignToFormation(Agent agent)
        {
            Formation suitableFormation = FindSuitableFormation(agent);
            
            if (suitableFormation != null)
            {
                JoinFormation(agent, suitableFormation);
                return suitableFormation.leader;
            }
            
            return null;
        }
        
        public void CreateNewFormation(List<Agent> agents, FormationType type)
        {
            if (agents.Count < 2) return;
            
            Agent leader = SelectBestLeader(agents);
            agents.Remove(leader);
            
            Formation newFormation = new Formation(leader, type, formationSpacing);
            
            foreach (Agent agent in agents)
            {
                newFormation.AddMember(agent);
            }
            
            activeFormations.Add(newFormation);
            SetupFormationBehaviors(newFormation);
        }
        
        public void ChangeFormationType(Formation formation, FormationType newType)
        {
            formation.formationType = newType;
            
            // Update all formation behaviors
            foreach (Agent member in formation.members)
            {
                var formationBehavior = member.behaviors.OfType<FormationBehavior>().FirstOrDefault();
                if (formationBehavior != null)
                {
                    formationBehavior.SetFormationType(newType);
                }
            }
        }
        
        public List<Formation> GetActiveFormations()
        {
            return new List<Formation>(activeFormations);
        }
        
        public Formation GetAgentFormation(Agent agent)
        {
            agentFormationMap.TryGetValue(agent, out Formation formation);
            return formation;
        }
        
        public int GetTotalFormationsCount()
        {
            return activeFormations.Count;
        }
        
        public int GetTotalAgentsInFormation()
        {
            return activeFormations.Sum(f => f.members.Count + 1); // +1 for leader
        }
    }
    
    [System.Serializable]
    public class Formation
    {
        public Agent leader;
        public List<Agent> members = new List<Agent>();
        public FormationType formationType;
        public float spacing;
        
        // Statistics
        public float formationTightness = 1f;
        public float averageSpeed = 0f;
        public Vector3 centerOfMass;
        
        public Formation(Agent leader, FormationType type, float spacing)
        {
            this.leader = leader;
            this.formationType = type;
            this.spacing = spacing;
        }
        
        public void AddMember(Agent agent)
        {
            if (!members.Contains(agent))
            {
                members.Add(agent);
            }
        }
        
        public void RemoveMember(Agent agent)
        {
            members.Remove(agent);
        }
        
        public void UpdateStatistics()
        {
            if (leader == null || members.Count == 0) return;
            
            // Calculate center of mass
            centerOfMass = leader.position;
            foreach (Agent member in members)
            {
                centerOfMass += member.position;
            }
            centerOfMass /= (members.Count + 1);
            
            // Calculate formation tightness (how close to ideal positions)
            float totalDeviation = 0f;
            foreach (Agent member in members)
            {
                var formationBehavior = member.behaviors.OfType<FormationBehavior>().FirstOrDefault();
                if (formationBehavior != null)
                {
                    float deviation = Vector3.Distance(member.position, formationBehavior.GetTargetPosition());
                    totalDeviation += deviation;
                }
            }
            
            formationTightness = members.Count > 0 ? (spacing * 2f) / (totalDeviation / members.Count + 0.1f) : 1f;
            formationTightness = Mathf.Clamp01(formationTightness);
            
            // Calculate average speed
            averageSpeed = leader.velocity.magnitude;
            foreach (Agent member in members)
            {
                averageSpeed += member.velocity.magnitude;
            }
            averageSpeed /= (members.Count + 1);
        }
        
        public int GetTotalSize()
        {
            return members.Count + 1; // +1 for leader
        }
        
        public bool IsValid()
        {
            return leader != null && leader.isAlive && members.Count > 0;
        }
    }
}