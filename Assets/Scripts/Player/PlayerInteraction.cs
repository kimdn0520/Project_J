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

        [Header("Directional Offsets (Local)")]
        [SerializeField] private Vector2 upOffset = new Vector2(0f, 0.65f);
        [SerializeField] private Vector2 downOffset = new Vector2(0f, -0.95f); // Extra long offset for DOWN facing to escape feet collider
        [SerializeField] private Vector2 leftOffset = new Vector2(-0.65f, 0f);
        [SerializeField] private Vector2 rightOffset = new Vector2(0.65f, 0f);

        [Header("Interaction Box Settings")]
        [SerializeField] private LayerMask interactableLayer = ~0; // Default to 'Everything'
        [SerializeField] private Vector2 interactionBoxSize = new Vector2(0.75f, 0.75f);
        
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

        public Vector3 GetTargetTriggerLocalPosition()
        {
            if (playerController == null)
                return downOffset;

            Vector2 dir = playerController.LastDirection;
            
            // Prioritize vertical/horizontal direction
            if (dir.y < -0.3f)
                return downOffset;
            if (dir.y > 0.3f)
                return upOffset;
            if (dir.x < -0.3f)
                return leftOffset;
            if (dir.x > 0.3f)
                return rightOffset;

            return downOffset; // Fallback to Down
        }

        private void UpdateTriggerPosition()
        {
            if (triggerTransform == null)
                return;

            triggerTransform.localPosition = GetTargetTriggerLocalPosition();
        }

        private void PerformInteraction()
        {
            IInteractable target = null;

            Vector3 checkPos = triggerTransform != null ? triggerTransform.position : (transform.position + GetTargetTriggerLocalPosition());

            // 1. Immediate OverlapBox check at current facing Trigger position (100% reliable)
            Collider2D[] colliders = Physics2D.OverlapBoxAll(checkPos, interactionBoxSize, 0f, interactableLayer);
            foreach (var col in colliders)
            {
                // Ignore self/player colliders
                if (col.transform.IsChildOf(transform) || col.CompareTag("Player")) continue;

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

            // 2. Fallback to TriggerZone cached list if OverlapBox didn't find any target
            if (target == null && triggerZone != null)
            {
                target = triggerZone.GetTargetInteractable();
            }

            if (target != null)
            {
                Debug.Log($"[PlayerInteraction] Interacting with target via trigger position ({checkPos})");
                target.Interact();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            Vector3 checkPos = triggerTransform != null ? triggerTransform.position : (transform.position + GetTargetTriggerLocalPosition());

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(checkPos, interactionBoxSize);
        }
    }
}
