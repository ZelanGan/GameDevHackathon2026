using System.Collections.Generic;
using UnityEngine;

namespace NightTrolleybus.Passengers
{
    /// <summary>Хто їде в салоні. Живе в Boot, бо сцена салону завантажується заново на кожен перегін.</summary>
    public class PassengerRoster : MonoBehaviour
    {
        public class Entry
        {
            public PassengerData Data;
            public int Seat;
            public bool GiftTaken;
        }

        public List<Entry> Entries { get; } = new List<Entry>();
    }
}
