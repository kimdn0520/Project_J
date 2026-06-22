namespace DialogSystem
{
    /// <summary>
    /// Interface for any object that the player can interact with.
    /// Used to decouple the player interaction triggers from specific implementations.
    /// </summary>
    public interface IInteractable
    {
        void Interact();
    }
}
