using UnityEngine;
using Cysharp.Threading.Tasks;

namespace MapSystem
{
    /// <summary>
    /// Trigger area to transition the player to another map scene and spawn point.
    /// Needs a Collider2D configured with 'Is Trigger' set to true.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SceneDoor : MonoBehaviour
    {
        [Header("Transition Target")]
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawnId;

        [Header("Trigger Configuration")]
        [SerializeField] private string playerTag = "Player";

        public void SetTarget(string sceneName, string spawnId)
        {
            targetScene = sceneName;
            targetSpawnId = spawnId;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            bool isPlayer = other.CompareTag(playerTag) || (other.transform.parent != null && other.transform.parent.CompareTag(playerTag));
            if (isPlayer)
            {
                TriggerTransition();
            }
        }

        private void TriggerTransition()
        {
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning($"[SceneDoor] Target scene is not set on {gameObject.name}");
                return;
            }

            if (SceneTransitionManager.Instance == null)
            {
                Debug.LogError("[SceneDoor] SceneTransitionManager Instance is missing in the scene.");
                return;
            }

            // Start transition without waiting for it synchronously (forgetting the task)
            SceneTransitionManager.Instance.LoadSceneAsync(targetScene, targetSpawnId).Forget();
        }
    }
}
