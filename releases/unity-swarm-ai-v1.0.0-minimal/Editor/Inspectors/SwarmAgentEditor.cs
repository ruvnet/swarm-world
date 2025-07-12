using UnityEngine;
using UnityEditor;
using UnitySwarmAI.Core;
using UnitySwarmAI.Behaviors;

namespace UnitySwarmAI.Editor
{
    /// <summary>
    /// Custom inspector for SwarmAgent with enhanced UI and debugging tools
    /// </summary>
    [CustomEditor(typeof(SwarmAgent))]
    public class SwarmAgentEditor : UnityEditor.Editor
    {
        private SerializedProperty perceptionRadiusProp;
        private SerializedProperty maxSpeedProp;
        private SerializedProperty maxForceProp;
        private SerializedProperty behaviorsProp;
        private SerializedProperty behaviorWeightsProp;
        private SerializedProperty showGizmosProp;
        private SerializedProperty gizmoColorProp;
        
        private bool showBehaviorSettings = true;
        private bool showDebugSettings = true;
        private bool showRuntimeInfo = false;
        
        private void OnEnable()
        {
            perceptionRadiusProp = serializedObject.FindProperty("perceptionRadius");
            maxSpeedProp = serializedObject.FindProperty("maxSpeed");
            maxForceProp = serializedObject.FindProperty("maxForce");
            behaviorsProp = serializedObject.FindProperty("behaviors");
            behaviorWeightsProp = serializedObject.FindProperty("behaviorWeights");
            showGizmosProp = serializedObject.FindProperty("showGizmos");
            gizmoColorProp = serializedObject.FindProperty("gizmoColor");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            SwarmAgent agent = (SwarmAgent)target;
            
            // Header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unity Swarm AI Agent", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Agent Properties
            EditorGUILayout.LabelField("Agent Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(perceptionRadiusProp);
            EditorGUILayout.PropertyField(maxSpeedProp);
            EditorGUILayout.PropertyField(maxForceProp);
            
            EditorGUILayout.Space();
            
            // Behavior Settings
            showBehaviorSettings = EditorGUILayout.Foldout(showBehaviorSettings, "Behavior Settings", true);
            if (showBehaviorSettings)
            {
                EditorGUI.indentLevel++;
                DrawBehaviorSettings();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Debug Settings
            showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Settings", true);
            if (showDebugSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(showGizmosProp);
                EditorGUILayout.PropertyField(gizmoColorProp);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Runtime Info (Play Mode Only)
            if (Application.isPlaying)
            {
                showRuntimeInfo = EditorGUILayout.Foldout(showRuntimeInfo, "Runtime Information", true);
                if (showRuntimeInfo)
                {
                    EditorGUI.indentLevel++;
                    DrawRuntimeInfo(agent);
                    EditorGUI.indentLevel--;
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawBehaviorSettings()
        {
            EditorGUILayout.PropertyField(behaviorsProp, true);
            
            // Sync behavior weights array
            if (behaviorsProp.arraySize != behaviorWeightsProp.arraySize)
            {
                behaviorWeightsProp.arraySize = behaviorsProp.arraySize;
            }
            
            // Draw behavior weights
            if (behaviorsProp.arraySize > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Behavior Weights", EditorStyles.miniLabel);
                
                for (int i = 0; i < behaviorsProp.arraySize; i++)
                {
                    var behaviorProp = behaviorsProp.GetArrayElementAtIndex(i);
                    var weightProp = behaviorWeightsProp.GetArrayElementAtIndex(i);
                    
                    if (behaviorProp.objectReferenceValue != null)
                    {
                        string behaviorName = behaviorProp.objectReferenceValue.name;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(behaviorName, GUILayout.Width(120));
                        EditorGUILayout.PropertyField(weightProp, GUIContent.none);
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            
            // Quick Setup Buttons
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Flocking Behavior"))
            {
                AddFlockingBehavior();
            }
            if (GUILayout.Button("Reset Weights"))
            {
                ResetBehaviorWeights();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawRuntimeInfo(SwarmAgent agent)
        {
            EditorGUI.BeginDisabledGroup(true);
            
            EditorGUILayout.Vector3Field("Position", agent.Position);
            EditorGUILayout.Vector3Field("Velocity", agent.Velocity);
            EditorGUILayout.FloatField("Speed", agent.Velocity.magnitude);
            EditorGUILayout.EnumPopup("Behavior State", agent.BehaviorState);
            EditorGUILayout.IntField("Agent ID", agent.AgentId);
            
            var neighbors = agent.GetNeighbors();
            EditorGUILayout.IntField("Neighbor Count", neighbors?.Count ?? 0);
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Debug buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Log Neighbors"))
            {
                LogNeighbors(agent);
            }
            if (GUILayout.Button("Highlight Agent"))
            {
                Selection.activeGameObject = agent.gameObject;
                SceneView.FrameLastActiveSceneView();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void AddFlockingBehavior()
        {
            string path = "Assets/FlockingBehavior.asset";
            var behavior = CreateInstance<FlockingBehavior>();
            AssetDatabase.CreateAsset(behavior, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();
            
            // Add to behaviors array
            behaviorsProp.arraySize++;
            var newElement = behaviorsProp.GetArrayElementAtIndex(behaviorsProp.arraySize - 1);
            newElement.objectReferenceValue = behavior;
            
            // Add corresponding weight
            behaviorWeightsProp.arraySize++;
            var newWeight = behaviorWeightsProp.GetArrayElementAtIndex(behaviorWeightsProp.arraySize - 1);
            newWeight.floatValue = 1f;
        }
        
        private void ResetBehaviorWeights()
        {
            for (int i = 0; i < behaviorWeightsProp.arraySize; i++)
            {
                var weightProp = behaviorWeightsProp.GetArrayElementAtIndex(i);
                weightProp.floatValue = 1f;
            }
        }
        
        private void LogNeighbors(SwarmAgent agent)
        {
            var neighbors = agent.GetNeighbors();
            if (neighbors == null || neighbors.Count == 0)
            {
                Debug.Log($"Agent {agent.AgentId} has no neighbors");
                return;
            }
            
            Debug.Log($"Agent {agent.AgentId} neighbors:");
            foreach (var neighbor in neighbors)
            {
                float distance = Vector3.Distance(agent.Position, neighbor.Position);
                Debug.Log($"  - Agent {neighbor.AgentId} at distance {distance:F2}");
            }
        }
    }
}