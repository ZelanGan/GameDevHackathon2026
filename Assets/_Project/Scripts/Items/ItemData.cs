using NightTrolleybus.Core;
using NightTrolleybus.Items.Effects;
using UnityEngine;

namespace NightTrolleybus.Items
{
    /// <summary>Опис предмета. Рядковий id потрібен для збережень.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Item", fileName = "Item")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemEffect[] effects;

        public bool IsUsable => effects != null && effects.Length > 0;
        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;

        /// <summary>Передає предмет гравцю. Без інвентаря в сцені предмет спрацьовує одразу.</summary>
        public bool Give()
        {
            if (GameContext.Inventory != null)
                return GameContext.Inventory.TryAdd(this);

            Use();
            return true;
        }

        public void Use()
        {
            foreach (var effect in effects)
                effect.Apply();
        }
    }
}
