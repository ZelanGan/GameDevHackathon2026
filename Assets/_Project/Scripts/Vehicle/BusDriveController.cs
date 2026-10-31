using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Швидкість тролейбуса. Важіль лишається на місці, навіть коли водій іде з кабіни.</summary>
    public class BusDriveController : MonoBehaviour
    {
        [SerializeField] private BusConfig config;
        [Tooltip("Швидкість у діапазоні 0..1")]
        [SerializeField] private FloatEventChannel speedChanged;

        private InputAction moveAction;

        public bool IsDriving { get; private set; }
        public float Throttle { get; private set; }
        public float Speed { get; private set; }

        private void Awake() => moveAction = InputSystem.actions.FindAction("Player/Move", true);

        private void Start() => speedChanged.Raise(Speed);

        public void SetDriving(bool driving) => IsDriving = driving;

        public void SetThrottle(float throttle) => Throttle = Mathf.Clamp01(throttle);

        private void Update()
        {
            if (IsDriving)
                SetThrottle(Throttle + moveAction.ReadValue<Vector2>().y * config.ThrottleChangePerSecond * Time.deltaTime);

            // Без енергії важіль не діє: тролейбус котиться до зупинки.
            bool hasPower = GameContext.Power != null &&
                GameContext.Power.TryConsume(config.PowerPerSecondAtFullSpeed * Speed * Time.deltaTime);
            bool polesOff = GameContext.Faults != null && GameContext.Faults.IsActive(FaultType.PolesOff);
            float target = hasPower && !polesOff ? Throttle : 0f;

            float newSpeed = Mathf.MoveTowards(Speed, target, config.AccelerationPerSecond * Time.deltaTime);
            if (Mathf.Approximately(newSpeed, Speed))
                return;

            Speed = newSpeed;
            speedChanged.Raise(Speed);
        }
    }
}
