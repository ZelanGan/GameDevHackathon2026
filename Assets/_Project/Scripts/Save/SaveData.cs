using System.Collections.Generic;

namespace NightTrolleybus.Save
{
    /// <summary>Вміст файлу збереження. Посилання на асети зберігаються рядковими id.</summary>
    public class SaveData
    {
        public int Seed;
        public int CurrentNodeId = -1;
        public List<int> VisitedNodeIds = new List<int>();

        public float Flame;
        public float Temperature;
        public float Power;

        public bool HeaterOn;
        public bool CabinLightOn;
        public bool DoorsOpen;
        public List<int> ActiveFaults = new List<int>();

        public List<string> ItemIds = new List<string>();
        public string WeaponId;
        public List<PassengerSave> Passengers = new List<PassengerSave>();
    }

    public class PassengerSave
    {
        public string AssetName;
        public int Seat;
        public bool GiftTaken;
    }
}
