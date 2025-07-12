using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SwarmAI.Examples
{
    /// <summary>
    /// Interactive Swarm Demo with mouse following, user interaction, and dynamic behavior control
    /// Features: Mouse attraction, repulsion zones, interactive obstacles, real-time behavior modification
    /// </summary>
    public class InteractiveSwarmDemo : MonoBehaviour
    {
        [Header("Interactive Swarm Configuration")]
        public SwarmManager swarmManager;
        public int swarmSize = 100;
        public Camera mainCamera;
        
        [Header("Interaction Settings")]
        public InteractionMode currentMode = InteractionMode.Attract;
        public float interactionRadius = 8f;
        public float interactionStrength = 5f;
        public bool enableMouseInteraction = true;
        public bool enableTouchInteraction = true;
        
        [Header("Swarm Prefabs")]
        public GameObject basicAgentPrefab;
        public GameObject interactiveAgentPrefab;
        
        [Header("Visual Effects")]
        public ParticleSystem attractionEffect;
        public ParticleSystem repulsionEffect;
        public LineRenderer interactionRadiusIndicator;
        public Material attractedAgentMaterial;
        public Material repelledAgentMaterial;
        public Material normalAgentMaterial;
        
        [Header("Interactive Elements")]
        public GameObject attractorPrefab;
        public GameObject repellerPrefab;
        public List<Transform> staticAttractors = new List<Transform>();
        public List<Transform> staticRepellers = new List<Transform>();
        
        [Header("UI Controls")]
        public Canvas controlsUI;
        public Slider swarmSizeSlider;
        public Slider interactionRadiusSlider;
        public Slider interactionStrengthSlider;
        public Dropdown interactionModeDropdown;
        public Toggle mouseInteractionToggle;
        public Button spawnAttractorButton;
        public Button spawnRepellerButton;
        public Button clearInteractorsButton;
        public Text interactionStatsText;
        
        [Header("Advanced Features")]
        public bool enableFlocking = true;
        public bool enableObstacleAvoidance = true;
        public bool enableColorChange = true;
        public float colorChangeSpeed = 2f;
        
        private Vector3 mouseWorldPosition;
        private List<Agent> swarmAgents = new List<Agent>();
        private List<Transform> dynamicInteractors = new List<Transform>();
        
        // Interaction tracking
        private int agentsInInteractionZone = 0;
        private float totalInteractionTime = 0f;
        private int totalInteractions = 0;
        private Vector3 averageSwarmPosition;
        
        void Start()
        {
            InitializeDemo();
            SetupUI();
            SpawnSwarm();
            SetupInteractionEffects();
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
            
            if (mainCamera == null)
                mainCamera = Camera.main;
            
            // Configure swarm for interaction
            swarmManager.initialAgentCount = 0;
            swarmManager.boundarySize = new Vector3(30, 15, 30);
            swarmManager.useSpatialHashing = true;
            swarmManager.spatialHashCellSize = 3f;
            swarmManager.enableLOD = false; // Keep all agents responsive
        }
        
        void SetupUI()
        {
            if (controlsUI == null) return;
            
            if (swarmSizeSlider != null)
            {
                swarmSizeSlider.value = swarmSize;
                swarmSizeSlider.onValueChanged.AddListener(OnSwarmSizeChanged);
            }
            
            if (interactionRadiusSlider != null)
            {
                interactionRadiusSlider.value = interactionRadius;
                interactionRadiusSlider.onValueChanged.AddListener(OnInteractionRadiusChanged);
            }
            
            if (interactionStrengthSlider != null)
            {
                interactionStrengthSlider.value = interactionStrength;
                interactionStrengthSlider.onValueChanged.AddListener(OnInteractionStrengthChanged);
            }
            
            if (interactionModeDropdown != null)
            {
                List<string> modeNames = System.Enum.GetNames(typeof(InteractionMode)).ToArray().ToList();
                interactionModeDropdown.ClearOptions();
                interactionModeDropdown.AddOptions(modeNames);
                interactionModeDropdown.value = (int)currentMode;
                interactionModeDropdown.onValueChanged.AddListener(OnInteractionModeChanged);
            }
            
            if (mouseInteractionToggle != null)
            {
                mouseInteractionToggle.isOn = enableMouseInteraction;
                mouseInteractionToggle.onValueChanged.AddListener(OnMouseInteractionToggled);
            }
            
            if (spawnAttractorButton != null)
                spawnAttractorButton.onClick.AddListener(SpawnAttractorAtMouse);
                
            if (spawnRepellerButton != null)
                spawnRepellerButton.onClick.AddListener(SpawnRepellerAtMouse);
                
            if (clearInteractorsButton != null)
                clearInteractorsButton.onClick.AddListener(ClearDynamicInteractors);
        }
        
        void SpawnSwarm()
        {
            swarmAgents.Clear();
            swarmManager.ClearSwarm();
            
            for (int i = 0; i < swarmSize; i++)
            {
                Vector3 spawnPos = Random.insideUnitSphere * 10f;
                spawnPos.y = Mathf.Abs(spawnPos.y) + 1f; // Keep above ground
                
                GameObject agentObj = interactiveAgentPrefab != null ?
                    Instantiate(interactiveAgentPrefab, spawnPos, Quaternion.identity) :
                    CreateDefaultInteractiveAgent(spawnPos);
                
                Agent agent = agentObj.GetComponent<Agent>();
                if (agent == null)
                    agent = agentObj.AddComponent<Agent>();
                
                // Configure agent for interaction
                agent.agentType = AgentType.Basic;
                agent.maxSpeed = Random.Range(3f, 6f);
                agent.maxForce = Random.Range(2f, 4f);
                agent.neighborRadius = Random.Range(3f, 5f);
                agent.health = 100f;
                agent.energy = 100f;
                
                // Add interactive behaviors
                AddInteractiveBehaviors(agent);
                
                // Visual setup
                SetupInteractiveAgentVisual(agent);
                
                // Add to management
                swarmManager.AddAgent(agent);
                swarmAgents.Add(agent);
            }
        }
        
        GameObject CreateDefaultInteractiveAgent(Vector3 position)
        {
            GameObject agent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            agent.name = "InteractiveAgent";
            agent.transform.position = position;
            agent.transform.localScale = Vector3.one * 0.3f;
            
            return agent;
        }
        
        void AddInteractiveBehaviors(Agent agent)
        {
            // Mouse interaction behavior
            MouseInteractionBehavior mouseInteraction = new MouseInteractionBehavior();
            mouseInteraction.interactionRadius = interactionRadius;
            mouseInteraction.interactionStrength = interactionStrength;
            mouseInteraction.interactionMode = currentMode;
            agent.AddBehavior(mouseInteraction);
            
            // Static interactor behavior
            StaticInteractorBehavior staticInteraction = new StaticInteractorBehavior();
            staticInteraction.attractors = staticAttractors;
            staticInteraction.repellers = staticRepellers;
            agent.AddBehavior(staticInteraction);
            
            // Basic flocking if enabled
            if (enableFlocking)
            {
                FlockingBehavior flocking = new FlockingBehavior();
                flocking.SetWeight(0.5f);
                agent.AddBehavior(flocking);
            }
            
            // Obstacle avoidance if enabled
            if (enableObstacleAvoidance)
            {
                SimpleObstacleAvoidance obstacleAvoidance = new SimpleObstacleAvoidance();
                obstacleAvoidance.SetWeight(3f);
                agent.AddBehavior(obstacleAvoidance);
            }
            
            // Boundary keeping
            BoundaryBehavior boundary = new BoundaryBehavior();
            boundary.boundaryCenter = Vector3.zero;
            boundary.boundarySize = swarmManager.boundarySize;
            agent.AddBehavior(boundary);
        }
        
        void SetupInteractiveAgentVisual(Agent agent)
        {
            Renderer renderer = agent.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = normalAgentMaterial != null ?
                    normalAgentMaterial :
                    new Material(Shader.Find("Standard")) { color = Random.ColorHSV(0.5f, 0.8f, 0.7f, 1f, 0.8f, 1f) };
                    
                renderer.material = material;
            }
            
            // Add trail for interaction visualization
            TrailRenderer trail = agent.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = agent.gameObject.AddComponent<TrailRenderer>();
                
            trail.time = 1.5f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.02f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.material.color = new Color(Random.value, Random.value, Random.value, 0.6f);
        }
        
        void SetupInteractionEffects()
        {
            // Setup interaction radius indicator
            if (interactionRadiusIndicator == null)
            {
                GameObject indicatorObj = new GameObject("InteractionRadiusIndicator");
                interactionRadiusIndicator = indicatorObj.AddComponent<LineRenderer>();
            }
            
            SetupRadiusIndicator();
        }
        
        void SetupRadiusIndicator()
        {
            if (interactionRadiusIndicator == null) return;
            
            interactionRadiusIndicator.material = new Material(Shader.Find("Sprites/Default"));
            interactionRadiusIndicator.color = currentMode == InteractionMode.Attract ? Color.green : Color.red;
            interactionRadiusIndicator.startWidth = 0.1f;
            interactionRadiusIndicator.endWidth = 0.1f;
            interactionRadiusIndicator.useWorldSpace = true;
            
            // Create circle
            int segments = 64;
            interactionRadiusIndicator.positionCount = segments + 1;
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * interactionRadius,
                    0,
                    Mathf.Sin(angle) * interactionRadius
                );
                interactionRadiusIndicator.SetPosition(i, position);
            }
        }
        
        void Update()
        {
            UpdateMouseInput();
            UpdateTouchInput();
            UpdateInteractionEffects();
            UpdateAgentColors();
            UpdateStatistics();
            UpdateUI();
        }
        
        void UpdateMouseInput()
        {
            if (!enableMouseInteraction) return;
            
            // Get mouse position in world space
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = mainCamera.transform.position.y; // Distance from camera
            
            mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPos);
            mouseWorldPosition.y = 2f; // Keep at reasonable height
            
            // Update interaction radius indicator position
            if (interactionRadiusIndicator != null)
            {
                interactionRadiusIndicator.transform.position = mouseWorldPosition;
            }
            
            // Handle mode switching with keyboard
            if (Input.GetKeyDown(KeyCode.Q))
            {
                CycleInteractionMode();
            }
            
            // Handle interaction strength with scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                interactionStrength = Mathf.Clamp(interactionStrength + scroll * 2f, 0.1f, 20f);
                UpdateInteractionStrength();
            }
            
            // Update all mouse interaction behaviors
            foreach (Agent agent in swarmAgents)
            {
                var mouseInteraction = agent.behaviors.Find(b => b is MouseInteractionBehavior) as MouseInteractionBehavior;
                if (mouseInteraction != null)
                {
                    mouseInteraction.mousePosition = mouseWorldPosition;
                    mouseInteraction.interactionRadius = interactionRadius;
                    mouseInteraction.interactionStrength = interactionStrength;
                    mouseInteraction.interactionMode = currentMode;
                }
            }
        }
        
        void UpdateTouchInput()
        {
            if (!enableTouchInteraction || Input.touchCount == 0) return;
            
            Touch touch = Input.GetTouch(0);
            Vector3 touchWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10f));
            
            // Use touch position as interaction point
            mouseWorldPosition = touchWorldPos;
        }
        
        void UpdateInteractionEffects()
        {
            // Play effects based on interaction
            agentsInInteractionZone = 0;
            
            foreach (Agent agent in swarmAgents)
            {
                float distance = Vector3.Distance(agent.position, mouseWorldPosition);
                
                if (distance < interactionRadius)
                {
                    agentsInInteractionZone++;
                    totalInteractionTime += Time.deltaTime;
                }
            }
            
            // Update particle effects
            if (agentsInInteractionZone > 0)
            {
                if (currentMode == InteractionMode.Attract && attractionEffect != null)
                {
                    if (!attractionEffect.isPlaying)
                    {
                        attractionEffect.transform.position = mouseWorldPosition;
                        attractionEffect.Play();
                    }
                }
                else if (currentMode == InteractionMode.Repel && repulsionEffect != null)
                {
                    if (!repulsionEffect.isPlaying)
                    {
                        repulsionEffect.transform.position = mouseWorldPosition;
                        repulsionEffect.Play();
                    }
                }
            }
            else
            {
                if (attractionEffect != null && attractionEffect.isPlaying)
                    attractionEffect.Stop();
                if (repulsionEffect != null && repulsionEffect.isPlaying)
                    repulsionEffect.Stop();
            }
        }
        
        void UpdateAgentColors()
        {
            if (!enableColorChange) return;
            
            foreach (Agent agent in swarmAgents)
            {
                Renderer renderer = agent.GetComponent<Renderer>();
                if (renderer == null) continue;
                
                float distance = Vector3.Distance(agent.position, mouseWorldPosition);
                Color targetColor;
                
                if (distance < interactionRadius)
                {
                    // Agent is in interaction zone
                    targetColor = currentMode == InteractionMode.Attract ? 
                        (attractedAgentMaterial?.color ?? Color.green) :
                        (repelledAgentMaterial?.color ?? Color.red);
                }
                else
                {
                    // Normal agent
                    targetColor = normalAgentMaterial?.color ?? Color.white;
                }
                
                // Smoothly transition color
                renderer.material.color = Color.Lerp(
                    renderer.material.color, 
                    targetColor, 
                    Time.deltaTime * colorChangeSpeed
                );
            }
        }
        
        void UpdateStatistics()
        {
            if (swarmAgents.Count == 0) return;
            
            // Calculate average swarm position
            averageSwarmPosition = Vector3.zero;
            foreach (Agent agent in swarmAgents)
            {
                averageSwarmPosition += agent.position;
            }
            averageSwarmPosition /= swarmAgents.Count;
            
            // Count total interactions
            if (agentsInInteractionZone > 0)
            {
                totalInteractions++;
            }
        }
        
        void UpdateUI()
        {
            if (interactionStatsText == null) return;
            
            float averageSpeed = 0f;
            float swarmSpread = 0f;
            
            foreach (Agent agent in swarmAgents)
            {
                averageSpeed += agent.velocity.magnitude;
                swarmSpread += Vector3.Distance(agent.position, averageSwarmPosition);
            }
            
            if (swarmAgents.Count > 0)
            {
                averageSpeed /= swarmAgents.Count;
                swarmSpread /= swarmAgents.Count;
            }
            
            float distanceToSwarm = Vector3.Distance(mouseWorldPosition, averageSwarmPosition);
            
            interactionStatsText.text = $"=== INTERACTIVE SWARM STATUS ===\\n" +
                                       $"Swarm Size: {swarmAgents.Count}\\n" +
                                       $"Interaction Mode: {currentMode}\\n" +
                                       $"Interaction Radius: {interactionRadius:F1}m\\n" +
                                       $"Interaction Strength: {interactionStrength:F1}\\n\\n" +
                                       $"Agents in Zone: {agentsInInteractionZone}\\n" +
                                       $"Average Speed: {averageSpeed:F1} m/s\\n" +
                                       $"Swarm Spread: {swarmSpread:F1}m\\n" +
                                       $"Distance to Swarm: {distanceToSwarm:F1}m\\n\\n" +
                                       $"Total Interactions: {totalInteractions}\\n" +
                                       $"Interaction Time: {totalInteractionTime:F0}s\\n" +
                                       $"Static Attractors: {staticAttractors.Count}\\n" +
                                       $"Static Repellers: {staticRepellers.Count}\\n\\n" +
                                       $"Controls:\\n" +
                                       $"Q - Cycle Mode\\n" +
                                       $"Scroll - Adjust Strength";
        }
        
        // UI Event Handlers
        void OnSwarmSizeChanged(float value)
        {
            int newSize = Mathf.RoundToInt(value);
            if (newSize != swarmSize)
            {
                swarmSize = newSize;
                SpawnSwarm();
            }
        }
        
        void OnInteractionRadiusChanged(float value)
        {
            interactionRadius = value;
            SetupRadiusIndicator();
        }
        
        void OnInteractionStrengthChanged(float value)
        {
            interactionStrength = value;
            UpdateInteractionStrength();
        }
        
        void OnInteractionModeChanged(int value)
        {
            currentMode = (InteractionMode)value;
            UpdateInteractionMode();
        }
        
        void OnMouseInteractionToggled(bool enabled)
        {
            enableMouseInteraction = enabled;
            if (interactionRadiusIndicator != null)
                interactionRadiusIndicator.enabled = enabled;
        }
        
        void UpdateInteractionStrength()
        {
            foreach (Agent agent in swarmAgents)
            {
                var mouseInteraction = agent.behaviors.Find(b => b is MouseInteractionBehavior) as MouseInteractionBehavior;
                if (mouseInteraction != null)
                {
                    mouseInteraction.interactionStrength = interactionStrength;
                }
            }
            
            if (interactionStrengthSlider != null)
                interactionStrengthSlider.value = interactionStrength;
        }
        
        void UpdateInteractionMode()
        {
            foreach (Agent agent in swarmAgents)
            {
                var mouseInteraction = agent.behaviors.Find(b => b is MouseInteractionBehavior) as MouseInteractionBehavior;
                if (mouseInteraction != null)
                {
                    mouseInteraction.interactionMode = currentMode;
                }
            }
            
            // Update radius indicator color
            if (interactionRadiusIndicator != null)
            {
                interactionRadiusIndicator.color = currentMode == InteractionMode.Attract ? Color.green : Color.red;
            }
        }
        
        public void CycleInteractionMode()
        {
            InteractionMode[] modes = (InteractionMode[])System.Enum.GetValues(typeof(InteractionMode));
            int currentIndex = System.Array.IndexOf(modes, currentMode);
            int nextIndex = (currentIndex + 1) % modes.Length;
            
            currentMode = modes[nextIndex];
            UpdateInteractionMode();
            
            if (interactionModeDropdown != null)
                interactionModeDropdown.value = nextIndex;
        }
        
        public void SpawnAttractorAtMouse()
        {
            GameObject attractor = attractorPrefab != null ?
                Instantiate(attractorPrefab, mouseWorldPosition, Quaternion.identity) :
                CreateDefaultAttractor(mouseWorldPosition);
                
            staticAttractors.Add(attractor.transform);
            dynamicInteractors.Add(attractor.transform);
            
            UpdateStaticInteractors();
        }
        
        public void SpawnRepellerAtMouse()
        {
            GameObject repeller = repellerPrefab != null ?
                Instantiate(repellerPrefab, mouseWorldPosition, Quaternion.identity) :
                CreateDefaultRepeller(mouseWorldPosition);
                
            staticRepellers.Add(repeller.transform);
            dynamicInteractors.Add(repeller.transform);
            
            UpdateStaticInteractors();
        }
        
        GameObject CreateDefaultAttractor(Vector3 position)
        {
            GameObject attractor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            attractor.name = "Attractor";
            attractor.transform.position = position;
            attractor.transform.localScale = Vector3.one * 2f;
            attractor.GetComponent<Renderer>().material.color = Color.green;
            DestroyImmediate(attractor.GetComponent<Collider>());
            
            return attractor;
        }
        
        GameObject CreateDefaultRepeller(Vector3 position)
        {
            GameObject repeller = GameObject.CreatePrimitive(PrimitiveType.Cube);
            repeller.name = "Repeller";
            repeller.transform.position = position;
            repeller.transform.localScale = Vector3.one * 2f;
            repeller.GetComponent<Renderer>().material.color = Color.red;
            DestroyImmediate(repeller.GetComponent<Collider>());
            
            return repeller;
        }
        
        void UpdateStaticInteractors()
        {
            foreach (Agent agent in swarmAgents)
            {
                var staticInteraction = agent.behaviors.Find(b => b is StaticInteractorBehavior) as StaticInteractorBehavior;
                if (staticInteraction != null)
                {
                    staticInteraction.attractors = staticAttractors;
                    staticInteraction.repellers = staticRepellers;
                }
            }
        }
        
        public void ClearDynamicInteractors()
        {
            foreach (Transform interactor in dynamicInteractors)
            {
                if (interactor != null)
                {
                    staticAttractors.Remove(interactor);
                    staticRepellers.Remove(interactor);
                    DestroyImmediate(interactor.gameObject);
                }
            }
            
            dynamicInteractors.Clear();
            UpdateStaticInteractors();
        }
        
        void OnDrawGizmos()
        {
            // Draw interaction zone
            if (enableMouseInteraction)
            {
                Gizmos.color = currentMode == InteractionMode.Attract ? Color.green : Color.red;
                Gizmos.DrawWireSphere(mouseWorldPosition, interactionRadius);
            }
            
            // Draw swarm center
            if (swarmAgents.Count > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(averageSwarmPosition, 1f);
            }
            
            // Draw static interactors
            Gizmos.color = Color.green;
            foreach (Transform attractor in staticAttractors)
            {
                if (attractor != null)
                    Gizmos.DrawWireSphere(attractor.position, 3f);
            }
            
            Gizmos.color = Color.red;
            foreach (Transform repeller in staticRepellers)
            {
                if (repeller != null)
                    Gizmos.DrawWireCube(repeller.position, Vector3.one * 3f);
            }
        }
    }
    
    // Interactive behaviors
    [System.Serializable]
    public class MouseInteractionBehavior : BaseBehavior
    {
        public Vector3 mousePosition;
        public float interactionRadius = 5f;
        public float interactionStrength = 3f;
        public InteractionMode interactionMode = InteractionMode.Attract;
        
        public MouseInteractionBehavior()
        {
            behaviorName = "Mouse Interaction";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            float distance = Vector3.Distance(agent.position, mousePosition);
            
            if (distance > interactionRadius) return Vector3.zero;
            
            Vector3 direction = interactionMode == InteractionMode.Attract ?
                (mousePosition - agent.position).normalized :
                (agent.position - mousePosition).normalized;
            
            // Inverse square falloff for realistic interaction
            float strength = interactionStrength / (1f + distance * distance * 0.1f);
            
            return direction * strength;
        }
    }
    
    [System.Serializable]
    public class StaticInteractorBehavior : BaseBehavior
    {
        public List<Transform> attractors = new List<Transform>();
        public List<Transform> repellers = new List<Transform>();
        public float interactionRadius = 8f;
        
        public StaticInteractorBehavior()
        {
            behaviorName = "Static Interactors";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            Vector3 totalForce = Vector3.zero;
            
            // Attraction forces
            foreach (Transform attractor in attractors)
            {
                if (attractor == null) continue;
                
                float distance = Vector3.Distance(agent.position, attractor.position);
                if (distance < interactionRadius)
                {
                    Vector3 direction = (attractor.position - agent.position).normalized;
                    float strength = (interactionRadius - distance) / interactionRadius;
                    totalForce += direction * strength * 2f;
                }
            }
            
            // Repulsion forces
            foreach (Transform repeller in repellers)
            {
                if (repeller == null) continue;
                
                float distance = Vector3.Distance(agent.position, repeller.position);
                if (distance < interactionRadius)
                {
                    Vector3 direction = (agent.position - repeller.position).normalized;
                    float strength = (interactionRadius - distance) / interactionRadius;
                    totalForce += direction * strength * 3f;
                }
            }
            
            return Vector3.ClampMagnitude(totalForce, agent.maxForce);
        }
    }
    
    [System.Serializable]
    public class SimpleObstacleAvoidance : BaseBehavior
    {
        public float lookAheadDistance = 3f;
        
        public SimpleObstacleAvoidance()
        {
            behaviorName = "Simple Obstacle Avoidance";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            Vector3 avoidanceForce = Vector3.zero;
            
            RaycastHit hit;
            if (Physics.Raycast(agent.position, agent.velocity.normalized, out hit, lookAheadDistance))
            {
                Vector3 avoidDirection = Vector3.Cross(agent.velocity.normalized, Vector3.up).normalized;
                if (Vector3.Dot(avoidDirection, hit.normal) < 0)
                    avoidDirection = -avoidDirection;
                
                float proximity = 1f - (hit.distance / lookAheadDistance);
                avoidanceForce = avoidDirection * proximity * agent.maxForce;
            }
            
            return avoidanceForce;
        }
    }
    
    [System.Serializable]
    public class BoundaryBehavior : BaseBehavior
    {
        public Vector3 boundaryCenter = Vector3.zero;
        public Vector3 boundarySize = new Vector3(20, 10, 20);
        
        public BoundaryBehavior()
        {
            behaviorName = "Boundary Keeping";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            Vector3 force = Vector3.zero;
            Vector3 localPos = agent.position - boundaryCenter;
            Vector3 halfSize = boundarySize * 0.5f;
            
            // Check each axis and apply force if near boundary
            if (Mathf.Abs(localPos.x) > halfSize.x * 0.8f)
            {
                force.x = -Mathf.Sign(localPos.x) * agent.maxForce;
            }
            
            if (Mathf.Abs(localPos.y) > halfSize.y * 0.8f)
            {
                force.y = -Mathf.Sign(localPos.y) * agent.maxForce;
            }
            
            if (Mathf.Abs(localPos.z) > halfSize.z * 0.8f)
            {
                force.z = -Mathf.Sign(localPos.z) * agent.maxForce;
            }
            
            return force;
        }
    }
    
    public enum InteractionMode
    {
        Attract,
        Repel,
        Neutral
    }
}