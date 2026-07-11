using UnityEngine;
using System.Collections;

namespace Framework.Lighting
{
    public class LightFlickerEffect : LightEffectBase
    {
        [Header("Flicker Settings")]
        [SerializeField] private float minIntensityPercent = 0.2f;
        [SerializeField] private float maxIntensityPercent = 1.2f;
        [SerializeField] private float flickerIntervalMin = 0.05f;
        [SerializeField] private float flickerIntervalMax = 0.2f;
        
        private Coroutine flickerCoroutine;
        private bool isPlaying = false;

        public override void PlayEffect()
        {
            if (isPlaying) return;
            isPlaying = true;
            flickerCoroutine = StartCoroutine(FlickerRoutine());
        }

        public override void StopEffect()
        {
            if (!isPlaying) return;
            isPlaying = false;
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
                flickerCoroutine = null;
            }
            ResetToDefault();
        }

        private IEnumerator FlickerRoutine()
        {
            while (isPlaying)
            {
                float randomInterval = Random.Range(flickerIntervalMin, flickerIntervalMax);
                float targetPercentage = Random.Range(minIntensityPercent, maxIntensityPercent);
                targetLight.intensity = baseIntensity * targetPercentage;
                
                yield return new WaitForSeconds(randomInterval);
            }
        }

        private void OnDisable()
        {
            StopEffect();
        }
    }
}
