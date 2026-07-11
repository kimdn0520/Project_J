using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Framework.Lighting
{
    [RequireComponent(typeof(Light2D))]
    public abstract class LightEffectBase : MonoBehaviour
    {
        protected Light2D targetLight;
        protected float baseIntensity;
        protected Color baseColor;

        protected virtual void Awake()
        {
            targetLight = GetComponent<Light2D>();
            baseIntensity = targetLight.intensity;
            baseColor = targetLight.color;
        }

        public abstract void PlayEffect();
        public abstract void StopEffect();
        
        public virtual void ResetToDefault()
        {
            if (targetLight != null)
            {
                targetLight.intensity = baseIntensity;
                targetLight.color = baseColor;
            }
        }
    }
}
