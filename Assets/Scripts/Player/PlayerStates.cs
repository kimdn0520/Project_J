using UnityEngine;

namespace Player
{
    // --- IDLE STATE ---
    public class PlayerIdleState : IPlayerState
    {
        public void Enter(PlayerController player)
        {
            player.UpdateAnimator(Vector2.zero);
        }

        public void Update(PlayerController player)
        {
            // Transition to Move state if movement input is detected
            if (player.CanMove && player.GetMoveInput().sqrMagnitude > 0.01f)
            {
                player.TransitionToState(player.MoveState);
            }
        }

        public void FixedUpdate(PlayerController player)
        {
            player.Rigidbody.linearVelocity = Vector2.zero;
        }

        public void Exit(PlayerController player)
        {
        }
    }

    // --- MOVE STATE ---
    public class PlayerMoveState : IPlayerState
    {
        public void Enter(PlayerController player)
        {
        }

        public void Update(PlayerController player)
        {
            Vector2 input = player.GetMoveInput();

            // Transition back to Idle if no input or movement is blocked
            if (input.sqrMagnitude <= 0.01f || !player.CanMove)
            {
                player.TransitionToState(player.IdleState);
                return;
            }

            player.UpdateAnimator(input);
        }

        public void FixedUpdate(PlayerController player)
        {
            Vector2 input = player.GetMoveInput();
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            Vector2 targetPos = player.Rigidbody.position + input * player.MoveSpeed * Time.fixedDeltaTime;
            player.Rigidbody.MovePosition(targetPos);
        }

        public void Exit(PlayerController player)
        {
        }
    }

    // --- BUSY STATE (Dialogue, Screen Transition, etc.) ---
    public class PlayerBusyState : IPlayerState
    {
        public void Enter(PlayerController player)
        {
            player.UpdateAnimator(Vector2.zero);
            player.Rigidbody.linearVelocity = Vector2.zero;
        }

        public void Update(PlayerController player)
        {
            // Recover to Idle state once the blocking condition is cleared
            if (player.CanMove)
            {
                player.TransitionToState(player.IdleState);
            }
        }

        public void FixedUpdate(PlayerController player)
        {
            player.Rigidbody.linearVelocity = Vector2.zero;
        }

        public void Exit(PlayerController player)
        {
        }
    }
}
