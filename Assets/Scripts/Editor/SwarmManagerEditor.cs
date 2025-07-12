#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SwarmAI;
using SwarmAI.Utils;
using SwarmAI.Behaviors;
using System.Linq;

namespace SwarmAI.Editor
{
    [CustomEditor(typeof(SwarmManager))]
    public class SwarmManagerEditor : UnityEditor.Editor
    {
        private SwarmManager manager;
        private SwarmPerformanceMonitor performanceMonitor;
        
        private bool showSpatialSettings = true;
        private bool showPerformanceSettings = true;
        private bool showDebugSettings = true;
        private bool showAgentSpawning = true;
        private bool showRuntimeMetrics = false;
        
        // Agent spawning
        private GameObject agentPrefab;
        private int agentCount = 50;
        private Bounds spawnArea = new Bounds(Vector3.zero, Vector3.one * 50f);
        private SwarmBehaviorPreset spawnPreset;
        
        // Serialized properties
        private SerializedProperty cellSizeProp;
        private SerializedProperty gridSizeProp;
        private SerializedProperty useSpatialPartitioningProp;
        private SerializedProperty maxAgentsPerFrameProp;
        private SerializedProperty updateFrequencyProp;
        private SerializedProperty enableLODProp;
        private SerializedProperty lodDistanceProp;
        private SerializedProperty showPerformanceMetricsProp;
        private SerializedProperty logMetricsProp;
        
