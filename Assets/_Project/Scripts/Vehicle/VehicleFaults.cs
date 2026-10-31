using System.Collections.Generic;
using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Єдине джерело правди про несправності тролейбуса.</summary>
    public class VehicleFaults : MonoBehaviour, IVehicleFaults
    {
        [Tooltip("Скільки енергії за секунду витікає через роз'єднані проводи")]
        [SerializeField] private float wiresLeakPerSecond = 1f;
        [SerializeField] private VoidEventChannel faultsChanged;

        private readonly HashSet<FaultType> active = new HashSet<FaultType>();

        private void Awake() => GameContext.Register(this);

        private void OnDestroy() => GameContext.Unregister(this);

        public bool IsActive(FaultType fault) => active.Contains(fault);

        public void Trigger(FaultType fault)
        {
            if (active.Add(fault))
                faultsChanged.Raise();
        }

        public void Clear(FaultType fault)
        {
            if (active.Remove(fault))
                faultsChanged.Raise();
        }

        private void Update()
        {
            if (IsActive(FaultType.WiresLoose))
                GameContext.Power?.TryConsume(wiresLeakPerSecond * Time.deltaTime);
        }
    }
}
