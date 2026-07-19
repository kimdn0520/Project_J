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
            // Wait until scene transition completely finishes (fade in complete)
            if (SceneTransitionManager.Instance != null)
            {
                await UniTask.WaitUntil(() => !SceneTransitionManager.Instance.IsTransitioning, cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            await UniTask.Delay(400, cancellationToken: this.GetCancellationTokenOnDestroy());

            // Set player facing UP (entering into lobby from exterior door)
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                var playerController = playerObj.GetComponent<Player.PlayerController>();
                if (playerController != null)
                {
                    playerController.SetFacingDirection(Vector2.up);
                }
            }

            if (DialogueManager.Instance != null)
            {
                bool entryDone = false;
                if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
                {
                    entryDone = SaveManager.Instance.CurrentSaveData.HasFlag(lobbyEntryFlagName);
                }

                if (!entryDone)
                {
                    Debug.Log($"[Map01LobbyController] Starting lobby entry dialogue: {lobbyEntryDialogueNodeId}");
                    DialogueManager.Instance.StartDialogue(lobbyEntryDialogueNodeId);
                }
            }
        }
    }
}
