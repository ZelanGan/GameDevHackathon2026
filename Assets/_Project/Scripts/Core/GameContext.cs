using NightTrolleybus.Flame;
using NightTrolleybus.Items;
using NightTrolleybus.Vehicle;
using UnityEngine;

namespace NightTrolleybus.Core
{
    /// <summary>Точка доступу до команд систем. Стан передають події, а дії з результатом ідуть сюди.</summary>
    public static class GameContext
    {
        public static IFlame Flame { get; private set; }
        public static IFear Fear { get; private set; }
        public static ITemperature Temperature { get; private set; }
        public static IPower Power { get; private set; }
        public static IVehicleFaults Faults { get; private set; }

        public static IInventory Inventory { get; private set; }

        public static void Register(IInventory inventory) => Inventory = inventory;

        public static void Unregister(IInventory inventory)
        {
            if (Inventory == inventory)
                Inventory = null;
        }

        public static void Register(IFlame flame) => Flame = flame;

        public static void Register(IFear fear) => Fear = fear;

        public static void Register(ITemperature temperature) => Temperature = temperature;

        public static void Register(IPower power) => Power = power;

        public static void Register(IVehicleFaults faults) => Faults = faults;

        public static void Unregister(IFlame flame)
        {
            if (Flame == flame)
                Flame = null;
        }

        public static void Unregister(IFear fear)
        {
            if (Fear == fear)
                Fear = null;
        }

        public static void Unregister(ITemperature temperature)
        {
            if (Temperature == temperature)
                Temperature = null;
        }

        public static void Unregister(IPower power)
        {
            if (Power == power)
                Power = null;
        }

        public static void Unregister(IVehicleFaults faults)
        {
            if (Faults == faults)
                Faults = null;
        }

        // Статичні поля можуть пережити запуск, якщо вимкнено перезавантаження домену.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Flame = null;
            Fear = null;
            Temperature = null;
            Power = null;
            Faults = null;
            Inventory = null;
        }
    }
}
