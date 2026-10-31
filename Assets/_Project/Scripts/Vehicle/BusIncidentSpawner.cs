using System.Collections;
using System.Collections.Generic;
using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    public enum BusIncident
    {
        PolesOff,
        FuseBlown,
        WiresLoose,
        Debris,
        LineFlicker,
        DarknessKnock
    }

    /// <summary>Під час руху час від часу створює незначні перешкоди.</summary>
    public class BusIncidentSpawner : MonoBehaviour
    {
        [SerializeField] private BusDriveController bus;
        [SerializeField] private float minInterval = 25f;
        [SerializeField] private float maxInterval = 45f;
        [Tooltip("Нижче цієї швидкості 0..1 перешкоди не виникають")]
        [SerializeField] private float minSpeed = 0.2f;
        [SerializeField] private float debrisPowerLoss = 5f;
        [SerializeField] private float flickerPowerLoss = 3f;
        [SerializeField] private float knockFear = 0.4f;
        [SerializeField] private float knockDuration = 6f;
        [SerializeField] private float knockHeatLoss = 5f;
        [SerializeField] private StringEventChannel noticeRaised;

        private readonly List<object> activeKnocks = new List<object>();
        private float timer;

        private void Start() => ResetTimer();

        private void Update()
        {
            if (bus.Speed < minSpeed)
                return;

            timer -= Time.deltaTime;
            if (timer > 0f)
                return;

            ResetTimer();
            Trigger((BusIncident)Random.Range(0, System.Enum.GetValues(typeof(BusIncident)).Length));
        }

        public void Trigger(BusIncident incident)
        {
            switch (incident)
            {
                case BusIncident.PolesOff:
                    GameContext.Faults?.Trigger(FaultType.PolesOff);
                    noticeRaised.Raise("Штанги злетіли з дротів!");
                    break;
                case BusIncident.FuseBlown:
                    GameContext.Faults?.Trigger(FaultType.FuseBlown);
                    noticeRaised.Raise("Згорів запобіжник!");
                    break;
                case BusIncident.WiresLoose:
                    GameContext.Faults?.Trigger(FaultType.WiresLoose);
                    noticeRaised.Raise("Роз'єдналися проводи, енергія витікає!");
                    break;
                case BusIncident.Debris:
                    GameContext.Power?.TryConsume(debrisPowerLoss);
                    noticeRaised.Raise("Удар об сміття на дорозі!");
                    break;
                case BusIncident.LineFlicker:
                    GameContext.Power?.TryConsume(flickerPowerLoss);
                    noticeRaised.Raise("Лінія мерехтить, напруга просіла.");
                    break;
                case BusIncident.DarknessKnock:
                    GameContext.Temperature?.AddHeat(-knockHeatLoss);
                    StartCoroutine(KnockFear());
                    noticeRaised.Raise("Темрява стукає у вікна...");
                    break;
            }
        }

        private IEnumerator KnockFear()
        {
            // Кожен стук є окремим джерелом, тому кілька стуків підряд посилюють страх.
            var source = new object();
            activeKnocks.Add(source);
            GameContext.Fear?.SetSource(source, knockFear);
            yield return new WaitForSeconds(knockDuration);
            activeKnocks.Remove(source);
            GameContext.Fear?.RemoveSource(source);
        }

        // Страх не має пережити сцену: корутина зупиняється разом з нею й сама джерело не прибере.
        private void OnDestroy()
        {
            foreach (object source in activeKnocks)
                GameContext.Fear?.RemoveSource(source);
        }

        private void ResetTimer() => timer = Random.Range(minInterval, maxInterval);
    }
}
