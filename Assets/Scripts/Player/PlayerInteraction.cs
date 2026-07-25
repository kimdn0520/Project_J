using UnityEngine;
using UnityEngine.InputSystem;
using DialogSystem;

namespace Player
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Trigger References")]
        [SerializeField] private Transform triggerTransform;
        [SerializeField] private PlayerTriggerZone triggerZone;
        [SerializeField] private float triggerOffset = 0.45f;

        [Header("Fallback Interaction Settings")]
        [SerializeField] private LayerMask interactableLayer = ~0; // Default to 'Everything'
        [SerializeField] private Vector2 interactionBoxSize = new Vector2(0.6f, 0.6f);
        
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

            SetupTriggerReferences();
        }

        private void SetupTriggerReferences()
        {
            if (triggerTransform == null)
            {
                Transform t = transform.Find("Trigger");
                if (t != null)
                {
                    triggerTransform = t;
                }
            }

            if (triggerTransform != null && triggerZone == null)
            {
                triggerZone = triggerTransform.GetComponent<PlayerTriggerZone>();
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
            UpdateTriggerPosition();

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

        private void UpdateTriggerPosition()
        {
            if (triggerTransform == null || playerController == null)
                return;

            Vector2 dir = playerController.LastDirection;
            if (dir == Vector2.zero)
                dir = Vector2.down;

            // Position the child Trigger transform slightly ahead in facing direction
            triggerTransform.localPosition = dir * triggerOffset;
        }

        private void PerformInteraction()
        {
            IInteractable target = null;

            // 1. Check TriggerZone first
            if (triggerZone != null)
            {
                target = triggerZone.GetTargetInteractable();
            }

            // 2. Fallback check with OverlapBox if TriggerZone didn't detect an interactable
            if (target == null)
            {
                Vector3 checkPos = triggerTransform != null ? triggerTransform.position : (transform.position + (Vector3)(playerController.LastDirection * triggerOffset));
                Collider2D[] colliders = Physics2D.OverlapBoxAll(checkPos, interactionBoxSize, 0f, interactableLayer);

                foreach (var col in colliders)
                {
                    // Ignore self colliders
                    if (col.transform.IsChildOf(transform)) continue;

                    IInteractable interactable = col.GetComponent<IInteractable>();
                    if (interactable == null)
                    {
                        interactable = col.GetComponentInParent<IInteractable>();
                    }

                    if (interactable != null)
                    {
                        target = interactable;
                        break;
                    }
                }
            }

            if (target != null)
            {
                Debug.Log($"[PlayerInteraction] Interacting with target via trigger");
                target.Interact();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            Vector2 dir = playerController != null ? playerController.LastDirection : Vector2.down;
            Vector3 checkPos = triggerTransform != null ? triggerTransform.position : (transform.position + (Vector3)(dir * triggerOffset));

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(checkPos, interactionBoxSize);
        }
    }
}
