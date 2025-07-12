#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SwarmAI;
using SwarmAI.Utils;
using System.Linq;

namespace SwarmAI.Editor
{
    [InitializeOnLoad]
    public class SwarmSceneViewDebugger
    {
        private static bool isEnabled = false;
        private static bool showPerceptionRadii = true;
        private static bool showVelocityVectors = true;
        private static bool showNeighborConnections = false;
        private static bool showSpatialGrid = false;
        private static bool showPerformanceOverlay = true;
        
        private static SwarmAgent selectedAgent;
        private static SwarmManager swarmManager;
        private static SwarmPerformanceMonitor performanceMonitor;
        
        private static GUIStyle overlayStyle;
        private static GUIStyle agentInfoStyle;
        
        static SwarmSceneViewDebugger()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            
            // Initialize styles
            InitializeStyles();
        }
        
        private static void InitializeStyles()
        {
            overlayStyle = new GUIStyle();
            overlayStyle.normal.background = MakeTexture(1, 1, new Color(0, 0, 0, 0.8f));
            overlayStyle.normal.textColor = Color.white;
            overlayStyle.padding = new RectOffset(10, 10, 10, 10);
            overlayStyle.fontSize = 12;
            
            agentInfoStyle = new GUIStyle();
            agentInfoStyle.normal.background = MakeTexture(1, 1, new Color(0.2f, 0.2f, 0.2f, 0.9f));
            agentInfoStyle.normal.textColor = Color.white;
            agentInfoStyle.padding = new RectOffset(5, 5, 5, 5);
            agentInfoStyle.fontSize = 10;
        }
        
        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        
        private static void OnHierarchyChanged()
        {
            // Refresh references when hierarchy changes
            if (swarmManager == null)
                swarmManager = Object.FindObjectOfType<SwarmManager>();
            
            if (performanceMonitor == null)
                performanceMonitor = Object.FindObjectOfType<SwarmPerformanceMonitor>();
        }
        
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!isEnabled) return;
            
            DrawDebugInformation();
            DrawSceneGUI();
            DrawPerformanceOverlay(sceneView);
            
            // Handle selection
            HandleSelection();
        }
        
        private static void DrawDebugInformation()
        {
            var agents = Object.FindObjectsOfType<SwarmAgent>();
            
            foreach (var agent in agents)
            {
                if (agent == null) continue;
                
                Vector3 position = agent.transform.position;
                
                // Perception radius
                if (showPerceptionRadii)
                {
                    Handles.color = agent == selectedAgent ? Color.yellow : new Color(1, 1, 0, 0.3f);
                    Handles.DrawWireDisc(position, Vector3.up, agent.PerceptionRadius);
                }
                
                // Velocity vectors
                if (showVelocityVectors && Application.isPlaying && agent.Velocity.magnitude > 0.1f)
                {
                    Handles.color = Color.cyan;
                    Vector3 velocityEnd = position + agent.Velocity.normalized * 2f;
                    Handles.DrawLine(position, velocityEnd);
                    Handles.ArrowHandleCap(0, velocityEnd, Quaternion.LookRotation(agent.Velocity), 0.5f, EventType.Repaint);
                }
                
                // Neighbor connections
                if (showNeighborConnections && Application.isPlaying && agent.Neighbors != null)
                {
                    Handles.color = agent == selectedAgent ? Color.green : new Color(0, 1, 0, 0.3f);
                    
                    foreach (var neighbor in agent.Neighbors)
                    {
                        if (neighbor != null)
                        {
                            Handles.DrawLine(position, neighbor.transform.position);
                        }
                    }
                }
                
                // Agent info when selected
                if (agent == selectedAgent)
                {
                    DrawAgentInfo(agent);
                }
            }
        }
        
        private static void DrawAgentInfo(SwarmAgent agent)
        {
            Vector3 position = agent.transform.position;
            Vector3 screenPos = Camera.current.WorldToScreenPoint(position);
            
            if (screenPos.z > 0)
            {
                // Convert to GUI coordinates
                screenPos.y = Camera.current.pixelHeight - screenPos.y;
                
                string info = $"Agent: {agent.name}\n";
                if (Application.isPlaying)
                {
                    info += $"Speed: {agent.Velocity.magnitude:F1}\n";
                    info += $"Neighbors: {agent.Neighbors?.Count ?? 0}\n";
                    info += $"Weights: S:{agent.SeparationWeight:F1} A:{agent.AlignmentWeight:F1} C:{agent.CohesionWeight:F1}";
                }
                else
                {
                    info += "Not running";
                }
                
                Handles.BeginGUI();
                
                Vector2 size = agentInfoStyle.CalcSize(new GUIContent(info));
                Rect rect = new Rect(screenPos.x + 10, screenPos.y - size.y - 10, size.x, size.y);
                
                GUI.Box(rect, info, agentInfoStyle);
                
                Handles.EndGUI();
            }
        }
        
        private static void DrawSceneGUI()
        {
            // Spatial grid
            if (showSpatialGrid && swarmManager != null && Application.isPlaying)
            {
                DrawSpatialGrid();
            }
        }
        
        private static void DrawSpatialGrid()
        {
            if (swarmManager.SpatialGrid == null) return;
            
            Handles.color = new Color(1, 1, 1, 0.1f);
            
            // Get grid parameters through reflection or expose them
            float cellSize = 10f; // Default value, should be exposed from SwarmManager
            int gridSize = 100;   // Default value, should be exposed from SwarmManager
            
            for (int x = -gridSize/2; x < gridSize/2; x++)
            {
                for (int z = -gridSize/2; z < gridSize/2; z++)
                {
                    Vector3 cellCenter = new Vector3(x * cellSize, 0, z * cellSize);
                    
                    // Only draw cells that have agents
                    int cellKey = x + z * gridSize;
                    if (swarmManager.SpatialGrid.ContainsKey(cellKey) && 
                        swarmManager.SpatialGrid[cellKey].Count > 0)
                    {
                        Handles.color = new Color(0, 1, 0, 0.3f);
                        Handles.DrawWireCube(cellCenter, Vector3.one * cellSize);
                        
                        // Draw agent count
                        Vector3 labelPos = cellCenter + Vector3.up * 0.5f;
                        Handles.Label(labelPos, swarmManager.SpatialGrid[cellKey].Count.ToString());
                    }
                }
            }
        }
        
        private static void DrawPerformanceOverlay(SceneView sceneView)
        {
            if (!showPerformanceOverlay || !Application.isPlaying) return;
            
            Handles.BeginGUI();
            
            string overlayText = "Swarm Debug Overlay\n\n";
            
            if (swarmManager != null)
            {
                overlayText += $"Total Agents: {swarmManager.TotalAgents}\n";
                overlayText += $"Average Neighbors: {swarmManager.AverageNeighbors:F1}\n";
                overlayText += $"Update Time: {swarmManager.UpdateTime:F2}ms\n";
                overlayText += $"Memory Usage: {swarmManager.MemoryUsage:F1}MB\n";
                
                if (swarmManager.SpatialGrid != null)
                {
                    overlayText += $"Grid Cells: {swarmManager.SpatialGrid.Count}\n";
                }
            }
            
            if (performanceMonitor != null)
            {
                overlayText += $"\nFPS: {performanceMonitor.CurrentFPS:F1}\n";
                overlayText += $"Status: {performanceMonitor.Status}\n";
            }
            
            if (selectedAgent != null)
            {
                overlayText += $"\nSelected Agent: {selectedAgent.name}\n";
                if (Application.isPlaying)
                {
                    overlayText += $"Position: {selectedAgent.transform.position}\n";
                    overlayText += $"Velocity: {selectedAgent.Velocity}\n";
                    overlayText += $"Speed: {selectedAgent.Velocity.magnitude:F2}\n";
                }
            }
            
            Vector2 size = overlayStyle.CalcSize(new GUIContent(overlayText));
            Rect rect = new Rect(10, 10, size.x, size.y);
            
            GUI.Box(rect, overlayText, overlayStyle);
            
            Handles.EndGUI();
        }
        
        private static void HandleSelection()
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
                {
                    SwarmAgent agent = hit.collider.GetComponent<SwarmAgent>();
                    if (agent != null)
                    {
                        selectedAgent = agent;
                        Selection.activeGameObject = agent.gameObject;
                        Event.current.Use();
                    }
                }
            }
        }
        
        [MenuItem("Tools/Swarm AI/Scene View Debugger")]
        private static void ShowDebuggerWindow()
        {
            SwarmDebuggerWindow.ShowWindow();
        }
    }
    
    public class SwarmDebuggerWindow : EditorWindow
    {
        private static SwarmDebuggerWindow window;
        
        public static void ShowWindow()
        {
            window = GetWindow<SwarmDebuggerWindow>("Swarm Debugger");
            window.Show();
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField("Swarm Scene View Debugger", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Enable/Disable
            bool newEnabled = EditorGUILayout.Toggle("Enable Scene Debug", SwarmSceneViewDebugger.isEnabled);
            if (newEnabled != SwarmSceneViewDebugger.isEnabled)
            {
                SwarmSceneViewDebugger.isEnabled = newEnabled;
                SceneView.RepaintAll();
            }
            
            EditorGUI.BeginDisabledGroup(!SwarmSceneViewDebugger.isEnabled);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visualization Options", EditorStyles.boldLabel);
            
            // Visualization toggles
            bool newPerceptionRadii = EditorGUILayout.Toggle("Show Perception Radii", SwarmSceneViewDebugger.showPerceptionRadii);
            if (newPerceptionRadii != SwarmSceneViewDebugger.showPerceptionRadii)
            {
                SwarmSceneViewDebugger.showPerceptionRadii = newPerceptionRadii;
                SceneView.RepaintAll();
            }
            
            bool newVelocityVectors = EditorGUILayout.Toggle("Show Velocity Vectors", SwarmSceneViewDebugger.showVelocityVectors);
            if (newVelocityVectors != SwarmSceneViewDebugger.showVelocityVectors)
            {
                SwarmSceneViewDebugger.showVelocityVectors = newVelocityVectors;
                SceneView.RepaintAll();
            }
            
            bool newNeighborConnections = EditorGUILayout.Toggle("Show Neighbor Connections", SwarmSceneViewDebugger.showNeighborConnections);
            if (newNeighborConnections != SwarmSceneViewDebugger.showNeighborConnections)
            {
                SwarmSceneViewDebugger.showNeighborConnections = newNeighborConnections;
                SceneView.RepaintAll();
            }
            
            bool newSpatialGrid = EditorGUILayout.Toggle("Show Spatial Grid", SwarmSceneViewDebugger.showSpatialGrid);
            if (newSpatialGrid != SwarmSceneViewDebugger.showSpatialGrid)
            {
                SwarmSceneViewDebugger.showSpatialGrid = newSpatialGrid;
                SceneView.RepaintAll();
            }
            
            bool newPerformanceOverlay = EditorGUILayout.Toggle("Show Performance Overlay", SwarmSceneViewDebugger.showPerformanceOverlay);
            if (newPerformanceOverlay != SwarmSceneViewDebugger.showPerformanceOverlay)
            {
                SwarmSceneViewDebugger.showPerformanceOverlay = newPerformanceOverlay;
                SceneView.RepaintAll();
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Quick actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Focus on Swarm"))
            {
                FocusOnSwarm();
            }
            
            if (GUILayout.Button("Select All Agents"))
            {
                SelectAllAgents();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset Debug Settings"))
            {
                ResetDebugSettings();
            }
            
            if (GUILayout.Button("Performance Report"))
            {
                ShowPerformanceReport();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Selected agent info
            if (SwarmSceneViewDebugger.selectedAgent != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Selected Agent", EditorStyles.boldLabel);
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("Agent", SwarmSceneViewDebugger.selectedAgent, typeof(SwarmAgent), true);
                EditorGUI.EndDisabledGroup();
                
                if (Application.isPlaying)
                {
                    var agent = SwarmSceneViewDebugger.selectedAgent;
                    EditorGUILayout.LabelField($"Position: {agent.transform.position}");
                    EditorGUILayout.LabelField($"Velocity: {agent.Velocity}");
                    EditorGUILayout.LabelField($"Speed: {agent.Velocity.magnitude:F2}");
                    EditorGUILayout.LabelField($"Neighbors: {agent.Neighbors?.Count ?? 0}");
                }
            }
        }
        
        private void FocusOnSwarm()
        {
            var agents = FindObjectsOfType<SwarmAgent>();
            if (agents.Length > 0)
            {
                Selection.objects = agents.Select(a => a.gameObject).ToArray();
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }
        
        private void SelectAllAgents()
        {
            var agents = FindObjectsOfType<SwarmAgent>();
            Selection.objects = agents.Select(a => a.gameObject).ToArray();
        }
        
        private void ResetDebugSettings()
        {
            SwarmSceneViewDebugger.showPerceptionRadii = true;
            SwarmSceneViewDebugger.showVelocityVectors = true;
            SwarmSceneViewDebugger.showNeighborConnections = false;
            SwarmSceneViewDebugger.showSpatialGrid = false;
            SwarmSceneViewDebugger.showPerformanceOverlay = true;
            SceneView.RepaintAll();
        }
        
        private void ShowPerformanceReport()
        {
            var monitor = FindObjectOfType<SwarmPerformanceMonitor>();
            if (monitor != null)
            {
                var recommendations = monitor.GetPerformanceRecommendations();
                string report = "Performance Report:\n\n";
                
                report += $"Current FPS: {monitor.CurrentFPS:F1}\n";
                report += $"Average FPS: {monitor.AverageFPS:F1}\n";
                report += $"Memory Usage: {monitor.CurrentMemoryMB:F1} MB\n";
                report += $"Status: {monitor.Status}\n\n";
                
                report += "Recommendations:\n";
                foreach (var rec in recommendations)
                {
                    report += $"• {rec}\n";
                }
                
                EditorUtility.DisplayDialog("Swarm Performance Report", report, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Performance Monitor", "Add a SwarmPerformanceMonitor component to get detailed performance metrics.", "OK");
            }
        }
    }
}
#endif