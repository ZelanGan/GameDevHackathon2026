using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    public enum DeviceKind
    {
        Heater,
        CabinLight
    }

    /// <summary>Вимикач у салоні. Сам пристрій живе в сцені Boot, тому його шукаємо під час запуску.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class DeviceSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private DeviceKind kind;
        [SerializeField] private string deviceName;

        private PowerConsumer device;

        public string Prompt => $"{(device.IsOn ? "Вимкнути" : "Увімкнути")}: {deviceName}";

        private void Awake()
        {
            device = kind == DeviceKind.Heater
                ? (PowerConsumer)FindAnyObjectByType<Heater>()
                : FindAnyObjectByType<CabinLight>();
        }

        public void Interact() => device.Toggle();
    }
}
