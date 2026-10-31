using System;
using System.IO;
using System.Linq;
using NightTrolleybus.Core;
using NightTrolleybus.Items;
using NightTrolleybus.Passengers;
using NightTrolleybus.Route;
using NightTrolleybus.Vehicle;
using Newtonsoft.Json;
using UnityEngine;

namespace NightTrolleybus.Save
{
    /// <summary>Зберігає прогрес у JSON між зупинками.</summary>
    public class SaveSystem : MonoBehaviour
    {
        private const string FileName = "save.json";

        [SerializeField] private GameDatabase database;
        [SerializeField] private RouteManager route;
        [SerializeField] private Inventory inventory;
        [SerializeField] private PlayerLoadout loadout;
        [SerializeField] private PassengerRoster roster;
        [SerializeField] private Heater heater;
        [SerializeField] private CabinLight cabinLight;
        [SerializeField] private DoorsWindows doors;

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public void Save()
        {
            var data = new SaveData
            {
                Seed = route.Seed,
                CurrentNodeId = route.Current != null ? route.Current.Id : -1,
                VisitedNodeIds = route.Visited.ToList(),
                Flame = GameContext.Flame.Current,
                Temperature = GameContext.Temperature.Current,
                Power = GameContext.Power.Current,
                HeaterOn = heater.IsOn,
                CabinLightOn = cabinLight.IsOn,
                DoorsOpen = doors.IsOpen,
                ActiveFaults = Enum.GetValues(typeof(FaultType)).Cast<FaultType>()
                    .Where(GameContext.Faults.IsActive).Select(fault => (int)fault).ToList(),
                ItemIds = inventory.Items.Select(item => item.Id).ToList(),
                WeaponId = loadout.Weapon.Id,
                Passengers = roster.Entries.Select(entry => new PassengerSave
                {
                    AssetName = entry.Data.name,
                    Seat = entry.Seat,
                    GiftTaken = entry.GiftTaken
                }).ToList()
            };

            File.WriteAllText(SavePath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        /// <summary>Повертає false, якщо збереження немає або його не вдалося прочитати.</summary>
        public bool TryLoad()
        {
            if (!File.Exists(SavePath))
                return false;

            SaveData data;
            try
            {
                data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(SavePath));
            }
            catch (Exception exception) when (exception is JsonException || exception is IOException)
            {
                Debug.LogWarning($"Не вдалося прочитати збереження, починаємо нову гру: {exception.Message}");
                return false;
            }

            if (data == null)
                return false;

            route.Restore(data.Seed, data.VisitedNodeIds, data.CurrentNodeId);
            GameContext.Flame.SetCurrent(data.Flame);
            GameContext.Temperature.SetCurrent(data.Temperature);
            GameContext.Power.SetCurrent(data.Power);
            heater.SetOn(data.HeaterOn);
            cabinLight.SetOn(data.CabinLightOn);
            doors.SetOpen(data.DoorsOpen);
            foreach (int fault in data.ActiveFaults)
                GameContext.Faults.Trigger((FaultType)fault);

            inventory.SetItems(data.ItemIds.Select(database.FindItem).Where(item => item != null));
            WeaponData weapon = database.FindWeapon(data.WeaponId);
            if (weapon != null)
                loadout.Equip(weapon);

            roster.Entries.Clear();
            foreach (PassengerSave saved in data.Passengers)
            {
                PassengerData passenger = database.FindPassenger(saved.AssetName);
                if (passenger != null)
                    roster.Entries.Add(new PassengerRoster.Entry { Data = passenger, Seat = saved.Seat, GiftTaken = saved.GiftTaken });
            }

            return true;
        }

        public void Delete()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
    }
}
