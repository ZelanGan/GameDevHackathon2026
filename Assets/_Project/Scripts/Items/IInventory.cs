namespace NightTrolleybus.Items
{
    /// <summary>Команди інвентаря для інших систем.</summary>
    public interface IInventory
    {
        /// <summary>Додає предмет. Повертає false, якщо місця немає.</summary>
        bool TryAdd(ItemData item);

        int Count(ItemData item);

        /// <summary>Забирає кілька однакових предметів. Повертає false, якщо їх не вистачає.</summary>
        bool TryRemove(ItemData item, int count);
    }
}
