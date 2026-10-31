namespace NightTrolleybus.Player
{
    /// <summary>Об'єкт, з яким гравець може взаємодіяти клавішею.</summary>
    public interface IInteractable
    {
        string Prompt { get; }

        void Interact();
    }
}
