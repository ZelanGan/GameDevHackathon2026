using NightTrolleybus.Core.Events;
using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Stops
{
    /// <summary>Двері тролейбуса на зупинці: завершують зупинку й ведуть назад на мапу.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class StopExit : MonoBehaviour, IInteractable
    {
        [SerializeField] private VoidEventChannel stopCompleted;

        public string Prompt => "Повернутися в тролейбус";

        public void Interact() => stopCompleted.Raise();
    }
}
