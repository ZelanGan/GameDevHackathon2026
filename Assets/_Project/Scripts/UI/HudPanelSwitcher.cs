using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.UI
{
    /// <summary>Показує частину інтерфейсу, що відповідає поточному стану гри.</summary>
    public class HudPanelSwitcher : MonoBehaviour
    {
        [SerializeField] private StringEventChannel gameStateChanged;
        [SerializeField] private GameObject ridePanel;
        [SerializeField] private GameObject stopPanel;
        [SerializeField] private GameObject inventoryPanel;
        [Tooltip("Шкали й стан Вогника: на мапі вони лише заважають")]
        [SerializeField] private GameObject[] hiddenOnMap;

        private void OnEnable() => gameStateChanged.Raised += OnStateChanged;

        private void OnDisable() => gameStateChanged.Raised -= OnStateChanged;

        private void OnStateChanged(string state)
        {
            ridePanel.SetActive(state == "Ride");
            stopPanel.SetActive(state == "Stop");
            inventoryPanel.SetActive(state == "Ride" || state == "Stop");
            foreach (GameObject element in hiddenOnMap)
                element.SetActive(state != "Map");
        }
    }
}
