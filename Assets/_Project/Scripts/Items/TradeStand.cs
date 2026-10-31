using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Items
{
    /// <summary>Місце обміну в магазині: предмети-валюта на зброю або на інший предмет.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class TradeStand : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemData costItem;
        [SerializeField] private int costCount = 1;
        [Tooltip("Заповнюється або зброя, або предмет")]
        [SerializeField] private WeaponData rewardWeapon;
        [SerializeField] private ItemData rewardItem;
        [SerializeField] private StringEventChannel noticeRaised;

        private string RewardName => rewardWeapon != null ? rewardWeapon.DisplayName : rewardItem.DisplayName;

        public string Prompt => Loc.F("{0} за {1} × {2}", RewardName, costCount, costItem.DisplayName);

        public string Label => Prompt;

        public void Interact()
        {
            if (GameContext.Inventory.Count(costItem) < costCount)
            {
                noticeRaised.Raise(Loc.F("Бракує: {0}", costItem.DisplayName));
                return;
            }

            if (rewardWeapon != null)
            {
                GameContext.Inventory.TryRemove(costItem, costCount);
                FindAnyObjectByType<PlayerLoadout>().Equip(rewardWeapon);
                GameContext.Audio?.Play(SoundId.Lock);
                noticeRaised.Raise(Loc.F("Нова зброя: {0}", rewardWeapon.DisplayName));
                return;
            }

            // Спершу забираємо плату, щоб місце звільнилося під нагороду.
            GameContext.Inventory.TryRemove(costItem, costCount);
            if (GameContext.Inventory.TryAdd(rewardItem))
            {
                GameContext.Audio?.Play(SoundId.Pickup);
            }
            else
            {
                for (int i = 0; i < costCount; i++)
                    GameContext.Inventory.TryAdd(costItem);
            }
        }
    }
}
