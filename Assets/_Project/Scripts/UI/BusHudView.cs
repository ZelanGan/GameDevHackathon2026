using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Частина інтерфейсу для салону: несправності.</summary>
    public class BusHudView : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel faultsChanged;
        [SerializeField] private Text faultsLabel;

        private void OnEnable() => faultsChanged.Raised += OnFaultsChanged;

        private void OnDisable() => faultsChanged.Raised -= OnFaultsChanged;

        private void OnFaultsChanged()
        {
            IVehicleFaults faults = GameContext.Faults;
            string text = string.Empty;
            if (faults.IsActive(FaultType.PolesOff))
                text += Loc.T("Штанги злетіли. ");
            if (faults.IsActive(FaultType.FuseBlown))
                text += Loc.T("Запобіжник згорів. ");
            if (faults.IsActive(FaultType.WiresLoose))
                text += Loc.T("Витік енергії. ");
            faultsLabel.text = text;
        }
    }
}
