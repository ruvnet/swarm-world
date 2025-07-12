using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using SwarmAI.Core;

namespace SwarmAI.Utils
{
    /// <summary>
    /// Comprehensive performance monitoring system for swarm simulations
    /// </summary>
    public class SwarmPerformanceMonitor : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text fpsText;
        [SerializeField] private Text agentCountText;
        [SerializeField] private Text avgNeighborsText;
        [SerializeField] private Text updateTimeText;
        [SerializeField] private Text memoryUsageText;
        [SerializeField] private Text spatialPartitioningText;
        
        [Header("Monitoring Settings")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private bool showDetailedStats = false;
        [SerializeField] private bool logPerformanceData = false;
        [SerializeField] private int performanceHistorySize = 100;
        
        [Header("Performance Thresholds")]
        [SerializeField] private float fpsWarningThreshold = 30f;
        [SerializeField] private float updateTimeWarningThreshold = 5f; // milliseconds
        [SerializeField] private long memoryWarningThreshold = 500; // MB
        
        private SwarmManager swarmManager;
        private float deltaTime;
        private float lastUpdateTime;
        private List<PerformanceSnapshot> performanceHistory;
        
        [System.Serializable]
        public struct PerformanceSnapshot
        {
            public float timestamp;
            public float fps;
            public int agentCount;
            public float averageNeighbors;
            public float updateTime;
            public long memoryUsage;
        }
        
        private void Start()
        {
            swarmManager = SwarmManager.Instance;
            performanceHistory = new List<PerformanceSnapshot>();
            
            if (swarmManager == null)
            {
                Debug.LogWarning("SwarmPerformanceMonitor: No SwarmManager found in scene!");
                enabled = false;
            }
        }
        
        private void Update()
        {
            if (!enableMonitoring) return;
            
            // Calculate FPS with smoothing
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            
            // Update UI periodically
            if (Time.time - lastUpdateTime > updateInterval)
            {
                UpdatePerformanceDisplay();
                RecordPerformanceSnapshot();
                CheckPerformanceWarnings();
                lastUpdateTime = Time.time;
            }
        }
        
        private void UpdatePerformanceDisplay()
        {
            float fps = 1.0f / deltaTime;
            SwarmPerformanceStats stats = swarmManager.GetPerformanceStats();
            
            // Update UI elements if they exist
            if (fpsText != null)
                fpsText.text = $"FPS: {fps:0.}";
                
            if (agentCountText != null)
                agentCountText.text = $"Agents: {stats.AgentCount}";
                
            if (avgNeighborsText != null)
                avgNeighborsText.text = $"Avg Neighbors: {stats.AverageNeighborCount:0.0}";
                
            if (updateTimeText != null)
                updateTimeText.text = $"Update Time: {stats.AverageUpdateTime:0.00}ms";
                
            if (memoryUsageText != null)
                memoryUsageText.text = $"Memory: {stats.MemoryUsage}MB";
                
            if (spatialPartitioningText != null)
            {
                string partitioningInfo = stats.SpatialPartitioningEnabled ? 
                    $"Partitioning: {stats.SpatialPartitionType}" : "Partitioning: Disabled";
                spatialPartitioningText.text = partitioningInfo;
            }
        }
        
        private void RecordPerformanceSnapshot()
        {
            float fps = 1.0f / deltaTime;
            SwarmPerformanceStats stats = swarmManager.GetPerformanceStats();
            
            PerformanceSnapshot snapshot = new PerformanceSnapshot
            {
                timestamp = Time.time,
                fps = fps,
                agentCount = stats.AgentCount,
                averageNeighbors = stats.AverageNeighborCount,
                updateTime = stats.AverageUpdateTime,
                memoryUsage = stats.MemoryUsage
            };
            
            performanceHistory.Add(snapshot);
            
            // Limit history size
            if (performanceHistory.Count > performanceHistorySize)
            {
                performanceHistory.RemoveAt(0);
            }
            
            if (logPerformanceData)
            {
                Debug.Log($"Performance: FPS={fps:F1}, Agents={stats.AgentCount}, " +
                         $"UpdateTime={stats.AverageUpdateTime:F2}ms, Memory={stats.MemoryUsage}MB");
            }
        }
        
        private void CheckPerformanceWarnings()
        {
            if (performanceHistory.Count == 0) return;
            
            PerformanceSnapshot latest = performanceHistory.Last();
            
            // FPS warning
            if (latest.fps < fpsWarningThreshold)
            {
                Debug.LogWarning($"Performance Warning: Low FPS ({latest.fps:F1})");
            }
            
            // Update time warning
            if (latest.updateTime > updateTimeWarningThreshold)
            {
                Debug.LogWarning($"Performance Warning: High update time ({latest.updateTime:F2}ms)");
            }
            
            // Memory warning
            if (latest.memoryUsage > memoryWarningThreshold)
            {
                Debug.LogWarning($"Performance Warning: High memory usage ({latest.memoryUsage}MB)");
            }
        }
        
        public PerformanceSnapshot[] GetPerformanceHistory()
        {
            return performanceHistory.ToArray();
        }
        
        public float GetAverageFPS(float timeWindow = 10f)
        {
            float cutoffTime = Time.time - timeWindow;
            var recentData = performanceHistory.Where(s => s.timestamp > cutoffTime);
            return recentData.Any() ? recentData.Average(s => s.fps) : 0f;
        }
        
        public float GetAverageUpdateTime(float timeWindow = 10f)
        {
            float cutoffTime = Time.time - timeWindow;
            var recentData = performanceHistory.Where(s => s.timestamp > cutoffTime);
            return recentData.Any() ? recentData.Average(s => s.updateTime) : 0f;
        }
        
        public void ExportPerformanceData(string filePath)
        {
            try
            {
                var lines = new List<string>
                {
                    "Timestamp,FPS,AgentCount,AvgNeighbors,UpdateTime,MemoryUsage"
                };
                
                foreach (var snapshot in performanceHistory)
                {
                    lines.Add($"{snapshot.timestamp:F2},{snapshot.fps:F2},{snapshot.agentCount}," +
                             $"{snapshot.averageNeighbors:F2},{snapshot.updateTime:F2},{snapshot.memoryUsage}");
                }
                
                System.IO.File.WriteAllLines(filePath, lines);
                Debug.Log($"Performance data exported to: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export performance data: {e.Message}");
            }
        }
        
        private void OnGUI()
        {
            if (!showDetailedStats || !enableMonitoring) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 350, 250));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Detailed Performance Stats", EditorGUIUtility.boldLabel);
            
