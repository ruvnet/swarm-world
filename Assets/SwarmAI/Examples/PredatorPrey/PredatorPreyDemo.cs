using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI.Examples
{
    /// <summary>
    /// Predator-Prey Ecosystem Simulation demonstrating natural selection and population dynamics
    /// Features: Dynamic populations, energy systems, hunting behaviors, ecosystem balance
    /// </summary>
    public class PredatorPreyDemo : MonoBehaviour
    {
        [Header("Ecosystem Configuration")]
        public SwarmManager swarmManager;
        public int initialPreyCount = 80;
        public int initialPredatorCount = 12;
        public Vector3 ecosystemSize = new Vector3(50, 10, 50);
        
        [Header("Prefabs")]
        public GameObject preyPrefab;
        public GameObject predatorPrefab;
        
        [Header("Population Dynamics")]
        [Range(0f, 10f)] public float preyReproductionRate = 2f;
        [Range(0f, 5f)] public float predatorReproductionRate = 0.5f;
        public int maxPreyPopulation = 150;
        public int maxPredatorPopulation = 25;
        public float reproductionCheckInterval = 5f;
        
        [Header("Visual Settings")]
        public Material preyMaterial;
        public Material predatorMaterial;
        public Material weakPreyMaterial;
        public Material huntingPredatorMaterial;
        
        [Header("Environment")]
        public GameObject[] environmentObjects;
        public Color grassColor = Color.green;
        public Color dirtColor = Color.brown;
        
        [Header("UI Controls")]
        public Canvas controlsUI;
        public Slider preyReproductionSlider;
        public Slider predatorReproductionSlider;
        public Button addPreyButton;
        public Button addPredatorButton;
        public Button resetEcosystemButton;
        public Text populationText;
        public Text ecosystemStatsText;
        public LineRenderer populationGraph;
        
        // Population tracking
        private List<Agent> preyAgents = new List<Agent>();
        private List<Agent> predatorAgents = new List<Agent>();
        private float lastReproductionCheck = 0f;
        private float sessionStartTime;
        
        // Statistics
        private int totalBirths = 0;
        private int totalDeaths = 0;
        private int totalKills = 0;
        private List<float> preyPopulationHistory = new List<float>();
        private List<float> predatorPopulationHistory = new List<float>();
        private int maxHistoryLength = 100;
        
        // Dynamic balance
        private float ecosystemBalance = 0.5f; // 0 = predator advantage, 1 = prey advantage
        private float balanceUpdateInterval = 2f;
        private float lastBalanceUpdate = 0f;
        
        void Start()
        {
            sessionStartTime = Time.time;
            InitializeEcosystem();
            SetupUI();
            CreateEnvironment();
            SpawnInitialPopulations();
        }
        
        void InitializeEcosystem()
        {
            if (swarmManager == null)
                swarmManager = FindObjectOfType<SwarmManager>();
                
            if (swarmManager == null)
            {
                GameObject swarmGO = new GameObject("SwarmManager");
                swarmManager = swarmGO.AddComponent<SwarmManager>();
            }
            
            // Configure for ecosystem simulation
            swarmManager.initialAgentCount = 0;
            swarmManager.boundarySize = ecosystemSize;
            swarmManager.useSpatialHashing = true;
            swarmManager.spatialHashCellSize = 5f;
            swarmManager.enableLOD = true;
            swarmManager.lodDistance = 30f;
            
            // Subscribe to agent events
            swarmManager.OnAgentSpawned += OnAgentSpawned;
            swarmManager.OnAgentDestroyed += OnAgentDestroyed;
        }
        
        void SetupUI()
        {
            if (controlsUI == null) return;
            
            if (preyReproductionSlider != null)
            {
                preyReproductionSlider.value = preyReproductionRate;
                preyReproductionSlider.onValueChanged.AddListener(OnPreyReproductionChanged);
            }
            
            if (predatorReproductionSlider != null)
            {
                predatorReproductionSlider.value = predatorReproductionRate;
                predatorReproductionSlider.onValueChanged.AddListener(OnPredatorReproductionChanged);
            }
            
            if (addPreyButton != null)
                addPreyButton.onClick.AddListener(() => SpawnPrey(5));
                
            if (addPredatorButton != null)
                addPredatorButton.onClick.AddListener(() => SpawnPredator(2));
                
            if (resetEcosystemButton != null)
                resetEcosystemButton.onClick.AddListener(ResetEcosystem);
        }
        
        void CreateEnvironment()
        {
            // Create ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "EcosystemGround";
            ground.transform.localScale = ecosystemSize * 0.1f;
            ground.GetComponent<Renderer>().material.color = grassColor;
            
            // Add some environmental features
            CreateEnvironmentalFeatures();
        }
        
        void CreateEnvironmentalFeatures()
        {
            // Create rocks/obstacles for more interesting behavior
            int obstacleCount = Random.Range(5, 12);
            
            for (int i = 0; i < obstacleCount; i++)
            {
                GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = "Obstacle";
                obstacle.transform.position = new Vector3(
                    Random.Range(-ecosystemSize.x * 0.4f, ecosystemSize.x * 0.4f),
                    Random.Range(0.5f, 2f),
                    Random.Range(-ecosystemSize.z * 0.4f, ecosystemSize.z * 0.4f)
                );
                obstacle.transform.localScale = new Vector3(
                    Random.Range(1f, 3f),
                    Random.Range(0.5f, 2f),
                    Random.Range(1f, 3f)
                );
                obstacle.GetComponent<Renderer>().material.color = dirtColor;
            }
            
            // Create water sources
            CreateWaterSources();
        }
        
        void CreateWaterSources()
        {
            int waterCount = Random.Range(2, 5);
            
            for (int i = 0; i < waterCount; i++)
            {
                GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                water.name = "WaterSource";
                water.transform.position = new Vector3(
                    Random.Range(-ecosystemSize.x * 0.3f, ecosystemSize.x * 0.3f),
                    0.1f,
                    Random.Range(-ecosystemSize.z * 0.3f, ecosystemSize.z * 0.3f)
                );
                water.transform.localScale = new Vector3(
                    Random.Range(3f, 6f),
                    0.1f,
                    Random.Range(3f, 6f)
                );
                water.GetComponent<Renderer>().material.color = Color.blue;
                water.GetComponent<Renderer>().material.SetFloat("_Metallic", 0.8f);
            }
        }
        
        void SpawnInitialPopulations()
        {
            // Spawn prey
            SpawnPrey(initialPreyCount);
            
            // Spawn predators
            SpawnPredator(initialPredatorCount);
        }
        
        void SpawnPrey(int count)
        {
            for (int i = 0; i < count && preyAgents.Count < maxPreyPopulation; i++)
            {
                Vector3 spawnPos = GetRandomSpawnPosition();
                
                GameObject preyObj = preyPrefab != null ? 
                    Instantiate(preyPrefab, spawnPos, Quaternion.identity) :
                    CreateDefaultPreyPrefab(spawnPos);
                
                Agent preyAgent = preyObj.GetComponent<Agent>();
                if (preyAgent == null)
                    preyAgent = preyObj.AddComponent<Agent>();
                
                // Configure prey
                preyAgent.agentType = AgentType.Prey;
                preyAgent.maxSpeed = Random.Range(4f, 7f);
                preyAgent.maxForce = Random.Range(2f, 4f);
                preyAgent.neighborRadius = Random.Range(3f, 5f);
                preyAgent.health = Random.Range(80f, 100f);
                preyAgent.energy = Random.Range(60f, 100f);
                
                // Add prey behavior
                PreyBehavior preyBehavior = new PreyBehavior();
                preyAgent.AddBehavior(preyBehavior);
                
                // Add to swarm and tracking
                swarmManager.AddAgent(preyAgent);
                preyAgents.Add(preyAgent);
                
                // Set visual
                SetupPreyVisual(preyAgent);
                
                // Subscribe to death event
                preyAgent.OnAgentDied += OnPreyDied;
            }
        }
        
        void SpawnPredator(int count)
        {
            for (int i = 0; i < count && predatorAgents.Count < maxPredatorPopulation; i++)
            {
                Vector3 spawnPos = GetRandomSpawnPosition();
                
                GameObject predatorObj = predatorPrefab != null ? 
                    Instantiate(predatorPrefab, spawnPos, Quaternion.identity) :
                    CreateDefaultPredatorPrefab(spawnPos);
                
                Agent predatorAgent = predatorObj.GetComponent<Agent>();
                if (predatorAgent == null)
                    predatorAgent = predatorObj.AddComponent<Agent>();
                
                // Configure predator
                predatorAgent.agentType = AgentType.Predator;
                predatorAgent.maxSpeed = Random.Range(5f, 8f);
                predatorAgent.maxForce = Random.Range(3f, 6f);
                predatorAgent.neighborRadius = Random.Range(8f, 12f);
                predatorAgent.health = Random.Range(90f, 100f);
                predatorAgent.energy = Random.Range(70f, 100f);
                
                // Add predator behavior
                PredatorBehavior predatorBehavior = new PredatorBehavior();
                predatorAgent.AddBehavior(predatorBehavior);
                
                // Add to swarm and tracking
                swarmManager.AddAgent(predatorAgent);
                predatorAgents.Add(predatorAgent);
                
                // Set visual
                SetupPredatorVisual(predatorAgent);
                
                // Subscribe to death event
                predatorAgent.OnAgentDied += OnPredatorDied;
            }
        }
        
        GameObject CreateDefaultPreyPrefab(Vector3 position)
        {
            GameObject prey = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            prey.name = "Prey";
            prey.transform.position = position;
            prey.transform.localScale = new Vector3(0.3f, 0.2f, 0.5f);
            return prey;
        }
        
        GameObject CreateDefaultPredatorPrefab(Vector3 position)
        {
            GameObject predator = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            predator.name = "Predator";
            predator.transform.position = position;
            predator.transform.localScale = new Vector3(0.4f, 0.25f, 0.7f);
            return predator;
        }
        
        void SetupPreyVisual(Agent preyAgent)
        {
            Renderer renderer = preyAgent.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = preyMaterial != null ? preyMaterial : 
                    new Material(Shader.Find("Standard")) { color = Color.blue };
                renderer.material = material;
            }
            
            // Add trail
            TrailRenderer trail = preyAgent.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = preyAgent.gameObject.AddComponent<TrailRenderer>();
                
            trail.time = 1f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.02f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.material.color = new Color(0.3f, 0.6f, 1f, 0.6f);
        }
        
        void SetupPredatorVisual(Agent predatorAgent)
        {
            Renderer renderer = predatorAgent.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = predatorMaterial != null ? predatorMaterial : 
                    new Material(Shader.Find("Standard")) { color = Color.red };
                renderer.material = material;
            }
            
            // Add trail
            TrailRenderer trail = predatorAgent.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = predatorAgent.gameObject.AddComponent<TrailRenderer>();
                
            trail.time = 1.5f;
            trail.startWidth = 0.15f;
            trail.endWidth = 0.03f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.material.color = new Color(1f, 0.3f, 0.3f, 0.7f);
        }
        
        Vector3 GetRandomSpawnPosition()
        {
            return new Vector3(
                Random.Range(-ecosystemSize.x * 0.4f, ecosystemSize.x * 0.4f),
                0.5f,
                Random.Range(-ecosystemSize.z * 0.4f, ecosystemSize.z * 0.4f)
            );
        }
        
        void Update()
        {
            UpdatePopulationDynamics();
            UpdateEcosystemBalance();
            UpdateVisuals();
            UpdateStatistics();
            UpdatePopulationGraph();
        }
        
        void UpdatePopulationDynamics()
        {
            if (Time.time - lastReproductionCheck < reproductionCheckInterval) return;
            lastReproductionCheck = Time.time;
            
            // Prey reproduction
            if (preyAgents.Count > 0)
            {
                float preyReproductionChance = preyReproductionRate * Time.deltaTime * 
                    (ecosystemBalance * 2f); // Higher balance = more prey reproduction
                
                if (Random.Range(0f, 1f) < preyReproductionChance)
                {
                    SpawnPrey(Random.Range(1, 4));
                    totalBirths++;
                }
            }
            
            // Predator reproduction (requires successful hunting)
            if (predatorAgents.Count > 0 && preyAgents.Count > predatorAgents.Count * 3)
            {
                float predatorReproductionChance = predatorReproductionRate * Time.deltaTime * 
                    ((1f - ecosystemBalance) * 2f); // Lower balance = more predator reproduction
                
                if (Random.Range(0f, 1f) < predatorReproductionChance)
                {
                    SpawnPredator(1);
                    totalBirths++;
                }
            }
        }
        
        void UpdateEcosystemBalance()
        {
            if (Time.time - lastBalanceUpdate < balanceUpdateInterval) return;
            lastBalanceUpdate = Time.time;
            
            // Calculate ecosystem balance based on population ratios
            float totalPopulation = preyAgents.Count + predatorAgents.Count;
            if (totalPopulation > 0)
            {
                float preyRatio = (float)preyAgents.Count / totalPopulation;
                ecosystemBalance = Mathf.Lerp(ecosystemBalance, preyRatio, 0.1f);
            }
            
            // Apply environmental pressures based on balance
            ApplyEnvironmentalPressures();
        }
        
        void ApplyEnvironmentalPressures()
        {
            // If ecosystem is unbalanced, apply pressures
            if (ecosystemBalance < 0.2f) // Too many predators
            {
                // Reduce predator energy recovery
                foreach (Agent predator in predatorAgents)
                {
                    predator.energy = Mathf.Max(0, predator.energy - 0.5f);
                }
            }
            else if (ecosystemBalance > 0.9f) // Too many prey
            {
                // Increase predator hunting success
                foreach (Agent predator in predatorAgents)
                {
                    predator.energy = Mathf.Min(100, predator.energy + 0.3f);
                }
            }
        }
        
        void UpdateVisuals()
        {
            // Update prey visuals based on state
            foreach (Agent prey in preyAgents.ToList())
            {
                if (prey == null || !prey.isAlive) continue;
                
                Renderer renderer = prey.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // Color based on health/energy
                    float healthPercent = prey.health / 100f;
                    float energyPercent = prey.energy / 100f;
                    
                    Color baseColor = preyMaterial?.color ?? Color.blue;
                    if (healthPercent < 0.3f || energyPercent < 0.3f)
                    {
                        renderer.material.color = Color.Lerp(Color.gray, baseColor, healthPercent);
                    }
                    else
                    {
                        renderer.material.color = baseColor;
                    }
                }
            }
            
            // Update predator visuals based on state
            foreach (Agent predator in predatorAgents.ToList())
            {
                if (predator == null || !predator.isAlive) continue;
                
                Renderer renderer = predator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color baseColor = predatorMaterial?.color ?? Color.red;
                    
                    // Check if hunting
                    PredatorBehavior predBehavior = predator.behaviors.OfType<PredatorBehavior>().FirstOrDefault();
                    if (predBehavior != null && predBehavior.GetCurrentState() == PredatorState.Hunting)
                    {
                        renderer.material.color = Color.Lerp(baseColor, Color.yellow, 0.3f);
                    }
                    else
                    {
                        renderer.material.color = baseColor;
                    }
                }
            }
        }
        
        void UpdateStatistics()
        {
            if (populationText == null && ecosystemStatsText == null) return;
            
            // Population display
            if (populationText != null)
            {
                populationText.text = $"ECOSYSTEM POPULATIONS\\n" +
                                     $"Prey: {preyAgents.Count} / {maxPreyPopulation}\\n" +
                                     $"Predators: {predatorAgents.Count} / {maxPredatorPopulation}\\n" +
                                     $"Total: {preyAgents.Count + predatorAgents.Count}";
            }
            
            // Detailed statistics
            if (ecosystemStatsText != null)
            {
                float sessionTime = Time.time - sessionStartTime;
                float predatorRatio = predatorAgents.Count > 0 ? 
                    (float)preyAgents.Count / predatorAgents.Count : float.PositiveInfinity;
                
                int huntingPredators = 0;
                int fleeingPrey = 0;
                
                foreach (Agent predator in predatorAgents)
                {
                    PredatorBehavior predBehavior = predator.behaviors.OfType<PredatorBehavior>().FirstOrDefault();
                    if (predBehavior?.GetCurrentState() == PredatorState.Hunting)
                        huntingPredators++;
                }
                
                foreach (Agent prey in preyAgents)
                {
                    PreyBehavior preyBehavior = prey.behaviors.OfType<PreyBehavior>().FirstOrDefault();
                    if (preyBehavior?.GetCurrentState() == PreyState.Fleeing)
                        fleeingPrey++;
                }
                
                ecosystemStatsText.text = $"=== ECOSYSTEM STATISTICS ===\\n" +
                                         $"Session Time: {sessionTime:F0}s\\n" +
                                         $"Ecosystem Balance: {ecosystemBalance:P0}\\n" +
                                         $"Prey:Predator Ratio: {predatorRatio:F1}:1\\n\\n" +
                                         $"Activity:\\n" +
                                         $"  Hunting Predators: {huntingPredators}\\n" +
                                         $"  Fleeing Prey: {fleeingPrey}\\n\\n" +
                                         $"Totals:\\n" +
                                         $"  Births: {totalBirths}\\n" +
                                         $"  Deaths: {totalDeaths}\\n" +
                                         $"  Kills: {totalKills}\\n\\n" +
                                         $"Reproduction Rates:\\n" +
                                         $"  Prey: {preyReproductionRate:F1}\\n" +
                                         $"  Predator: {predatorReproductionRate:F1}";
            }
        }
        
        void UpdatePopulationGraph()
        {
            if (populationGraph == null) return;
            
            // Record population data
            preyPopulationHistory.Add(preyAgents.Count);
            predatorPopulationHistory.Add(predatorAgents.Count * 5); // Scale for visibility
            
            // Limit history length
            while (preyPopulationHistory.Count > maxHistoryLength)
            {
                preyPopulationHistory.RemoveAt(0);
                predatorPopulationHistory.RemoveAt(0);
            }
            
            // Update line renderer
            populationGraph.positionCount = preyPopulationHistory.Count;
            for (int i = 0; i < preyPopulationHistory.Count; i++)
            {
                Vector3 pos = new Vector3(i * 0.1f, preyPopulationHistory[i] * 0.01f, 0);
                populationGraph.SetPosition(i, pos);
            }
        }
        
        // Event handlers
        void OnAgentSpawned(Agent agent)
        {
            // Handled in spawn methods
        }
        
        void OnAgentDestroyed(Agent agent)
        {
            totalDeaths++;
        }
        
        void OnPreyDied(Agent prey)
        {
            preyAgents.Remove(prey);
            totalKills++;
        }
        
        void OnPredatorDied(Agent predator)
        {
            predatorAgents.Remove(predator);
        }
        
        // UI event handlers
        void OnPreyReproductionChanged(float value)
        {
            preyReproductionRate = value;
        }
        
        void OnPredatorReproductionChanged(float value)
        {
            predatorReproductionRate = value;
        }
        
        public void ResetEcosystem()
        {
            // Clear all agents
            swarmManager.ClearSwarm();
            preyAgents.Clear();
            predatorAgents.Clear();
            
            // Reset statistics
            totalBirths = 0;
            totalDeaths = 0;
            totalKills = 0;
            sessionStartTime = Time.time;
            ecosystemBalance = 0.5f;
            
            preyPopulationHistory.Clear();
            predatorPopulationHistory.Clear();
            
            // Respawn initial populations
            SpawnInitialPopulations();
        }
        
        void OnDrawGizmos()
        {
            // Draw ecosystem boundary
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, ecosystemSize);
            
            // Draw balance indicator
            Gizmos.color = Color.Lerp(Color.red, Color.green, ecosystemBalance);
            Gizmos.DrawWireSphere(Vector3.up * 10f, 2f);
        }
    }
}