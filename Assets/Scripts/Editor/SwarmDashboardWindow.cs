#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SwarmAI;
using SwarmAI.Utils;
using SwarmAI.Behaviors;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI.Editor
{
    public class SwarmDashboardWindow : EditorWindow
    {
        private static SwarmDashboardWindow window;
        
        // Tab system
        private enum Tab { Overview, Agents, Performance, Presets, Tools }
        private Tab currentTab = Tab.Overview;
        private string[] tabNames = { "Overview", "Agents", "Performance", "Presets", "Tools" };
        
        // References
        private SwarmManager swarmManager;
        private SwarmPerformanceMonitor performanceMonitor;
        private SwarmAgent[] allAgents;
        private SwarmBehaviorPreset[] allPresets;
        
        // GUI state
        private Vector2 scrollPosition;
        private bool autoRefresh = true;
        private float refreshInterval = 1f;
        private float lastRefreshTime;
        
        // Performance data
        private List<float> fpsHistory = new List<float>();
        private List<float> memoryHistory = new List<float>();
        private const int MAX_HISTORY_SAMPLES = 60;
        
        [MenuItem("Tools/Swarm AI/Dashboard")]
        public static void ShowWindow()
        {
            window = GetWindow<SwarmDashboardWindow>("Swarm Dashboard");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        void OnEnable()
        {
            RefreshReferences();
            EditorApplication.update += OnEditorUpdate;
        }
        
        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        
        void OnEditorUpdate()
        {
            if (autoRefresh && Time.realtimeSinceStartup - lastRefreshTime > refreshInterval)
            {
                RefreshReferences();
                CollectPerformanceData();
                lastRefreshTime = Time.realtimeSinceStartup;
                Repaint();
            }
        }
        
        void OnGUI()
        {
            DrawHeader();
            DrawTabs();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (currentTab)
            {
                case Tab.Overview:
                    DrawOverviewTab();
                    break;
                case Tab.Agents:
                    DrawAgentsTab();
                    break;
                case Tab.Performance:
                    DrawPerformanceTab();
                    break;
                case Tab.Presets:
                    DrawPresetsTab();
                    break;
                case Tab.Tools:
                    DrawToolsTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
            
            DrawFooter();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal("toolbar");
            
            GUILayout.Label("🐝 Swarm AI Dashboard", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshReferences();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTabs()
        {
            currentTab = (Tab)GUILayout.Toolbar((int)currentTab, tabNames);
        }
        
        private void DrawOverviewTab()
        {
            EditorGUILayout.LabelField("System Overview", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // System status
            DrawSystemStatus();
            EditorGUILayout.Space();
            
            // Quick stats
            DrawQuickStats();
            EditorGUILayout.Space();
            
            // Performance summary
            DrawPerformanceSummary();
        }
        
        private void DrawSystemStatus()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("System Status", EditorStyles.boldLabel);
            
            // Swarm Manager status
            var managerStatus = swarmManager != null ? "✅ Active" : "❌ Not Found";
            var managerColor = swarmManager != null ? Color.green : Color.red;
            
            var originalColor = GUI.color;
            GUI.color = managerColor;
            EditorGUILayout.LabelField($"Swarm Manager: {managerStatus}");
            GUI.color = originalColor;
            
            // Performance Monitor status
            var monitorStatus = performanceMonitor != null ? "✅ Active" : "⚠️ Not Found";
            var monitorColor = performanceMonitor != null ? Color.green : Color.yellow;
            
            GUI.color = monitorColor;
            EditorGUILayout.LabelField($"Performance Monitor: {monitorStatus}");
            GUI.color = originalColor;
            
            // Application status
            var appStatus = Application.isPlaying ? "▶️ Running" : "⏸️ Stopped";
            EditorGUILayout.LabelField($"Application: {appStatus}");
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawQuickStats()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Quick Statistics", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // Agents column
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Agents", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total: {allAgents?.Length ?? 0}");
            if (swarmManager != null)
            {
                EditorGUILayout.LabelField($"Active: {swarmManager.TotalAgents}");
                EditorGUILayout.LabelField($"Avg Neighbors: {swarmManager.AverageNeighbors:F1}");
            }
            EditorGUILayout.EndVertical();
            
            // Performance column
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Performance", EditorStyles.boldLabel);
            if (performanceMonitor != null)
            {
                EditorGUILayout.LabelField($"FPS: {performanceMonitor.CurrentFPS:F1}");
                EditorGUILayout.LabelField($"Memory: {performanceMonitor.CurrentMemoryMB:F1} MB");
                EditorGUILayout.LabelField($"Status: {performanceMonitor.Status}");
            }
            else
            {
                EditorGUILayout.LabelField("No monitor");
            }
            EditorGUILayout.EndVertical();
            
            // Presets column
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Available: {allPresets?.Length ?? 0}");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPerformanceSummary()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Performance Summary", EditorStyles.boldLabel);
            
            if (performanceMonitor != null)
            {
                // Status indicator
                var status = performanceMonitor.Status;
                var statusColor = GetStatusColor(status);
                
                GUI.color = statusColor;
                EditorGUILayout.LabelField($"Overall Status: {status}", EditorStyles.boldLabel);
                GUI.color = Color.white;
                
                // Quick metrics
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Current FPS: {performanceMonitor.CurrentFPS:F1}");
                EditorGUILayout.LabelField($"Average FPS: {performanceMonitor.AverageFPS:F1}");
                EditorGUILayout.EndHorizontal();
                
                // Recommendations
                var recommendations = performanceMonitor.GetPerformanceRecommendations();
                if (recommendations.Count > 0)
                {
                    EditorGUILayout.LabelField("Top Recommendation:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"• {recommendations[0]}", EditorStyles.wordWrappedLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Add a SwarmPerformanceMonitor component for detailed performance analysis.", MessageType.Info);
                
                if (GUILayout.Button("Add Performance Monitor"))
                {
                    if (swarmManager != null)
                    {
                        swarmManager.gameObject.AddComponent<SwarmPerformanceMonitor>();
                        RefreshReferences();
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAgentsTab()
        {
            EditorGUILayout.LabelField("Agent Management", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (allAgents == null || allAgents.Length == 0)
            {
                EditorGUILayout.HelpBox("No SwarmAgent components found in the scene.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.LabelField($"Found {allAgents.Length} agents:", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Agent list
            foreach (var agent in allAgents)
            {
                if (agent == null) continue;
                
                EditorGUILayout.BeginHorizontal("box");
                
                EditorGUILayout.ObjectField(agent, typeof(SwarmAgent), true);
                
                if (Application.isPlaying)
                {
                    EditorGUILayout.LabelField($"Speed: {agent.Velocity.magnitude:F1}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Neighbors: {agent.Neighbors?.Count ?? 0}", GUILayout.Width(80));
                }
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = agent.gameObject;
                    SceneView.lastActiveSceneView.FrameSelected();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            
            // Bulk actions
            EditorGUILayout.LabelField("Bulk Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Select All Agents"))
            {
                Selection.objects = allAgents.Select(a => a.gameObject).ToArray();
            }
            
            if (GUILayout.Button("Focus on Swarm"))
            {
                Selection.objects = allAgents.Select(a => a.gameObject).ToArray();
                SceneView.lastActiveSceneView.FrameSelected();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawPerformanceTab()
        {
            EditorGUILayout.LabelField("Performance Analysis", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (performanceMonitor == null)
            {
                EditorGUILayout.HelpBox("No SwarmPerformanceMonitor found. Add one for detailed metrics.", MessageType.Warning);
                
                if (GUILayout.Button("Add Performance Monitor"))
                {
                    if (swarmManager != null)
                    {
                        swarmManager.gameObject.AddComponent<SwarmPerformanceMonitor>();
                        RefreshReferences();
                    }
                }
                return;
            }
            
            // Current metrics
            DrawCurrentMetrics();
            EditorGUILayout.Space();
            
            // Performance graphs
            DrawPerformanceGraphs();
            EditorGUILayout.Space();
            
            // Recommendations
            DrawRecommendations();
            EditorGUILayout.Space();
            
            // Actions
            DrawPerformanceActions();
        }
        
        private void DrawCurrentMetrics()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Current Metrics", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Frame Rate");
            EditorGUILayout.LabelField($"Current: {performanceMonitor.CurrentFPS:F1} FPS");
            EditorGUILayout.LabelField($"Average: {performanceMonitor.AverageFPS:F1} FPS");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Memory Usage");
            EditorGUILayout.LabelField($"Current: {performanceMonitor.CurrentMemoryMB:F1} MB");
            EditorGUILayout.LabelField($"Average: {performanceMonitor.AverageMemoryMB:F1} MB");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Swarm Metrics");
            if (swarmManager != null)
            {
                EditorGUILayout.LabelField($"Agents: {swarmManager.TotalAgents}");
                EditorGUILayout.LabelField($"Update Time: {swarmManager.UpdateTime:F2} ms");
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            // Status
            var status = performanceMonitor.Status;
            var statusColor = GetStatusColor(status);
            
            GUI.color = statusColor;
            EditorGUILayout.LabelField($"Overall Status: {status}", EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPerformanceGraphs()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Performance Graphs", EditorStyles.boldLabel);
            
            // FPS graph
            if (fpsHistory.Count > 1)
            {
                EditorGUILayout.LabelField("FPS History");
                DrawGraph(fpsHistory, Color.green, 0f, 120f, 100f);
            }
            
            EditorGUILayout.Space();
            
            // Memory graph
            if (memoryHistory.Count > 1)
            {
                EditorGUILayout.LabelField("Memory History (MB)");
                DrawGraph(memoryHistory, Color.blue, 0f, memoryHistory.Max() * 1.1f, 50f);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawGraph(List<float> values, Color color, float minY, float maxY, float height)
        {
            var rect = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 1f));
            
            // Draw graph
            if (values.Count > 1)
            {
                var points = new Vector3[values.Count];
                
                for (int i = 0; i < values.Count; i++)
                {
                    float x = rect.x + (i / (float)(values.Count - 1)) * rect.width;
                    float y = rect.y + rect.height - ((values[i] - minY) / (maxY - minY)) * rect.height;
                    points[i] = new Vector3(x, y, 0);
                }
                
                Handles.BeginGUI();
                Handles.color = color;
                Handles.DrawPolyLine(points);
                Handles.EndGUI();
            }
            
            // Draw current value
            if (values.Count > 0)
            {
                var currentValue = values[values.Count - 1];
                GUI.Label(new Rect(rect.x + 5, rect.y + 5, 100, 20), $"Current: {currentValue:F1}");
            }
        }
        
        private void DrawRecommendations()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Performance Recommendations", EditorStyles.boldLabel);
            
            var recommendations = performanceMonitor.GetPerformanceRecommendations();
            
            foreach (var recommendation in recommendations)
            {
                EditorGUILayout.LabelField($"• {recommendation}", EditorStyles.wordWrappedLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPerformanceActions()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Export Metrics"))
            {
                performanceMonitor.ExportToCSV($"swarm_metrics_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            
            if (GUILayout.Button("Clear Data"))
            {
                performanceMonitor.ClearData();
                fpsHistory.Clear();
                memoryHistory.Clear();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPresetsTab()
        {
            EditorGUILayout.LabelField("Behavior Presets", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            if (allPresets == null || allPresets.Length == 0)
            {
                EditorGUILayout.HelpBox("No SwarmBehaviorPreset assets found in the project.", MessageType.Info);
                
                if (GUILayout.Button("Create New Preset"))
                {
                    CreateNewPreset();
                }
                return;
            }
            
            EditorGUILayout.LabelField($"Found {allPresets.Length} presets:", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Preset list
            foreach (var preset in allPresets)
            {
                if (preset == null) continue;
                
                EditorGUILayout.BeginHorizontal("box");
                
                EditorGUILayout.ObjectField(preset, typeof(SwarmBehaviorPreset), false);
                EditorGUILayout.LabelField($"Score: {preset.GetPerformanceScore():F1}", GUILayout.Width(80));
                
                if (GUILayout.Button("Apply to Selected", GUILayout.Width(120)))
                {
                    ApplyPresetToSelected(preset);
                }
                
                if (GUILayout.Button("Apply to All", GUILayout.Width(100)))
                {
                    ApplyPresetToAll(preset);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            
            // Actions
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create New Preset"))
            {
                CreateNewPreset();
            }
            
            if (GUILayout.Button("Refresh Presets"))
            {
                RefreshReferences();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawToolsTab()
        {
            EditorGUILayout.LabelField("Development Tools", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Scene tools
            DrawSceneTools();
            EditorGUILayout.Space();
            
            // Debug tools
            DrawDebugTools();
            EditorGUILayout.Space();
            
            // Utility tools
            DrawUtilityTools();
        }
        
        private void DrawSceneTools()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Scene Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Open Scene Debugger"))
            {
                SwarmDebuggerWindow.ShowWindow();
            }
            
            if (GUILayout.Button("Focus on Swarm"))
            {
                FocusOnSwarm();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Select All Agents"))
            {
                SelectAllAgents();
            }
            
            if (GUILayout.Button("Create Test Swarm"))
            {
                CreateTestSwarm();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDebugTools()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
            
            if (swarmManager != null)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Clear All Agents"))
                {
                    if (EditorUtility.DisplayDialog("Clear All Agents", 
                        $"This will destroy all {swarmManager.TotalAgents} agents. Continue?", 
                        "Yes", "Cancel"))
                    {
                        swarmManager.ClearAllAgents();
                        RefreshReferences();
                    }
                }
                
                if (GUILayout.Button("Rebuild Spatial Grid"))
                {
                    // This would need to be implemented in SwarmManager
                    Debug.Log("Spatial grid rebuild requested");
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawUtilityTools()
        {
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Utility Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Generate Report"))
            {
                GenerateSystemReport();
            }
            
            if (GUILayout.Button("Open Documentation"))
            {
                Application.OpenURL("https://github.com/ruvnet/claude-flow/tree/main/examples");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal("toolbar");
            
            EditorGUILayout.LabelField($"Last Update: {System.DateTime.Now:HH:mm:ss}", EditorStyles.miniLabel);
            
            GUILayout.FlexibleSpace();
            
            refreshInterval = EditorGUILayout.Slider("Refresh Rate", refreshInterval, 0.1f, 5f, GUILayout.Width(200));
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void RefreshReferences()
        {
            swarmManager = Object.FindObjectOfType<SwarmManager>();
            performanceMonitor = Object.FindObjectOfType<SwarmPerformanceMonitor>();
            allAgents = Object.FindObjectsOfType<SwarmAgent>();
            
            // Find all behavior presets in the project
            string[] guids = AssetDatabase.FindAssets("t:SwarmBehaviorPreset");
            allPresets = new SwarmBehaviorPreset[guids.Length];
            
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                allPresets[i] = AssetDatabase.LoadAssetAtPath<SwarmBehaviorPreset>(path);
            }
        }
        
        private void CollectPerformanceData()
        {
            if (performanceMonitor == null) return;
            
            fpsHistory.Add(performanceMonitor.CurrentFPS);
            memoryHistory.Add(performanceMonitor.CurrentMemoryMB);
            
            if (fpsHistory.Count > MAX_HISTORY_SAMPLES)
                fpsHistory.RemoveAt(0);
            
            if (memoryHistory.Count > MAX_HISTORY_SAMPLES)
                memoryHistory.RemoveAt(0);
        }
        
        private Color GetStatusColor(SwarmPerformanceMonitor.PerformanceStatus status)
        {
            switch (status)
            {
                case SwarmPerformanceMonitor.PerformanceStatus.Good: return Color.green;
                case SwarmPerformanceMonitor.PerformanceStatus.Warning: return Color.yellow;
                case SwarmPerformanceMonitor.PerformanceStatus.Critical: return Color.red;
                default: return Color.white;
            }
        }
        
        private void ApplyPresetToSelected(SwarmBehaviorPreset preset)
        {
            var selectedObjects = Selection.gameObjects;
            int count = 0;
            
            foreach (var obj in selectedObjects)
            {
                var agent = obj.GetComponent<SwarmAgent>();
                if (agent != null)
                {
                    Undo.RecordObject(agent, "Apply Behavior Preset");
                    preset.ApplyToAgent(agent);
                    EditorUtility.SetDirty(agent);
                    count++;
                }
            }
            
            Debug.Log($"Applied preset '{preset.PresetName}' to {count} selected agents");
        }
        
        private void ApplyPresetToAll(SwarmBehaviorPreset preset)
        {
            if (allAgents == null || allAgents.Length == 0) return;
            
            if (EditorUtility.DisplayDialog("Apply to All Agents", 
                $"Apply preset '{preset.PresetName}' to all {allAgents.Length} agents?", 
                "Yes", "Cancel"))
            {
                foreach (var agent in allAgents)
                {
                    if (agent != null)
                    {
                        Undo.RecordObject(agent, "Apply Behavior Preset to All");
                        preset.ApplyToAgent(agent);
                        EditorUtility.SetDirty(agent);
                    }
                }
                
                Debug.Log($"Applied preset '{preset.PresetName}' to all {allAgents.Length} agents");
            }
        }
        
        private void CreateNewPreset()
        {
            var preset = ScriptableObject.CreateInstance<SwarmBehaviorPreset>();
            
            string path = EditorUtility.SaveFilePanelInProject(
                "Create New Behavior Preset",
                "NewSwarmBehavior",
                "asset",
                "Choose location for the new behavior preset"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
                RefreshReferences();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = preset;
            }
        }
        
        private void FocusOnSwarm()
        {
            if (allAgents != null && allAgents.Length > 0)
            {
                Selection.objects = allAgents.Select(a => a.gameObject).ToArray();
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }
        
        private void SelectAllAgents()
        {
            if (allAgents != null && allAgents.Length > 0)
            {
                Selection.objects = allAgents.Select(a => a.gameObject).ToArray();
            }
        }
        
        private void CreateTestSwarm()
        {
            // This would open a wizard for creating test swarms
            Debug.Log("Test swarm creation requested - implement wizard");
        }
        
        private void GenerateSystemReport()
        {
            string report = "Swarm AI System Report\n";
            report += $"Generated: {System.DateTime.Now}\n\n";
            
            report += "System Status:\n";
            report += $"• Swarm Manager: {(swarmManager != null ? "Active" : "Missing")}\n";
            report += $"• Performance Monitor: {(performanceMonitor != null ? "Active" : "Missing")}\n";
            report += $"• Application State: {(Application.isPlaying ? "Running" : "Stopped")}\n\n";
            
            report += "Statistics:\n";
            report += $"• Total Agents: {allAgents?.Length ?? 0}\n";
            report += $"• Available Presets: {allPresets?.Length ?? 0}\n";
            
            if (swarmManager != null)
            {
                report += $"• Active Agents: {swarmManager.TotalAgents}\n";
                report += $"• Average Neighbors: {swarmManager.AverageNeighbors:F1}\n";
                report += $"• Update Time: {swarmManager.UpdateTime:F2} ms\n";
            }
            
            if (performanceMonitor != null)
            {
                report += $"• Current FPS: {performanceMonitor.CurrentFPS:F1}\n";
                report += $"• Memory Usage: {performanceMonitor.CurrentMemoryMB:F1} MB\n";
                report += $"• Performance Status: {performanceMonitor.Status}\n";
            }
            
            Debug.Log(report);
            EditorUtility.DisplayDialog("System Report", "Report generated and logged to console.", "OK");
        }
    }
}
#endif