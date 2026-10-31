namespace NightTrolleybus.Flame
{
    /// <summary>Джерела страху Вогника: темрява, тіньові пасажири.</summary>
    public interface IFear
    {
        /// <summary>Страх у діапазоні 0..1.</summary>
        float Normalized { get; }

        /// <summary>Додає або оновлює джерело страху. Ключем є сам об'єкт-джерело.</summary>
        void SetSource(object source, float amount);

        void RemoveSource(object source);
    }
}
