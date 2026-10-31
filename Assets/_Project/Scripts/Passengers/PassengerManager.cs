using NightTrolleybus.Core.Events;
using NightTrolleybus.Route;
using NightTrolleybus.Vehicle;
using UnityEngine;

namespace NightTrolleybus.Passengers
{
    /// <summary>Посадка й висадка. Пасажир заходить лише на зупинці, коли тролейбус стоїть із відчиненими дверима.</summary>
    public class PassengerManager : MonoBehaviour
    {
        [SerializeField] private BusDriveController bus;
        [SerializeField] private TripController trip;
        [SerializeField] private PassengerData[] pool;
        [SerializeField] private Transform[] seats;
        [SerializeField] private float boardingDelay = 3f;
        [Tooltip("Шанс, що на цій зупинці взагалі хтось чекає")]
        [Range(0f, 1f)]
        [SerializeField] private float boardingChance = 0.6f;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Material material;
        [SerializeField] private StringEventChannel noticeRaised;

        private PassengerRoster roster;
        private DoorsWindows doors;
        private bool boardingFinished;
        private float boardingTimer;

        public bool CanLeave => bus.Speed <= 0f;

        private void Awake()
        {
            roster = FindAnyObjectByType<PassengerRoster>();
            doors = FindAnyObjectByType<DoorsWindows>();
        }

        private void Start()
        {
            foreach (PassengerRoster.Entry entry in roster.Entries)
                Spawn(entry);
        }

        private void Update()
        {
            if (boardingFinished || !trip.CanExit || !doors.IsOpen)
            {
                boardingTimer = 0f;
                return;
            }

            boardingTimer += Time.deltaTime;
            if (boardingTimer < boardingDelay)
                return;

            // На одній зупинці заходить не більше одного пасажира.
            boardingFinished = true;
            int freeSeat = FindFreeSeat();
            if (freeSeat >= 0 && Random.value <= boardingChance)
                Board(pool[Random.Range(0, pool.Length)], freeSeat);
        }

        private int FindFreeSeat()
        {
            for (int seat = 0; seat < seats.Length; seat++)
            {
                if (!roster.Entries.Exists(entry => entry.Seat == seat))
                    return seat;
            }

            return -1;
        }

        public void Board(PassengerData data, int seat)
        {
            var entry = new PassengerRoster.Entry { Data = data, Seat = seat };
            roster.Entries.Add(entry);
            Spawn(entry);
            noticeRaised.Raise($"Зайшов пасажир: {data.DisplayName}");
        }

        private void Spawn(PassengerRoster.Entry entry)
        {
            var passengerObject = new GameObject(entry.Data.DisplayName);
            passengerObject.transform.SetParent(seats[entry.Seat], false);
            passengerObject.transform.localPosition = new Vector3(0f, 0.3f, 0f);

            var spriteRenderer = passengerObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sharedMaterial = material;
            spriteRenderer.color = entry.Data.Color;
            spriteRenderer.sortingOrder = 2;
            var trigger = passengerObject.AddComponent<BoxCollider2D>();
            trigger.size = sprite.bounds.size;
            trigger.isTrigger = true;

            passengerObject.AddComponent<Passenger>().Initialize(entry, this);
        }

        public void Remove(Passenger passenger)
        {
            roster.Entries.Remove(passenger.Entry);
            Destroy(passenger.gameObject);
        }
    }
}
