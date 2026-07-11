using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Cysharp.Threading.Tasks;

namespace Framework.Lighting
{
    public class LightingManager : SingletonMonoBehaviour<LightingManager>
    {
        [Header("Global Settings")]
        [SerializeField] private Light2D globalLight;

        private readonly Dictionary<string, List<LightChannelMember>> channelMap = new Dictionary<string, List<LightChannelMember>>();
        private float originalGlobalIntensity;
        private Color originalGlobalColor;

        protected override void Awake()
        {
            base.Awake();
            FindGlobalLightIfNull();
        }

        private void FindGlobalLightIfNull()
        {
            if (globalLight == null)
            {
                // Find global light by checking lights in scene
                var lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
                foreach (var light in lights)
                {
                    if (light.lightType == Light2D.LightType.Global)
                    {
                        globalLight = light;
                        break;
                    }
                }
            }

            if (globalLight != null)
            {
                originalGlobalIntensity = globalLight.intensity;
                originalGlobalColor = globalLight.color;
            }
        }

        /// <summary>
        /// Registers a light member to the manager.
        /// </summary>
        public void RegisterLight(LightChannelMember lightMember)
        {
            string channel = lightMember.ChannelId;
            if (!channelMap.ContainsKey(channel))
            {
                channelMap[channel] = new List<LightChannelMember>();
            }

            if (!channelMap[channel].Contains(lightMember))
            {
                channelMap[channel].Add(lightMember);
            }
        }

        /// <summary>
        /// Unregisters a light member from the manager.
        /// </summary>
        public void UnregisterLight(LightChannelMember lightMember)
        {
            string channel = lightMember.ChannelId;
            if (channelMap.ContainsKey(channel))
            {
                channelMap[channel].Remove(lightMember);
                if (channelMap[channel].Count == 0)
                {
                    channelMap.Remove(channel);
                }
            }
        }

        /// <summary>
        /// Fades all lights in a channel to a target intensity.
        /// </summary>
        public async UniTask FadeChannelIntensityAsync(string channel, float targetIntensity, float duration)
        {
            if (!channelMap.TryGetValue(channel, out var list)) return;

            var tasks = new List<UniTask>();
            foreach (var member in list)
            {
                tasks.Add(member.FadeIntensityAsync(targetIntensity, duration));
            }

            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// Fades all lights in a channel to a target color.
        /// </summary>
        public async UniTask FadeChannelColorAsync(string channel, Color targetColor, float duration)
        {
            if (!channelMap.TryGetValue(channel, out var list)) return;

            var tasks = new List<UniTask>();
            foreach (var member in list)
            {
                tasks.Add(member.FadeColorAsync(targetColor, duration));
            }

            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// Starts registered effects for all lights in a channel.
        /// </summary>
        public void StartChannelEffects(string channel)
        {
            if (!channelMap.TryGetValue(channel, out var list)) return;
            foreach (var member in list)
            {
                member.PlayRegisteredEffect();
            }
        }

        /// <summary>
        /// Stops registered effects for all lights in a channel.
        /// </summary>
        public void StopChannelEffects(string channel)
        {
            if (!channelMap.TryGetValue(channel, out var list)) return;
            foreach (var member in list)
            {
                member.StopRegisteredEffect();
            }
        }

        /// <summary>
        /// Resets all lights in a channel to their default states.
        /// </summary>
        public void ResetChannel(string channel)
        {
            if (!channelMap.TryGetValue(channel, out var list)) return;
            foreach (var member in list)
            {
                member.ResetToDefault();
            }
        }

        /// <summary>
        /// Fades the global light's intensity.
        /// </summary>
        public async UniTask FadeGlobalIntensityAsync(float targetIntensity, float duration)
        {
            FindGlobalLightIfNull();
            if (globalLight == null) return;

            if (duration <= 0)
            {
                globalLight.intensity = targetIntensity;
                return;
            }

            await DG.Tweening.DOTween.To(() => globalLight.intensity, x => globalLight.intensity = x, targetIntensity, duration).ToUniTask();
        }

        /// <summary>
        /// Fades the global light's color.
        /// </summary>
        public async UniTask FadeGlobalColorAsync(Color targetColor, float duration)
        {
            FindGlobalLightIfNull();
            if (globalLight == null) return;

            if (duration <= 0)
            {
                globalLight.color = targetColor;
                return;
            }

            await DG.Tweening.DOTween.To(() => globalLight.color, x => globalLight.color = x, targetColor, duration).ToUniTask();
        }

        /// <summary>
        /// Resets the global light to its startup state.
        /// </summary>
        public void ResetGlobalLight()
        {
            if (globalLight != null)
            {
                globalLight.intensity = originalGlobalIntensity;
                globalLight.color = originalGlobalColor;
            }
        }
    }
}
