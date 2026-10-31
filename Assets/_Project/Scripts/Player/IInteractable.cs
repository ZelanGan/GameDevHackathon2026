namespace NightTrolleybus.Player
{
    /// <summary>Об'єкт, з яким гравець може взаємодіяти клавішею.</summary>
    public interface IInteractable
    {
        string Prompt { get; }

        /// <summary>Чи можна діяти зараз: якщо ні, кнопка E над об'єктом не з'являється.</summary>
        bool CanInteract => true;

        /// <summary>Короткий напис над кнопкою. Лише там, де без нього не зрозуміти дію, наприклад ціна.</summary>
        string Label => null;

        void Interact();
    }
}
