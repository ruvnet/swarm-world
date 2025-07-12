using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI
{
    /// <summary>
    /// Manages pheromone trails for ant colony simulation
    /// </summary>
    public class PheromoneManager : MonoBehaviour
    {
        [Header("Pheromone Settings")]
        public float pheromoneLifetime = 30f;
        public float decayRate = 0.98f;
        public float minStrength = 0.1f;
        public int maxPheromones = 5000;
        
        [Header("Visual Settings")]
        public bool visualizePheromones = true;
        public Material foodTrailMaterial;
        public Material colonyTrailMaterial;
        public float trailWidth = 0.1f;
        
        [Header("Performance")]
        public bool useGridOptimization = true;
        public float gridCellSize = 2f;
        
        private List<Pheromone> pheromones = new List<Pheromone>();
        private Dictionary<Vector2Int, List<Pheromone>> pheromoneGrid;
        private LineRenderer foodTrailRenderer;
        private LineRenderer colonyTrailRenderer;
        
        void Start()
        {
            InitializePheromoneSystem();
        }
        
        void InitializePheromoneSystem()
        {
            if (useGridOptimization)
            {
                pheromoneGrid = new Dictionary<Vector2Int, List<Pheromone>>();
            }
            
            CreateTrailRenderers();
        }
        
        void CreateTrailRenderers()
        {
            if (!visualizePheromones) return;
            
            // Food trail renderer
            GameObject foodTrailObj = new GameObject("FoodTrailRenderer");
            foodTrailObj.transform.SetParent(transform);
            foodTrailRenderer = foodTrailObj.AddComponent<LineRenderer>();
            
            if (foodTrailMaterial != null)
                foodTrailRenderer.material = foodTrailMaterial;
            else
                foodTrailRenderer.material = CreateDefaultMaterial(Color.green);
                
            foodTrailRenderer.startWidth = trailWidth;
            foodTrailRenderer.endWidth = trailWidth * 0.5f;
            foodTrailRenderer.useWorldSpace = true;
            
            // Colony trail renderer
            GameObject colonyTrailObj = new GameObject("ColonyTrailRenderer");
            colonyTrailObj.transform.SetParent(transform);
            colonyTrailRenderer = colonyTrailObj.AddComponent<LineRenderer>();
            
            if (colonyTrailMaterial != null)
                colonyTrailRenderer.material = colonyTrailMaterial;
            else
                colonyTrailRenderer.material = CreateDefaultMaterial(Color.blue);
                
            colonyTrailRenderer.startWidth = trailWidth;
            colonyTrailRenderer.endWidth = trailWidth * 0.5f;
            colonyTrailRenderer.useWorldSpace = true;
        }
        
        Material CreateDefaultMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            return mat;
        }
        
        void Update()
        {
            UpdatePheromones();
            UpdateVisuals();
        }
        
        void UpdatePheromones()
        {
            for (int i = pheromones.Count - 1; i >= 0; i--)
            {
                Pheromone pheromone = pheromones[i];
                
                // Decay pheromone
                pheromone.strength *= decayRate;
                pheromone.age += Time.deltaTime;
                
                // Remove weak or old pheromones
                if (pheromone.strength < minStrength || pheromone.age > pheromoneLifetime)
                {
                    RemovePheromone(i);
                }
            }
            
            // Limit total pheromones for performance
            while (pheromones.Count > maxPheromones)
            {
                RemovePheromone(0);
            }
        }
        
        void RemovePheromone(int index)
        {
            if (useGridOptimization)
            {
                Pheromone pheromone = pheromones[index];
                Vector2Int gridPos = GetGridPosition(pheromone.position);
                
                if (pheromoneGrid.ContainsKey(gridPos))
                {
                    pheromoneGrid[gridPos].Remove(pheromone);
                    if (pheromoneGrid[gridPos].Count == 0)
                        pheromoneGrid.Remove(gridPos);
                }
            }
            
            pheromones.RemoveAt(index);
        }
        
        void UpdateVisuals()
        {
            if (!visualizePheromones) return;
            
            UpdateTrailRenderer(foodTrailRenderer, PheromoneType.FoodTrail);
            UpdateTrailRenderer(colonyTrailRenderer, PheromoneType.ColonyTrail);
        }
        
        void UpdateTrailRenderer(LineRenderer renderer, PheromoneType type)
        {
            if (renderer == null) return;
            
            List<Pheromone> typePheromones = pheromones
                .Where(p => p.type == type && p.strength > minStrength * 2)
                .OrderBy(p => p.age)
                .ToList();
            
            renderer.positionCount = typePheromones.Count;
            
            for (int i = 0; i < typePheromones.Count; i++)
            {
                renderer.SetPosition(i, typePheromones[i].position);
            }
            
            // Update trail transparency based on pheromone strength
            if (typePheromones.Count > 0)
            {
                float avgStrength = typePheromones.Average(p => p.strength);
                Color color = renderer.material.color;
                color.a = Mathf.Clamp01(avgStrength);
                renderer.material.color = color;
            }
        }
        
        public void DropPheromone(Vector3 position, PheromoneType type, float strength)
        {
            // Check if there's already a pheromone very close to this position
            bool foundNearby = false;
            float mergeDistance = 0.5f;
            
            foreach (Pheromone existing in pheromones)
            {
                if (existing.type == type && Vector3.Distance(existing.position, position) < mergeDistance)
                {
                    // Strengthen existing pheromone instead of creating new one
                    existing.strength = Mathf.Max(existing.strength, strength);
                    existing.age = 0f; // Reset age
                    foundNearby = true;
                    break;
                }
            }
            
            if (!foundNearby)
            {
                Pheromone newPheromone = new Pheromone
                {
                    position = position,
                    type = type,
                    strength = strength,
                    age = 0f
                };
                
                pheromones.Add(newPheromone);
                
                if (useGridOptimization)
                {
                    Vector2Int gridPos = GetGridPosition(position);
                    if (!pheromoneGrid.ContainsKey(gridPos))
                        pheromoneGrid[gridPos] = new List<Pheromone>();
                    
                    pheromoneGrid[gridPos].Add(newPheromone);
                }
            }
        }
        
        public List<Pheromone> GetPheromonesInRadius(Vector3 position, float radius, PheromoneType type)
        {
            List<Pheromone> result = new List<Pheromone>();
            
            if (useGridOptimization)
            {
                // Use grid optimization
                int cellRadius = Mathf.CeilToInt(radius / gridCellSize);
                Vector2Int centerCell = GetGridPosition(position);
                
                for (int x = -cellRadius; x <= cellRadius; x++)
                {
                    for (int z = -cellRadius; z <= cellRadius; z++)
                    {
                        Vector2Int cellPos = centerCell + new Vector2Int(x, z);
                        
                        if (pheromoneGrid.ContainsKey(cellPos))
                        {
                            foreach (Pheromone pheromone in pheromoneGrid[cellPos])
                            {
                                if (pheromone.type == type && 
                                    Vector3.Distance(pheromone.position, position) <= radius)
                                {
                                    result.Add(pheromone);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Brute force search
                foreach (Pheromone pheromone in pheromones)
                {
                    if (pheromone.type == type && 
                        Vector3.Distance(pheromone.position, position) <= radius)
                    {
                        result.Add(pheromone);
                    }
                }
            }
            
            return result;
        }
        
        Vector2Int GetGridPosition(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / gridCellSize),
                Mathf.FloorToInt(worldPosition.z / gridCellSize)
            );
        }
        
        public void ClearPheromones()
        {
            pheromones.Clear();
            if (pheromoneGrid != null)
                pheromoneGrid.Clear();
        }
        
        public void ClearPheromones(PheromoneType type)
        {
            for (int i = pheromones.Count - 1; i >= 0; i--)
            {
                if (pheromones[i].type == type)
                {
                    RemovePheromone(i);
                }
            }
        }
        
        public int GetPheromoneCount(PheromoneType type)
        {
            return pheromones.Count(p => p.type == type);
        }
        
        public float GetAveragePheromoneStrength(PheromoneType type)
        {
            var typePheromones = pheromones.Where(p => p.type == type);
            return typePheromones.Any() ? typePheromones.Average(p => p.strength) : 0f;
        }
        
        void OnDrawGizmos()
        {
            if (pheromones == null || !visualizePheromones) return;
            
            foreach (Pheromone pheromone in pheromones)
            {
                Color gizmoColor = pheromone.type == PheromoneType.FoodTrail ? Color.green : Color.blue;
                gizmoColor.a = pheromone.strength;
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(pheromone.position, 0.2f * pheromone.strength);
            }
        }
    }
    
    [System.Serializable]
    public class Pheromone
    {
        public Vector3 position;
        public PheromoneType type;
        public float strength;
        public float age;
    }
    
    public enum PheromoneType
    {
        FoodTrail,
        ColonyTrail,
        DangerMarker,
        ExplorationMarker
    }
}