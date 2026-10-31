namespace NightTrolleybus.Flame
{
    /// <summary>Команди й читання стану Вогника для інших систем.</summary>
    public interface IFlame
    {
        float Current { get; }
        float Normalized { get; }
        FlameState State { get; }

        /// <summary>Встановлює силу полум'я із збереження.</summary>
        void SetCurrent(float value);

        /// <summary>Додає полум'я. Згаслий Вогник відновити не можна.</summary>
        void Restore(float amount);

        /// <summary>Витрачає полум'я, якщо його вистачає. Вогник не може згаснути від власної здібності.</summary>
        bool TrySpend(float amount);

        /// <summary>Забирає полум'я ззовні. На відміну від здібності, це може загасити Вогника.</summary>
        void Drain(float amount);
    }
}
