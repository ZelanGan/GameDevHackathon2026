using System;
using NightTrolleybus.Items;
using NightTrolleybus.Passengers;
using UnityEngine;

namespace NightTrolleybus.Save
{
    /// <summary>Перелік даних, які можна відновити зі збереження за рядковим id.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Game Database", fileName = "GameDatabase")]
    public class GameDatabase : ScriptableObject
    {
        [SerializeField] private ItemData[] items;
        [SerializeField] private WeaponData[] weapons;
        [SerializeField] private PassengerData[] passengers;

        public ItemData FindItem(string id) => Array.Find(items, item => item.Id == id);

        public WeaponData FindWeapon(string id) => Array.Find(weapons, weapon => weapon.Id == id);

        public PassengerData FindPassenger(string assetName) => Array.Find(passengers, passenger => passenger.name == assetName);
    }
}
