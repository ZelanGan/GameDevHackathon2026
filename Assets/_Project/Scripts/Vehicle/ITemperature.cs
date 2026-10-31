namespace NightTrolleybus.Vehicle
{
    /// <summary>Команди й читання температури салону для інших систем.</summary>
    public interface ITemperature
    {
        float Current { get; }
        float Normalized { get; }

        void SetCurrent(float value);

        /// <summary>Змінює температуру: додатне значення гріє, від'ємне охолоджує.</summary>
        void AddHeat(float amount);
    }
}
