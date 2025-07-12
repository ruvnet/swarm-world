using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI.Examples
{
    /// <summary>
    /// Formation Flying Demo demonstrating military/aerospace style formations
    /// Features: V-formation, line formations, diamond patterns, dynamic formation changes
    /// </summary>
    public class FormationFlyingDemo : MonoBehaviour
    {
        [Header("Formation Configuration")]
        public SwarmManager swarmManager;
        public FormationManager formationManager;
        public int totalAircraftCount = 24;
        public int aircraftPerFormation = 8;
        
        [Header("Aircraft Prefabs")]
        public GameObject fighterJetPrefab;
        public GameObject transportPrefab;
        public GameObject leaderPrefab;
        
        [Header("Formation Settings")]
        public FormationType defaultFormation = FormationType.VFormation;
        public float formationSpacing = 5f;
        public Vector3 flightArea = new Vector3(100, 20, 100);
        
        [Header("Flight Dynamics")]
        public float cruiseSpeed = 15f;
        public float maxSpeed = 25f;
        public float flightAltitude = 10f;
        public Vector3 windDirection = Vector3.forward;
        public float windStrength = 2f;
        
        [Header("Visual Effects")]
        public ParticleSystem contrailEffect;
        public Material jetMaterial;
        public Material leaderMaterial;
        public Color formationLightColor = Color.blue;
        
        [Header("UI Controls")]
        public Canvas controlsUI;
        public Dropdown formationTypeDropdown;
        public Slider formationSpacingSlider;
        public Slider windStrengthSlider;
        public Button changeFormationButton;
        public Button addSquadronButton;
        public Button maneuverButton;
        public Text formationStatsText;
        
        [Header("Mission Waypoints")]
        public Transform[] waypoints;
        public bool followWaypoints = true;
        public float waypointReachDistance = 15f;
        
        private List<Formation> squadrons = new List<Formation>();
        private int currentWaypointIndex = 0;
        private float missionStartTime;
        private Dictionary<Agent, ParticleSystem> aircraftContrails = new Dictionary<Agent, ParticleSystem>();
        
        // Formation statistics
        private float averageFormationTightness = 0f;
        private int totalFormationChanges = 0;
        private float totalFlightDistance = 0f;
        
        void Start()
        {
            missionStartTime = Time.time;
            InitializeDemo();
            SetupUI();
            CreateFlightEnvironment();
            SpawnAircraft();
            InitializeWaypoints();
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
            
            // Configure for formation flying
            swarmManager.initialAgentCount = 0;
            swarmManager.boundarySize = flightArea;
            swarmManager.useSpatialHashing = true;
            swarmManager.spatialHashCellSize = 8f;
            swarmManager.enableLOD = true;
            swarmManager.lodDistance = 50f;
            
            // Setup formation manager
            if (formationManager == null)
            {
                GameObject formationGO = new GameObject("FormationManager");
                formationManager = formationGO.AddComponent<FormationManager>();
            }
            
            formationManager.defaultFormation = defaultFormation;
            formationManager.formationSpacing = formationSpacing;
            formationManager.maxFormationSize = aircraftPerFormation;
        }
        
        void SetupUI()
        {
            if (controlsUI == null) return;
            
            if (formationTypeDropdown != null)
            {
                List<string> formationNames = System.Enum.GetNames(typeof(FormationType)).ToList();
                formationTypeDropdown.ClearOptions();
                formationTypeDropdown.AddOptions(formationNames);
                formationTypeDropdown.value = (int)defaultFormation;
                formationTypeDropdown.onValueChanged.AddListener(OnFormationTypeChanged);
            }
            
            if (formationSpacingSlider != null)
            {
                formationSpacingSlider.value = formationSpacing;
                formationSpacingSlider.onValueChanged.AddListener(OnFormationSpacingChanged);
            }
            
            if (windStrengthSlider != null)
            {
                windStrengthSlider.value = windStrength;
                windStrengthSlider.onValueChanged.AddListener(OnWindStrengthChanged);
            }
            
            if (changeFormationButton != null)
                changeFormationButton.onClick.AddListener(CycleFormationType);
                
            if (addSquadronButton != null)
                addSquadronButton.onClick.AddListener(AddNewSquadron);
                
            if (maneuverButton != null)
                maneuverButton.onClick.AddListener(ExecuteManeuver);
        }
        
        void CreateFlightEnvironment()
        {
            // Create sky environment
            Camera.main.backgroundColor = new Color(0.5f, 0.8f, 1f, 1f);
            RenderSettings.fogColor = new Color(0.8f, 0.9f, 1f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogStartDistance = 50f;
            RenderSettings.fogEndDistance = 200f;
            
            // Create ground reference
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.down * 5f;
            ground.transform.localScale = flightArea * 0.2f;
            ground.GetComponent<Renderer>().material.color = new Color(0.3f, 0.6f, 0.3f, 1f);
            
            // Create some cloud effects
            CreateClouds();
        }
        
        void CreateClouds()
        {
            int cloudCount = Random.Range(8, 15);
            
            for (int i = 0; i < cloudCount; i++)
            {
                GameObject cloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cloud.name = "Cloud";
                cloud.transform.position = new Vector3(
                    Random.Range(-flightArea.x * 0.6f, flightArea.x * 0.6f),
                    Random.Range(flightAltitude - 5f, flightAltitude + 15f),
                    Random.Range(-flightArea.z * 0.6f, flightArea.z * 0.6f)
                );
                cloud.transform.localScale = Vector3.one * Random.Range(8f, 15f);
                
                Renderer cloudRenderer = cloud.GetComponent<Renderer>();
                cloudRenderer.material.color = new Color(1f, 1f, 1f, 0.7f);
                cloudRenderer.material.SetFloat("_Mode", 3); // Transparent mode
                
                // Make clouds non-solid
                DestroyImmediate(cloud.GetComponent<Collider>());
            }
        }
        
        void InitializeWaypoints()
        {
            if (waypoints == null || waypoints.Length == 0)
            {
                CreateDefaultWaypoints();
            }
        }
        
        void CreateDefaultWaypoints()
        {
            GameObject waypointParent = new GameObject("FlightWaypoints");
            waypoints = new Transform[6];
            
            Vector3[] waypointPositions = {
                new Vector3(0, flightAltitude, 30),
                new Vector3(40, flightAltitude, 40),
                new Vector3(60, flightAltitude, 0),
                new Vector3(40, flightAltitude, -40),
                new Vector3(-40, flightAltitude, -30),
                new Vector3(-20, flightAltitude, 20)
            };
            
            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"Waypoint_{i + 1}");
                waypoint.transform.SetParent(waypointParent.transform);
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                
                // Visual indicator
                GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                indicator.transform.SetParent(waypoint.transform);
                indicator.transform.localPosition = Vector3.zero;
                indicator.transform.localScale = Vector3.one * 2f;
                indicator.GetComponent<Renderer>().material.color = Color.yellow;
                DestroyImmediate(indicator.GetComponent<Collider>());
            }
        }
        
        void SpawnAircraft()
        {
            int squadronCount = Mathf.CeilToInt((float)totalAircraftCount / aircraftPerFormation);
            
            for (int squadron = 0; squadron < squadronCount; squadron++)
            {
                SpawnSquadron(squadron);
            }
        }
        
        void SpawnSquadron(int squadronIndex)
        {
            List<Agent> squadronAgents = new List<Agent>();
            Vector3 squadronSpawnArea = new Vector3(
                squadronIndex * 15f - (squadronIndex * 7.5f),
                flightAltitude,
                -squadronIndex * 10f
            );
            
            int aircraftInThisSquadron = Mathf.Min(aircraftPerFormation, 
                totalAircraftCount - squadronIndex * aircraftPerFormation);
            
            for (int i = 0; i < aircraftInThisSquadron; i++)
            {
                Vector3 spawnPos = squadronSpawnArea + new Vector3(
                    Random.Range(-5f, 5f),
                    Random.Range(-2f, 2f),
                    Random.Range(-8f, 8f)
                );
                
                GameObject aircraftPrefab = SelectAircraftPrefab(i);
                GameObject aircraftObj = aircraftPrefab != null ? 
                    Instantiate(aircraftPrefab, spawnPos, Quaternion.identity) :
                    CreateDefaultAircraft(spawnPos, i == 0);
                
                Agent aircraft = aircraftObj.GetComponent<Agent>();
                if (aircraft == null)
                    aircraft = aircraftObj.AddComponent<Agent>();
                
                // Configure aircraft
                aircraft.agentType = i == 0 ? AgentType.Leader : AgentType.Basic;
                aircraft.maxSpeed = i == 0 ? maxSpeed * 0.9f : cruiseSpeed;
                aircraft.maxForce = Random.Range(4f, 8f);
                aircraft.neighborRadius = Random.Range(12f, 20f);
                aircraft.health = 100f;
                aircraft.energy = 100f;
                
                // Add mission behavior (waypoint following for leaders)
                if (i == 0)
                {
                    AddMissionBehavior(aircraft);
                }
                
                // Add wind effects
                AddWindBehavior(aircraft);
                
                // Visual setup
                SetupAircraftVisual(aircraft, i == 0);
                
                // Add to swarm
                swarmManager.AddAgent(aircraft);
                squadronAgents.Add(aircraft);
                
                // Create contrail effect
                CreateContrailEffect(aircraft);
            }
            
            // Create formation from squadron
            if (squadronAgents.Count > 1)
            {
                formationManager.CreateNewFormation(squadronAgents, defaultFormation);
            }
        }
        
        GameObject SelectAircraftPrefab(int index)
        {
            if (index == 0 && leaderPrefab != null)
                return leaderPrefab;
            else if (fighterJetPrefab != null)
                return fighterJetPrefab;
            else if (transportPrefab != null)
                return transportPrefab;
            
            return null;
        }
        
        GameObject CreateDefaultAircraft(Vector3 position, bool isLeader)
        {
            GameObject aircraft = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            aircraft.name = isLeader ? "Leader Aircraft" : "Fighter Aircraft";
            aircraft.transform.position = position;
            aircraft.transform.localScale = isLeader ? 
                new Vector3(0.5f, 0.3f, 1.2f) : 
                new Vector3(0.4f, 0.25f, 1f);
            
            return aircraft;
        }
        
        void SetupAircraftVisual(Agent aircraft, bool isLeader)
        {
            Renderer renderer = aircraft.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = isLeader ? 
                    (leaderMaterial != null ? leaderMaterial : new Material(Shader.Find("Standard")) { color = Color.yellow }) :
                    (jetMaterial != null ? jetMaterial : new Material(Shader.Find("Standard")) { color = Color.gray });
                    
                renderer.material = material;
            }
            
            // Add formation lights
            AddFormationLights(aircraft, isLeader);
            
            // Add trail for flight path
            TrailRenderer trail = aircraft.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = aircraft.gameObject.AddComponent<TrailRenderer>();
                
            trail.time = 3f;
            trail.startWidth = isLeader ? 0.3f : 0.2f;
            trail.endWidth = 0.05f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.material.color = isLeader ? 
                new Color(1f, 1f, 0f, 0.8f) : 
                new Color(0.8f, 0.8f, 1f, 0.6f);
        }
        
        void AddFormationLights(Agent aircraft, bool isLeader)
        {
            // Navigation lights simulation
            GameObject lightObj = new GameObject("FormationLight");
            lightObj.transform.SetParent(aircraft.transform);
            lightObj.transform.localPosition = Vector3.up * 0.2f;
            
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = isLeader ? Color.red : formationLightColor;
            light.intensity = 0.5f;
            light.range = 8f;
            
            // Blinking effect for realism
            LightBlinker blinker = lightObj.AddComponent<LightBlinker>();
            blinker.blinkRate = isLeader ? 1f : 2f;
        }
        
        void AddMissionBehavior(Agent aircraft)
        {
            // Add waypoint following behavior to leaders
            WaypointBehavior waypointBehavior = new WaypointBehavior();
            waypointBehavior.waypoints = waypoints.ToList();
            waypointBehavior.waypointReachDistance = waypointReachDistance;
            aircraft.AddBehavior(waypointBehavior);
        }
        
        void AddWindBehavior(Agent aircraft)
        {
            // Simple wind effect behavior
            WindBehavior windBehavior = new WindBehavior();
            windBehavior.windDirection = windDirection;
            windBehavior.windStrength = windStrength;
            aircraft.AddBehavior(windBehavior);
        }
        
        void CreateContrailEffect(Agent aircraft)
        {
            if (contrailEffect == null) return;
            
            GameObject contrailObj = Instantiate(contrailEffect.gameObject);
            contrailObj.transform.SetParent(aircraft.transform);
            contrailObj.transform.localPosition = Vector3.back * 0.5f;
            
            ParticleSystem contrail = contrailObj.GetComponent<ParticleSystem>();
            aircraftContrails[aircraft] = contrail;
            
            // Configure contrail based on aircraft speed
            var emission = contrail.emission;
            emission.rateOverTime = aircraft.maxSpeed * 2f;
        }
        
        void Update()
        {
            UpdateMission();
            UpdateFormationStatistics();
            UpdateVisualEffects();
            UpdateUI();
        }
        
        void UpdateMission()
        {
            if (!followWaypoints || waypoints == null || waypoints.Length == 0) return;
            
            // Check if formations have reached current waypoint
            bool allFormationsAtWaypoint = true;
            
            foreach (Formation formation in formationManager.GetActiveFormations())
            {
                if (formation.leader != null)
                {
                    float distance = Vector3.Distance(formation.leader.position, waypoints[currentWaypointIndex].position);
                    if (distance > waypointReachDistance)
                    {
                        allFormationsAtWaypoint = false;
                        break;
                    }
                }
            }
            
            if (allFormationsAtWaypoint)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                
                // Update waypoint behaviors
                foreach (Formation formation in formationManager.GetActiveFormations())
                {
                    if (formation.leader != null)
                    {
                        var waypointBehavior = formation.leader.behaviors.OfType<WaypointBehavior>().FirstOrDefault();
                        if (waypointBehavior != null)
                        {
                            waypointBehavior.currentWaypointIndex = currentWaypointIndex;
                        }
                    }
                }
            }
        }
        
        void UpdateFormationStatistics()
        {
            var formations = formationManager.GetActiveFormations();
            
            if (formations.Count > 0)
            {
                averageFormationTightness = formations.Average(f => f.formationTightness);
            }
            
            // Track flight distance
            foreach (Agent aircraft in swarmManager.agents)
            {
                totalFlightDistance += aircraft.velocity.magnitude * Time.deltaTime;
            }
        }
        
        void UpdateVisualEffects()
        {
            // Update contrails based on flight conditions
            foreach (var kvp in aircraftContrails.ToList())
            {
                Agent aircraft = kvp.Key;
                ParticleSystem contrail = kvp.Value;
                
                if (aircraft == null || !aircraft.isAlive)
                {
                    aircraftContrails.Remove(aircraft);
                    if (contrail != null)
                        DestroyImmediate(contrail.gameObject);
                    continue;
                }
                
                // Adjust contrail based on speed and altitude
                var emission = contrail.emission;
                float speedFactor = aircraft.velocity.magnitude / maxSpeed;
                float altitudeFactor = Mathf.Clamp01(aircraft.position.y / 20f);
                
                emission.rateOverTime = speedFactor * altitudeFactor * 30f;
            }
        }
        
        void UpdateUI()
        {
            if (formationStatsText == null) return;
            
            float missionTime = Time.time - missionStartTime;
            var formations = formationManager.GetActiveFormations();
            int totalAircraft = formationManager.GetTotalAgentsInFormation();
            
            // Count aircraft by state
            int inFormation = 0;
            int forming = 0;
            int breaking = 0;
            
            foreach (Agent aircraft in swarmManager.agents)
            {
                var formationBehavior = aircraft.behaviors.OfType<FormationBehavior>().FirstOrDefault();
                if (formationBehavior != null)
                {
                    switch (formationBehavior.GetCurrentState())
                    {
                        case FormationState.InFormation:
                            inFormation++;
                            break;
                        case FormationState.Forming:
                        case FormationState.Reforming:
                            forming++;
                            break;
                        case FormationState.Breaking:
                            breaking++;
                            break;
                    }
                }
            }
            
            formationStatsText.text = $"=== FORMATION FLIGHT STATUS ===\\n" +
                                     $"Mission Time: {missionTime:F0}s\\n" +
                                     $"Current Waypoint: {currentWaypointIndex + 1}/{waypoints.Length}\\n\\n" +
                                     $"Squadrons: {formations.Count}\\n" +
                                     $"Total Aircraft: {totalAircraft}\\n" +
                                     $"  • In Formation: {inFormation}\\n" +
                                     $"  • Forming: {forming}\\n" +
                                     $"  • Breaking: {breaking}\\n\\n" +
                                     $"Formation Quality: {averageFormationTightness:P0}\\n" +
                                     $"Formation Changes: {totalFormationChanges}\\n" +
                                     $"Distance Flown: {totalFlightDistance:F0}m\\n" +
                                     $"Wind Strength: {windStrength:F1}\\n\\n" +
                                     $"Active Formation: {defaultFormation}";
        }
        
        // UI Event Handlers
        void OnFormationTypeChanged(int value)
        {
            FormationType newType = (FormationType)value;
            ChangeAllFormationsTo(newType);
        }
        
        void OnFormationSpacingChanged(float value)
        {
            formationSpacing = value;
            formationManager.formationSpacing = value;
            
            // Update existing formations
            foreach (Formation formation in formationManager.GetActiveFormations())
            {
                formation.spacing = value;
                
                foreach (Agent member in formation.members)
                {
                    var formationBehavior = member.behaviors.OfType<FormationBehavior>().FirstOrDefault();
                    if (formationBehavior != null)
                    {
                        formationBehavior.formationSpacing = value;
                    }
                }
            }
        }
        
        void OnWindStrengthChanged(float value)
        {
            windStrength = value;
            
            // Update wind behaviors
            foreach (Agent aircraft in swarmManager.agents)
            {
                var windBehavior = aircraft.behaviors.OfType<WindBehavior>().FirstOrDefault();
                if (windBehavior != null)
                {
                    windBehavior.windStrength = value;
                }
            }
        }
        
        public void CycleFormationType()
        {
            FormationType[] types = (FormationType[])System.Enum.GetValues(typeof(FormationType));
            int currentIndex = System.Array.IndexOf(types, defaultFormation);
            int nextIndex = (currentIndex + 1) % types.Length;
            
            defaultFormation = types[nextIndex];
            ChangeAllFormationsTo(defaultFormation);
            
            if (formationTypeDropdown != null)
                formationTypeDropdown.value = nextIndex;
                
            totalFormationChanges++;
        }
        
        void ChangeAllFormationsTo(FormationType newType)
        {
            defaultFormation = newType;
            
            foreach (Formation formation in formationManager.GetActiveFormations())
            {
                formationManager.ChangeFormationType(formation, newType);
            }
        }
        
        public void AddNewSquadron()
        {
            SpawnSquadron(formationManager.GetActiveFormations().Count);
        }
        
        public void ExecuteManeuver()
        {
            // Execute a formation maneuver (e.g., split, rejoin)
            var formations = formationManager.GetActiveFormations();
            if (formations.Count > 0)
            {
                Formation formation = formations[Random.Range(0, formations.Count)];
                
                // Temporarily break formation
                foreach (Agent member in formation.members)
                {
                    var formationBehavior = member.behaviors.OfType<FormationBehavior>().FirstOrDefault();
                    if (formationBehavior != null)
                    {
                        formationBehavior.allowFormationBreaking = true;
                        // Force a temporary break
                        member.transform.position += Random.insideUnitSphere * 5f;
                    }
                }
            }
        }
        
        void OnDrawGizmos()
        {
            // Draw flight area
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, flightArea);
            
            // Draw waypoints
            if (waypoints != null)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(waypoints[i].position, 3f);
                        
                        // Draw path between waypoints
                        if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                        }
                        else if (i == waypoints.Length - 1 && waypoints[0] != null)
                        {
                            Gizmos.DrawLine(waypoints[i].position, waypoints[0].position);
                        }
                    }
                }
                
                // Highlight current waypoint
                if (currentWaypointIndex < waypoints.Length && waypoints[currentWaypointIndex] != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(waypoints[currentWaypointIndex].position, 5f);
                }
            }
        }
    }
    
    // Helper components
    public class LightBlinker : MonoBehaviour
    {
        public float blinkRate = 1f;
        private Light lightComponent;
        private float timer = 0f;
        
        void Start()
        {
            lightComponent = GetComponent<Light>();
        }
        
        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= 1f / blinkRate)
            {
                timer = 0f;
                if (lightComponent != null)
                    lightComponent.enabled = !lightComponent.enabled;
            }
        }
    }
    
    // Additional behaviors for formation flying
    [System.Serializable]
    public class WaypointBehavior : BaseBehavior
    {
        public List<Transform> waypoints = new List<Transform>();
        public int currentWaypointIndex = 0;
        public float waypointReachDistance = 10f;
        
        public WaypointBehavior()
        {
            behaviorName = "Waypoint Following";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
                return Vector3.zero;
            
            Transform currentWaypoint = waypoints[currentWaypointIndex];
            if (currentWaypoint == null) return Vector3.zero;
            
            Vector3 desired = currentWaypoint.position - agent.position;
            desired.Normalize();
            desired *= agent.maxSpeed;
            
            Vector3 steer = desired - agent.velocity;
            return Vector3.ClampMagnitude(steer, agent.maxForce);
        }
    }
    
    [System.Serializable]
    public class WindBehavior : BaseBehavior
    {
        public Vector3 windDirection = Vector3.forward;
        public float windStrength = 1f;
        
        public WindBehavior()
        {
            behaviorName = "Wind Effects";
        }
        
        public override Vector3 Calculate(Agent agent)
        {
            return windDirection.normalized * windStrength;
        }
    }
}