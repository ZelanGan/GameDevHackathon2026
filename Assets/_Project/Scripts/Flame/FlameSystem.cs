using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Flame
{
    /// <summary>Єдине джерело правди про силу полум'я: шкала, згасання, стани.</summary>
    public class FlameSystem : MonoBehaviour, IFlame
    {
        [SerializeField] private FlameConfig config;

        [Header("Канали подій")]
        [Tooltip("Сила полум'я у діапазоні 0..1")]
        [SerializeField] private FloatEventChannel flameChanged;
        [SerializeField] private FlameStateEventChannel flameStateChanged;
        [SerializeField] private VoidEventChannel flameExtinguished;
        [SerializeField] private VoidEventChannel fuelReceived;
        [Tooltip("Температура салону у діапазоні 0..1")]
        [SerializeField] private FloatEventChannel temperatureChanged;
        [Tooltip("Страх Вогника у діапазоні 0..1")]
        [SerializeField] private FloatEventChannel fearChanged;
        [Tooltip("Чи гравець із Вогником у салоні: температура салону діє лише там")]
        [SerializeField] private BoolEventChannel inBusChanged;

        private float temperature = 1f;
        private bool inBus;
        private float coldDrainMultiplier = 1f;
        private float fearDrainMultiplier = 1f;

        public float Current { get; private set; }
        public float Normalized => Current / config.MaxFlame;
        public FlameState State { get; private set; }

        private void Awake()
        {
            Current = config.StartFlame;
            // Оцінка від найсильнішого стану, щоб гістерезис не занизив стартовий стан.
            State = config.EvaluateState(Current, FlameState.Bright);
            GameContext.Register(this);
        }

        private void OnEnable()
        {
            temperatureChanged.Raised += OnTemperatureChanged;
            fearChanged.Raised += OnFearChanged;
            inBusChanged.Raised += OnInBusChanged;
        }

        private void OnDisable()
        {
            temperatureChanged.Raised -= OnTemperatureChanged;
            fearChanged.Raised -= OnFearChanged;
            inBusChanged.Raised -= OnInBusChanged;
        }

        private void OnDestroy() => GameContext.Unregister(this);

        private void Start()
        {
            flameChanged.Raise(Normalized);
            flameStateChanged.Raise(State);
        }

        private void Update()
        {
            if (State == FlameState.Extinguished)
                return;

            SetFlame(Current - config.BaseDrainPerSecond * coldDrainMultiplier * fearDrainMultiplier * Time.deltaTime);
        }

        private void OnTemperatureChanged(float normalized)
        {
            temperature = normalized;
            RefreshCold();
        }

        private void OnInBusChanged(bool value)
        {
            inBus = value;
            RefreshCold();
        }

        private void RefreshCold() => coldDrainMultiplier = inBus ? config.GetColdDrainMultiplier(temperature) : 1f;

        private void OnFearChanged(float normalized) => fearDrainMultiplier = config.GetFearDrainMultiplier(normalized);

        public void SetCurrent(float value)
        {
            State = config.EvaluateState(value, FlameState.Bright);
            SetFlame(value);
            flameStateChanged.Raise(State);
        }

        public void Restore(float amount)
        {
            if (State == FlameState.Extinguished)
                return;

            SetFlame(Current + amount);
            fuelReceived.Raise();
        }

        public bool TrySpend(float amount)
        {
            if (State == FlameState.Extinguished || Current <= amount)
                return false;

            SetFlame(Current - amount);
            return true;
        }

        public void Drain(float amount)
        {
            if (State != FlameState.Extinguished)
                SetFlame(Current - amount);
        }

        private void SetFlame(float value)
        {
            Current = Mathf.Clamp(value, 0f, config.MaxFlame);
            flameChanged.Raise(Normalized);

            FlameState newState = config.EvaluateState(Current, State);
            if (newState == State)
                return;

            State = newState;
            flameStateChanged.Raise(State);

            if (State == FlameState.Extinguished)
                flameExtinguished.Raise();
        }
    }
}
