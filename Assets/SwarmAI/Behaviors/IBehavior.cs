using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Interface for all swarm behaviors
    /// </summary>
    public interface IBehavior
    {
        Vector3 Calculate(Agent agent);
        float GetWeight();
        void SetWeight(float weight);
        bool IsEnabled();
        void SetEnabled(bool enabled);
        string GetName();
    }
    
    /// <summary>
    /// Base implementation of IBehavior
    /// </summary>
    [System.Serializable]
    public abstract class BaseBehavior : IBehavior
    {
        [SerializeField] protected float weight = 1f;
        [SerializeField] protected bool enabled = true;
        [SerializeField] protected string behaviorName;
        
        public abstract Vector3 Calculate(Agent agent);
        
        public virtual float GetWeight() => weight;
        public virtual void SetWeight(float weight) => this.weight = weight;
        public virtual bool IsEnabled() => enabled;
        public virtual void SetEnabled(bool enabled) => this.enabled = enabled;
        public virtual string GetName() => behaviorName;
    }
}