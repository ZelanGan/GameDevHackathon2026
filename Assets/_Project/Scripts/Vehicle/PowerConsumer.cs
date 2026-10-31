using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Пристрій, що працює лише тоді, коли ввімкнений і є енергія. Працює й тоді, коли гравець вийшов із салону.</summary>
    public abstract class PowerConsumer : MonoBehaviour
    {
        [SerializeField] private float powerPerSecond = 1f;
        [SerializeField] private BoolEventChannel runningChanged;

        public bool IsOn { get; private set; }
        public bool IsRunning { get; private set; }

        private void Start() => runningChanged.Raise(IsRunning);

        public void Toggle() => IsOn = !IsOn;

        public void SetOn(bool value) => IsOn = value;

        private void Update()
        {
            bool fuseBlown = GameContext.Faults != null && GameContext.Faults.IsActive(FaultType.FuseBlown);
            bool running = IsOn && !fuseBlown && GameContext.Power != null && GameContext.Power.TryConsume(powerPerSecond * Time.deltaTime);
            if (running != IsRunning)
            {
                IsRunning = running;
                runningChanged.Raise(running);
            }

            if (running)
                Tick(Time.deltaTime);
        }

        protected virtual void Tick(float deltaTime)
        {
        }
    }
}
