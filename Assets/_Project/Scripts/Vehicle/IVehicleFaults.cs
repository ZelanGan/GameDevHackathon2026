namespace NightTrolleybus.Vehicle
{
    /// <summary>Команди й читання несправностей тролейбуса для інших систем.</summary>
    public interface IVehicleFaults
    {
        bool IsActive(FaultType fault);

        void Trigger(FaultType fault);

        void Clear(FaultType fault);
    }
}
