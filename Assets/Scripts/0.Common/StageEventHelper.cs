using UnityEngine;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Core
{
    /// <summary>
    /// Utility helper for writing sequential stage cutscene actions using UniTask.
    /// Provides easy wrappers for camera shake, sound playing, and delay wait.
    /// </summary>
    public static class StageEventHelper
    {
        /// <summary>
        /// Shakes the main camera with specified intensity and duration.
        /// </summary>
        public static async UniTask ShakeCameraAsync(float duration, float strength = 1f, int vibrato = 10, float randomness = 90f)
        {
            var camera = Camera.main;
            if (camera == null) return;

            // Simple shake using DOTween
            await camera.transform.DOShakePosition(duration, strength, vibrato, randomness).ToUniTask();
        }

        /// <summary>
        /// Delays the execution flow in seconds.
        /// </summary>
        public static async UniTask DelayAsync(float seconds)
        {
            if (seconds <= 0) return;
            await UniTask.Delay(System.TimeSpan.FromSeconds(seconds));
        }

        /// <summary>
        /// Plays a sound effect via SoundManager.
        /// </summary>
        public static void PlaySound(string soundName)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(soundName);
            }
        }
    }
}
