using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace Framework.Lighting
{
    [RequireComponent(typeof(Light2D))]
    public class LightChannelMember : MonoBehaviour
    {
        [Header("Channel Settings")]
        [SerializeField] private string channelId = "Default";
        
        [Header("Auto Play Effect (Optional)")]
        [SerializeField] private LightEffectBase autoPlayEffect;

        private Light2D targetLight;
        private float baseIntensity;
        private Color baseColor;
        private Tween fadeTween;

        public string ChannelId => channelId;
        public Light2D Light => targetLight;

        private void Awake()
        {
            targetLight = GetComponent<Light2D>();
            baseIntensity = targetLight.intensity;
            baseColor = targetLight.color;
        }

        private void Start()
        {
            // Register to manager
            if (LightingManager.Instance != null)
            {
                LightingManager.Instance.RegisterLight(this);
            }

            if (autoPlayEffect != null)
            {
                autoPlayEffect.PlayEffect();
            }
        }

        private void OnDestroy()
        {
            // Unregister from manager
            if (LightingManager.Instance != null)
            {
                LightingManager.Instance.UnregisterLight(this);
            }
            KillFadeTween();
        }

        public void PlayRegisteredEffect()
        {
            var effect = GetComponent<LightEffectBase>();
            if (effect != null) effect.PlayEffect();
        }

        public void StopRegisteredEffect()
        {
            var effect = GetComponent<LightEffectBase>();
            if (effect != null) effect.StopEffect();
        }

        public async UniTask FadeIntensityAsync(float targetIntensity, float duration)
        {
            KillFadeTween();
            if (duration <= 0)
            {
                targetLight.intensity = targetIntensity;
                return;
            }

            fadeTween = DOTween.To(() => targetLight.intensity, x => targetLight.intensity = x, targetIntensity, duration);
            await fadeTween.ToUniTask();
        }

        public async UniTask FadeColorAsync(Color targetColor, float duration)
        {
            KillFadeTween();
            if (duration <= 0)
            {
                targetLight.color = targetColor;
                return;
            }

            fadeTween = DOTween.To(() => targetLight.color, x => targetLight.color = x, targetColor, duration);
            await fadeTween.ToUniTask();
        }

        public void ResetToDefault()
        {
            KillFadeTween();
            targetLight.intensity = baseIntensity;
            targetLight.color = baseColor;
            
            var effect = GetComponent<LightEffectBase>();
            if (effect != null) effect.ResetToDefault();
        }

        private void KillFadeTween()
        {
            if (fadeTween != null && fadeTween.IsActive())
            {
                fadeTween.Kill();
            }
        }
    }
}
