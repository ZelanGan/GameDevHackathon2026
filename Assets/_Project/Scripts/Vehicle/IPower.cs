namespace NightTrolleybus.Vehicle
{
    /// <summary>Команди й читання енергії тролейбуса для інших систем.</summary>
    public interface IPower
    {
        float Current { get; }
        float Normalized { get; }

        void SetCurrent(float value);

        void Restore(float amount);

        /// <summary>Витрачає енергію. Повертає false, якщо енергії вже немає.</summary>
        bool TryConsume(float amount);
    }
}
