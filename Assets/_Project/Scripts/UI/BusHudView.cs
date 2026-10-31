using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Частина інтерфейсу для салону: несправності й ремонт.</summary>
    public class BusHudView : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel faultsChanged;
        [SerializeField] private StringEventChannel repairTextChanged;
        [SerializeField] private Text faultsLabel;
        [SerializeField] private Text repairLabel;

        private void OnEnable()
        {
            faultsChanged.Raised += OnFaultsChanged;
            repairTextChanged.Raised += OnRepairTextChanged;
        }

        private void OnDisable()
        {
            faultsChanged.Raised -= OnFaultsChanged;
            repairTextChanged.Raised -= OnRepairTextChanged;
        }

        private void OnRepairTextChanged(string text) => repairLabel.text = text;

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
