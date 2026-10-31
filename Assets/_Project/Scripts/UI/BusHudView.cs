using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Vehicle;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Частина інтерфейсу для салону: швидкість, шлях, страх, несправності й ремонт.</summary>
    public class BusHudView : MonoBehaviour
    {
        [SerializeField] private FloatEventChannel speedChanged;
        [SerializeField] private FloatEventChannel tripChanged;
        [SerializeField] private FloatEventChannel fearChanged;
        [SerializeField] private VoidEventChannel faultsChanged;
        [SerializeField] private StringEventChannel repairTextChanged;
        [SerializeField] private Text speedLabel;
        [SerializeField] private Text fearLabel;
        [SerializeField] private Text faultsLabel;
        [SerializeField] private Text repairLabel;

        private float speed;
        private float trip;

        private void OnEnable()
        {
            speedChanged.Raised += OnSpeedChanged;
            tripChanged.Raised += OnTripChanged;
            fearChanged.Raised += OnFearChanged;
            faultsChanged.Raised += OnFaultsChanged;
            repairTextChanged.Raised += OnRepairTextChanged;
        }

        private void OnDisable()
        {
            speedChanged.Raised -= OnSpeedChanged;
            tripChanged.Raised -= OnTripChanged;
            fearChanged.Raised -= OnFearChanged;
            faultsChanged.Raised -= OnFaultsChanged;
            repairTextChanged.Raised -= OnRepairTextChanged;
        }

        private void OnSpeedChanged(float normalized)
        {
            speed = normalized;
            RefreshMotion();
        }

        private void OnTripChanged(float normalized)
        {
            trip = normalized;
            RefreshMotion();
        }

        private void RefreshMotion() =>
            speedLabel.text = $"Швидкість: {Mathf.RoundToInt(speed * 100f)}%   Шлях: {Mathf.RoundToInt(trip * 100f)}%";

        private void OnFearChanged(float normalized) => fearLabel.text = $"Страх: {Mathf.RoundToInt(normalized * 100f)}%";

        private void OnRepairTextChanged(string text) => repairLabel.text = text;

        private void OnFaultsChanged()
        {
            IVehicleFaults faults = GameContext.Faults;
            string text = string.Empty;
            if (faults.IsActive(FaultType.PolesOff))
                text += "Штанги злетіли. ";
            if (faults.IsActive(FaultType.FuseBlown))
                text += "Запобіжник згорів. ";
            if (faults.IsActive(FaultType.WiresLoose))
                text += "Витік енергії. ";
            faultsLabel.text = text;
        }
    }
}
