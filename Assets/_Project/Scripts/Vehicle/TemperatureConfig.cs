using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Налаштування температури салону. Під час гри цей асет не змінюється.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Config/Temperature", fileName = "TemperatureConfig")]
    public class TemperatureConfig : ScriptableObject
    {
        [SerializeField] private float maxTemperature = 100f;
        [SerializeField] private float startTemperature = 60f;
        [Tooltip("Охолодження за секунду із зачиненими дверима")]
        [SerializeField] private float coolingPerSecond = 0.6f;
        [Tooltip("Додаткове охолодження за секунду з відчиненими дверима")]
        [SerializeField] private float openDoorsCoolingPerSecond = 2f;

        public float MaxTemperature => maxTemperature;
        public float StartTemperature => startTemperature;
        public float CoolingPerSecond => coolingPerSecond;
        public float OpenDoorsCoolingPerSecond => openDoorsCoolingPerSecond;
    }
}
