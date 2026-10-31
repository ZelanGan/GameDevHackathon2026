using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>Міні-гра ремонту. Логіка не залежить від інтерфейсу: вона лише дає текст для показу.</summary>
    public abstract class RepairMinigame
    {
        public abstract string Text { get; }

        /// <summary>Повертає true, коли ремонт завершено.</summary>
        public abstract bool Tick(Keyboard keyboard, float deltaTime);

        /// <summary>Номер натиснутої клавіші 1..3 або 0, якщо жодної.</summary>
        protected static int ReadDigit(Keyboard keyboard)
        {
            if (keyboard.digit1Key.wasPressedThisFrame)
                return 1;
            if (keyboard.digit2Key.wasPressedThisFrame)
                return 2;
            return keyboard.digit3Key.wasPressedThisFrame ? 3 : 0;
        }
    }
}
