using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Route
{
    /// <summary>Вихід із салону. Двері видно й можна використати, лише коли тролейбус стоїть на зупинці.</summary>
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class BusExit : MonoBehaviour, IInteractable
    {
        [SerializeField] private TripController trip;

        private Collider2D trigger;
        private SpriteRenderer doorSprite;

        public string Prompt => "Вийти на зупинку";

        private void Awake()
        {
            trigger = GetComponent<Collider2D>();
            doorSprite = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            bool canExit = trip.CanExit;
            trigger.enabled = canExit;
            doorSprite.enabled = canExit;
        }

        public void Interact() => trip.Exit();
    }
}
