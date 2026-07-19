using UnityEngine;
using DialogSystem;
using SaveSystem;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace MapSystem
{
    public class Map00ExteriorController : MapControllerBase
    {
        [Header("Prologue Config")]
        [SerializeField] private string introDialogueNodeId = "exterior_intro_1";
        [SerializeField] private string introFlagName = "exterior_intro_done";
        [SerializeField] private string nextSceneName = "Map_01_Lobby";
        [SerializeField] private string nextSpawnId = "Spawn_FromExterior";

        [Header("Door Visual Target")]
        [SerializeField] private Transform hotelDoorTransform;
        [SerializeField] private Transform doorEntrancePoint;

        protected override void RegisterSceneEvents()
        {
            RegisterEvent("sfx_door_creak", OnDoorCreakSFX);
        }

        private void Start()
        {
            CheckAndStartIntroDialogue().Forget();
        }

        private async UniTaskVoid CheckAndStartIntroDialogue()
        {
            await UniTask.Delay(500, cancellationToken: this.GetCancellationTokenOnDestroy());

            if (DialogueManager.Instance != null && SaveManager.Instance != null)
            {
                bool introDone = SaveManager.Instance.CurrentSaveData != null && 
                                 SaveManager.Instance.CurrentSaveData.HasFlag(introFlagName);

                if (!introDone)
                {
                    // Face player upwards towards the door during dialogue
                    SetPlayerFacingUp();

                    // Start intro dialogue
                    DialogueManager.Instance.StartDialogue(introDialogueNodeId);

                    // Wait until the entire dialogue is completely finished by user input
                    await UniTask.WaitUntil(() => DialogueManager.Instance.IsDialogueActive, cancellationToken: this.GetCancellationTokenOnDestroy());
                    await UniTask.WaitUntil(() => !DialogueManager.Instance.IsDialogueActive, cancellationToken: this.GetCancellationTokenOnDestroy());

                    // Now trigger auto walking into hotel door ONLY AFTER dialogue is 100% finished
                    OnAutoEnterHotel();
                }
            }
        }

        private void SetPlayerFacingUp()
        {
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
        }

        private void OnDoorCreakSFX()
        {
            Debug.Log("[Map00ExteriorController] SFX Door Creak & Door Opening Motion!");
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("door_open");
            }

            // Animate single door creak motion
            if (hotelDoorTransform != null)
            {
                hotelDoorTransform.DOLocalRotate(new Vector3(0, 0, -8f), 1.2f).SetEase(Ease.OutQuad);
                hotelDoorTransform.DOLocalMoveX(0.2f, 1.2f).SetEase(Ease.OutQuad);
            }
        }

        private void OnAutoEnterHotel()
        {
            Debug.Log("[Map00ExteriorController] Intro dialogue fully finished. Auto walking player towards the door...");
            
            if (SaveManager.Instance != null && SaveManager.Instance.CurrentSaveData != null)
            {
                SaveManager.Instance.CurrentSaveData.SetFlag(introFlagName, true);
            }

            PlayPlayerEnterDoorMotion().Forget();
        }

        private async UniTaskVoid PlayPlayerEnterDoorMotion()
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");

            Vector3 targetDoorPos = doorEntrancePoint != null ? doorEntrancePoint.position : new Vector3(0, 2.0f, 0);

            if (playerObj != null)
            {
                Player.PlayerController playerController = playerObj.GetComponent<Player.PlayerController>();
                Animator animator = playerObj.GetComponentInChildren<Animator>();
                SpriteRenderer spriteRenderer = playerObj.GetComponentInChildren<SpriteRenderer>();

                // Lock player control
                if (playerController != null)
                {
                    playerController.TransitionToState(playerController.BusyState);
                    playerController.SetFacingDirection(Vector2.up);
                }

                // Trigger up-walk animation
                if (animator != null)
                {
                    animator.SetFloat("MoveX", 0f);
                    animator.SetFloat("MoveY", 1.0f);
                    animator.SetFloat("LastMoveX", 0f);
                    animator.SetFloat("LastMoveY", 1.0f);
                    animator.SetBool("IsMoving", true);
                }

                // Move player smoothly towards the door
                float duration = 1.8f;
                playerObj.transform.DOMove(targetDoorPos, duration).SetEase(Ease.Linear);

                await UniTask.Delay((int)(duration * 1000 * 0.65f), cancellationToken: this.GetCancellationTokenOnDestroy());

                // Fade out sprite as player reaches door threshold
                if (spriteRenderer != null)
                {
                    spriteRenderer.DOFade(0f, duration * 0.35f);
                }

                await UniTask.Delay((int)(duration * 1000 * 0.4f), cancellationToken: this.GetCancellationTokenOnDestroy());

                if (animator != null)
                {
                    animator.SetBool("IsMoving", false);
                }
            }

            // Load Lobby scene seamlessly
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadSceneAsync(nextSceneName, nextSpawnId).Forget();
            }
        }

        public void SetDoorTransform(Transform door, Transform entrancePoint)
        {
            hotelDoorTransform = door;
            doorEntrancePoint = entrancePoint;
        }
    }
}
