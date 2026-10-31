using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>Місце ремонту. Доступне й підсвічене, лише поки є його несправність.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class RepairStation : MonoBehaviour, IInteractable
    {
        [SerializeField] private FaultType fault;
        [SerializeField] private string actionName;
        [SerializeField] private RepairHost host;
        [SerializeField] private VoidEventChannel faultsChanged;
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField] private Color normalColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color faultColor = new Color(1f, 0.25f, 0.15f);

        private Collider2D trigger;

        public string Prompt => actionName;

        private void Awake() => trigger = GetComponent<Collider2D>();

        private void OnEnable() => faultsChanged.Raised += Refresh;

        private void OnDisable() => faultsChanged.Raised -= Refresh;

        private void Start() => Refresh();

        public void Interact() => host.Open(fault);

        private void Refresh()
        {
            bool isActive = GameContext.Faults != null && GameContext.Faults.IsActive(fault);
            // Вимкнений колайдер не перехоплює підказку в сусідніх об'єктів.
            trigger.enabled = isActive;
            indicator.color = isActive ? faultColor : normalColor;
        }
    }
}
