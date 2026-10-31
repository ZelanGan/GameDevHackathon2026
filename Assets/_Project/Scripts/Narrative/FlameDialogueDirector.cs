using System.Collections.Generic;
using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Flame;
using NightTrolleybus.Route;
using UnityEngine;

namespace NightTrolleybus.Narrative
{
    /// <summary>Перетворює події гри на репліки Вогника й стежить, щоб він не говорив надто часто.</summary>
    public class FlameDialogueDirector : MonoBehaviour
    {
        [SerializeField] private DialogueLine[] lines;
        [SerializeField] private RouteManager route;
        [Tooltip("Найменша пауза між репліками в секундах")]
        [SerializeField] private float minInterval = 8f;
        [SerializeField] private float coldThreshold = 0.25f;
        [SerializeField] private float fearThreshold = 0.4f;

        [Header("Канали подій")]
        [SerializeField] private StringEventChannel bubbleRaised;
        [SerializeField] private FlameStateEventChannel flameStateChanged;
        [SerializeField] private VoidEventChannel fuelReceived;
        [SerializeField] private FloatEventChannel temperatureChanged;
        [SerializeField] private FloatEventChannel fearChanged;
        [SerializeField] private VoidEventChannel flashTriggered;
        [SerializeField] private VoidEventChannel powerDepleted;
        [SerializeField] private VoidEventChannel faultsChanged;
        [SerializeField] private BoolEventChannel inBusChanged;
        [SerializeField] private StringEventChannel gameStateChanged;

        private readonly HashSet<DialogueLine> spoken = new HashSet<DialogueLine>();
        private DialogueLine lastLine;
        private float nextAllowedTime;
        private bool inBus;
        private bool wasCold;
        private bool wasFrightened;

        private void OnEnable()
        {
            flameStateChanged.Raised += OnFlameStateChanged;
            fuelReceived.Raised += OnFuelReceived;
            temperatureChanged.Raised += OnTemperatureChanged;
            fearChanged.Raised += OnFearChanged;
            flashTriggered.Raised += OnFlash;
            powerDepleted.Raised += OnPowerDepleted;
            faultsChanged.Raised += OnFaultsChanged;
            inBusChanged.Raised += OnInBusChanged;
            gameStateChanged.Raised += OnGameStateChanged;
            route.NodeSelected += OnNodeSelected;
        }

        private void OnDisable()
        {
            flameStateChanged.Raised -= OnFlameStateChanged;
            fuelReceived.Raised -= OnFuelReceived;
            temperatureChanged.Raised -= OnTemperatureChanged;
            fearChanged.Raised -= OnFearChanged;
            flashTriggered.Raised -= OnFlash;
            powerDepleted.Raised -= OnPowerDepleted;
            faultsChanged.Raised -= OnFaultsChanged;
            inBusChanged.Raised -= OnInBusChanged;
            gameStateChanged.Raised -= OnGameStateChanged;
            route.NodeSelected -= OnNodeSelected;
        }

        private void OnFlameStateChanged(FlameState state)
        {
            switch (state)
            {
                case FlameState.Bright:
                    Say(DialogueTrigger.BecameBright);
                    break;
                case FlameState.Weak:
                    Say(DialogueTrigger.BecameWeak);
                    break;
                case FlameState.Critical:
                    // Критичний стан важливіший за паузу між репліками.
                    Say(DialogueTrigger.BecameCritical, true);
                    break;
            }
        }

        private void OnFuelReceived() => Say(DialogueTrigger.FuelReceived, true);

        private void OnTemperatureChanged(float normalized)
        {
            bool isCold = normalized < coldThreshold;
            if (isCold && !wasCold && inBus)
                Say(DialogueTrigger.ColdInCabin);
            wasCold = isCold;
        }

        private void OnFearChanged(float normalized)
        {
            bool isFrightened = normalized >= fearThreshold;
            if (isFrightened && !wasFrightened)
                Say(DialogueTrigger.Frightened);
            wasFrightened = isFrightened;
        }

        private void OnFlash() => Say(DialogueTrigger.Flash);

        private void OnPowerDepleted() => Say(DialogueTrigger.PowerOut, true);

        private void OnFaultsChanged() => Say(DialogueTrigger.BusBroken);

        private void OnInBusChanged(bool value) => inBus = value;

        private void OnGameStateChanged(string state)
        {
            if (state == "Stop")
                Say(DialogueTrigger.LeftBus, true);
        }

        private void OnNodeSelected(RouteNode node)
        {
            switch (node.Type)
            {
                case NodeType.Depot:
                    Say(DialogueTrigger.RouteDepot, true);
                    break;
                case NodeType.Supermarket:
                    Say(DialogueTrigger.RouteSupermarket, true);
                    break;
                case NodeType.GunShop:
                    Say(DialogueTrigger.RouteGunShop, true);
                    break;
                case NodeType.Event:
                    Say(DialogueTrigger.RouteEvent, true);
                    break;
                case NodeType.EmptyRoad:
                    Say(DialogueTrigger.RouteEmptyRoad, true);
                    break;
                default:
                    Say(DialogueTrigger.RouteFinal, true);
                    break;
            }
        }

        private void Say(DialogueTrigger trigger, bool ignoreInterval = false)
        {
            if (!ignoreInterval && Time.unscaledTime < nextAllowedTime)
                return;

            DialogueLine line = PickLine(trigger);
            if (line == null)
                return;

            lastLine = line;
            spoken.Add(line);
            nextAllowedTime = Time.unscaledTime + minInterval;
            bubbleRaised.Raise(line.Text);
        }

        private DialogueLine PickLine(DialogueTrigger trigger)
        {
            float flame = GameContext.Flame != null ? GameContext.Flame.Current : 0f;
            var candidates = new List<DialogueLine>();
            foreach (DialogueLine line in lines)
            {
                if (line.Trigger == trigger && line.Matches(flame) && !(line.OnlyOnce && spoken.Contains(line)))
                    candidates.Add(line);
            }

            // Не повторюємо ту саму репліку двічі поспіль, якщо є інша.
            if (candidates.Count > 1)
                candidates.Remove(lastLine);

            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
        }
    }
}
