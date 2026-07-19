using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cysharp.Threading.Tasks;
using SaveSystem;
using MapSystem;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace UI
{
    public class TitleUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private CanvasGroup titleCanvasGroup;
        [SerializeField] private CanvasGroup titleFadeCanvasGroup;

        [Header("Target Start Scene")]
        [SerializeField] private string startSceneName = "Map_00_HotelExterior";
        [SerializeField] private string startSpawnId = "Spawn_Default";

        private bool isStarting = false;

        private void Start()
        {
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameClicked);
            }
        }

        public void OnStartGameClicked()
        {
            if (isStarting) return;
            isStarting = true;

            if (startGameButton != null)
            {
                startGameButton.interactable = false;
            }

            StartGameSequence().Forget();
        }

        private async UniTaskVoid StartGameSequence()
        {
            Debug.Log("[TitleUI] Starting Game Sequence...");

            // 1. Play Click SFX
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("button_click");
            }

            // 2. Smooth Fade Out Title screen locally (Title screen turns 100% black)
            if (titleFadeCanvasGroup != null)
            {
                titleFadeCanvasGroup.blocksRaycasts = true;
                await titleFadeCanvasGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad).ToUniTask();
            }
            else if (titleCanvasGroup != null)
            {
                await titleCanvasGroup.DOFade(0f, 0.5f).SetEase(Ease.OutQuad).ToUniTask();
            }

            // 3. Ensure Persistent scene is loaded additively if not present
            Scene persistentScene = SceneManager.GetSceneByName("Persistent");
            if (!persistentScene.isLoaded)
            {
                Debug.Log("[TitleUI] Persistent scene is not loaded. Loading additively...");
                var asyncOp = SceneManager.LoadSceneAsync("Persistent", LoadSceneMode.Additive);
                await asyncOp.ToUniTask();
                await UniTask.Yield();
            }

            // 4. Start new game state in SaveManager
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.StartNewGame(startSceneName, startSpawnId);
            }

            // 5. Trigger transition under black screen (Unload Title -> Load Map_00 -> Fade In)
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.StartGameFromBlackSceneAsync(startSceneName, startSpawnId).Forget();
            }
            else
            {
                SceneManager.LoadSceneAsync(startSceneName, LoadSceneMode.Single);
            }
        }
    }
}
