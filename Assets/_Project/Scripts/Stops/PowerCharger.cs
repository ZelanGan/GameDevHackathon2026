using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Stops
{
    /// <summary>Зарядна станція в депо: один раз за зупинку поповнює енергію тролейбуса.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class PowerCharger : MonoBehaviour, IInteractable
    {
        [SerializeField] private float amount = 40f;
        [SerializeField] private StringEventChannel noticeRaised;

        private bool used;

        public string Prompt => used ? "Зарядна станція порожня" : $"Зарядити тролейбус (+{Mathf.RoundToInt(amount)} енергії)";

        public void Interact()
        {
            if (used)
                return;

            used = true;
            GameContext.Power?.Restore(amount);
            noticeRaised.Raise("Тролейбус заряджено");
        }
    }
}
