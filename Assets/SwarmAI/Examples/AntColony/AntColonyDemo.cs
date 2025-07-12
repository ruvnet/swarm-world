using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SwarmAI.Examples
{
    /// <summary>
    /// Ant Colony Simulation with pheromone trails demonstrating emergent pathfinding
    /// Features: Dynamic pheromone trails, food sources, colony management, real-time statistics
    /// </summary>
    public class AntColonyDemo : MonoBehaviour
    {
        [Header("Colony Configuration")]
        public SwarmManager swarmManager;
        public Transform colony;
        public GameObject antPrefab;
        public int antCount = 60;
        
        [Header("Environment")]
        public GameObject foodSourcePrefab;
        public int foodSourceCount = 5;
        public Vector3 environmentSize = new Vector3(40, 0, 40);
        public float minFoodDistance = 8f;
        
        [Header("Pheromone System")]
        public PheromoneManager pheromoneManager;
        public bool visualizePheromones = true;
        public float pheromoneLifetime = 30f;
        
        [Header("UI Controls")]
        public Canvas controlsUI;
        public Slider antCountSlider;
        public Slider pheromoneLifetimeSlider;
        public Toggle visualizePheromoneToggle;
        public Button resetButton;
        public Button addFoodButton;
        public Text statisticsText;
        
        [Header("Visual Effects")]
        public ParticleSystem antSpawnEffect;
        public LineRenderer pathVisualization;
        public Material antMaterial;
        
        private List<FoodSource> foodSources = new List<FoodSource>();
        private Dictionary<Agent, AntBehavior> antBehaviors = new Dictionary<Agent, AntBehavior>();
        private float lastStatUpdate = 0f;
        private int totalFoodCollected = 0;
        
        // Statistics tracking
        private float sessionStartTime;
        private List<Vector3> bestPath = new List<Vector3>();
        
        void Start()
        {
            sessionStartTime = Time.time;
            InitializeDemo();
            SetupUI();
            CreateEnvironment();
            SpawnAnts();
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
            
            // Configure swarm for ant simulation
            swarmManager.initialAgentCount = 0;
            swarmManager.boundarySize = environmentSize;
            swarmManager.useSpatialHashing = true;
            swarmManager.spatialHashCellSize = 3f;
            
            // Setup pheromone manager
            if (pheromoneManager == null)
            {
                GameObject pheromoneGO = new GameObject("PheromoneManager");
                pheromoneManager = pheromoneGO.AddComponent<PheromoneManager>();
            }
            
            pheromoneManager.pheromoneLifetime = pheromoneLifetime;
            pheromoneManager.visualizePheromones = visualizePheromones;
            
            // Create colony if not assigned
            if (colony == null)
            {
                colony = CreateColony();
            }
        }
        
        Transform CreateColony()
        {
            GameObject colonyObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            colonyObj.name = "AntColony";
            colonyObj.transform.position = Vector3.zero;
            colonyObj.transform.localScale = new Vector3(3f, 0.5f, 3f);
            
            Renderer renderer = colonyObj.GetComponent<Renderer>();
            renderer.material.color = Color.red;
            
            // Add colony marker
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.SetParent(colonyObj.transform);
            marker.transform.localPosition = Vector3.up * 0.5f;
            marker.transform.localScale = Vector3.one * 0.5f;
            marker.GetComponent<Renderer>().material.color = Color.yellow;
            
            return colonyObj.transform;
        }
        
        void SetupUI()
        {
            if (controlsUI == null) return;
            
            if (antCountSlider != null)
            {
                antCountSlider.value = antCount;
                antCountSlider.onValueChanged.AddListener(OnAntCountChanged);
            }
            
            if (pheromoneLifetimeSlider != null)
            {
                pheromoneLifetimeSlider.value = pheromoneLifetime;
                pheromoneLifetimeSlider.onValueChanged.AddListener(OnPheromoneLifetimeChanged);
            }
            
            if (visualizePheromoneToggle != null)
            {
                visualizePheromoneToggle.isOn = visualizePheromones;
                visualizePheromoneToggle.onValueChanged.AddListener(OnVisualizePheromoneToggled);
            }
            
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetSimulation);
                
            if (addFoodButton != null)
                addFoodButton.onClick.AddListener(AddRandomFoodSource);
        }
        
        void CreateEnvironment()
        {
            // Clear existing food sources
            foreach (FoodSource food in foodSources)
            {
                if (food != null)
                    DestroyImmediate(food.gameObject);
            }
            foodSources.Clear();
            
            // Create food sources
            for (int i = 0; i < foodSourceCount; i++)
            {
                CreateFoodSource();
            }
            
            // Create ground plane
            CreateGround();
        }
        
        void CreateFoodSource()
        {
            Vector3 position;
            int attempts = 0;
            
            do
            {
                position = new Vector3(
                    Random.Range(-environmentSize.x * 0.4f, environmentSize.x * 0.4f),
                    0.5f,
                    Random.Range(-environmentSize.z * 0.4f, environmentSize.z * 0.4f)
                );
                attempts++;
            }
            while (Vector3.Distance(position, colony.position) < minFoodDistance && attempts < 50);
            
            GameObject foodObj;
            if (foodSourcePrefab != null)
            {
                foodObj = Instantiate(foodSourcePrefab, position, Quaternion.identity);
            }
            else
            {
                foodObj = CreateDefaultFoodSource(position);
            }
            
            FoodSource foodSource = foodObj.GetComponent<FoodSource>();
            if (foodSource == null)
                foodSource = foodObj.AddComponent<FoodSource>();
                
            foodSources.Add(foodSource);
        }
        
        GameObject CreateDefaultFoodSource(Vector3 position)
        {
            GameObject food = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            food.name = "FoodSource";
            food.transform.position = position;
            food.transform.localScale = Vector3.one * 0.8f;
            
            Renderer renderer = food.GetComponent<Renderer>();
            renderer.material.color = Color.yellow;
            
            return food;
        }
        
        void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = environmentSize * 0.1f; // Plane is 10x10 by default
            ground.GetComponent<Renderer>().material.color = new Color(0.8f, 0.7f, 0.5f, 1f); // Sandy color
        }
        
        void SpawnAnts()
        {
            // Clear existing ants
            swarmManager.ClearSwarm();
            antBehaviors.Clear();
            
            GameObject prefabToUse = antPrefab != null ? antPrefab : CreateDefaultAntPrefab();
            swarmManager.agentPrefab = prefabToUse;
            
            for (int i = 0; i < antCount; i++)
            {
                // Spawn near colony
                Vector3 spawnPos = colony.position + Random.insideUnitSphere * 2f;
                spawnPos.y = 0.1f;
                
                Agent ant = swarmManager.SpawnAgent(spawnPos, AgentType.Worker);
                
                // Configure ant properties
                ant.maxSpeed = Random.Range(2f, 4f);
                ant.maxForce = Random.Range(1f, 3f);
                ant.neighborRadius = Random.Range(1f, 2f);
                
                // Add ant behavior
                AntBehavior antBehavior = new AntBehavior();
                antBehavior.SetColony(colony);
                ant.AddBehavior(antBehavior);
                
                antBehaviors[ant] = antBehavior;
                
                // Visual setup
                SetupAntVisual(ant);
            }
            
            // Play spawn effect
            if (antSpawnEffect != null)
            {
                antSpawnEffect.transform.position = colony.position;
                antSpawnEffect.Play();
            }
        }
        
        GameObject CreateDefaultAntPrefab()
        {
            GameObject ant = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ant.name = "Ant";
            ant.transform.localScale = new Vector3(0.15f, 0.05f, 0.3f);
            
            // Add collider for detection
            SphereCollider trigger = ant.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1f;
            
            return ant;
        }
        
        void SetupAntVisual(Agent ant)
        {
            Renderer renderer = ant.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (antMaterial != null)
                {
                    renderer.material = antMaterial;
                }
                else
                {
                    renderer.material.color = Color.black;
                }
            }
            
            // Setup trail
            TrailRenderer trail = ant.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = ant.gameObject.AddComponent<TrailRenderer>();
                
            trail.time = 2f;
            trail.startWidth = 0.02f;
            trail.endWidth = 0.01f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.material.color = new Color(0.5f, 0.3f, 0.1f, 0.7f); // Brown trail
        }
        
        void Update()
        {
            UpdateStatistics();
            UpdatePathVisualization();
            CheckFoodCollection();
            
            // Dynamic environment changes
            if (Random.Range(0f, 1f) < 0.001f) // Very rare
            {
                CreateFoodSource();
            }
        }
        
        void UpdateStatistics()
        {
            if (Time.time - lastStatUpdate < 1f) return; // Update once per second
            lastStatUpdate = Time.time;
            
            if (statisticsText == null) return;
            
            int antsSearching = 0;
            int antsReturning = 0;
            int antsFollowingTrail = 0;
            
            foreach (var kvp in antBehaviors)
            {
                switch (kvp.Value.currentState)
                {
                    case AntState.SearchingFood:
                        antsSearching++;
                        break;
                    case AntState.ReturningToColony:
                        antsReturning++;
                        break;
                    case AntState.FollowingTrail:
                        antsFollowingTrail++;
                        break;
                }
            }
            
            int activeFoodSources = 0;
            int totalFoodAvailable = 0;
            foreach (FoodSource food in foodSources)
            {
                if (food.HasFood())
                {
                    activeFoodSources++;
                    totalFoodAvailable += food.currentFood;
                }
            }
            
            float sessionTime = Time.time - sessionStartTime;
            int foodTrailCount = pheromoneManager.GetPheromoneCount(PheromoneType.FoodTrail);
            int colonyTrailCount = pheromoneManager.GetPheromoneCount(PheromoneType.ColonyTrail);
            
            statisticsText.text = $"=== ANT COLONY STATISTICS ===\\n" +
                                 $"Session Time: {sessionTime:F0}s\\n" +
                                 $"Total Ants: {swarmManager.agents.Count}\\n" +
                                 $"  • Searching: {antsSearching}\\n" +
                                 $"  • Returning: {antsReturning}\\n" +
                                 $"  • Following Trail: {antsFollowingTrail}\\n\\n" +
                                 $"Food Sources: {activeFoodSources}/{foodSources.Count}\\n" +
                                 $"Total Food: {totalFoodAvailable}\\n" +
                                 $"Food Collected: {totalFoodCollected}\\n\\n" +
                                 $"Pheromone Trails:\\n" +
                                 $"  • Food: {foodTrailCount}\\n" +
                                 $"  • Colony: {colonyTrailCount}\\n\\n" +
                                 $"FPS: {(1.0f / Time.deltaTime):F1}";
        }
        
        void UpdatePathVisualization()
        {
            // Optional: Visualize most successful paths
            if (pathVisualization == null) return;
            
            // Find ants currently returning to colony (successful food gathering)
            List<Vector3> activePath = new List<Vector3>();
            
            foreach (var kvp in antBehaviors)
            {
                if (kvp.Value.currentState == AntState.ReturningToColony)
                {
                    activePath.Add(kvp.Key.position);
                }
            }
            
            if (activePath.Count > 1)
            {
                pathVisualization.positionCount = activePath.Count;
                pathVisualization.SetPositions(activePath.ToArray());
            }
            else
            {
                pathVisualization.positionCount = 0;
            }
        }
        
        void CheckFoodCollection()
        {
            // Track when ants successfully return with food
            foreach (var kvp in antBehaviors)
            {
                Agent ant = kvp.Key;
                AntBehavior behavior = kvp.Value;
                
                if (behavior.currentState == AntState.ReturningToColony &&
                    Vector3.Distance(ant.position, colony.position) < 1.5f)
                {
                    // Ant reached colony with food
                    totalFoodCollected++;
                    // This would normally be handled by the ant behavior state machine
                }
            }
        }
        
        // UI Event Handlers
        void OnAntCountChanged(float value)
        {
            int newCount = Mathf.RoundToInt(value);
            if (newCount != antCount)
            {
                antCount = newCount;
                SpawnAnts();
            }
        }
        
        void OnPheromoneLifetimeChanged(float value)
        {
            pheromoneLifetime = value;
            if (pheromoneManager != null)
                pheromoneManager.pheromoneLifetime = value;
        }
        
        void OnVisualizePheromoneToggled(bool value)
        {
            visualizePheromones = value;
            if (pheromoneManager != null)
                pheromoneManager.visualizePheromones = value;
        }
        
        public void ResetSimulation()
        {
            sessionStartTime = Time.time;
            totalFoodCollected = 0;
            
            if (pheromoneManager != null)
                pheromoneManager.ClearPheromones();
                
            CreateEnvironment();
            SpawnAnts();
        }
        
        public void AddRandomFoodSource()
        {
            CreateFoodSource();
        }
        
        public void RemoveFoodSource()
        {
            if (foodSources.Count > 1)
            {
                FoodSource lastFood = foodSources[foodSources.Count - 1];
                foodSources.RemoveAt(foodSources.Count - 1);
                if (lastFood != null)
                    DestroyImmediate(lastFood.gameObject);
            }
        }
        
        public void SetPheromoneVisualization(bool enabled)
        {
            visualizePheromones = enabled;
            if (pheromoneManager != null)
                pheromoneManager.visualizePheromones = enabled;
        }
        
        void OnDrawGizmos()
        {
            // Draw environment boundary
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, environmentSize);
            
            // Draw colony detection area
            if (colony != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(colony.position, 1.5f);
            }
        }
    }
}