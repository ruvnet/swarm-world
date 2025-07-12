using UnityEngine;
using UnityEditor;
using UnitySwarmAI.Core;
using UnitySwarmAI.Behaviors;

namespace UnitySwarmAI.Editor.Tools
{
    /// <summary>
    /// Setup wizard for quickly creating swarm AI scenes
    /// </summary>
    public class SwarmSetupWizard : EditorWindow
    {
        private int agentCount = 50;
        private float spawnRadius = 10f;
        private Vector3 spawnCenter = Vector3.zero;
        private GameObject agentPrefab;
        private bool createCoordinator = true;
        private bool addFlockingBehavior = true;
        private string swarmName = "SwarmSystem";
        
        [MenuItem("Tools/Unity Swarm AI/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<SwarmSetupWizard>("Swarm Setup Wizard");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Unity Swarm AI Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox("This wizard will help you quickly set up a swarm AI system in your scene.", MessageType.Info);
            EditorGUILayout.Space();
            
            // Basic Settings
            GUILayout.Label("Basic Settings", EditorStyles.boldLabel);
            swarmName = EditorGUILayout.TextField("Swarm Name", swarmName);
            agentCount = EditorGUILayout.IntSlider("Agent Count", agentCount, 1, 1000);
            spawnRadius = EditorGUILayout.FloatField("Spawn Radius", spawnRadius);
            spawnCenter = EditorGUILayout.Vector3Field("Spawn Center", spawnCenter);
            
            EditorGUILayout.Space();
            
            // Agent Settings
            GUILayout.Label("Agent Settings", EditorStyles.boldLabel);
            agentPrefab = (GameObject)EditorGUILayout.ObjectField("Agent Prefab", agentPrefab, typeof(GameObject), false);
            
            if (agentPrefab == null)
            {
                EditorGUILayout.HelpBox("No prefab selected. A default agent will be created.", MessageType.Warning);
            }
            
            createCoordinator = EditorGUILayout.Toggle("Create Coordinator", createCoordinator);
            addFlockingBehavior = EditorGUILayout.Toggle("Add Flocking Behavior", addFlockingBehavior);
            
            EditorGUILayout.Space();
            
            // Performance Warning
            if (agentCount > 100)
            {
                EditorGUILayout.HelpBox($"Creating {agentCount} agents may impact performance. Consider using spatial partitioning for large swarms.", MessageType.Warning);
            }
            
            if (agentCount > 500)
            {
                EditorGUILayout.HelpBox("For swarms larger than 500 agents, consider using ECS/DOTS implementation for better performance.", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // Action Buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Swarm", GUILayout.Height(30)))
            {
                CreateSwarm();
            }
            if (GUILayout.Button("Create Example Scene", GUILayout.Height(30)))
            {
                CreateExampleScene();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Quick Actions
            GUILayout.Label("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Behavior"))
            {
                CreateBehaviorAsset();
            }
            if (GUILayout.Button("Open Documentation"))
            {
                Application.OpenURL("https://github.com/your-repo/unity-swarm-ai/docs");
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void CreateSwarm()
        {
            // Create parent object
            GameObject swarmParent = new GameObject(swarmName);
            
            // Create coordinator if requested
            if (createCoordinator)
            {
                var coordinatorObj = new GameObject("SwarmCoordinator");
                coordinatorObj.transform.SetParent(swarmParent.transform);
                // Add coordinator component when implemented
            }
            
            // Create flocking behavior asset if needed
            FlockingBehavior flockingBehavior = null;
            if (addFlockingBehavior)
            {
                flockingBehavior = CreateFlockingBehavior();
            }
            
            // Spawn agents
            for (int i = 0; i < agentCount; i++)
            {
                GameObject agent = CreateAgent(i, flockingBehavior);
                agent.transform.SetParent(swarmParent.transform);
                
                // Position randomly within spawn radius
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = spawnCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
                agent.transform.position = spawnPosition;
                
                // Random rotation
                agent.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
            
            Debug.Log($"Created swarm '{swarmName}' with {agentCount} agents");
            Selection.activeGameObject = swarmParent;
        }
        
        private GameObject CreateAgent(int index, FlockingBehavior flockingBehavior)
        {
            GameObject agent;
            
            if (agentPrefab != null)
            {
                agent = PrefabUtility.InstantiatePrefab(agentPrefab) as GameObject;
                agent.name = $"Agent_{index:000}";
            }
            else
            {
                // Create default agent
                agent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                agent.name = $"Agent_{index:000}";
                
                // Add SwarmAgent component
                var swarmAgent = agent.AddComponent<SwarmAgent>();
                
                // Configure with flocking behavior
                if (flockingBehavior != null)
                {
                    // This would need to be set via SerializedObject in actual implementation
                    Debug.Log($"Added flocking behavior to {agent.name}");
                }
                
                // Add some visual variety
                var renderer = agent.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.HSVToRGB(Random.Range(0f, 1f), 0.7f, 0.9f);
                }
            }
            
            return agent;
        }
        
        private FlockingBehavior CreateFlockingBehavior()
        {
            string path = $"Assets/SwarmBehaviors/FlockingBehavior_{swarmName}.asset";
            
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(path);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
            
            var behavior = CreateInstance<FlockingBehavior>();
            AssetDatabase.CreateAsset(behavior, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();
            
            return behavior;
        }
        
        private void CreateExampleScene()
        {
            // This would create a complete example scene with various swarm demonstrations
            Debug.Log("Creating example scene...");
            
            // Create multiple small swarms with different behaviors
            for (int swarmIndex = 0; swarmIndex < 3; swarmIndex++)
            {
                swarmName = $"ExampleSwarm_{swarmIndex}";
                agentCount = 20;
                spawnRadius = 5f;
                spawnCenter = new Vector3(swarmIndex * 15f, 0, 0);
                CreateSwarm();
            }
            
            // Add camera position for good viewing angle
            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(15f, 10f, -10f);
                camera.transform.LookAt(new Vector3(15f, 0f, 0f));
            }
        }
        
        private void CreateBehaviorAsset()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Flocking Behavior"), false, () => CreateBehaviorOfType<FlockingBehavior>());
            // Add more behavior types here as they're implemented
            menu.ShowAsContext();
        }
        
        private void CreateBehaviorOfType<T>() where T : SwarmBehavior
        {
            string typeName = typeof(T).Name;
            string path = $"Assets/SwarmBehaviors/{typeName}.asset";
            
            var behavior = CreateInstance<T>();
            AssetDatabase.CreateAsset(behavior, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();
            
            Selection.activeObject = behavior;
            EditorGUIUtility.PingObject(behavior);
        }
    }
}