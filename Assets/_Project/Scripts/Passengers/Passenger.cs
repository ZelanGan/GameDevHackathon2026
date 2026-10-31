using NightTrolleybus.Core;
using NightTrolleybus.Items;
using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Passengers
{
    /// <summary>Пасажир на сидінні.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Passenger : MonoBehaviour, IInteractable
    {
        private PassengerRoster.Entry entry;
        private PassengerManager manager;

        public PassengerRoster.Entry Entry => entry;

        private ItemData PendingGift => entry.GiftTaken ? null : entry.Data.Gift;

        public string Prompt
        {
            get
            {
                if (PendingGift != null)
                    return $"{entry.Data.DisplayName}: взяти «{PendingGift.DisplayName}»";

                return manager.CanLeave
                    ? $"Висадити: {entry.Data.DisplayName}"
                    : $"{entry.Data.DisplayName}: висадити можна лише на зупинці";
            }
        }

        public void Initialize(PassengerRoster.Entry rosterEntry, PassengerManager passengerManager)
        {
            entry = rosterEntry;
            manager = passengerManager;

            if (entry.Data.Fear > 0f)
                GameContext.Fear?.SetSource(this, entry.Data.Fear);
        }

        public void Interact()
        {
            if (PendingGift != null)
            {
                if (PendingGift.Give())
                    entry.GiftTaken = true;
            }
            else if (manager.CanLeave)
            {
                manager.Remove(this);
            }
        }

        private void Update()
        {
            if (entry.Data.HeatStealPerSecond > 0f)
                GameContext.Temperature?.AddHeat(-entry.Data.HeatStealPerSecond * Time.deltaTime);
        }

        private void OnDestroy() => GameContext.Fear?.RemoveSource(this);
    }
}
