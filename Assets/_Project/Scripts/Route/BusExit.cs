using NightTrolleybus.Core;
using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Route
{
    /// <summary>Вихід через двері салону. Вийти можна, лише коли тролейбус стоїть на зупинці.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class BusExit : MonoBehaviour, IInteractable
    {
        [SerializeField] private TripController trip;

        private Collider2D trigger;

        public string Prompt => "Вийти на зупинку";

        private void Awake() => trigger = GetComponent<Collider2D>();

        private void Update() => trigger.enabled = trip.CanExit;

        public void Interact()
        {
            GameContext.Audio?.Play(SoundId.DoorOpen);
            trip.Exit();
        }
    }
}