            if (swarmManager != null)
            {
                SwarmPerformanceStats stats = swarmManager.GetPerformanceStats();
                float fps = 1.0f / deltaTime;
                
                GUILayout.Label($"FPS: {fps:F1} (Avg 10s: {GetAverageFPS():F1})");
                GUILayout.Label($"Agents: {stats.AgentCount}");
                GUILayout.Label($"Avg Neighbors: {stats.AverageNeighborCount:F1}");
                GUILayout.Label($"Update Time: {stats.AverageUpdateTime:F2}ms (Avg: {GetAverageUpdateTime():F2}ms)");
                GUILayout.Label($"Memory Usage: {stats.MemoryUsage}MB");
                GUILayout.Label($"Spatial Partitioning: {stats.SpatialPartitionType}");
                
                GUILayout.Space(10);
                GUILayout.Label("Controls:", EditorGUIUtility.boldLabel);
                
                if (GUILayout.Button("Export Performance Data"))
                {
                    string fileName = $"swarm_performance_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
                    ExportPerformanceData(fileName);
                }
                
                if (GUILayout.Button("Clear History"))
                {
                    performanceHistory.Clear();
                }
                
                if (GUILayout.Button("Force GC"))
                {
                    System.GC.Collect();
                }
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void OnValidate()
        {
            updateInterval = Mathf.Max(0.1f, updateInterval);
            fpsWarningThreshold = Mathf.Max(1f, fpsWarningThreshold);
            updateTimeWarningThreshold = Mathf.Max(0.1f, updateTimeWarningThreshold);
            memoryWarningThreshold = Mathf.Max(1, memoryWarningThreshold);
            performanceHistorySize = Mathf.Max(10, performanceHistorySize);
        }
    }
}