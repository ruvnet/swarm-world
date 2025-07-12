using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SwarmAI.Examples
{
    /// <summary>
    /// Performance Stress Test demonstrating 1000+ agents with optimization techniques
    /// Features: Scalable performance, LOD systems, spatial optimization, real-time metrics
    /// </summary>
    public class PerformanceStressTest : MonoBehaviour
    {
        [Header("Stress Test Configuration")]
        public SwarmManager swarmManager;
        public int maxAgentCount = 2000;
        public int batchSpawnSize = 100;
        public float spawnInterval = 1f;
        
        [Header("Performance Optimization")]
        public bool enableSpatialHashing = true;
        public bool enableLOD = true;
        public bool enableBehaviorLOD = true;
        public int maxAgentsPerFrame = 200;
        public float lodDistance1 = 25f; // Full detail
        public float lodDistance2 = 50f; // Reduced detail
        public float lodDistance3 = 100f; // Minimal detail
        
        [Header("Test Scenarios")]
        public TestScenario currentScenario = TestScenario.MassiveFlocking;
        public bool autoRunScenarios = false;
        public float scenarioSwitchInterval = 30f;
        
        [Header("Agent Configuration")]
        public GameObject lightweightAgentPrefab;
        public GameObject standardAgentPrefab;
        public GameObject heavyAgentPrefab;
        public bool useInstancedRendering = true;
        public bool enableTrails = false; // Expensive for large numbers
        
        [Header("Behavior Settings")]
        public bool enableFlocking = true;
        public bool enableObstacleAvoidance = false; // Can be expensive
        public bool enableBoundaryForces = true;
        public float behaviorUpdateFrequency = 0.1f; // Seconds between behavior updates
        
        [Header("Visual Optimization")]
        public bool enableFrustumCulling = true;
        public bool enableOcclusionCulling = false;
        public Material instancedMaterial;
        public Mesh agentMesh;
        
        [Header("UI Controls")]
        public Canvas controlsUI;
        public Slider agentCountSlider;
        public Slider maxAgentsPerFrameSlider;
        public Toggle spatialHashingToggle;
        public Toggle lodToggle;
        public Toggle behaviorsToggle;
        public Button spawnBatchButton;
        public Button clearAllButton;
        public Button runBenchmarkButton;
        public Text performanceStatsText;
        public Text systemInfoText;
        
        // Performance tracking
        private List<Agent> testAgents = new List<Agent>();
        private PerformanceMetrics metrics = new PerformanceMetrics();
        private Stopwatch updateStopwatch = new Stopwatch();
        private Queue<float> frameTimeHistory = new Queue<float>();
        private int frameTimeHistorySize = 60;
        
        // LOD management
        private Dictionary<Agent, AgentLODLevel> agentLODLevels = new Dictionary<Agent, AgentLODLevel>();
        private Camera playerCamera;
        
        // Instanced rendering
        private Matrix4x4[] instanceMatrices;
        private Color[] instanceColors;
        private MaterialPropertyBlock materialPropertyBlock;
        
        // Benchmark results
        private List<BenchmarkResult> benchmarkResults = new List<BenchmarkResult>();
        
        void Start()
        {
            InitializeStressTest();
            SetupUI();
            StartCoroutine(SpawnAgentsGradually());
            
            if (autoRunScenarios)
            {
                StartCoroutine(AutoRunScenarios());
            }
        }
        
        void InitializeStressTest()
        {
            if (swarmManager == null)
                swarmManager = FindObjectOfType<SwarmManager>();
                
            if (swarmManager == null)
            {
                GameObject swarmGO = new GameObject("SwarmManager");
                swarmManager = swarmGO.AddComponent<SwarmManager>();
            }
            
            if (playerCamera == null)
                playerCamera = Camera.main;
            
            // Configure swarm for high performance
            swarmManager.initialAgentCount = 0;
            swarmManager.boundarySize = new Vector3(100, 30, 100);
            swarmManager.useSpatialHashing = enableSpatialHashing;
            swarmManager.spatialHashCellSize = 5f;
            swarmManager.enableLOD = enableLOD;
            swarmManager.lodDistance = lodDistance1;
            swarmManager.maxAgentsPerFrame = maxAgentsPerFrame;
            
            // Initialize performance tracking
            metrics.Reset();
            updateStopwatch.Start();
            
            // Setup instanced rendering
            if (useInstancedRendering)
            {
                SetupInstancedRendering();
            }
            
            UnityEngine.Debug.Log($"Stress Test Initialized - Target: {maxAgentCount} agents");
        }
        
        void SetupInstancedRendering()
        {
            instanceMatrices = new Matrix4x4[maxAgentCount];
            instanceColors = new Color[maxAgentCount];
            materialPropertyBlock = new MaterialPropertyBlock();
            
            if (instancedMaterial == null)
            {
                instancedMaterial = new Material(Shader.Find("Standard"));
            }
            
            if (agentMesh == null)
            {
                // Create a simple mesh for agents
                agentMesh = CreateSimpleAgentMesh();
            }
        }
        
        Mesh CreateSimpleAgentMesh()
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            return mesh;
        }
        
        void SetupUI()
        {
            if (controlsUI == null) return;
            
            if (agentCountSlider != null)
            {
                agentCountSlider.maxValue = maxAgentCount;
                agentCountSlider.value = 0;
                agentCountSlider.onValueChanged.AddListener(OnTargetAgentCountChanged);
            }
            
            if (maxAgentsPerFrameSlider != null)
            {
                maxAgentsPerFrameSlider.value = maxAgentsPerFrame;
                maxAgentsPerFrameSlider.onValueChanged.AddListener(OnMaxAgentsPerFrameChanged);
            }
            
            if (spatialHashingToggle != null)
            {
                spatialHashingToggle.isOn = enableSpatialHashing;
                spatialHashingToggle.onValueChanged.AddListener(OnSpatialHashingToggled);
            }
            
            if (lodToggle != null)
            {
                lodToggle.isOn = enableLOD;
                lodToggle.onValueChanged.AddListener(OnLODToggled);
            }
            
            if (behaviorsToggle != null)
            {
                behaviorsToggle.isOn = enableFlocking;
                behaviorsToggle.onValueChanged.AddListener(OnBehaviorsToggled);
            }
            
            if (spawnBatchButton != null)
                spawnBatchButton.onClick.AddListener(SpawnAgentBatch);
                
            if (clearAllButton != null)
                clearAllButton.onClick.AddListener(ClearAllAgents);
                
            if (runBenchmarkButton != null)
                runBenchmarkButton.onClick.AddListener(() => StartCoroutine(RunBenchmark()));
        }
        
        IEnumerator SpawnAgentsGradually()
        {
            while (testAgents.Count < maxAgentCount)
            {
                SpawnAgentBatch();
                yield return new WaitForSeconds(spawnInterval);
            }
        }
        
        void SpawnAgentBatch()
        {
            int agentsToSpawn = Mathf.Min(batchSpawnSize, maxAgentCount - testAgents.Count);
            
            for (int i = 0; i < agentsToSpawn; i++)
            {
                SpawnOptimizedAgent();
            }
            
            UnityEngine.Debug.Log($"Spawned batch of {agentsToSpawn} agents. Total: {testAgents.Count}");
        }
        
        void SpawnOptimizedAgent()
        {
            Vector3 spawnPos = Random.insideUnitSphere * 40f;
            spawnPos.y = Mathf.Abs(spawnPos.y) + 5f;
            
            GameObject agentObj = CreateOptimizedAgent(spawnPos);
            Agent agent = agentObj.GetComponent<Agent>();
            
            if (agent == null)
                agent = agentObj.AddComponent<Agent>();
            
            // Configure for performance
            agent.agentType = AgentType.Basic;
            agent.maxSpeed = Random.Range(4f, 8f);
            agent.maxForce = Random.Range(2f, 5f);
            agent.neighborRadius = Random.Range(5f, 8f);
            agent.health = 100f;
            agent.energy = 100f;
            
            // Disable expensive components for performance
            agent.showDebugGizmos = false;
            
            // Add optimized behaviors based on scenario
            AddOptimizedBehaviors(agent);
            
            // Setup LOD
            SetupAgentLOD(agent);
            
            // Add to management
            swarmManager.AddAgent(agent);
            testAgents.Add(agent);
            agentLODLevels[agent] = AgentLODLevel.High;
        }
        
        GameObject CreateOptimizedAgent(Vector3 position)
        {
            GameObject agent;
            
            if (useInstancedRendering)
            {
                // Create minimal agent for instanced rendering
                agent = new GameObject("OptimizedAgent");
                agent.transform.position = position;
                
                // No mesh renderer needed for instanced rendering
            }
            else
            {
                // Use appropriate prefab based on performance needs
                GameObject prefabToUse = lightweightAgentPrefab;
                
                if (testAgents.Count < 500 && standardAgentPrefab != null)
                    prefabToUse = standardAgentPrefab;
                else if (testAgents.Count < 100 && heavyAgentPrefab != null)
                    prefabToUse = heavyAgentPrefab;
                
                if (prefabToUse != null)
                {
                    agent = Instantiate(prefabToUse, position, Quaternion.identity);
                }
                else
                {
                    agent = CreateDefaultLightweightAgent(position);
                }
            }
            
            return agent;
        }
        
        GameObject CreateDefaultLightweightAgent(Vector3 position)
        {
            GameObject agent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            agent.name = "LightweightAgent";
            agent.transform.position = position;
            agent.transform.localScale = Vector3.one * 0.5f;
            
            // Remove collider for performance
            DestroyImmediate(agent.GetComponent<Collider>());
            
            // Use simple material
            Renderer renderer = agent.GetComponent<Renderer>();
            renderer.material = instancedMaterial ?? new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
            
            return agent;
        }
        
        void AddOptimizedBehaviors(Agent agent)
        {
            switch (currentScenario)
            {
                case TestScenario.MassiveFlocking:
                    if (enableFlocking)
                    {
                        var flocking = new OptimizedFlockingBehavior();
                        flocking.updateFrequency = behaviorUpdateFrequency;
                        agent.AddBehavior(flocking);
                    }
                    break;
                    
                case TestScenario.RandomMovement:
                    var wander = new OptimizedWanderBehavior();
                    wander.updateFrequency = behaviorUpdateFrequency;
                    agent.AddBehavior(wander);
                    break;
                    
                case TestScenario.CentralAttraction:
                    var seek = new OptimizedSeekBehavior();
                    seek.target = Vector3.zero;
                    seek.updateFrequency = behaviorUpdateFrequency;
                    agent.AddBehavior(seek);
                    break;
                    
                case TestScenario.MixedBehaviors:
                    if (Random.Range(0f, 1f) < 0.5f)
                    {
                        var flocking = new OptimizedFlockingBehavior();
                        flocking.updateFrequency = behaviorUpdateFrequency;
                        agent.AddBehavior(flocking);
                    }
                    else
                    {
                        var wander = new OptimizedWanderBehavior();
                        wander.updateFrequency = behaviorUpdateFrequency;
                        agent.AddBehavior(wander);
                    }
                    break;
            }
            
            // Add boundary behavior for all scenarios
            if (enableBoundaryForces)
            {
                var boundary = new OptimizedBoundaryBehavior();
                boundary.bounds = swarmManager.boundarySize;
                boundary.updateFrequency = behaviorUpdateFrequency * 2f; // Less frequent updates
                agent.AddBehavior(boundary);
            }
        }
        
        void SetupAgentLOD(Agent agent)
        {
            if (!enableLOD) return;
            
            // Assign LOD level based on distance from camera
            float distance = Vector3.Distance(agent.position, playerCamera.transform.position);
            
            if (distance < lodDistance1)
                agentLODLevels[agent] = AgentLODLevel.High;
            else if (distance < lodDistance2)
                agentLODLevels[agent] = AgentLODLevel.Medium;
            else if (distance < lodDistance3)
                agentLODLevels[agent] = AgentLODLevel.Low;
            else
                agentLODLevels[agent] = AgentLODLevel.Disabled;
        }
        
        void Update()
        {
            updateStopwatch.Restart();
            
            UpdatePerformanceMetrics();
            UpdateLODSystem();
            UpdateInstancedRendering();
            UpdateUI();
            
            updateStopwatch.Stop();
            
            // Track frame time
            frameTimeHistory.Enqueue(updateStopwatch.ElapsedMilliseconds);
            if (frameTimeHistory.Count > frameTimeHistorySize)
            {
                frameTimeHistory.Dequeue();
            }
        }
        
        void UpdatePerformanceMetrics()
        {
            metrics.frameCount++;
            metrics.currentFPS = 1f / Time.unscaledDeltaTime;
            metrics.currentAgentCount = testAgents.Count;
            metrics.memoryUsageMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false) / (1024f * 1024f);
            
            // Update averages
            if (Time.time - metrics.lastMetricUpdate > 1f)
            {
                metrics.averageFPS = (metrics.averageFPS + metrics.currentFPS) * 0.5f;
                metrics.lastMetricUpdate = Time.time;
            }
            
            // Track minimums and maximums
            if (metrics.currentFPS < metrics.minFPS || metrics.minFPS == 0)
                metrics.minFPS = metrics.currentFPS;
            if (metrics.currentFPS > metrics.maxFPS)
                metrics.maxFPS = metrics.currentFPS;
        }
        
        void UpdateLODSystem()
        {
            if (!enableLOD) return;
            
            // Update LOD levels periodically for performance
            if (Time.frameCount % 30 == 0) // Every 30 frames
            {
                UpdateAgentLODLevels();
            }
        }
        
        void UpdateAgentLODLevels()
        {
            foreach (Agent agent in testAgents)
            {
                if (agent == null) continue;
                
                float distance = Vector3.Distance(agent.position, playerCamera.transform.position);
                AgentLODLevel newLOD;
                
                if (distance < lodDistance1)
                    newLOD = AgentLODLevel.High;
                else if (distance < lodDistance2)
                    newLOD = AgentLODLevel.Medium;
                else if (distance < lodDistance3)
                    newLOD = AgentLODLevel.Low;
                else
                    newLOD = AgentLODLevel.Disabled;
                
                if (agentLODLevels[agent] != newLOD)
                {
                    agentLODLevels[agent] = newLOD;
                    ApplyLODToAgent(agent, newLOD);
                }
            }
        }
        
        void ApplyLODToAgent(Agent agent, AgentLODLevel lod)
        {
            switch (lod)
            {
                case AgentLODLevel.High:
                    agent.enabled = true;
                    break;
                    
                case AgentLODLevel.Medium:
                    agent.enabled = true;
                    // Reduce update frequency
                    break;
                    
                case AgentLODLevel.Low:
                    agent.enabled = true;
                    // Further reduce update frequency
                    break;
                    
                case AgentLODLevel.Disabled:
                    agent.enabled = false;
                    break;
            }
        }
        
        void UpdateInstancedRendering()
        {
            if (!useInstancedRendering || instanceMatrices == null) return;
            
            int visibleAgents = 0;
            
            for (int i = 0; i < testAgents.Count && visibleAgents < instanceMatrices.Length; i++)
            {
                Agent agent = testAgents[i];
                if (agent == null || !agent.isAlive) continue;
                
                // Skip disabled LOD agents
                if (enableLOD && agentLODLevels.ContainsKey(agent) && 
                    agentLODLevels[agent] == AgentLODLevel.Disabled)
                    continue;
                
                // Frustum culling
                if (enableFrustumCulling)
                {
                    Bounds agentBounds = new Bounds(agent.position, Vector3.one);
                    if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(playerCamera), agentBounds))
                        continue;
                }
                
                // Create instance matrix
                instanceMatrices[visibleAgents] = Matrix4x4.TRS(
                    agent.position,
                    agent.transform.rotation,
                    Vector3.one * 0.5f
                );
                
                // Set color based on speed or state
                float speed = agent.velocity.magnitude;
                instanceColors[visibleAgents] = Color.HSVToRGB(speed / 10f, 0.8f, 1f);
                
                visibleAgents++;
            }
            
            // Render instances
            if (visibleAgents > 0)
            {
                Graphics.DrawMeshInstanced(
                    agentMesh,
                    0,
                    instancedMaterial,
                    instanceMatrices,
                    visibleAgents
                );
            }
            
            metrics.renderedAgents = visibleAgents;
        }
        
        void UpdateUI()
        {
            if (performanceStatsText != null)
            {
                float avgFrameTime = frameTimeHistory.Count > 0 ? frameTimeHistory.Average() : 0f;
                int highLODCount = agentLODLevels.Count(kvp => kvp.Value == AgentLODLevel.High);
                int mediumLODCount = agentLODLevels.Count(kvp => kvp.Value == AgentLODLevel.Medium);
                int lowLODCount = agentLODLevels.Count(kvp => kvp.Value == AgentLODLevel.Low);
                int disabledLODCount = agentLODLevels.Count(kvp => kvp.Value == AgentLODLevel.Disabled);
                
                performanceStatsText.text = $"=== PERFORMANCE STRESS TEST ===\\n" +
                                           $"Scenario: {currentScenario}\\n" +
                                           $"Total Agents: {metrics.currentAgentCount:N0}\\n" +
                                           $"Rendered Agents: {metrics.renderedAgents:N0}\\n\\n" +
                                           $"FPS: {metrics.currentFPS:F1}\\n" +
                                           $"Avg FPS: {metrics.averageFPS:F1}\\n" +
                                           $"Min/Max FPS: {metrics.minFPS:F1}/{metrics.maxFPS:F1}\\n" +
                                           $"Frame Time: {avgFrameTime:F1}ms\\n\\n" +
                                           $"Memory Usage: {metrics.memoryUsageMB:F1}MB\\n\\n" +
                                           $"LOD Distribution:\\n" +
                                           $"  High: {highLODCount}\\n" +
                                           $"  Medium: {mediumLODCount}\\n" +
                                           $"  Low: {lowLODCount}\\n" +
                                           $"  Disabled: {disabledLODCount}\\n\\n" +
                                           $"Optimizations:\\n" +
                                           $"  Spatial Hash: {enableSpatialHashing}\\n" +
                                           $"  LOD: {enableLOD}\\n" +
                                           $"  Instanced: {useInstancedRendering}\\n" +
                                           $"  Max/Frame: {maxAgentsPerFrame}";
            }
            
            if (systemInfoText != null)
            {
                systemInfoText.text = $"=== SYSTEM INFO ===\\n" +
                                      $"Platform: {Application.platform}\\n" +
                                      $"CPU: {SystemInfo.processorType}\\n" +
                                      $"CPU Cores: {SystemInfo.processorCount}\\n" +
                                      $"RAM: {SystemInfo.systemMemorySize}MB\\n" +
                                      $"GPU: {SystemInfo.graphicsDeviceName}\\n" +
                                      $"VRAM: {SystemInfo.graphicsMemorySize}MB\\n" +
                                      $"Unity: {Application.unityVersion}";
            }
        }
        
        IEnumerator AutoRunScenarios()
        {
            TestScenario[] scenarios = (TestScenario[])System.Enum.GetValues(typeof(TestScenario));
            
            while (autoRunScenarios)
            {
                foreach (TestScenario scenario in scenarios)
                {
                    currentScenario = scenario;
                    UnityEngine.Debug.Log($"Switching to scenario: {scenario}");
                    
                    // Update all agent behaviors
                    foreach (Agent agent in testAgents)
                    {
                        if (agent != null)
                        {
                            // Clear existing behaviors
                            agent.behaviors.Clear();
                            AddOptimizedBehaviors(agent);
                        }
                    }
                    
                    yield return new WaitForSeconds(scenarioSwitchInterval);
                }
            }
        }
        
        IEnumerator RunBenchmark()
        {
            UnityEngine.Debug.Log("Starting Performance Benchmark...");
            benchmarkResults.Clear();
            
            int[] testSizes = { 100, 250, 500, 750, 1000, 1500, 2000 };
            
            foreach (int testSize in testSizes)
            {
                if (testSize > maxAgentCount) continue;
                
                // Clear and spawn specific number of agents
                ClearAllAgents();
                
                for (int i = 0; i < testSize; i++)
                {
                    SpawnOptimizedAgent();
                }
                
                yield return new WaitForSeconds(2f); // Stabilization time
                
                // Measure performance for 10 seconds
                BenchmarkResult result = new BenchmarkResult();
                result.agentCount = testSize;
                result.startTime = Time.time;
                
                List<float> fpssamples = new List<float>();
                
                for (int frame = 0; frame < 600; frame++) // 10 seconds at 60fps
                {
                    fpssamples.Add(1f / Time.unscaledDeltaTime);
                    yield return null;
                }
                
                result.averageFPS = fpssamples.Average();
                result.minFPS = fpssamples.Min();
                result.maxFPS = fpssamples.Max();
                result.memoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false) / (1024f * 1024f);
                
                benchmarkResults.Add(result);
                
                UnityEngine.Debug.Log($"Benchmark {testSize} agents: Avg FPS {result.averageFPS:F1}, " +
                                     $"Min {result.minFPS:F1}, Max {result.maxFPS:F1}, Memory {result.memoryUsage:F1}MB");
            }
            
            LogBenchmarkResults();
        }
        
        void LogBenchmarkResults()
        {
            UnityEngine.Debug.Log("=== BENCHMARK RESULTS ===");
            foreach (BenchmarkResult result in benchmarkResults)
            {
                UnityEngine.Debug.Log($"{result.agentCount} agents: {result.averageFPS:F1} FPS avg, " +
                                     $"{result.minFPS:F1} min, {result.maxFPS:F1} max, {result.memoryUsage:F1}MB");
            }
        }
        
        // UI Event Handlers
        void OnTargetAgentCountChanged(float value)
        {
            int targetCount = Mathf.RoundToInt(value);
            
            if (targetCount > testAgents.Count)
            {
                // Spawn more agents
                while (testAgents.Count < targetCount)
                {
                    SpawnOptimizedAgent();
                }
            }
            else if (targetCount < testAgents.Count)
            {
                // Remove excess agents
                while (testAgents.Count > targetCount)
                {
                    Agent agentToRemove = testAgents[testAgents.Count - 1];
                    testAgents.RemoveAt(testAgents.Count - 1);
                    
                    if (agentToRemove != null)
                    {
                        swarmManager.RemoveAgent(agentToRemove);
                        agentLODLevels.Remove(agentToRemove);
                        DestroyImmediate(agentToRemove.gameObject);
                    }
                }
            }
        }
        
        void OnMaxAgentsPerFrameChanged(float value)
        {
            maxAgentsPerFrame = Mathf.RoundToInt(value);
            swarmManager.maxAgentsPerFrame = maxAgentsPerFrame;
        }
        
        void OnSpatialHashingToggled(bool enabled)
        {
            enableSpatialHashing = enabled;
            swarmManager.useSpatialHashing = enabled;
        }
        
        void OnLODToggled(bool enabled)
        {
            enableLOD = enabled;
            swarmManager.enableLOD = enabled;
        }
        
        void OnBehaviorsToggled(bool enabled)
        {
            enableFlocking = enabled;
            
            // Update all agents
            foreach (Agent agent in testAgents)
            {
                if (agent != null)
                {
                    agent.behaviors.Clear();
                    AddOptimizedBehaviors(agent);
                }
            }
        }
        
        public void ClearAllAgents()
        {
            foreach (Agent agent in testAgents)
            {
                if (agent != null)
                {
                    swarmManager.RemoveAgent(agent);
                    DestroyImmediate(agent.gameObject);
                }
            }
            
            testAgents.Clear();
            agentLODLevels.Clear();
            metrics.Reset();
            
            if (agentCountSlider != null)
                agentCountSlider.value = 0;
        }
        
        void OnDrawGizmos()
        {
            // Draw LOD distance indicators
            if (enableLOD && playerCamera != null)
            {
                Vector3 camPos = playerCamera.transform.position;
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(camPos, lodDistance1);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(camPos, lodDistance2);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(camPos, lodDistance3);
            }
        }
    }
    
    // Optimized behaviors for stress testing
    [System.Serializable]
    public class OptimizedFlockingBehavior : BaseBehavior
    {
        public float updateFrequency = 0.1f;
        private float lastUpdate = 0f;
        private Vector3 cachedForce = Vector3.zero;
        
        public OptimizedFlockingBehavior()
        {
            behaviorName = "Optimized Flocking";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            if (Time.time - lastUpdate < updateFrequency)
                return cachedForce;
            
            lastUpdate = Time.time;
            
            // Simplified flocking calculation
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            int count = 0;
            
            foreach (Agent neighbor in agent.neighbors)
            {
                if (count > 8) break; // Limit neighbor checks for performance
                
                float distance = Vector3.Distance(agent.position, neighbor.position);
                
                if (distance < 3f)
                {
                    separation += (agent.position - neighbor.position) / distance;
                    alignment += neighbor.velocity;
                    cohesion += neighbor.position;
                    count++;
                }
            }
            
            if (count > 0)
            {
                separation /= count;
                alignment = (alignment / count - agent.velocity).normalized;
                cohesion = (cohesion / count - agent.position).normalized;
                
                cachedForce = (separation * 2f + alignment + cohesion) * agent.maxForce * 0.5f;
            }
            else
            {
                cachedForce = Vector3.zero;
            }
            
            return cachedForce;
        }
    }
    
    [System.Serializable]
    public class OptimizedWanderBehavior : BaseBehavior
    {
        public float updateFrequency = 0.2f;
        private float lastUpdate = 0f;
        private Vector3 cachedForce = Vector3.zero;
        
        public OptimizedWanderBehavior()
        {
            behaviorName = "Optimized Wander";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            if (Time.time - lastUpdate < updateFrequency)
                return cachedForce;
            
            lastUpdate = Time.time;
            
            Vector3 randomDirection = Random.insideUnitSphere;
            randomDirection.y *= 0.3f; // Reduce vertical movement
            cachedForce = randomDirection.normalized * agent.maxForce * 0.8f;
            
            return cachedForce;
        }
    }
    
    [System.Serializable]
    public class OptimizedSeekBehavior : BaseBehavior
    {
        public Vector3 target = Vector3.zero;
        public float updateFrequency = 0.15f;
        private float lastUpdate = 0f;
        private Vector3 cachedForce = Vector3.zero;
        
        public OptimizedSeekBehavior()
        {
            behaviorName = "Optimized Seek";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            if (Time.time - lastUpdate < updateFrequency)
                return cachedForce;
            
            lastUpdate = Time.time;
            
            Vector3 desired = (target - agent.position).normalized * agent.maxSpeed;
            cachedForce = (desired - agent.velocity) * 0.5f;
            
            return cachedForce;
        }
    }
    
    [System.Serializable]
    public class OptimizedBoundaryBehavior : BaseBehavior
    {
        public Vector3 bounds = new Vector3(50, 20, 50);
        public float updateFrequency = 0.3f;
        private float lastUpdate = 0f;
        private Vector3 cachedForce = Vector3.zero;
        
        public OptimizedBoundaryBehavior()
        {
            behaviorName = "Optimized Boundary";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            if (Time.time - lastUpdate < updateFrequency)
                return cachedForce;
            
            lastUpdate = Time.time;
            
            Vector3 force = Vector3.zero;
            Vector3 pos = agent.position;
            Vector3 halfBounds = bounds * 0.5f;
            
            if (Mathf.Abs(pos.x) > halfBounds.x * 0.8f)
                force.x = -Mathf.Sign(pos.x) * agent.maxForce;
            if (Mathf.Abs(pos.y) > halfBounds.y * 0.8f)
                force.y = -Mathf.Sign(pos.y) * agent.maxForce;
            if (Mathf.Abs(pos.z) > halfBounds.z * 0.8f)
                force.z = -Mathf.Sign(pos.z) * agent.maxForce;
            
            cachedForce = force * 0.5f;
            return cachedForce;
        }
    }
    
    // Data structures
    [System.Serializable]
    public class PerformanceMetrics
    {
        public float currentFPS;
        public float averageFPS;
        public float minFPS;
        public float maxFPS;
        public int currentAgentCount;
        public int renderedAgents;
        public float memoryUsageMB;
        public int frameCount;
        public float lastMetricUpdate;
        
        public void Reset()
        {
            currentFPS = averageFPS = minFPS = maxFPS = 0f;
            currentAgentCount = renderedAgents = frameCount = 0;
            memoryUsageMB = 0f;
            lastMetricUpdate = Time.time;
        }
    }
    
    [System.Serializable]
    public class BenchmarkResult
    {
        public int agentCount;
        public float averageFPS;
        public float minFPS;
        public float maxFPS;
        public float memoryUsage;
        public float startTime;
    }
    
    public enum TestScenario
    {
        MassiveFlocking,
        RandomMovement,
        CentralAttraction,
        MixedBehaviors
    }
    
    public enum AgentLODLevel
    {
        High,
        Medium,
        Low,
        Disabled
    }
}