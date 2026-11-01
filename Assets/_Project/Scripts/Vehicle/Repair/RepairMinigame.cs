using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>Міні-гра ремонту. Логіка не залежить від інтерфейсу: вона лише відкриває свій стан для показу.</summary>
    public abstract class RepairMinigame
    {
        private int pressedChoice;

        /// <summary>Скільки разів гравець помилився: інтерфейс показує кожну помилку.</summary>
        public int Mistakes { get; protected set; }

        /// <summary>Вибір мишею: спрацює в наступному Tick, як і клавіша.</summary>
        public void Press(int choice) => pressedChoice = choice;

        /// <summary>Повертає true, коли ремонт завершено.</summary>
        public abstract bool Tick(Keyboard keyboard, float deltaTime);

        /// <summary>Вибір мишею або 0, якщо його не було.</summary>
        protected int ConsumePress()
        {
            int choice = pressedChoice;
            pressedChoice = 0;
            return choice;
        }

        /// <summary>Вибраний номер 1..3 або 0, якщо вибору не було.</summary>
        protected int ReadChoice(Keyboard keyboard)
        {
            int choice = ConsumePress();
            if (choice != 0)
                return choice;
            if (keyboard.digit1Key.wasPressedThisFrame)
                return 1;
            if (keyboard.digit2Key.wasPressedThisFrame)
                return 2;
            return keyboard.digit3Key.wasPressedThisFrame ? 3 : 0;
        }
    }
}
