using UnityEngine;
using UnityEngine.InputSystem;
using DialogSystem;
using MapSystem;

namespace Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : SingletonMonoBehaviour<PlayerController>
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 4f;

        [Header("Animation Parameter Names")]
        [SerializeField] private string moveXParam = "MoveX";
        [SerializeField] private string moveYParam = "MoveY";
        [SerializeField] private string lastMoveXParam = "LastMoveX";
        [SerializeField] private string lastMoveYParam = "LastMoveY";
        [SerializeField] private string isMovingParam = "IsMoving";

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private PlayerInput playerInput;
        private InputAction moveAction;

        private Vector2 lastDirection = Vector2.down; // Default looking down
        private IPlayerState currentState;

        // Concrete States
        public PlayerIdleState IdleState { get; private set; }
        public PlayerMoveState MoveState { get; private set; }
        public PlayerBusyState BusyState { get; private set; }

        public Rigidbody2D Rigidbody => rb;
        public float MoveSpeed => moveSpeed;
        public Vector2 LastDirection => lastDirection;

        public bool CanMove
        {
            get
            {
                if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                    return false;
                
                if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsTransitioning)
                    return false;

                return true;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            
            if (!gameObject.CompareTag("Player"))
            {
                gameObject.tag = "Player";
            }

            rb = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();
            
            // Search in children to allow decoupling Animator/SpriteRenderer from the physical root
            animator = GetComponentInChildren<Animator>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (playerInput != null)
            {
                moveAction = playerInput.actions.FindAction("Move");
            }

            // Initialize States
            IdleState = new PlayerIdleState();
            MoveState = new PlayerMoveState();
            BusyState = new PlayerBusyState();
        }

        private void Start()
        {
            // Initial state
            currentState = IdleState;
            currentState.Enter(this);
        }

        private void Update()
        {
            // Lazy lookup in case components were added dynamically
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Handle State update
            currentState.Update(this);

            // Force transition to Busy state if CanMove becomes false dynamically
            if (!CanMove && currentState != BusyState)
            {
                TransitionToState(BusyState);
            }
        }

        private void FixedUpdate()
        {
            currentState.FixedUpdate(this);
        }

        public void TransitionToState(IPlayerState newState)
        {
            if (currentState == newState) return;

            currentState.Exit(this);
            currentState = newState;
            currentState.Enter(this);
        }

        public Vector2 GetMoveInput()
        {
            if (moveAction == null) return Vector2.zero;
            return moveAction.ReadValue<Vector2>();
        }

        public void UpdateAnimator(Vector2 currentMove)
        {
            bool isMoving = currentMove.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                lastDirection = currentMove.normalized;
            }

            if (animator != null)
            {
                animator.SetFloat(moveXParam, currentMove.x);
                animator.SetFloat(moveYParam, currentMove.y);
                animator.SetBool(isMovingParam, isMoving);

                animator.SetFloat(lastMoveXParam, lastDirection.x);
                animator.SetFloat(lastMoveYParam, lastDirection.y);
            }

            if (spriteRenderer != null)
            {
                // Flip sprite horizontally when moving or facing left/right.
                if (lastDirection.x < -0.01f)
                {
                    spriteRenderer.flipX = false; // Face Left (original)
                }
                else if (lastDirection.x > 0.01f)
                {
                    spriteRenderer.flipX = true;  // Face Right (flipped)
                }
            }
        }
    }
}
