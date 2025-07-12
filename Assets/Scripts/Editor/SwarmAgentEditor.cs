#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SwarmAI;
using SwarmAI.Behaviors;

namespace SwarmAI.Editor
{
    [CustomEditor(typeof(SwarmAgent))]
    public class SwarmAgentEditor : UnityEditor.Editor
    {
        private SwarmAgent agent;
        private SwarmBehaviorPreset selectedPreset;
        private bool showAdvancedSettings = false;
        private bool showRuntimeMetrics = false;
        private bool showDebugControls = false;
        
        // Serialized properties
        private SerializedProperty maxSpeedProp;
        private SerializedProperty maxForceProp;
        private SerializedProperty perceptionRadiusProp;
        private SerializedProperty agentLayerProp;
        private SerializedProperty separationWeightProp;
        private SerializedProperty alignmentWeightProp;
        private SerializedProperty cohesionWeightProp;
        private SerializedProperty showDebugInfoProp;
        private SerializedProperty gizmoColorProp;
        
        // Runtime monitoring
        private float lastMetricsUpdate;
        private const float METRICS_UPDATE_INTERVAL = 0.5f;
        
        void OnEnable()
        {
            agent = (SwarmAgent)target;
            
            // Get serialized properties
            maxSpeedProp = serializedObject.FindProperty("maxSpeed");
            maxForceProp = serializedObject.FindProperty("maxForce");
            perceptionRadiusProp = serializedObject.FindProperty("perceptionRadius");
            agentLayerProp = serializedObject.FindProperty("agentLayer");
            separationWeightProp = serializedObject.FindProperty("separationWeight");
            alignmentWeightProp = serializedObject.FindProperty("alignmentWeight");
            cohesionWeightProp = serializedObject.FindProperty("cohesionWeight");
            showDebugInfoProp = serializedObject.FindProperty("showDebugInfo");
            gizmoColorProp = serializedObject.FindProperty("gizmoColor");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            DrawHeader();
            EditorGUILayout.Space();
            
            DrawPresetSection();
            EditorGUILayout.Space();
            
            DrawMovementSection();
            EditorGUILayout.Space();
            
            DrawBehaviorSection();
            EditorGUILayout.Space();
            
            DrawDebugSection();
            EditorGUILayout.Space();
            
            if (Application.isPlaying)
            {
                DrawRuntimeSection();
                EditorGUILayout.Space();
            }
            
            DrawAdvancedSection();
            
            serializedObject.ApplyModifiedProperties();
            
            // Repaint for runtime updates
            if (Application.isPlaying && Time.time - lastMetricsUpdate > METRICS_UPDATE_INTERVAL)
            {
                lastMetricsUpdate = Time.time;
                Repaint();
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal("box");
            
            GUILayout.Label("🐝", GUILayout.Width(30), GUILayout.Height(30));
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Swarm Agent Configuration", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Agent ID: {agent.GetInstanceID()}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawPresetSection()
        {
            EditorGUILayout.LabelField("Behavior Presets", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal("box");
            
            selectedPreset = EditorGUILayout.ObjectField("Apply Preset", selectedPreset, typeof(SwarmBehaviorPreset), false) as SwarmBehaviorPreset;
            
            EditorGUI.BeginDisabledGroup(selectedPreset == null);
            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                ApplyPreset(selectedPreset);
            }
            EditorGUI.EndDisabledGroup();
            
            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                CreatePresetFromCurrent();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (selectedPreset != null)
            {
                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.LabelField("Preset Information", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Name: {selectedPreset.PresetName}");
                EditorGUILayout.LabelField($"Description: {selectedPreset.Description}");
                EditorGUILayout.LabelField($"Performance Score: {selectedPreset.GetPerformanceScore():F1}/100");
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawMovementSection()
        {
            EditorGUILayout.LabelField("Movement Parameters", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.PropertyField(maxSpeedProp, new GUIContent("Max Speed", "Maximum movement speed"));
            EditorGUILayout.Slider(maxSpeedProp, 0.1f, 20f);
            
            EditorGUILayout.PropertyField(maxForceProp, new GUIContent("Max Force", "Maximum steering force"));
            EditorGUILayout.Slider(maxForceProp, 0.1f, 50f);
            
            EditorGUILayout.PropertyField(perceptionRadiusProp, new GUIContent("Perception Radius", "How far the agent can see neighbors"));
            EditorGUILayout.Slider(perceptionRadiusProp, 1f, 20f);
            
            EditorGUILayout.PropertyField(agentLayerProp, new GUIContent("Agent Layer", "Layer mask for detecting other agents"));
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBehaviorSection()
        {
            EditorGUILayout.LabelField("Behavior Weights", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // Real-time weight adjustment with visual feedback
            DrawWeightSlider(separationWeightProp, "Separation", "Avoids crowding neighbors", Color.red);
            DrawWeightSlider(alignmentWeightProp, "Alignment", "Steers towards average heading of neighbors", Color.blue);
            DrawWeightSlider(cohesionWeightProp, "Cohesion", "Steers towards average position of neighbors", Color.green);
            
            EditorGUILayout.Space();
            
            // Behavior balance visualization
            DrawBehaviorBalance();
            
            EditorGUILayout.Space();
            
            // Quick preset buttons
            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Flocking"))
                SetWeights(1.5f, 1.0f, 1.0f);
            if (GUILayout.Button("Schooling"))
                SetWeights(2.0f, 1.5f, 0.8f);
            if (GUILayout.Button("Swarming"))
                SetWeights(1.0f, 0.5f, 2.0f);
            if (GUILayout.Button("Wandering"))
                SetWeights(0.5f, 0.3f, 0.2f);
                
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawWeightSlider(SerializedProperty prop, string label, string tooltip, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Color indicator
            var rect = GUILayoutUtility.GetRect(15, 15);
            EditorGUI.DrawRect(rect, color);
            
            EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
            EditorGUILayout.Slider(prop, 0f, 5f, GUIContent.none);
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawBehaviorBalance()
        {
            float total = separationWeightProp.floatValue + alignmentWeightProp.floatValue + cohesionWeightProp.floatValue;
            if (total <= 0) return;
            
            EditorGUILayout.LabelField("Behavior Balance", EditorStyles.boldLabel);
            
            var rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            
            float sepRatio = separationWeightProp.floatValue / total;
            float alignRatio = alignmentWeightProp.floatValue / total;
            float cohRatio = cohesionWeightProp.floatValue / total;
            
            // Draw segmented bar
            var sepRect = new Rect(rect.x, rect.y, rect.width * sepRatio, rect.height);
            var alignRect = new Rect(sepRect.xMax, rect.y, rect.width * alignRatio, rect.height);
            var cohRect = new Rect(alignRect.xMax, rect.y, rect.width * cohRatio, rect.height);
            
            EditorGUI.DrawRect(sepRect, Color.red);
            EditorGUI.DrawRect(alignRect, Color.blue);
            EditorGUI.DrawRect(cohRect, Color.green);
            
            EditorGUI.DrawRect(rect, Color.clear);
            
            // Labels
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Sep: {sepRatio:P0}", GUILayout.Width(60));
            EditorGUILayout.LabelField($"Align: {alignRatio:P0}", GUILayout.Width(70));
            EditorGUILayout.LabelField($"Coh: {cohRatio:P0}", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawDebugSection()
        {
            EditorGUILayout.LabelField("Debug & Visualization", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.PropertyField(showDebugInfoProp, new GUIContent("Show Debug Info", "Display gizmos and debug information in scene view"));
            EditorGUILayout.PropertyField(gizmoColorProp, new GUIContent("Gizmo Color", "Color for debug gizmos"));
            
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Focus in Scene"))
                {
                    Selection.activeGameObject = agent.gameObject;
                    SceneView.lastActiveSceneView.FrameSelected();
                }
                
                if (GUILayout.Button("Highlight Neighbors"))
                {
                    HighlightNeighbors();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRuntimeSection()
        {
            showRuntimeMetrics = EditorGUILayout.Foldout(showRuntimeMetrics, "Runtime Metrics", true);
            
            if (showRuntimeMetrics)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Vector3Field("Position", agent.transform.position);
                EditorGUILayout.Vector3Field("Velocity", agent.Velocity);
                EditorGUILayout.FloatField("Speed", agent.Velocity.magnitude);
                EditorGUILayout.IntField("Neighbors", agent.Neighbors?.Count ?? 0);
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.Space();
                
                // Neighbor list
                if (agent.Neighbors != null && agent.Neighbors.Count > 0)
                {
                    EditorGUILayout.LabelField($"Neighbors ({agent.Neighbors.Count})", EditorStyles.boldLabel);
                    
                    for (int i = 0; i < Mathf.Min(agent.Neighbors.Count, 5); i++)
                    {
                        var neighbor = agent.Neighbors[i];
                        if (neighbor != null)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.ObjectField($"  {i+1}", neighbor.gameObject, typeof(GameObject), true);
                            float distance = Vector3.Distance(agent.transform.position, neighbor.transform.position);
                            EditorGUILayout.LabelField($"{distance:F1}m", GUILayout.Width(50));
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    if (agent.Neighbors.Count > 5)
                    {
                        EditorGUILayout.LabelField($"  ... and {agent.Neighbors.Count - 5} more");
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawAdvancedSection()
        {
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
            
            if (showAdvancedSettings)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.LabelField("Performance Tuning", EditorStyles.boldLabel);
                
                EditorGUILayout.HelpBox("These settings can significantly impact performance. Adjust carefully for large swarms.", MessageType.Info);
                
                // Performance warnings
                if (perceptionRadiusProp.floatValue > 10f)
                {
                    EditorGUILayout.HelpBox("Large perception radius may impact performance with many agents.", MessageType.Warning);
                }
                
                float totalWeight = separationWeightProp.floatValue + alignmentWeightProp.floatValue + cohesionWeightProp.floatValue;
                if (totalWeight < 0.1f)
                {
                    EditorGUILayout.HelpBox("Very low behavior weights may result in inactive agents.", MessageType.Warning);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void SetWeights(float separation, float alignment, float cohesion)
        {
            separationWeightProp.floatValue = separation;
            alignmentWeightProp.floatValue = alignment;
            cohesionWeightProp.floatValue = cohesion;
        }
        
        private void ApplyPreset(SwarmBehaviorPreset preset)
        {
            if (preset == null) return;
            
            Undo.RecordObject(agent, "Apply Swarm Behavior Preset");
            
            maxSpeedProp.floatValue = preset.MaxSpeed;
            maxForceProp.floatValue = preset.MaxForce;
            perceptionRadiusProp.floatValue = preset.PerceptionRadius;
            separationWeightProp.floatValue = preset.SeparationWeight;
            alignmentWeightProp.floatValue = preset.AlignmentWeight;
            cohesionWeightProp.floatValue = preset.CohesionWeight;
            
            EditorUtility.SetDirty(agent);
        }
        
        private void CreatePresetFromCurrent()
        {
            var preset = ScriptableObject.CreateInstance<SwarmBehaviorPreset>();
            
            // Set values from current agent
            var so = new SerializedObject(preset);
            so.FindProperty("maxSpeed").floatValue = maxSpeedProp.floatValue;
            so.FindProperty("maxForce").floatValue = maxForceProp.floatValue;
            so.FindProperty("perceptionRadius").floatValue = perceptionRadiusProp.floatValue;
            so.FindProperty("separationWeight").floatValue = separationWeightProp.floatValue;
            so.FindProperty("alignmentWeight").floatValue = alignmentWeightProp.floatValue;
            so.FindProperty("cohesionWeight").floatValue = cohesionWeightProp.floatValue;
            so.FindProperty("presetName").stringValue = $"Custom_{agent.name}";
            so.FindProperty("description").stringValue = $"Preset created from {agent.name}";
            so.ApplyModifiedProperties();
            
            // Save as asset
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Behavior Preset",
                $"SwarmBehavior_{agent.name}",
                "asset",
                "Choose location to save the behavior preset"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(preset, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = preset;
            }
        }
        
        private void HighlightNeighbors()
        {
            if (agent.Neighbors == null) return;
            
            var gameObjects = new GameObject[agent.Neighbors.Count + 1];
            gameObjects[0] = agent.gameObject;
            
            for (int i = 0; i < agent.Neighbors.Count; i++)
            {
                if (agent.Neighbors[i] != null)
                {
                    gameObjects[i + 1] = agent.Neighbors[i].gameObject;
                }
            }
            
            Selection.objects = gameObjects;
        }
        
        void OnSceneGUI()
        {
            if (agent == null) return;
            
            // Draw perception radius handle
            Handles.color = Color.yellow;
            float newRadius = Handles.RadiusHandle(Quaternion.identity, agent.transform.position, perceptionRadiusProp.floatValue);
            
            if (newRadius != perceptionRadiusProp.floatValue)
            {
                Undo.RecordObject(agent, "Change Perception Radius");
                perceptionRadiusProp.floatValue = newRadius;
            }
            
            // Draw velocity vector
            if (Application.isPlaying && agent.Velocity.magnitude > 0.1f)
            {
                Handles.color = Color.cyan;
                Handles.ArrowHandleCap(0, agent.transform.position, 
                    Quaternion.LookRotation(agent.Velocity), 
                    agent.Velocity.magnitude, EventType.Repaint);
            }
        }
    }
}
#endif