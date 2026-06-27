using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace MapSystem
{
    /// <summary>
    /// Manages scene transitions (loading/unloading maps additively), screen fading, and player spawning.
    /// Works as a singleton and resides in the Persistent scene.
    /// </summary>
    public class SceneTransitionManager : SingletonMonoBehaviour<SceneTransitionManager>
    {
        [Header("Fade UI Settings")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        public string NextSpawnId { get; private set; }
        public bool IsTransitioning { get; private set; } = false;

        private string currentMapSceneName;

        protected override void Awake()
        {
            base.Awake();

            // Auto-create fade UI if it is not manually assigned
            if (fadeCanvasGroup == null)
            {
                CreateFadeCanvas();
            }

            // Find currently loaded map scene (other than the Persistent scene itself)
            // This is useful when playing from a map scene directly in the Unity Editor.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != gameObject.scene.name && scene.name != "Persistent")
                {
                    currentMapSceneName = scene.name;
                    break;
                }
            }
        }

        /// <summary>
        /// Initiates transition to target scene and teleports player to specific spawn point.
        /// </summary>
        public async UniTask LoadSceneAsync(string targetScene, string targetSpawnId, Vector3? overridePosition = null, Quaternion? overrideRotation = null)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("[SceneTransitionManager] Transition is already in progress.");
                return;
            }

            IsTransitioning = true;
            NextSpawnId = targetSpawnId;

            try
            {
                // 1. Fade Out (Screen turns black)
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.blocksRaycasts = true; // Block UI/Input during transition
                    await fadeCanvasGroup.DOFade(1f, fadeDuration).ToUniTask();
                }

                // 2. Unload current map scene if it exists
                if (!string.IsNullOrEmpty(currentMapSceneName) && IsSceneLoaded(currentMapSceneName))
                {
                    await SceneManager.UnloadSceneAsync(currentMapSceneName).ToUniTask();
                }

                // 3. Load new map scene additively
                await SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive).ToUniTask();
                currentMapSceneName = targetScene;

                // 4. Set newly loaded scene as Active Scene so lighting and physics behave properly
                Scene loadedScene = SceneManager.GetSceneByName(targetScene);
                if (loadedScene.IsValid())
                {
                    SceneManager.SetActiveScene(loadedScene);
                }

                // 5. Teleport player to the target spawn point or override position
                TeleportPlayer(targetSpawnId, overridePosition, overrideRotation);

                // 6. Fade In (Screen returns to normal)
                if (fadeCanvasGroup != null)
                {
                    await fadeCanvasGroup.DOFade(0f, fadeDuration).ToUniTask();
                    fadeCanvasGroup.blocksRaycasts = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneTransitionManager] Error during scene transition: {ex}");
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        private bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.isLoaded;
        }

        private void TeleportPlayer(string spawnId, Vector3? overridePosition = null, Quaternion? overrideRotation = null)
        {
            // Find the player object in the scene
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[SceneTransitionManager] Player object with tag 'Player' not found in the scene.");
                return;
            }

            if (overridePosition.HasValue)
            {
                player.transform.position = overridePosition.Value;
                if (overrideRotation.HasValue)
                {
                    player.transform.rotation = overrideRotation.Value;
                }

                // If Rigidbody2D is present, sync physics state to prevent rollback
                if (player.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.position = overridePosition.Value;
                    rb.linearVelocity = Vector2.zero;
                }
                return;
            }

            if (string.IsNullOrEmpty(spawnId)) return;

            // Find all SpawnPoint components in the newly loaded scene
            SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            SpawnPoint targetPoint = null;

            foreach (var point in spawnPoints)
            {
                if (point.SpawnId == spawnId)
                {
                    targetPoint = point;
                    break;
                }
            }

            if (targetPoint == null)
            {
                Debug.LogError($"[SceneTransitionManager] SpawnPoint with ID '{spawnId}' not found in the scene.");
                return;
            }

            // Teleport player
            player.transform.position = targetPoint.transform.position;
            player.transform.rotation = targetPoint.transform.rotation;

            // If Rigidbody2D is present, sync physics state to prevent rollback
            if (player.TryGetComponent<Rigidbody2D>(out var rb2d))
            {
                rb2d.position = targetPoint.transform.position;
                rb2d.linearVelocity = Vector2.zero;
            }
        }

        private void CreateFadeCanvas()
        {
            GameObject canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(this.transform);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            fadeCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;

            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform);

            UnityEngine.UI.Image image = imageObj.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.black;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
