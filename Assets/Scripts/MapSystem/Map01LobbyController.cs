using UnityEngine;
using DialogSystem;
using SaveSystem;
using Cysharp.Threading.Tasks;

namespace MapSystem
{
    public class Map01LobbyController : MapControllerBase
    {
        [Header("Lobby Config")]
        [SerializeField] private string lobbyEntryDialogueNodeId = "lobby_entry_1";
        [SerializeField] private string lobbyEntryFlagName = "lobby_entry_done";

        protected override void RegisterSceneEvents()
        {
        }

        private void Start()
        {
            CheckAndStartLobbyEntryDialogue().Forget();
        }

        private async UniTaskVoid CheckAndStartLobbyEntryDialogue()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            Player.PlayerController playerController = playerObj != null ? playerObj.GetComponent<Player.PlayerController>() : null;

            bool entryDone = false;
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
            {
                entryDone = SaveManager.Instance.CurrentSaveData.HasFlag(lobbyEntryFlagName);
            }

            // Immediately block player movement if entry dialogue is pending
            if (!entryDone && playerController != null)
            {
                playerController.SetControlEnabled(false);
                playerController.SetFacingDirection(Vector2.up);
            }

            // Wait until scene transition completely finishes (fade in complete)
            if (SceneTransitionManager.Instance != null)
            {
                await UniTask.WaitUntil(() => !SceneTransitionManager.Instance.IsTransitioning, cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            await UniTask.Delay(400, cancellationToken: this.GetCancellationTokenOnDestroy());

            if (!entryDone && DialogueManager.Instance != null)
            {
                Debug.Log($"[Map01LobbyController] Starting lobby entry dialogue: {lobbyEntryDialogueNodeId}");
                DialogueManager.Instance.StartDialogue(lobbyEntryDialogueNodeId);

                // Wait until dialogue finishes, then unlock control
                await UniTask.WaitUntil(() => DialogueManager.Instance.IsDialogueActive, cancellationToken: this.GetCancellationTokenOnDestroy());
                await UniTask.WaitUntil(() => !DialogueManager.Instance.IsDialogueActive, cancellationToken: this.GetCancellationTokenOnDestroy());

                if (playerController != null)
                {
                    playerController.SetControlEnabled(true);
                }
            }
        }
    }
}
