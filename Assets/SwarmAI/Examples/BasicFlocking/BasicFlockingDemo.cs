using UnityEngine;
using UnityEngine.UI;

namespace SwarmAI.Examples
{
    /// <summary>
    /// Basic Flocking Demo showcasing classic boids behavior with 50-100 agents
    /// Demonstrates: Separation, Alignment, Cohesion with real-time parameter tuning
    /// </summary>
    public class BasicFlockingDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        public SwarmManager swarmManager;
        public GameObject birdPrefab;
        public int agentCount = 75;
        
        [Header("Flocking Parameters")]
        [Range(0f, 3f)] public float separationWeight = 1.5f;
        [Range(0f, 3f)] public float alignmentWeight = 1.0f;
        [Range(0f, 3f)] public float cohesionWeight = 1.0f;
        
        [Header("Visual Settings")]
        public Material birdMaterial;
        public Material fishMaterial;
        public bool isFishMode = false;
        public Color[] agentColors = { Color.blue, Color.cyan, Color.white, Color.yellow };
        
        [Header("Environment")]
        public Transform water; // For fish mode
        public ParticleSystem bubbles; // For underwater effect
        public Light sunlight;
        
        [Header("UI Controls")]
        public Canvas controlsUI;
        public Slider separationSlider;
        public Slider alignmentSlider;
        public Slider cohesionSlider;
        public Slider agentCountSlider;
        public Toggle fishModeToggle;
        public Text performanceText;
        
        private FlockingBehavior flockingBehavior;
        private float lastUpdateTime;
        
        void Start()
        {
            InitializeDemo();
            SetupUI();
            SpawnAgents();
        }
        
        void InitializeDemo()
        {
            if (swarmManager == null)
                swarmManager = FindObjectOfType<SwarmManager>();
                
            if (swarmManager == null)
            {
                GameObject swarmGO = new GameObject("SwarmManager");
                swarmManager = swarmGO.AddComponent<SwarmManager>();
            }
            
            // Configure swarm manager for this demo
            swarmManager.initialAgentCount = 0; // We'll spawn manually
            swarmManager.boundarySize = new Vector3(30, 15, 30);
            swarmManager.useSpatialHashing = true;
            swarmManager.enableLOD = true;
            
            // Create flocking behavior
            flockingBehavior = new FlockingBehavior();
            flockingBehavior.separationWeight = separationWeight;
            flockingBehavior.alignmentWeight = alignmentWeight;
            flockingBehavior.cohesionWeight = cohesionWeight;
        }
        
        void SetupUI()
        {
            if (controlsUI == null) return;
            
            if (separationSlider != null)
            {
                separationSlider.value = separationWeight;
                separationSlider.onValueChanged.AddListener(OnSeparationChanged);
            }
            
            if (alignmentSlider != null)
            {
                alignmentSlider.value = alignmentWeight;
                alignmentSlider.onValueChanged.AddListener(OnAlignmentChanged);
            }
            
            if (cohesionSlider != null)
            {
                cohesionSlider.value = cohesionWeight;
                cohesionSlider.onValueChanged.AddListener(OnCohesionChanged);
            }
            
            if (agentCountSlider != null)
            {
                agentCountSlider.value = agentCount;
                agentCountSlider.onValueChanged.AddListener(OnAgentCountChanged);
            }
            
            if (fishModeToggle != null)
            {
                fishModeToggle.isOn = isFishMode;
                fishModeToggle.onValueChanged.AddListener(OnFishModeToggled);
            }
        }
        
        void SpawnAgents()
        {
            // Clear existing agents
            swarmManager.ClearSwarm();
            
            GameObject prefabToUse = isFishMode ? 
                (birdPrefab != null ? birdPrefab : CreateDefaultFishPrefab()) : 
                (birdPrefab != null ? birdPrefab : CreateDefaultBirdPrefab());
            
            swarmManager.agentPrefab = prefabToUse;
            
            for (int i = 0; i < agentCount; i++)
            {
                Agent agent = swarmManager.SpawnAgent();
                
                // Add flocking behavior to each agent
                FlockingBehavior agentFlocking = new FlockingBehavior();
                agentFlocking.separationWeight = separationWeight;
                agentFlocking.alignmentWeight = alignmentWeight;
                agentFlocking.cohesionWeight = cohesionWeight;
                agent.AddBehavior(agentFlocking);
                
                // Configure agent properties for demo
                agent.maxSpeed = Random.Range(3f, 7f);
                agent.maxForce = Random.Range(2f, 4f);
                agent.neighborRadius = Random.Range(2f, 4f);
                
                // Set visual properties
                SetAgentVisual(agent, i);
            }
            
            ConfigureEnvironment();
        }
        
        void SetAgentVisual(Agent agent, int index)
        {
            Renderer renderer = agent.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = isFishMode ? fishMaterial : birdMaterial;
                if (mat != null)
                {
                    renderer.material = mat;
                }
                
                // Assign color variation
                Color baseColor = agentColors[index % agentColors.Length];
                renderer.material.color = baseColor + Random.ColorHSV(0, 0, 0, 0, 0.8f, 1f);
            }
            
