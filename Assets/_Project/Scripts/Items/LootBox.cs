using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Items
{
    /// <summary>Ящик, що відкривається один раз і дає випадковий предмет. За повного інвентаря лишається зачиненим.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class LootBox : MonoBehaviour, IInteractable
    {
        private ItemData[] loot;
        private Sprite openSprite;
        private StringEventChannel noticeRaised;
        private bool isOpen;

        public string Prompt => "Відкрити ящик";

        public bool CanInteract => !isOpen;

        public void Initialize(ItemData[] items, Sprite opened, StringEventChannel notice)
        {
            loot = items;
            openSprite = opened;
            noticeRaised = notice;
        }

        public void Interact()
        {
            if (isOpen)
                return;

            // Порожня клітинка в таблиці лута означає порожній ящик.
            ItemData item = loot[Random.Range(0, loot.Length)];
            if (item != null && !item.Give())
                return;

            isOpen = true;
            GetComponent<SpriteRenderer>().sprite = openSprite;
            GameContext.Audio?.Play(item != null ? SoundId.Pickup : SoundId.Lock);
            if (item == null)
                noticeRaised.Raise(Loc.T("Порожньо"));
        }
    }
}
