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

            if (fadeCanvasGroup == null)
            {
                CreateFadeCanvas();
            }
            else
            {
                Canvas canvas = fadeCanvasGroup.GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    canvas.sortingOrder = 32767;
                }
            }

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

        public async UniTask StartGameFromBlackSceneAsync(string targetScene, string targetSpawnId)
        {
            if (IsTransitioning) return;
            IsTransitioning = true;
            NextSpawnId = targetSpawnId;

            try
            {
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.alpha = 1f;
                    fadeCanvasGroup.blocksRaycasts = true;
                    fadeCanvasGroup.transform.SetAsLastSibling();
                }

                int sceneCount = SceneManager.sceneCount;
                for (int i = sceneCount - 1; i >= 0; i--)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded && scene.name != "Persistent" && scene.name != targetScene)
                    {
                        Debug.Log($"[SceneTransitionManager] Unloading scene under black screen: {scene.name}");
                        await SceneManager.UnloadSceneAsync(scene).ToUniTask();
                    }
                }

                if (!IsSceneLoaded(targetScene))
                {
                    Debug.Log($"[SceneTransitionManager] Loading new map scene additively: {targetScene}");
                    await SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive).ToUniTask();
                }
                
                currentMapSceneName = targetScene;

                Scene loadedScene = SceneManager.GetSceneByName(targetScene);
                if (loadedScene.IsValid())
                {
                    SceneManager.SetActiveScene(loadedScene);
                }

                TeleportPlayer(targetSpawnId);

                var cameraFollow = FindAnyObjectByType<Core.CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.SnapToTarget();
                }

                await UniTask.Yield();
                await UniTask.Yield();

                if (fadeCanvasGroup != null)
                {
                    await fadeCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad).ToUniTask();
                    fadeCanvasGroup.blocksRaycasts = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneTransitionManager] Error in StartGameFromBlackSceneAsync: {ex}");
            }
            finally
            {
                IsTransitioning = false;
            }
        }

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
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.blocksRaycasts = true;
                    fadeCanvasGroup.transform.SetAsLastSibling();
                    await fadeCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad).ToUniTask();
                }

                int sceneCount = SceneManager.sceneCount;
                for (int i = sceneCount - 1; i >= 0; i--)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded && scene.name != "Persistent" && scene.name != targetScene)
                    {
                        Debug.Log($"[SceneTransitionManager] Unloading scene under black screen: {scene.name}");
                        await SceneManager.UnloadSceneAsync(scene).ToUniTask();
                    }
                }

                if (!IsSceneLoaded(targetScene))
                {
                    Debug.Log($"[SceneTransitionManager] Loading new map scene additively: {targetScene}");
                    await SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive).ToUniTask();
                }
                
                currentMapSceneName = targetScene;

                Scene loadedScene = SceneManager.GetSceneByName(targetScene);
                if (loadedScene.IsValid())
                {
                    SceneManager.SetActiveScene(loadedScene);
                }

                TeleportPlayer(targetSpawnId, overridePosition, overrideRotation);

                var cameraFollow = FindAnyObjectByType<Core.CameraFollow>();
                if (cameraFollow != null)
                {
                    cameraFollow.SnapToTarget();
                }

                await UniTask.Yield();
                await UniTask.Yield();

                if (fadeCanvasGroup != null)
                {
                    await fadeCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad).ToUniTask();
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
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");

            if (player == null)
            {
                Debug.LogWarning("[SceneTransitionManager] Player object not found in the scene.");
                return;
            }

            // Kill any leftover DOTween animations
            player.transform.DOKill();
            
            SpriteRenderer[] srs = player.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs)
            {
                sr.DOKill();
                sr.color = Color.white; // Restore opacity
                if (sr.sortingOrder < 10) sr.sortingOrder = 10;
            }

            var playerController = player.GetComponent<Player.PlayerController>();
            if (playerController != null && playerController.IdleState != null)
            {
                playerController.TransitionToState(playerController.IdleState);
            }

            if (overridePosition.HasValue)
            {
                player.transform.position = overridePosition.Value;
                if (overrideRotation.HasValue) player.transform.rotation = overrideRotation.Value;

                if (player.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.position = overridePosition.Value;
                    rb.linearVelocity = Vector2.zero;
                }
                return;
            }

            if (string.IsNullOrEmpty(spawnId)) return;

            // Flexible SpawnPoint Search Algorithm
            Transform targetTransform = null;

            // 1. Search by SpawnPoint component SpawnId
            SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            foreach (var point in spawnPoints)
            {
                if (point != null && !string.IsNullOrEmpty(point.SpawnId) && point.SpawnId.Equals(spawnId, StringComparison.OrdinalIgnoreCase))
                {
                    targetTransform = point.transform;
                    break;
                }
            }

            // 2. Search by GameObject name
            if (targetTransform == null)
            {
                foreach (var point in spawnPoints)
                {
                    if (point != null && point.gameObject.name.Equals(spawnId, StringComparison.OrdinalIgnoreCase))
                    {
                        targetTransform = point.transform;
                        break;
                    }
                }
            }

            // 3. Search by loose name match
            if (targetTransform == null)
            {
                string cleanTargetId = spawnId.Replace("SpawnPoint_", "").Replace("Spawn_", "");
                foreach (var point in spawnPoints)
                {
                    if (point != null)
                    {
                        string cleanPointId = (point.SpawnId ?? point.gameObject.name).Replace("SpawnPoint_", "").Replace("Spawn_", "");
                        if (cleanPointId.Equals(cleanTargetId, StringComparison.OrdinalIgnoreCase))
                        {
                            targetTransform = point.transform;
                            break;
                        }
                    }
                }

                if (targetTransform == null)
                {
                    GameObject rawSpawnObj = GameObject.Find(spawnId);
                    if (rawSpawnObj != null) targetTransform = rawSpawnObj.transform;
                }
            }

            if (targetTransform == null)
            {
                Debug.LogWarning($"[SceneTransitionManager] SpawnPoint '{spawnId}' not found. Defaulting player to (0, -2, 0).");
                player.transform.position = new Vector3(0, -2f, 0);
            }
            else
            {
                player.transform.position = targetTransform.position;
                player.transform.rotation = targetTransform.rotation;
                Debug.Log($"[SceneTransitionManager] Teleported player to {targetTransform.gameObject.name} at {targetTransform.position}");
            }

            if (player.TryGetComponent<Rigidbody2D>(out var rb2d))
            {
                rb2d.position = player.transform.position;
                rb2d.linearVelocity = Vector2.zero;
            }

            // Set player facing UP by default if entering from exterior
            if (spawnId.Contains("Exterior") || spawnId.Contains("Exterior", StringComparison.OrdinalIgnoreCase))
            {
                if (playerController != null)
                {
                    playerController.SetFacingDirection(Vector2.up);
                }
            }
        }

        private void CreateFadeCanvas()
        {
            GameObject canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(this.transform);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;

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
