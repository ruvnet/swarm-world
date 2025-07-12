using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI.Examples
{
    /// <summary>
    /// Crowd Simulation Demo with obstacle avoidance, goal seeking, and realistic pedestrian behavior
    /// Features: Pathfinding, social forces, panic situations, bottlenecks, emergency evacuation
    /// </summary>
    public class CrowdSimulationDemo : MonoBehaviour
    {
        [Header("Crowd Configuration")]
        public SwarmManager swarmManager;
        public int pedestrianCount = 120;
        public Vector3 simulationArea = new Vector3(40, 2, 40);
        
        [Header("Pedestrian Prefabs")]
        public GameObject pedestrianPrefab;
        public GameObject emergencyExitSign;
        
        [Header("Environment")]
        public GameObject[] obstaclePrefabs;
        public int obstacleCount = 15;
        public Transform[] destinations;
        public Transform[] emergencyExits;
        
        [Header("Crowd Behavior")]
        [Range(0f, 5f)] public float socialDistanceRadius = 1.2f;
        [Range(0f, 10f)] public float goalSeekingWeight = 3f;
        [Range(0f, 8f)] public float obstacleAvoidanceWeight = 6f;
        [Range(0f, 5f)] public float crowdFollowingWeight = 1.5f;
        
        [Header("Simulation Modes")]
        public SimulationMode currentMode = SimulationMode.Normal;
        public bool enablePanicMode = false;
        public float panicPropagationRadius = 8f;
        public float panicIntensity = 3f;
        
        [Header("Visual Settings")]
        public Material normalPedestrianMaterial;
        public Material panickedPedestrianMaterial;
        public Material slowPedestrianMaterial;
        public Color normalTrailColor = Color.blue;
        public Color panicTrailColor = Color.red;
        
        [Header("UI Controls")]
        public Canvas controlsUI;
        public Slider pedestrianCountSlider;
        public Slider socialDistanceSlider;
        public Button panicButton;
        public Button evacuateButton;
        public Button resetButton;
        public Toggle showHeatmapToggle;
        public Text crowdStatsText;
        
        private List<Agent> pedestrians = new List<Agent>();
        private List<GameObject> obstacles = new List<GameObject>();
        private Vector3 panicCenter;
        private float panicStartTime;
        private bool isEvacuating = false;
        
        // Crowd analytics
        private float averageSpeed = 0f;
        private float crowdDensity = 0f;
        private int bottleneckedPedestrians = 0;
        private float evacuationTime = 0f;
        private Dictionary<Vector2Int, int> heatmap = new Dictionary<Vector2Int, int>();
        
        void Start()
        {
            InitializeSimulation();
            SetupUI();
            CreateEnvironment();
            SpawnPedestrians();
        }
        
        void InitializeSimulation()
        {
            if (swarmManager == null)
                swarmManager = FindObjectOfType<SwarmManager>();
                
            if (swarmManager == null)
            {
                GameObject swarmGO = new GameObject("SwarmManager");
                swarmManager = swarmGO.AddComponent<SwarmManager>();
            }
            
            // Configure for crowd simulation
            swarmManager.initialAgentCount = 0;
            swarmManager.boundarySize = simulationArea;
            swarmManager.useSpatialHashing = true;
            swarmManager.spatialHashCellSize = 2f;
            swarmManager.enableLOD = false; // Keep all pedestrians active for realism
        }
        
        void SetupUI()
        {
            if (controlsUI == null) return;
            
            if (pedestrianCountSlider != null)
            {
                pedestrianCountSlider.value = pedestrianCount;
                pedestrianCountSlider.onValueChanged.AddListener(OnPedestrianCountChanged);
            }
            
            if (socialDistanceSlider != null)
            {
                socialDistanceSlider.value = socialDistanceRadius;
                socialDistanceSlider.onValueChanged.AddListener(OnSocialDistanceChanged);
            }
            
            if (panicButton != null)
                panicButton.onClick.AddListener(TriggerPanic);
                
            if (evacuateButton != null)
                evacuateButton.onClick.AddListener(TriggerEvacuation);
                
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetSimulation);
                
            if (showHeatmapToggle != null)
                showHeatmapToggle.onValueChanged.AddListener(OnShowHeatmapToggled);
        }
        
        void CreateEnvironment()
        {
            // Create ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "CrowdGround";
            ground.transform.localScale = simulationArea * 0.1f;
            ground.GetComponent<Renderer>().material.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            
            // Create boundaries (walls)
            CreateBoundaryWalls();
            
            // Create obstacles
            CreateObstacles();
            
            // Create destinations if not assigned
            if (destinations == null || destinations.Length == 0)
            {
                CreateDestinations();
            }
            
            // Create emergency exits
            if (emergencyExits == null || emergencyExits.Length == 0)
            {
                CreateEmergencyExits();
            }
        }
        
        void CreateBoundaryWalls()
        {
            float halfX = simulationArea.x * 0.5f;
            float halfZ = simulationArea.z * 0.5f;
            float wallHeight = 3f;
            
            // Create walls
            Vector3[] wallPositions = {
                new Vector3(0, wallHeight * 0.5f, halfZ),      // North
                new Vector3(0, wallHeight * 0.5f, -halfZ),     // South  
                new Vector3(halfX, wallHeight * 0.5f, 0),      // East
                new Vector3(-halfX, wallHeight * 0.5f, 0)      // West
            };
            
            Vector3[] wallScales = {
                new Vector3(simulationArea.x, wallHeight, 0.5f),
                new Vector3(simulationArea.x, wallHeight, 0.5f),
                new Vector3(0.5f, wallHeight, simulationArea.z),
                new Vector3(0.5f, wallHeight, simulationArea.z)
            };
            
            for (int i = 0; i < wallPositions.Length; i++)
            {
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"BoundaryWall_{i}";
                wall.transform.position = wallPositions[i];
                wall.transform.localScale = wallScales[i];
                wall.GetComponent<Renderer>().material.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }
        
        void CreateObstacles()
        {
            obstacles.Clear();
            
            for (int i = 0; i < obstacleCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-simulationArea.x * 0.3f, simulationArea.x * 0.3f),
                    1f,
                    Random.Range(-simulationArea.z * 0.3f, simulationArea.z * 0.3f)
                );
                
                GameObject obstacle;
                if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
                {
                    GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                    obstacle = Instantiate(prefab, position, Quaternion.identity);
                }
                else
                {
                    obstacle = CreateDefaultObstacle(position);
                }
                
                obstacles.Add(obstacle);
            }
        }
        
        GameObject CreateDefaultObstacle(Vector3 position)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Obstacle";
            obstacle.transform.position = position;
            obstacle.transform.localScale = new Vector3(
                Random.Range(1.5f, 4f),
                Random.Range(1f, 3f),
                Random.Range(1.5f, 4f)
            );
            obstacle.GetComponent<Renderer>().material.color = new Color(0.7f, 0.5f, 0.3f, 1f);
            
            return obstacle;
        }
        
        void CreateDestinations()
        {
            GameObject destinationParent = new GameObject("Destinations");
            destinations = new Transform[6];
            
            Vector3[] destPositions = {
                new Vector3(15, 0.5f, 15),
                new Vector3(-15, 0.5f, 15),
                new Vector3(15, 0.5f, -15),
                new Vector3(-15, 0.5f, -15),
                new Vector3(0, 0.5f, 18),
                new Vector3(0, 0.5f, -18)
            };
            
            for (int i = 0; i < destPositions.Length; i++)
            {
                GameObject dest = new GameObject($"Destination_{i + 1}");
                dest.transform.SetParent(destinationParent.transform);
                dest.transform.position = destPositions[i];
                destinations[i] = dest.transform;
                
                // Visual indicator
                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                indicator.transform.SetParent(dest.transform);
                indicator.transform.localPosition = Vector3.zero;
                indicator.transform.localScale = new Vector3(3f, 0.1f, 3f);
                indicator.GetComponent<Renderer>().material.color = Color.green;
                DestroyImmediate(indicator.GetComponent<Collider>());
            }
        }
        
        void CreateEmergencyExits()
        {
            GameObject exitParent = new GameObject("EmergencyExits");
            emergencyExits = new Transform[4];
            
            Vector3[] exitPositions = {
                new Vector3(simulationArea.x * 0.45f, 0.5f, 0),     // East exit
                new Vector3(-simulationArea.x * 0.45f, 0.5f, 0),   // West exit
                new Vector3(0, 0.5f, simulationArea.z * 0.45f),    // North exit
                new Vector3(0, 0.5f, -simulationArea.z * 0.45f)    // South exit
            };
            
            for (int i = 0; i < exitPositions.Length; i++)
            {
                GameObject exit = new GameObject($"EmergencyExit_{i + 1}");
                exit.transform.SetParent(exitParent.transform);
                exit.transform.position = exitPositions[i];
                emergencyExits[i] = exit.transform;
                
                // Emergency exit visual
                GameObject exitSign = emergencyExitSign != null ?
                    Instantiate(emergencyExitSign, exit.transform) :
                    CreateDefaultExitSign();
                    
                exitSign.transform.SetParent(exit.transform);
                exitSign.transform.localPosition = Vector3.up * 2f;
            }
        }
        
        GameObject CreateDefaultExitSign()
        {
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "ExitSign";
            sign.transform.localScale = new Vector3(2f, 1f, 0.2f);
            sign.GetComponent<Renderer>().material.color = Color.red;
            
            // Add exit text simulation
            GameObject textObj = new GameObject("ExitText");
            textObj.transform.SetParent(sign.transform);
            return sign;
        }
        
        void SpawnPedestrians()
        {
            pedestrians.Clear();
            swarmManager.ClearSwarm();
            
            for (int i = 0; i < pedestrianCount; i++)
            {
                Vector3 spawnPos = GetRandomSpawnPosition();
                
                GameObject pedestrianObj = pedestrianPrefab != null ?
                    Instantiate(pedestrianPrefab, spawnPos, Quaternion.identity) :
                    CreateDefaultPedestrian(spawnPos);
                
                Agent pedestrian = pedestrianObj.GetComponent<Agent>();
                if (pedestrian == null)
                    pedestrian = pedestrianObj.AddComponent<Agent>();
                
                // Configure pedestrian
                pedestrian.agentType = AgentType.Basic;
                pedestrian.maxSpeed = Random.Range(1f, 3f); // Realistic walking speeds
                pedestrian.maxForce = Random.Range(1f, 2f);
                pedestrian.neighborRadius = socialDistanceRadius * 2f;
                pedestrian.separationRadius = socialDistanceRadius;
                pedestrian.health = 100f;
                pedestrian.energy = Random.Range(70f, 100f);
                
                // Add crowd behaviors
                AddCrowdBehaviors(pedestrian);
                
                // Visual setup
                SetupPedestrianVisual(pedestrian);
                
                // Add to management
                swarmManager.AddAgent(pedestrian);
                pedestrians.Add(pedestrian);
            }
        }
        
        Vector3 GetRandomSpawnPosition()
        {
            Vector3 position;
            int attempts = 0;
            
            do
            {
                position = new Vector3(
                    Random.Range(-simulationArea.x * 0.3f, simulationArea.x * 0.3f),
                    0.1f,
                    Random.Range(-simulationArea.z * 0.3f, simulationArea.z * 0.3f)
                );
                attempts++;
            }
            while (IsPositionBlocked(position) && attempts < 50);
            
            return position;
        }
        
        bool IsPositionBlocked(Vector3 position)
        {
            // Check if position is too close to obstacles
            foreach (GameObject obstacle in obstacles)
            {
                if (Vector3.Distance(position, obstacle.transform.position) < 3f)
                    return true;
            }
            
            // Check if too close to other pedestrians
            foreach (Agent pedestrian in pedestrians)
            {
                if (Vector3.Distance(position, pedestrian.position) < socialDistanceRadius)
                    return true;
            }
            
            return false;
        }
        
        GameObject CreateDefaultPedestrian(Vector3 position)
        {
            GameObject pedestrian = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            pedestrian.name = "Pedestrian";
            pedestrian.transform.position = position;
            pedestrian.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            
            return pedestrian;
        }
        
        void AddCrowdBehaviors(Agent pedestrian)
        {
            // Assign random destination
            Transform destination = destinations[Random.Range(0, destinations.Length)];
            
            // Goal seeking behavior
            GoalSeekingBehavior goalBehavior = new GoalSeekingBehavior();
            goalBehavior.target = destination;
            goalBehavior.SetWeight(goalSeekingWeight);
            pedestrian.AddBehavior(goalBehavior);
            
            // Crowd flocking behavior (modified for pedestrians)
            CrowdFlockingBehavior crowdBehavior = new CrowdFlockingBehavior();
            crowdBehavior.socialDistance = socialDistanceRadius;
            crowdBehavior.SetWeight(crowdFollowingWeight);
            pedestrian.AddBehavior(crowdBehavior);
            
            // Obstacle avoidance
            ObstacleAvoidanceBehavior obstacleBehavior = new ObstacleAvoidanceBehavior();
            obstacleBehavior.SetWeight(obstacleAvoidanceWeight);
            pedestrian.AddBehavior(obstacleBehavior);
        }
        
        void SetupPedestrianVisual(Agent pedestrian)
        {
            Renderer renderer = pedestrian.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = normalPedestrianMaterial != null ?
                    normalPedestrianMaterial :
                    new Material(Shader.Find("Standard")) { color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f) };
                    
                renderer.material = material;
            }
            
            // Add trail for movement visualization
            TrailRenderer trail = pedestrian.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = pedestrian.gameObject.AddComponent<TrailRenderer>();
                
            trail.time = 2f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.02f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.material.color = normalTrailColor;
        }
        
        void Update()
        {
            UpdateSimulation();
            UpdatePanicPropagation();
            UpdateCrowdAnalytics();
            UpdateHeatmap();
            UpdateUI();
        }
        
        void UpdateSimulation()
        {
            switch (currentMode)
            {
                case SimulationMode.Normal:
                    // Normal crowd behavior
                    break;
                    
                case SimulationMode.Panic:
                    HandlePanicMode();
                    break;
                    
                case SimulationMode.Evacuation:
                    HandleEvacuationMode();
                    break;
            }
        }
        
        void HandlePanicMode()
        {
            if (!enablePanicMode) return;
            
            // Spread panic from center
            foreach (Agent pedestrian in pedestrians)
            {
                float distanceToPanic = Vector3.Distance(pedestrian.position, panicCenter);
                
                if (distanceToPanic < panicPropagationRadius)
                {
                    // Convert to panic behavior
                    ConvertToPanicBehavior(pedestrian);
                }
            }
        }
        
        void HandleEvacuationMode()
        {
            if (!isEvacuating) return;
            
            // Direct all pedestrians to nearest emergency exit
            foreach (Agent pedestrian in pedestrians)
            {
                Transform nearestExit = GetNearestEmergencyExit(pedestrian.position);
                
                // Update goal behavior to target emergency exit
                var goalBehavior = pedestrian.behaviors.OfType<GoalSeekingBehavior>().FirstOrDefault();
                if (goalBehavior != null)
                {
                    goalBehavior.target = nearestExit;
                    goalBehavior.SetWeight(goalSeekingWeight * 2f); // Increase urgency
                }
            }
        }
        
        void ConvertToPanicBehavior(Agent pedestrian)
        {
            // Increase speed and erratic movement
            pedestrian.maxSpeed *= panicIntensity;
            pedestrian.maxForce *= panicIntensity * 0.5f;
            
            // Change visual to panic state
            Renderer renderer = pedestrian.GetComponent<Renderer>();
            if (renderer != null && panickedPedestrianMaterial != null)
            {
                renderer.material = panickedPedestrianMaterial;
            }
            
            TrailRenderer trail = pedestrian.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.material.color = panicTrailColor;
            }
            
            // Add panic behavior
            PanicBehavior panicBehavior = new PanicBehavior();
            panicBehavior.panicCenter = panicCenter;
            pedestrian.AddBehavior(panicBehavior);
        }
        
        Transform GetNearestEmergencyExit(Vector3 position)
        {
            Transform nearest = emergencyExits[0];
            float nearestDistance = Vector3.Distance(position, nearest.position);
            
            foreach (Transform exit in emergencyExits)
            {
                float distance = Vector3.Distance(position, exit.position);
                if (distance < nearestDistance)
                {
                    nearest = exit;
                    nearestDistance = distance;
                }
            }
            
            return nearest;
        }
        
        void UpdatePanicPropagation()
        {
            if (currentMode != SimulationMode.Panic) return;
            
            // Expand panic radius over time
            float timeSincePanic = Time.time - panicStartTime;
            panicPropagationRadius = Mathf.Min(20f, 5f + timeSincePanic * 2f);
        }
        
        void UpdateCrowdAnalytics()
        {
            if (pedestrians.Count == 0) return;
            
            // Calculate average speed
            averageSpeed = pedestrians.Average(p => p.velocity.magnitude);
            
            // Calculate crowd density (pedestrians per square meter)
            float occupiedArea = simulationArea.x * simulationArea.z * 0.8f; // Usable area
            crowdDensity = pedestrians.Count / occupiedArea;
            
            // Count bottlenecked pedestrians (moving very slowly)
            bottleneckedPedestrians = pedestrians.Count(p => p.velocity.magnitude < 0.5f);
            
            // Update evacuation time
            if (isEvacuating)
            {
                evacuationTime = Time.time - panicStartTime;
            }
        }
        
        void UpdateHeatmap()
        {
            if (showHeatmapToggle == null || !showHeatmapToggle.isOn) return;
            
            // Update heatmap data
            foreach (Agent pedestrian in pedestrians)
            {
                Vector2Int gridPos = new Vector2Int(
                    Mathf.FloorToInt(pedestrian.position.x / 2f),
                    Mathf.FloorToInt(pedestrian.position.z / 2f)
                );
                
                if (heatmap.ContainsKey(gridPos))
                    heatmap[gridPos]++;
                else
                    heatmap[gridPos] = 1;
            }
        }
        
        void UpdateUI()
        {
            if (crowdStatsText == null) return;
            
            int panickedCount = pedestrians.Count(p => p.behaviors.Any(b => b is PanicBehavior));
            int evacuatedCount = pedestrians.Count(p => 
                emergencyExits.Any(exit => Vector3.Distance(p.position, exit.position) < 3f));
            
            crowdStatsText.text = $"=== CROWD SIMULATION STATUS ===\\n" +
                                 $"Total Pedestrians: {pedestrians.Count}\\n" +
                                 $"Average Speed: {averageSpeed:F1} m/s\\n" +
                                 $"Crowd Density: {crowdDensity:F2} p/m²\\n" +
                                 $"Bottlenecked: {bottleneckedPedestrians}\\n\\n" +
                                 $"Simulation Mode: {currentMode}\\n" +
                                 $"Panicked: {panickedCount}\\n" +
                                 $"Evacuated: {evacuatedCount}\\n\\n" +
                                 $"Social Distance: {socialDistanceRadius:F1}m\\n" +
                                 $"Goal Seeking: {goalSeekingWeight:F1}\\n" +
                                 $"Obstacle Avoidance: {obstacleAvoidanceWeight:F1}\\n\\n";
            
            if (isEvacuating)
            {
                crowdStatsText.text += $"Evacuation Time: {evacuationTime:F0}s\\n";
                float evacuationProgress = (float)evacuatedCount / pedestrians.Count * 100f;
                crowdStatsText.text += $"Evacuation Progress: {evacuationProgress:F0}%";
            }
        }
        
        // UI Event Handlers
        void OnPedestrianCountChanged(float value)
        {
            int newCount = Mathf.RoundToInt(value);
            if (newCount != pedestrianCount)
            {
                pedestrianCount = newCount;
                SpawnPedestrians();
            }
        }
        
        void OnSocialDistanceChanged(float value)
        {
            socialDistanceRadius = value;
            
            // Update existing pedestrians
            foreach (Agent pedestrian in pedestrians)
            {
                pedestrian.separationRadius = value;
                pedestrian.neighborRadius = value * 2f;
                
                var crowdBehavior = pedestrian.behaviors.OfType<CrowdFlockingBehavior>().FirstOrDefault();
                if (crowdBehavior != null)
                {
                    crowdBehavior.socialDistance = value;
                }
            }
        }
        
        void OnShowHeatmapToggled(bool enabled)
        {
            if (!enabled)
            {
                heatmap.Clear();
            }
        }
        
        public void TriggerPanic()
        {
            currentMode = SimulationMode.Panic;
            enablePanicMode = true;
            panicCenter = Vector3.zero; // Center of simulation
            panicStartTime = Time.time;
            panicPropagationRadius = 5f;
        }
        
        public void TriggerEvacuation()
        {
            currentMode = SimulationMode.Evacuation;
            isEvacuating = true;
            panicStartTime = Time.time;
            
            // Also trigger panic for realism
            TriggerPanic();
        }
        
        public void ResetSimulation()
        {
            currentMode = SimulationMode.Normal;
            enablePanicMode = false;
            isEvacuating = false;
            evacuationTime = 0f;
            heatmap.Clear();
            
            SpawnPedestrians();
        }
        
        void OnDrawGizmos()
        {
            // Draw simulation area
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, simulationArea);
            
            // Draw panic radius if active
            if (enablePanicMode && currentMode == SimulationMode.Panic)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(panicCenter, panicPropagationRadius);
            }
            
            // Draw social distance indicators for selected pedestrian
            if (pedestrians.Count > 0)
            {
                Agent selectedPedestrian = pedestrians[0];
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(selectedPedestrian.position, socialDistanceRadius);
            }
        }
    }
    
    // Additional behaviors for crowd simulation
    [System.Serializable]
    public class GoalSeekingBehavior : BaseBehavior
    {
        public Transform target;
        public float satisfactionRadius = 2f;
        
        public GoalSeekingBehavior()
        {
            behaviorName = "Goal Seeking";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            if (target == null) return Vector3.zero;
            
            float distance = Vector3.Distance(agent.position, target.position);
            
            if (distance < satisfactionRadius)
            {
                // Reached goal, pick new random goal or wander
                return Vector3.zero;
            }
            
            Vector3 desired = target.position - agent.position;
            desired.Normalize();
            desired *= agent.maxSpeed;
            
            Vector3 steer = desired - agent.velocity;
            return Vector3.ClampMagnitude(steer, agent.maxForce);
        }
    }
    
    [System.Serializable]
    public class CrowdFlockingBehavior : BaseBehavior
    {
        public float socialDistance = 1.2f;
        public float alignmentRadius = 3f;
        
        public CrowdFlockingBehavior()
        {
            behaviorName = "Crowd Flocking";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            Vector3 separation = CalculateSeparation(agent);
            Vector3 alignment = CalculateAlignment(agent);
            
            return separation * 2f + alignment * 0.5f;
        }
        
        Vector3 CalculateSeparation(Agent agent)
        {
            Vector3 steer = Vector3.zero;
            int count = 0;
            
            foreach (Agent neighbor in agent.neighbors)
            {
                float distance = Vector3.Distance(agent.position, neighbor.position);
                
                if (distance > 0 && distance < socialDistance)
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
        
        Vector3 CalculateAlignment(Agent agent)
        {
            Vector3 averageVelocity = Vector3.zero;
            int count = 0;
            
            foreach (Agent neighbor in agent.neighbors)
            {
                float distance = Vector3.Distance(agent.position, neighbor.position);
                
                if (distance > 0 && distance < alignmentRadius)
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
                return Vector3.ClampMagnitude(steer, agent.maxForce);
            }
            
            return Vector3.zero;
        }
    }
    
    [System.Serializable]
    public class ObstacleAvoidanceBehavior : BaseBehavior
    {
        public float lookAheadDistance = 3f;
        
        public ObstacleAvoidanceBehavior()
        {
            behaviorName = "Obstacle Avoidance";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            Vector3 avoidanceForce = Vector3.zero;
            
            // Cast rays to detect obstacles
            Vector3[] checkDirections = {
                agent.velocity.normalized,
                Quaternion.AngleAxis(30, Vector3.up) * agent.velocity.normalized,
                Quaternion.AngleAxis(-30, Vector3.up) * agent.velocity.normalized
            };
            
            foreach (Vector3 direction in checkDirections)
            {
                RaycastHit hit;
                if (Physics.Raycast(agent.position + Vector3.up * 0.5f, direction, out hit, lookAheadDistance))
                {
                    Vector3 avoidDirection = Vector3.Cross(direction, Vector3.up).normalized;
                    float proximity = 1f - (hit.distance / lookAheadDistance);
                    avoidanceForce += avoidDirection * proximity * agent.maxForce;
                }
            }
            
            return avoidanceForce;
        }
    }
    
    [System.Serializable]
    public class PanicBehavior : BaseBehavior
    {
        public Vector3 panicCenter;
        public float erraticStrength = 2f;
        
        public PanicBehavior()
        {
            behaviorName = "Panic";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            // Flee from panic center
            Vector3 fleeForce = (agent.position - panicCenter).normalized * agent.maxSpeed;
            
            // Add erratic movement
            Vector3 erraticForce = Random.insideUnitSphere * erraticStrength;
            erraticForce.y = 0;
            
            return fleeForce + erraticForce;
        }
    }
    
    public enum SimulationMode
    {
        Normal,
        Panic,
        Evacuation
    }
}