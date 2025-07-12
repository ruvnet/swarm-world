#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using SwarmAI.Behaviors;
using SwarmAI;

namespace SwarmAI.Editor
{
    [CustomEditor(typeof(SwarmBehaviorPreset))]
    public class SwarmBehaviorPresetEditor : UnityEditor.Editor
    {
        private SwarmBehaviorPreset preset;
        private bool showPreview = true;
        private bool showPerformanceAnalysis = true;
        private bool showPresetLibrary = false;
        
        // Preview simulation
        private Vector3[] previewPositions;
        private Vector3[] previewVelocities;
        private int previewAgentCount = 20;
        private float previewTime = 0f;
        private bool isSimulating = false;
        
        // Performance analysis
        private float performanceScore;
        private string[] performanceNotes;
        
        void OnEnable()
        {
            preset = (SwarmBehaviorPreset)target;
            InitializePreview();
            AnalyzePerformance();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            EditorGUILayout.Space();
            
            DrawPresetInfo();
            EditorGUILayout.Space();
            
            DrawMovementParameters();
            EditorGUILayout.Space();
            
            DrawBehaviorWeights();
            EditorGUILayout.Space();
            
            DrawAdvancedSettings();
            EditorGUILayout.Space();
            
            DrawPerformanceSettings();
            EditorGUILayout.Space();
            
            DrawPreview();
            EditorGUILayout.Space();
            
            DrawPerformanceAnalysis();
            EditorGUILayout.Space();
            
            DrawActions();
            
            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
            {
                AnalyzePerformance();
                if (isSimulating)
                {
                    UpdatePreview();
                }
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal("box");
            
            // Icon
            if (preset.PreviewIcon != null)
            {
                var iconRect = GUILayoutUtility.GetRect(40, 40);
                GUI.DrawTexture(iconRect, preset.PreviewIcon.texture);
            }
            else
            {
                GUILayout.Label("🎯", GUILayout.Width(40), GUILayout.Height(40));
            }
            
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Swarm Behavior Preset", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Performance Score: {preset.GetPerformanceScore():F1}/100", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawPresetInfo()
        {
            EditorGUILayout.LabelField("Preset Information", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("presetName"), new GUIContent("Name"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), new GUIContent("Description"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("previewIcon"), new GUIContent("Icon"));
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawMovementParameters()
        {
            EditorGUILayout.LabelField("Movement Parameters", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            var maxSpeedProp = serializedObject.FindProperty("maxSpeed");
            var maxForceProp = serializedObject.FindProperty("maxForce");
            var perceptionRadiusProp = serializedObject.FindProperty("perceptionRadius");
            
            EditorGUILayout.PropertyField(maxSpeedProp, new GUIContent("Max Speed"));
            EditorGUILayout.Slider(maxSpeedProp, 0.1f, 20f);
            
            EditorGUILayout.PropertyField(maxForceProp, new GUIContent("Max Force"));
            EditorGUILayout.Slider(maxForceProp, 0.1f, 50f);
            
            EditorGUILayout.PropertyField(perceptionRadiusProp, new GUIContent("Perception Radius"));
            EditorGUILayout.Slider(perceptionRadiusProp, 1f, 20f);
            
            // Performance warnings
            if (perceptionRadiusProp.floatValue > 10f)
            {
                EditorGUILayout.HelpBox("Large perception radius may impact performance.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBehaviorWeights()
        {
            EditorGUILayout.LabelField("Behavior Weights", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            var separationProp = serializedObject.FindProperty("separationWeight");
            var alignmentProp = serializedObject.FindProperty("alignmentWeight");
            var cohesionProp = serializedObject.FindProperty("cohesionWeight");
            
            // Weight sliders with color indicators
            DrawColoredWeightSlider(separationProp, "Separation", "Avoids crowding neighbors", Color.red);
            DrawColoredWeightSlider(alignmentProp, "Alignment", "Steers towards average heading", Color.blue);
            DrawColoredWeightSlider(cohesionProp, "Cohesion", "Steers towards average position", Color.green);
            
            EditorGUILayout.Space();
            
            // Behavior balance visualization
            DrawBehaviorBalance(separationProp.floatValue, alignmentProp.floatValue, cohesionProp.floatValue);
            
            EditorGUILayout.Space();
            
            // Quick presets
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
        
        private void DrawColoredWeightSlider(SerializedProperty prop, string label, string tooltip, Color color)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Color indicator
            var rect = GUILayoutUtility.GetRect(15, 15);
            EditorGUI.DrawRect(rect, color);
            
            EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
            EditorGUILayout.Slider(prop, 0f, 5f, GUIContent.none);
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawBehaviorBalance(float separation, float alignment, float cohesion)
        {
            float total = separation + alignment + cohesion;
            if (total <= 0) return;
            
            EditorGUILayout.LabelField("Behavior Balance", EditorStyles.boldLabel);
            
            var rect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            
            float sepRatio = separation / total;
            float alignRatio = alignment / total;
            float cohRatio = cohesion / total;
            
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
        
        private void DrawAdvancedSettings()
        {
            EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useObstacleAvoidance"));
            
            EditorGUI.BeginDisabledGroup(!serializedObject.FindProperty("useObstacleAvoidance").boolValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleAvoidanceDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("obstacleLayer"));
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPerformanceSettings()
        {
            EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableLOD"));
            
            EditorGUI.BeginDisabledGroup(!serializedObject.FindProperty("enableLOD").boolValue);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lodDistance"));
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("updateFrequency"));
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPreview()
        {
            showPreview = EditorGUILayout.Foldout(showPreview, "Real-time Preview", true);
            
            if (showPreview)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.BeginHorizontal();
                
                previewAgentCount = EditorGUILayout.IntSlider("Preview Agents", previewAgentCount, 5, 50);
                
                if (GUILayout.Button(isSimulating ? "Stop" : "Start", GUILayout.Width(60)))
                {
                    isSimulating = !isSimulating;
                    if (isSimulating)
                    {
                        InitializePreview();
                        EditorApplication.update += UpdatePreview;
                    }
                    else
                    {
                        EditorApplication.update -= UpdatePreview;
                    }
                }
                
                if (GUILayout.Button("Reset", GUILayout.Width(60)))
                {
                    InitializePreview();
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (previewPositions != null)
                {
                    // Draw preview
                    var previewRect = GUILayoutUtility.GetRect(0, 200, GUILayout.ExpandWidth(true));
                    
                    // Background
                    EditorGUI.DrawRect(previewRect, new Color(0.1f, 0.1f, 0.1f, 1f));
                    
                    // Draw agents
                    for (int i = 0; i < previewPositions.Length; i++)
                    {
                        var pos = previewPositions[i];
                        var screenPos = new Vector2(
                            previewRect.x + (pos.x + 25f) / 50f * previewRect.width,
                            previewRect.y + (pos.z + 25f) / 50f * previewRect.height
                        );
                        
                        if (screenPos.x >= previewRect.x && screenPos.x <= previewRect.xMax &&
                            screenPos.y >= previewRect.y && screenPos.y <= previewRect.yMax)
                        {
                            var agentRect = new Rect(screenPos.x - 2, screenPos.y - 2, 4, 4);
                            EditorGUI.DrawRect(agentRect, Color.cyan);
                        }
                    }
                    
                    EditorGUILayout.LabelField($"Simulation Time: {previewTime:F1}s", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawPerformanceAnalysis()
        {
            showPerformanceAnalysis = EditorGUILayout.Foldout(showPerformanceAnalysis, "Performance Analysis", true);
            
            if (showPerformanceAnalysis)
            {
                EditorGUILayout.BeginVertical("box");
                
                // Performance score with color
                var scoreColor = performanceScore > 80f ? Color.green : 
                                performanceScore > 60f ? Color.yellow : Color.red;
                
                var originalColor = GUI.color;
                GUI.color = scoreColor;
                EditorGUILayout.LabelField($"Performance Score: {performanceScore:F1}/100", EditorStyles.boldLabel);
                GUI.color = originalColor;
                
                EditorGUILayout.Space();
                
                // Performance notes
                if (performanceNotes != null)
                {
                    EditorGUILayout.LabelField("Analysis Notes:", EditorStyles.boldLabel);
                    
                    foreach (var note in performanceNotes)
                    {
                        EditorGUILayout.LabelField($"• {note}", EditorStyles.wordWrappedLabel);
                    }
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Apply to Selected Agents"))
            {
                ApplyToSelectedAgents();
            }
            
            if (GUILayout.Button("Apply to All Agents"))
            {
                ApplyToAllAgents();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create Variant"))
            {
                CreateVariant();
            }
            
            if (GUILayout.Button("Export Settings"))
            {
                ExportSettings();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void SetWeights(float separation, float alignment, float cohesion)
        {
            serializedObject.FindProperty("separationWeight").floatValue = separation;
            serializedObject.FindProperty("alignmentWeight").floatValue = alignment;
            serializedObject.FindProperty("cohesionWeight").floatValue = cohesion;
        }
        
        private void InitializePreview()
        {
            previewPositions = new Vector3[previewAgentCount];
            previewVelocities = new Vector3[previewAgentCount];
            previewTime = 0f;
            
            for (int i = 0; i < previewAgentCount; i++)
            {
                previewPositions[i] = new Vector3(
                    Random.Range(-20f, 20f),
                    0,
                    Random.Range(-20f, 20f)
                );
                previewVelocities[i] = Random.insideUnitSphere * preset.MaxSpeed;
                previewVelocities[i].y = 0;
            }
        }
        
        private void UpdatePreview()
        {
            if (!isSimulating || previewPositions == null) return;
            
            float deltaTime = 0.016f; // ~60 FPS
            previewTime += deltaTime;
            
            // Simple boids simulation for preview
            for (int i = 0; i < previewPositions.Length; i++)
            {
                Vector3 separation = Vector3.zero;
                Vector3 alignment = Vector3.zero;
                Vector3 cohesion = Vector3.zero;
                int neighborCount = 0;
                
                for (int j = 0; j < previewPositions.Length; j++)
                {
                    if (i == j) continue;
                    
                    float distance = Vector3.Distance(previewPositions[i], previewPositions[j]);
                    if (distance < preset.PerceptionRadius)
                    {
                        // Separation
                        if (distance < 2f && distance > 0)
                        {
                            Vector3 diff = previewPositions[i] - previewPositions[j];
                            diff.Normalize();
                            diff /= distance;
                            separation += diff;
                        }
                        
                        // Alignment
                        alignment += previewVelocities[j];
                        
                        // Cohesion
                        cohesion += previewPositions[j];
                        
                        neighborCount++;
                    }
                }
                
                if (neighborCount > 0)
                {
                    alignment /= neighborCount;
                    alignment.Normalize();
                    alignment *= preset.MaxSpeed;
                    alignment -= previewVelocities[i];
                    alignment = Vector3.ClampMagnitude(alignment, preset.MaxForce);
                    
                    cohesion /= neighborCount;
                    cohesion -= previewPositions[i];
                    cohesion.Normalize();
                    cohesion *= preset.MaxSpeed;
                    cohesion -= previewVelocities[i];
                    cohesion = Vector3.ClampMagnitude(cohesion, preset.MaxForce);
                    
                    separation.Normalize();
                    separation *= preset.MaxSpeed;
                    separation -= previewVelocities[i];
                    separation = Vector3.ClampMagnitude(separation, preset.MaxForce);
                }
                
                Vector3 acceleration = separation * preset.SeparationWeight +
                                      alignment * preset.AlignmentWeight +
                                      cohesion * preset.CohesionWeight;
                
                previewVelocities[i] += acceleration * deltaTime;
                previewVelocities[i] = Vector3.ClampMagnitude(previewVelocities[i], preset.MaxSpeed);
                previewPositions[i] += previewVelocities[i] * deltaTime;
                
                // Wrap around bounds
                if (previewPositions[i].x < -25f) previewPositions[i].x = 25f;
                if (previewPositions[i].x > 25f) previewPositions[i].x = -25f;
                if (previewPositions[i].z < -25f) previewPositions[i].z = 25f;
                if (previewPositions[i].z > 25f) previewPositions[i].z = -25f;
            }
            
            Repaint();
        }
        
        private void AnalyzePerformance()
        {
            performanceScore = preset.GetPerformanceScore();
            
            var notes = new System.Collections.Generic.List<string>();
            
            if (preset.PerceptionRadius > 10f)
                notes.Add("Large perception radius may cause performance issues");
            
            if (preset.UpdateFrequency > 60)
                notes.Add("High update frequency may impact frame rate");
            
            if (!preset.EnableLOD)
                notes.Add("LOD disabled - consider enabling for better performance");
            
            if (preset.UseObstacleAvoidance)
                notes.Add("Obstacle avoidance adds computational overhead");
            
            float totalWeight = preset.SeparationWeight + preset.AlignmentWeight + preset.CohesionWeight;
            if (totalWeight < 0.1f)
                notes.Add("Very low behavior weights may result in inactive agents");
            
            if (notes.Count == 0)
                notes.Add("Configuration looks optimal for performance");
            
            performanceNotes = notes.ToArray();
        }
        
        private void ApplyToSelectedAgents()
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
            
            if (count > 0)
            {
                Debug.Log($"Applied behavior preset to {count} agents");
            }
            else
            {
                EditorUtility.DisplayDialog("No Agents Selected", "Please select one or more GameObjects with SwarmAgent components.", "OK");
            }
        }
        
        private void ApplyToAllAgents()
        {
            var agents = FindObjectsOfType<SwarmAgent>();
            
            if (agents.Length == 0)
            {
                EditorUtility.DisplayDialog("No Agents Found", "No SwarmAgent components found in the scene.", "OK");
                return;
            }
            
            if (EditorUtility.DisplayDialog("Apply to All Agents", 
                $"This will apply the preset to all {agents.Length} agents in the scene. Continue?", 
                "Yes", "Cancel"))
            {
                foreach (var agent in agents)
                {
                    Undo.RecordObject(agent, "Apply Behavior Preset to All");
                    preset.ApplyToAgent(agent);
                    EditorUtility.SetDirty(agent);
                }
                
                Debug.Log($"Applied behavior preset to {agents.Length} agents");
            }
        }
        
        private void CreateVariant()
        {
            var variant = preset.CreateVariant($"{preset.PresetName}_Variant");
            
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Behavior Preset Variant",
                $"{preset.PresetName}_Variant",
                "asset",
                "Choose location to save the behavior preset variant"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(variant, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = variant;
            }
        }
        
        private void ExportSettings()
        {
            var json = JsonUtility.ToJson(preset, true);
            string path = EditorUtility.SaveFilePanel("Export Behavior Settings", "", $"{preset.PresetName}_settings", "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"Exported behavior settings to: {path}");
            }
        }
        
        void OnDisable()
        {
            if (isSimulating)
            {
                EditorApplication.update -= UpdatePreview;
                isSimulating = false;
            }
        }
    }
}
#endif