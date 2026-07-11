using UnityEngine;
using Framework.Lighting;
using Cysharp.Threading.Tasks;

namespace MapSystem
{
    /// <summary>
    /// Scene controller for Map_01_Start.
    /// Manages scene-local events like darkening the main room lights or making them flicker.
    /// </summary>
    public class Map01StartController : MapControllerBase
    {
        [Header("Lighting Settings")]
        [SerializeField] private string roomChannelId = "Room_01";
        [SerializeField] private float fadeDuration = 1.0f;

        protected override void RegisterSceneEvents()
        {
            // Register scene-specific events using the base class helper method.
            // These will be auto-unregistered when this component (and the scene) is disabled/destroyed.
            RegisterEvent("darken_room", OnDarkenRoom);
            RegisterEvent("restore_lighting", OnRestoreLighting);
            RegisterEvent("light_flicker_start", OnStartFlicker);
            RegisterEvent("light_flicker_stop", OnStopFlicker);
        }

        private void OnDarkenRoom()
        {
            if (LightingManager.Instance != null)
            {
                LightingManager.Instance.FadeChannelIntensityAsync(roomChannelId, 0.1f, fadeDuration).Forget();
            }
        }

        private void OnRestoreLighting()
        {
            if (LightingManager.Instance != null)
            {
                LightingManager.Instance.ResetChannel(roomChannelId);
            }
        }

        private void OnStartFlicker()
        {
            if (LightingManager.Instance != null)
            {
                LightingManager.Instance.StartChannelEffects(roomChannelId);
            }
        }

        private void OnStopFlicker()
        {
            if (LightingManager.Instance != null)
            {
                LightingManager.Instance.StopChannelEffects(roomChannelId);
            }
        }
    }
}
