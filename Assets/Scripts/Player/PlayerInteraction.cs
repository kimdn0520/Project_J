using UnityEngine;
using UnityEngine.InputSystem;
using DialogSystem;

namespace Player
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactRange = 1.5f;
        [SerializeField] private LayerMask interactableLayer = ~0; // Default to 'Everything'
        [SerializeField] private Vector2 interactionBoxSize = new Vector2(1.0f, 1.0f);
        
        [Header("Cooldown Settings")]
        [SerializeField] private float dialogueEndCooldown = 0.3f; // Prevent immediate re-trigger after dialogue ends

        private PlayerController playerController;
        private PlayerInput playerInput;
        private InputAction interactAction;
        
        private float lastDialogueEndTime = -10f;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            playerInput = GetComponent<PlayerInput>();

            if (playerInput != null)
            {
                interactAction = playerInput.actions.FindAction("Interact");
            }
        }

        private void Start()
        {
            // Register callback to capture the exact time dialogue closes
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnd += OnDialogueEnded;
            }
        }

        private void OnDestroy()
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.OnDialogueEnd -= OnDialogueEnded;
            }
        }

        private void OnDialogueEnded()
        {
            lastDialogueEndTime = Time.time;
        }

        private void Update()
        {
            // Block interaction if player cannot move (e.g. during active dialogue or transition)
            if (!playerController.CanMove)
                return;

            // Block interaction if a dialogue just closed to prevent immediate double-interact (Space mash loop)
            if (Time.time - lastDialogueEndTime < dialogueEndCooldown)
                return;

            bool shouldInteract = false;

            // 1. Direct keyboard fallback check (Space, Z, Enter)
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.spaceKey.wasPressedThisFrame || 
                    keyboard.zKey.wasPressedThisFrame || 
                    keyboard.enterKey.wasPressedThisFrame)
                {
                    shouldInteract = true;
                }
            }

            // 2. Input System Action check (e.g., E key)
            if (interactAction != null && interactAction.triggered)
            {
                shouldInteract = true;
            }

            if (shouldInteract)
            {
                PerformInteraction();
            }
        }

        private void PerformInteraction()
        {
            Vector2 lookDir = playerController.LastDirection;
            Vector2 checkPosition = (Vector2)transform.position + lookDir * (interactRange * 0.5f);

            // Use OverlapBox to detect interactable objects in front of the player
            Collider2D[] colliders = Physics2D.OverlapBoxAll(checkPosition, interactionBoxSize, 0f, interactableLayer);

            foreach (var col in colliders)
            {
                IInteractable interactable = col.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    interactable = col.GetComponentInParent<IInteractable>();
                }

                if (interactable != null)
                {
                    Debug.Log($"[PlayerInteraction] Interacting with: {col.gameObject.name}");
                    interactable.Interact();
                    break; // Only interact with one target
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            Vector2 lookDir = playerController != null ? playerController.LastDirection : Vector2.down;
            Vector2 checkPosition = (Vector2)transform.position + lookDir * (interactRange * 0.5f);

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(checkPosition, interactionBoxSize);
        }
    }
}
