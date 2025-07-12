using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Food source for ant colony simulation
    /// </summary>
    public class FoodSource : MonoBehaviour
    {
        [Header("Food Properties")]
        public int totalFood = 100;
        public int currentFood;
        public float detectionRadius = 1.5f;
        public float regenerationRate = 0.1f;
        public int maxFood = 100;
        
        [Header("Visual")]
        public Transform foodVisual;
        public ParticleSystem consumeEffect;
        public Color fullColor = Color.yellow;
        public Color emptyColor = Color.brown;
        
        private Renderer foodRenderer;
        private float lastRegenTime;
        
        void Start()
        {
            currentFood = totalFood;
            
            if (foodVisual == null)
                foodVisual = transform;
                
            foodRenderer = foodVisual.GetComponent<Renderer>();
            
            UpdateVisual();
        }
        
        void Update()
        {
            RegenerateFood();
            UpdateVisual();
        }
        
        void RegenerateFood()
        {
            if (currentFood < maxFood && Time.time - lastRegenTime > regenerationRate)
            {
                currentFood++;
                lastRegenTime = Time.time;
            }
        }
        
        void UpdateVisual()
        {
            if (foodRenderer != null)
            {
                float foodPercent = (float)currentFood / maxFood;
                foodRenderer.material.color = Color.Lerp(emptyColor, fullColor, foodPercent);
                
                // Scale based on food amount
                float scale = Mathf.Lerp(0.3f, 1f, foodPercent);
                foodVisual.localScale = Vector3.one * scale;
            }
        }
        
        public bool ConsumeFood()
        {
            if (currentFood > 0)
            {
                currentFood--;
                
                if (consumeEffect != null)
                    consumeEffect.Play();
                    
                return true;
            }
            
            return false;
        }
        
        public float GetFoodPercentage()
        {
            return (float)currentFood / maxFood;
        }
        
        public bool HasFood()
        {
            return currentFood > 0;
        }
        
        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
            // Draw food indicator
            Gizmos.color = Color.Lerp(emptyColor, fullColor, GetFoodPercentage());
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
}