            // Configure trail for movement visualization
            TrailRenderer trail = agent.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.time = isFishMode ? 2f : 1f;
                trail.startWidth = isFishMode ? 0.1f : 0.05f;
                trail.endWidth = 0.01f;
                
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { 
                        new GradientColorKey(renderer.material.color, 0.0f), 
                        new GradientColorKey(Color.clear, 1.0f) 
                    },
                    new GradientAlphaKey[] { 
                        new GradientAlphaKey(1.0f, 0.0f), 
                        new GradientAlphaKey(0.0f, 1.0f) 
                    }
                );
                trail.colorGradient = gradient;
            }
        }
        
        void ConfigureEnvironment()
        {
            if (isFishMode)
            {
                // Underwater environment
                if (water != null)
                    water.gameObject.SetActive(true);
                    
                if (bubbles != null)
                    bubbles.gameObject.SetActive(true);
                    
                if (sunlight != null)
                {
                    sunlight.color = new Color(0.4f, 0.8f, 1f, 1f); // Blue tint
                    sunlight.intensity = 0.6f;
                }
                
                Camera.main.backgroundColor = new Color(0.1f, 0.3f, 0.5f, 1f);
                RenderSettings.fogColor = new Color(0.2f, 0.4f, 0.6f, 1f);
                RenderSettings.fog = true;
                RenderSettings.fogStartDistance = 10f;
                RenderSettings.fogEndDistance = 50f;
            }
            else
            {
                // Sky environment
                if (water != null)
                    water.gameObject.SetActive(false);
                    
                if (bubbles != null)
                    bubbles.gameObject.SetActive(false);
                    
                if (sunlight != null)
                {
                    sunlight.color = Color.white;
                    sunlight.intensity = 1f;
                }
                
                Camera.main.backgroundColor = new Color(0.5f, 0.8f, 1f, 1f);
                RenderSettings.fogColor = new Color(0.8f, 0.9f, 1f, 1f);
                RenderSettings.fog = true;
                RenderSettings.fogStartDistance = 20f;
                RenderSettings.fogEndDistance = 100f;
            }
        }
        
        GameObject CreateDefaultBirdPrefab()
        {
            GameObject bird = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bird.name = "Bird";
            bird.transform.localScale = new Vector3(0.3f, 0.1f, 0.5f);
            
            // Add trail renderer
            TrailRenderer trail = bird.AddComponent<TrailRenderer>();
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.time = 1f;
            trail.startWidth = 0.05f;
            trail.endWidth = 0.01f;
            
            return bird;
        }
        
        GameObject CreateDefaultFishPrefab()
        {
            GameObject fish = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fish.name = "Fish";
            fish.transform.localScale = new Vector3(0.2f, 0.1f, 0.4f);
            
            // Add trail renderer for wake effect
            TrailRenderer trail = fish.AddComponent<TrailRenderer>();
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.time = 2f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.01f;
            
            return fish;
        }
        
        void Update()
        {
            UpdatePerformanceDisplay();
            
            // Real-time parameter updates
            if (Time.time - lastUpdateTime > 0.1f) // Update 10 times per second
            {
                UpdateFlockingParameters();
                lastUpdateTime = Time.time;
            }
        }
        
        void UpdateFlockingParameters()
        {
            foreach (Agent agent in swarmManager.agents)
            {
                foreach (IBehavior behavior in agent.behaviors)
                {
                    if (behavior is FlockingBehavior flocking)
                    {
                        flocking.separationWeight = separationWeight;
                        flocking.alignmentWeight = alignmentWeight;
                        flocking.cohesionWeight = cohesionWeight;
                    }
                }
            }
        }
        
        void UpdatePerformanceDisplay()
        {
            if (performanceText == null) return;
            
            performanceText.text = $"Agents: {swarmManager.agents.Count}\\n" +
                                  $"FPS: {(1.0f / Time.deltaTime):F1}\\n" +
                                  $"Mode: {(isFishMode ? "Fish" : "Birds")}\\n" +
                                  $"Sep: {separationWeight:F1} | Align: {alignmentWeight:F1} | Coh: {cohesionWeight:F1}";
        }
        
        // UI Event Handlers
        void OnSeparationChanged(float value)
        {
            separationWeight = value;
        }
        
        void OnAlignmentChanged(float value)
        {
            alignmentWeight = value;
        }
        
        void OnCohesionChanged(float value)
        {
            cohesionWeight = value;
        }
        
        void OnAgentCountChanged(float value)
        {
            int newCount = Mathf.RoundToInt(value);
            if (newCount != agentCount)
            {
                agentCount = newCount;
                SpawnAgents();
            }
        }
        
        void OnFishModeToggled(bool value)
        {
            if (value != isFishMode)
            {
                isFishMode = value;
                SpawnAgents();
            }
        }
        
        // Public methods for external control
        public void SetFlockingMode(bool fishMode)
        {
            isFishMode = fishMode;
            SpawnAgents();
        }
        
        public void AddAgent()
        {
            Agent agent = swarmManager.SpawnAgent();
            FlockingBehavior agentFlocking = new FlockingBehavior();
            agentFlocking.separationWeight = separationWeight;
            agentFlocking.alignmentWeight = alignmentWeight;
            agentFlocking.cohesionWeight = cohesionWeight;
            agent.AddBehavior(agentFlocking);
            
            SetAgentVisual(agent, swarmManager.agents.Count - 1);
        }
        
        public void RemoveAgent()
        {
            if (swarmManager.agents.Count > 1)
            {
                Agent lastAgent = swarmManager.agents[swarmManager.agents.Count - 1];
                lastAgent.Die();
            }
        }
        
        public void ResetDemo()
        {
            SpawnAgents();
        }
    }
}