using NightTrolleybus.Core;
using UnityEngine;

namespace NightTrolleybus.Items
{
    /// <summary>Ближня зброя гравця.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Weapon", fileName = "Weapon")]
    public class WeaponData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float cooldown = 0.4f;
        [SerializeField] private float range = 1.4f;

        public string Id => id;
        public string DisplayName => Loc.T(displayName);
        public float Damage => damage;
        public float Cooldown => cooldown;
        public float Range => range;
    }
}
