using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Єдине джерело правди про температуру салону.</summary>
    public class TemperatureSystem : MonoBehaviour, ITemperature
    {
        [SerializeField] private TemperatureConfig config;

        [Header("Канали подій")]
        [Tooltip("Температура у діапазоні 0..1")]
        [SerializeField] private FloatEventChannel temperatureChanged;
        [SerializeField] private BoolEventChannel doorsOpenChanged;

        private bool doorsOpen;

        public float Current { get; private set; }
        public float Normalized => Current / config.MaxTemperature;

        private void Awake()
        {
            Current = config.StartTemperature;
            GameContext.Register(this);
        }

        private void OnEnable() => doorsOpenChanged.Raised += OnDoorsOpenChanged;

        private void OnDisable() => doorsOpenChanged.Raised -= OnDoorsOpenChanged;

        private void OnDestroy() => GameContext.Unregister(this);

        private void Start() => temperatureChanged.Raise(Normalized);

        private void Update()
        {
            float cooling = config.CoolingPerSecond + (doorsOpen ? config.OpenDoorsCoolingPerSecond : 0f);
            AddHeat(-cooling * Time.deltaTime);
        }

        public void AddHeat(float amount) => SetCurrent(Current + amount);

        public void SetCurrent(float value)
        {
            Current = Mathf.Clamp(value, 0f, config.MaxTemperature);
            temperatureChanged.Raise(Normalized);
        }

        private void OnDoorsOpenChanged(bool isOpen) => doorsOpen = isOpen;
    }
}
