using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Єдине джерело правди про енергію тролейбуса. Нуль енергії не є поразкою.</summary>
    public class PowerSystem : MonoBehaviour, IPower
    {
        [SerializeField] private PowerConfig config;

        [Header("Канали подій")]
        [Tooltip("Енергія у діапазоні 0..1")]
        [SerializeField] private FloatEventChannel powerChanged;
        [SerializeField] private VoidEventChannel powerDepleted;
        [SerializeField] private VoidEventChannel powerRestored;

        public float Current { get; private set; }
        public float Normalized => Current / config.MaxPower;

        private void Awake()
        {
            Current = config.StartPower;
            GameContext.Register(this);
        }

        private void OnDestroy() => GameContext.Unregister(this);

        private void Start() => powerChanged.Raise(Normalized);

        public void SetCurrent(float value) => SetPower(value);

        public void Restore(float amount) => SetPower(Current + amount);

        public bool TryConsume(float amount)
        {
            if (Current <= 0f)
                return false;

            SetPower(Current - amount);
            return true;
        }

        private void SetPower(float value)
        {
            bool hadPower = Current > 0f;
            Current = Mathf.Clamp(value, 0f, config.MaxPower);
            powerChanged.Raise(Normalized);

            if (hadPower && Current <= 0f)
                powerDepleted.Raise();
            else if (!hadPower && Current > 0f)
                powerRestored.Raise();
        }
    }
}
