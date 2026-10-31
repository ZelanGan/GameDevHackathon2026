using UnityEngine;

namespace NightTrolleybus.Items
{
    /// <summary>Поточна зброя. Живе в Boot, тому не зникає між зупинками.</summary>
    public class PlayerLoadout : MonoBehaviour
    {
        [SerializeField] private WeaponData startingWeapon;

        public WeaponData Weapon { get; private set; }

        private void Awake() => Weapon = startingWeapon;

        public void Equip(WeaponData weapon) => Weapon = weapon;
    }
}