        void OnEnable()
        {
            manager = (SwarmManager)target;
            performanceMonitor = manager.GetComponent<SwarmPerformanceMonitor>();
            
            // Get serialized properties
            cellSizeProp = serializedObject.FindProperty("cellSize");
            gridSizeProp = serializedObject.FindProperty("gridSize");
            useSpatialPartitioningProp = serializedObject.FindProperty("useSpatialPartitioning");
            maxAgentsPerFrameProp = serializedObject.FindProperty("maxAgentsPerFrame");
            updateFrequencyProp = serializedObject.FindProperty("updateFrequency");
            enableLODProp = serializedObject.FindProperty("enableLOD");
            lodDistanceProp = serializedObject.FindProperty("lodDistance");
            showPerformanceMetricsProp = serializedObject.FindProperty("showPerformanceMetrics");
            logMetricsProp = serializedObject.FindProperty("logMetrics");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            EditorGUILayout.Space();
            
            DrawSpatialSettings();
            EditorGUILayout.Space();
            
            DrawPerformanceSettings();
            EditorGUILayout.Space();
            
            DrawDebugSettings();
            EditorGUILayout.Space();
            
            DrawAgentSpawning();
            EditorGUILayout.Space();
            
            if (Application.isPlaying)
            {
                DrawRuntimeMetrics();
                EditorGUILayout.Space();
                DrawPerformanceMonitoring();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal("box");
            
            GUILayout.Label("🏭", GUILayout.Width(30), GUILayout.Height(30));
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Swarm Manager", EditorStyles.boldLabel);
            
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField($"Managing {manager.TotalAgents} agents", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Not running", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSpatialSettings()
        {
            showSpatialSettings = EditorGUILayout.Foldout(showSpatialSettings, "Spatial Partitioning", true);
            
            if (showSpatialSettings)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.PropertyField(useSpatialPartitioningProp, new GUIContent("Enable Spatial Partitioning", "Use spatial grid for faster neighbor queries"));
                
                EditorGUI.BeginDisabledGroup(!useSpatialPartitioningProp.boolValue);
                
                EditorGUILayout.PropertyField(cellSizeProp, new GUIContent("Cell Size", "Size of each spatial grid cell"));
                EditorGUILayout.Slider(cellSizeProp, 1f, 50f);
                
                EditorGUILayout.PropertyField(gridSizeProp, new GUIContent("Grid Size", "Number of cells in each dimension"));
                EditorGUILayout.Slider(gridSizeProp, 10f, 500f);
                
                EditorGUI.EndDisabledGroup();
                
                // Performance estimation
                if (useSpatialPartitioningProp.boolValue)
                {
                    float worldSize = cellSizeProp.floatValue * gridSizeProp.floatValue;
                    EditorGUILayout.HelpBox($"Grid covers {worldSize}x{worldSize} world units", MessageType.Info);
                    
                    if (cellSizeProp.floatValue < 5f)
                    {
                        EditorGUILayout.HelpBox("Small cell size may create too many cells for large worlds", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Spatial partitioning disabled. May impact performance with many agents.", MessageType.Warning);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawPerformanceSettings()
        {
            showPerformanceSettings = EditorGUILayout.Foldout(showPerformanceSettings, "Performance Settings", true);
            
            if (showPerformanceSettings)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.PropertyField(maxAgentsPerFrameProp, new GUIContent("Max Agents Per Frame", "Maximum number of agents to update per frame"));
                EditorGUILayout.Slider(maxAgentsPerFrameProp, 10f, 1000f);
                
                EditorGUILayout.PropertyField(updateFrequencyProp, new GUIContent("Update Frequency", "Target updates per second"));
                EditorGUILayout.Slider(updateFrequencyProp, 10f, 120f);
                
                EditorGUILayout.PropertyField(enableLODProp, new GUIContent("Enable LOD", "Use Level of Detail system"));
                
                EditorGUI.BeginDisabledGroup(!enableLODProp.boolValue);
                EditorGUILayout.PropertyField(lodDistanceProp, new GUIContent("LOD Distance", "Distance at which to reduce update frequency"));
                EditorGUILayout.Slider(lodDistanceProp, 10f, 200f);
                EditorGUI.EndDisabledGroup();
                
                // Performance recommendations
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Performance Recommendations", EditorStyles.boldLabel);
                
                float targetFPS = 60f;
                float estimatedLoad = (manager.TotalAgents / (float)maxAgentsPerFrameProp.floatValue) * updateFrequencyProp.floatValue;
                
                if (estimatedLoad > targetFPS)
                {
                    EditorGUILayout.HelpBox($"High load estimated: {estimatedLoad:F1} updates/sec. Consider reducing agent count or update frequency.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Load looks good: {estimatedLoad:F1} updates/sec", MessageType.Info);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawDebugSettings()
        {
            showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug & Monitoring", true);
            
            if (showDebugSettings)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.PropertyField(showPerformanceMetricsProp, new GUIContent("Show Performance Metrics", "Display performance overlay"));
                EditorGUILayout.PropertyField(logMetricsProp, new GUIContent("Log Metrics", "Write performance metrics to console"));
                
                EditorGUILayout.Space();
                
                // Performance monitor setup
                EditorGUILayout.LabelField("Performance Monitor", EditorStyles.boldLabel);
                
                if (performanceMonitor == null)
                {
                    EditorGUILayout.HelpBox("No Performance Monitor found. Add one for detailed metrics.", MessageType.Info);
                    
                    if (GUILayout.Button("Add Performance Monitor"))
                    {
                        performanceMonitor = manager.gameObject.AddComponent<SwarmPerformanceMonitor>();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Performance Monitor attached and ready.", MessageType.Info);
                    
                    if (Application.isPlaying)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        if (GUILayout.Button("Export Metrics"))
                        {
                            performanceMonitor.ExportToCSV($"swarm_metrics_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
                        }
                        
                        if (GUILayout.Button("Clear Data"))
                        {
                            performanceMonitor.ClearData();
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawAgentSpawning()
        {
            showAgentSpawning = EditorGUILayout.Foldout(showAgentSpawning, "Agent Spawning Tools", true);
            
            if (showAgentSpawning)
            {
                EditorGUILayout.BeginVertical("box");
                
                agentPrefab = EditorGUILayout.ObjectField("Agent Prefab", agentPrefab, typeof(GameObject), false) as GameObject;
                agentCount = EditorGUILayout.IntSlider("Agent Count", agentCount, 1, 1000);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField("Spawn Area", EditorStyles.boldLabel);
                spawnArea.center = EditorGUILayout.Vector3Field("Center", spawnArea.center);
                spawnArea.size = EditorGUILayout.Vector3Field("Size", spawnArea.size);
                
                EditorGUILayout.Space();
                
                spawnPreset = EditorGUILayout.ObjectField("Behavior Preset", spawnPreset, typeof(SwarmBehaviorPreset), false) as SwarmBehaviorPreset;
                
                EditorGUILayout.Space();
                
                EditorGUI.BeginDisabledGroup(agentPrefab == null);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Spawn Agents"))
                {
                    SpawnAgents();
                }
                
                if (GUILayout.Button("Clear All Agents"))
                {
                    if (EditorUtility.DisplayDialog("Clear All Agents", 
                        $"This will destroy all {manager.TotalAgents} agents. Continue?", 
                        "Yes", "Cancel"))
                    {
                        manager.ClearAllAgents();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.EndDisabledGroup();
                
                // Validate prefab
                if (agentPrefab != null)
                {
                    var agentComponent = agentPrefab.GetComponent<SwarmAgent>();
                    if (agentComponent == null)
                    {
                        EditorGUILayout.HelpBox("Prefab must have a SwarmAgent component!", MessageType.Error);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Valid agent prefab detected.", MessageType.Info);
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawRuntimeMetrics()
        {
            showRuntimeMetrics = EditorGUILayout.Foldout(showRuntimeMetrics, "Runtime Metrics", true);
            
            if (showRuntimeMetrics)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUI.BeginDisabledGroup(true);
                
                EditorGUILayout.IntField("Total Agents", manager.TotalAgents);
                EditorGUILayout.FloatField("Average Neighbors", manager.AverageNeighbors);
                EditorGUILayout.FloatField("Update Time (ms)", manager.UpdateTime);
                EditorGUILayout.FloatField("Memory Usage (MB)", manager.MemoryUsage);
                
                if (manager.SpatialGrid != null)
                {
                    EditorGUILayout.IntField("Grid Cells Used", manager.SpatialGrid.Count);
                }
                
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawPerformanceMonitoring()
        {
            if (performanceMonitor == null) return;
            
            EditorGUILayout.LabelField("Live Performance Monitor", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUI.BeginDisabledGroup(true);
            
            EditorGUILayout.FloatField("Current FPS", performanceMonitor.CurrentFPS);
            EditorGUILayout.FloatField("Average FPS", performanceMonitor.AverageFPS);
            EditorGUILayout.FloatField("Memory (MB)", performanceMonitor.CurrentMemoryMB);
            
            EditorGUI.EndDisabledGroup();
            
            // Status indicator
            var status = performanceMonitor.Status;
            var statusColor = status == SwarmPerformanceMonitor.PerformanceStatus.Good ? Color.green :
                             status == SwarmPerformanceMonitor.PerformanceStatus.Warning ? Color.yellow : Color.red;
            
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = statusColor;
            EditorGUILayout.LabelField($"Status: {status}", EditorStyles.boldLabel);
            GUI.backgroundColor = originalColor;
            
            // Recommendations
            var recommendations = performanceMonitor.GetPerformanceRecommendations();
            if (recommendations.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Recommendations:", EditorStyles.boldLabel);
                
                foreach (var recommendation in recommendations)
                {
                    EditorGUILayout.LabelField($"• {recommendation}", EditorStyles.wordWrappedLabel);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void SpawnAgents()
        {
            if (agentPrefab == null) return;
            
            Undo.RecordObject(manager, "Spawn Swarm Agents");
            
            // Spawn agents
            manager.SpawnTestAgents(agentCount, agentPrefab, spawnArea);
            
            // Apply preset if specified
            if (spawnPreset != null)
            {
                var agents = FindObjectsOfType<SwarmAgent>();
                foreach (var agent in agents)
                {
                    spawnPreset.ApplyToAgent(agent);
                }
            }
            
            EditorUtility.SetDirty(manager);
        }
        
        void OnSceneGUI()
        {
            if (manager == null) return;
            
            // Draw spawn area bounds
            if (showAgentSpawning)
            {
                Handles.color = Color.cyan;
                Handles.DrawWireCube(spawnArea.center, spawnArea.size);
                
                // Handle for moving spawn area
                Vector3 newCenter = Handles.PositionHandle(spawnArea.center, Quaternion.identity);
                if (newCenter != spawnArea.center)
                {
                    spawnArea.center = newCenter;
                    Repaint();
                }
            }
            
            // Draw spatial grid if enabled
            if (useSpatialPartitioningProp.boolValue && showDebugSettings)
            {
                Handles.color = new Color(1, 1, 1, 0.1f);
                
                float cellSize = cellSizeProp.floatValue;
                int gridSize = gridSizeProp.intValue;
                
                for (int x = -gridSize/2; x < gridSize/2; x++)
                {
                    for (int z = -gridSize/2; z < gridSize/2; z++)
                    {
                        Vector3 cellCenter = new Vector3(x * cellSize, 0, z * cellSize);
                        Handles.DrawWireCube(cellCenter, Vector3.one * cellSize);
                    }
                }
            }
        }
    }
}
#endif