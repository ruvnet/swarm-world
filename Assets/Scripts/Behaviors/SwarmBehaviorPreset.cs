using UnityEngine;

namespace SwarmAI.Behaviors
{
    [CreateAssetMenu(fileName = "New Swarm Behavior", menuName = "Swarm AI/Behavior Preset")]
    public class SwarmBehaviorPreset : ScriptableObject
    {
        [Header("Behavior Preset Information")]
        [SerializeField] private string presetName = "Default Behavior";
        [TextArea(3, 5)]
        [SerializeField] private string description = "A basic swarm behavior preset";
        [SerializeField] private Sprite previewIcon;
        
        [Header("Movement Parameters")]
        [SerializeField] private float maxSpeed = 5f;
        [SerializeField] private float maxForce = 10f;
        [SerializeField] private float perceptionRadius = 5f;
        
        [Header("Behavior Weights")]
        [Range(0f, 5f)]
        [SerializeField] private float separationWeight = 1.5f;
        [Range(0f, 5f)]
        [SerializeField] private float alignmentWeight = 1.0f;
        [Range(0f, 5f)]
        [SerializeField] private float cohesionWeight = 1.0f;
        
        [Header("Advanced Settings")]
        [SerializeField] private bool useObstacleAvoidance = false;
        [SerializeField] private float obstacleAvoidanceDistance = 3f;
        [SerializeField] private LayerMask obstacleLayer = 1;
        
        [Header("Performance Settings")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodDistance = 50f;
        [SerializeField] private int updateFrequency = 30;
        
        // Public properties
        public string PresetName => presetName;
        public string Description => description;
        public Sprite PreviewIcon => previewIcon;
        public float MaxSpeed => maxSpeed;
        public float MaxForce => maxForce;
        public float PerceptionRadius => perceptionRadius;
        public float SeparationWeight => separationWeight;
        public float AlignmentWeight => alignmentWeight;
        public float CohesionWeight => cohesionWeight;
        public bool UseObstacleAvoidance => useObstacleAvoidance;
        public float ObstacleAvoidanceDistance => obstacleAvoidanceDistance;
        public LayerMask ObstacleLayer => obstacleLayer;
        public bool EnableLOD => enableLOD;
        public float LodDistance => lodDistance;
        public int UpdateFrequency => updateFrequency;
        
        /// <summary>
        /// Apply this preset to a SwarmAgent
        /// </summary>
        public void ApplyToAgent(SwarmAgent agent)
        {
            if (agent == null) return;
            
            agent.SetMovementParameters(maxSpeed, maxForce);
            agent.SetPerceptionRadius(perceptionRadius);
            agent.SetBehaviorWeights(separationWeight, alignmentWeight, cohesionWeight);
        }
        
        /// <summary>
        /// Apply this preset to all agents managed by a SwarmManager
        /// </summary>
        public void ApplyToSwarm(SwarmManager manager)
        {
            if (manager == null) return;
            
            var agents = FindObjectsOfType<SwarmAgent>();
            foreach (var agent in agents)
            {
                ApplyToAgent(agent);
            }
            
            manager.SetPerformanceSettings(1000, updateFrequency, enableLOD);
        }
        
        /// <summary>
        /// Create a copy of this preset with modified values
        /// </summary>
        public SwarmBehaviorPreset CreateVariant(string variantName)
        {
            var variant = CreateInstance<SwarmBehaviorPreset>();
            variant.presetName = variantName;
            variant.description = $"Variant of {presetName}";
            variant.maxSpeed = maxSpeed;
            variant.maxForce = maxForce;
            variant.perceptionRadius = perceptionRadius;
            variant.separationWeight = separationWeight;
            variant.alignmentWeight = alignmentWeight;
            variant.cohesionWeight = cohesionWeight;
            variant.useObstacleAvoidance = useObstacleAvoidance;
            variant.obstacleAvoidanceDistance = obstacleAvoidanceDistance;
            variant.obstacleLayer = obstacleLayer;
            variant.enableLOD = enableLOD;
            variant.lodDistance = lodDistance;
            variant.updateFrequency = updateFrequency;
            
            return variant;
        }
        
        /// <summary>
        /// Validate the preset parameters
        /// </summary>
        public bool IsValid()
        {
            return maxSpeed > 0 && maxForce > 0 && perceptionRadius > 0 &&
                   separationWeight >= 0 && alignmentWeight >= 0 && cohesionWeight >= 0;
        }
        
        /// <summary>
        /// Get a performance score estimate for this preset
        /// </summary>
        public float GetPerformanceScore()
        {
            float score = 100f;
            
            // Penalize high perception radius (expensive neighbor queries)
            if (perceptionRadius > 10f) score -= (perceptionRadius - 10f) * 2f;
            
            // Penalize high update frequency
            if (updateFrequency > 60) score -= (updateFrequency - 60) * 0.5f;
            
            // Bonus for LOD
            if (enableLOD) score += 10f;
            
            // Bonus for obstacle avoidance if enabled
            if (useObstacleAvoidance) score -= 5f; // Slight penalty for complexity
            
            return Mathf.Clamp(score, 0f, 100f);
        }
    }
}