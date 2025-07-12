using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI.Utils
{
    public class SwarmPerformanceMonitor : MonoBehaviour
    {
        [Header("Monitoring Settings")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float sampleInterval = 1f;
        [SerializeField] private int maxSamples = 60;
        [SerializeField] private bool displayOnScreen = true;
        
        [Header("Performance Thresholds")]
        [SerializeField] private float warningFPS = 30f;
        [SerializeField] private float criticalFPS = 15f;
        [SerializeField] private float maxMemoryMB = 100f;
        
        // Performance data
        private Queue<float> fpsSamples = new Queue<float>();
        private Queue<float> memorySamples = new Queue<float>();
        private Queue<int> agentCountSamples = new Queue<int>();
        private Queue<float> updateTimeSamples = new Queue<float>();
        
        private float lastSampleTime;
        private SwarmManager swarmManager;
        
        // Current metrics
        public float CurrentFPS { get; private set; }
        public float AverageFPS { get; private set; }
        public float CurrentMemoryMB { get; private set; }
        public float AverageMemoryMB { get; private set; }
        public int CurrentAgentCount { get; private set; }
        public float CurrentUpdateTime { get; private set; }
        public float AverageUpdateTime { get; private set; }
        
        // Performance status
        public PerformanceStatus Status { get; private set; }
        
        public enum PerformanceStatus
        {
            Good,
            Warning,
            Critical
        }
        
        void Start()
        {
            swarmManager = SwarmManager.Instance;
            if (swarmManager == null)
            {
                swarmManager = FindObjectOfType<SwarmManager>();
            }
        }
        
        void Update()
        {
            if (!enableMonitoring) return;
            
            if (Time.time - lastSampleTime >= sampleInterval)
            {
                CollectSample();
                lastSampleTime = Time.time;
            }
        }
        
        private void CollectSample()
        {
            // Collect FPS
            CurrentFPS = 1f / Time.unscaledDeltaTime;
            fpsSamples.Enqueue(CurrentFPS);
            if (fpsSamples.Count > maxSamples) fpsSamples.Dequeue();
            AverageFPS = fpsSamples.Average();
            
            // Collect Memory
            CurrentMemoryMB = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(0) / (1024f * 1024f);
            memorySamples.Enqueue(CurrentMemoryMB);
            if (memorySamples.Count > maxSamples) memorySamples.Dequeue();
            AverageMemoryMB = memorySamples.Average();
            
            // Collect Agent Count
            CurrentAgentCount = swarmManager != null ? swarmManager.TotalAgents : 0;
            agentCountSamples.Enqueue(CurrentAgentCount);
            if (agentCountSamples.Count > maxSamples) agentCountSamples.Dequeue();
            
            // Collect Update Time
            CurrentUpdateTime = swarmManager != null ? swarmManager.UpdateTime : 0f;
            updateTimeSamples.Enqueue(CurrentUpdateTime);
            if (updateTimeSamples.Count > maxSamples) updateTimeSamples.Dequeue();
            AverageUpdateTime = updateTimeSamples.Average();
            
            // Update performance status
            UpdatePerformanceStatus();
        }
        
        private void UpdatePerformanceStatus()
        {
            if (CurrentFPS < criticalFPS || CurrentMemoryMB > maxMemoryMB * 1.5f)
            {
                Status = PerformanceStatus.Critical;
            }
            else if (CurrentFPS < warningFPS || CurrentMemoryMB > maxMemoryMB)
            {
                Status = PerformanceStatus.Warning;
            }
            else
            {
                Status = PerformanceStatus.Good;
            }
        }
        
        void OnGUI()
        {
            if (!displayOnScreen || !enableMonitoring) return;
            
            int y = 10;
            int lineHeight = 20;
            
            // Background
            GUI.Box(new Rect(10, 10, 300, 200), "Swarm Performance Monitor");
            
            // FPS
            GUI.color = GetColorForFPS(CurrentFPS);
            GUI.Label(new Rect(20, y += lineHeight * 2, 280, lineHeight), $"FPS: {CurrentFPS:F1} (Avg: {AverageFPS:F1})");
            
            // Memory
            GUI.color = GetColorForMemory(CurrentMemoryMB);
            GUI.Label(new Rect(20, y += lineHeight, 280, lineHeight), $"Memory: {CurrentMemoryMB:F1} MB (Avg: {AverageMemoryMB:F1} MB)");
            
            // Agents
            GUI.color = Color.white;
            GUI.Label(new Rect(20, y += lineHeight, 280, lineHeight), $"Agents: {CurrentAgentCount}");
            
            // Update Time
            GUI.color = GetColorForUpdateTime(CurrentUpdateTime);
            GUI.Label(new Rect(20, y += lineHeight, 280, lineHeight), $"Update Time: {CurrentUpdateTime:F2} ms (Avg: {AverageUpdateTime:F2} ms)");
            
            // Status
            GUI.color = GetColorForStatus(Status);
            GUI.Label(new Rect(20, y += lineHeight, 280, lineHeight), $"Status: {Status}");
            
            // Spatial Grid Info
            if (swarmManager != null && swarmManager.SpatialGrid != null)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(20, y += lineHeight, 280, lineHeight), $"Grid Cells: {swarmManager.SpatialGrid.Count}");
            }
            
            GUI.color = Color.white;
        }
        
        private Color GetColorForFPS(float fps)
        {
            if (fps < criticalFPS) return Color.red;
            if (fps < warningFPS) return Color.yellow;
            return Color.green;
        }
        
        private Color GetColorForMemory(float memory)
        {
            if (memory > maxMemoryMB * 1.5f) return Color.red;
            if (memory > maxMemoryMB) return Color.yellow;
            return Color.green;
        }
        
        private Color GetColorForUpdateTime(float updateTime)
        {
            if (updateTime > 10f) return Color.red;
            if (updateTime > 5f) return Color.yellow;
            return Color.green;
        }
        
        private Color GetColorForStatus(PerformanceStatus status)
        {
            switch (status)
            {
                case PerformanceStatus.Good: return Color.green;
                case PerformanceStatus.Warning: return Color.yellow;
                case PerformanceStatus.Critical: return Color.red;
                default: return Color.white;
            }
        }
        
        /// <summary>
        /// Export performance data to CSV
        /// </summary>
        public void ExportToCSV(string filename = "swarm_performance.csv")
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Time,FPS,Memory(MB),AgentCount,UpdateTime(ms)");
            
            var fpsArray = fpsSamples.ToArray();
            var memoryArray = memorySamples.ToArray();
            var agentArray = agentCountSamples.ToArray();
            var updateArray = updateTimeSamples.ToArray();
            
            int count = Mathf.Min(fpsArray.Length, memoryArray.Length);
            count = Mathf.Min(count, agentArray.Length);
            count = Mathf.Min(count, updateArray.Length);
            
            for (int i = 0; i < count; i++)
            {
                csv.AppendLine($"{i * sampleInterval},{fpsArray[i]:F2},{memoryArray[i]:F2},{agentArray[i]},{updateArray[i]:F2}");
            }
            
            System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filename), csv.ToString());
            Debug.Log($"Performance data exported to: {System.IO.Path.Combine(Application.persistentDataPath, filename)}");
        }
        
        /// <summary>
        /// Clear all collected performance data
        /// </summary>
        public void ClearData()
        {
            fpsSamples.Clear();
            memorySamples.Clear();
            agentCountSamples.Clear();
            updateTimeSamples.Clear();
        }
        
        /// <summary>
        /// Get performance recommendations
        /// </summary>
        public List<string> GetPerformanceRecommendations()
        {
            var recommendations = new List<string>();
            
            if (AverageFPS < warningFPS)
            {
                recommendations.Add("Consider reducing agent count or perception radius");
                recommendations.Add("Enable spatial partitioning if not already enabled");
                recommendations.Add("Use LOD system for distant agents");
            }
            
            if (AverageMemoryMB > maxMemoryMB)
            {
                recommendations.Add("Check for memory leaks in agent scripts");
                recommendations.Add("Consider object pooling for agents");
                recommendations.Add("Reduce texture quality or mesh complexity");
            }
            
            if (AverageUpdateTime > 5f)
            {
                recommendations.Add("Optimize neighbor search algorithms");
                recommendations.Add("Reduce update frequency for some agents");
                recommendations.Add("Consider GPU-based computation for large swarms");
            }
            
            if (recommendations.Count == 0)
            {
                recommendations.Add("Performance is within acceptable limits");
            }
            
            return recommendations;
        }
    }
}