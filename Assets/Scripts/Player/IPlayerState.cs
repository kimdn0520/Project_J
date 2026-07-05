namespace Player
{
    /// <summary>
    /// Base interface for player states in the State Pattern.
    /// </summary>
    public interface IPlayerState
    {
        void Enter(PlayerController player);
        void Update(PlayerController player);
        void FixedUpdate(PlayerController player);
        void Exit(PlayerController player);
    }
}
