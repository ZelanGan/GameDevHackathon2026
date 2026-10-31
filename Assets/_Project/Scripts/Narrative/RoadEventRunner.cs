using System;
using System.Collections.Generic;
using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.UI;
using UnityEngine;

namespace NightTrolleybus.Narrative
{
    /// <summary>Веде подію: показує вибір, застосовує наслідки, не повторює події, поки не пройдено всі.</summary>
    public class RoadEventRunner : MonoBehaviour
    {
        [SerializeField] private EventDialogView view;
        [SerializeField] private RoadEventData introEvent;
        [SerializeField] private RoadEventData[] events;
        [SerializeField] private StringEventChannel objectiveChanged;

        private readonly List<RoadEventData> unused = new List<RoadEventData>();
        private Action onFinished;

        public void RunIntro(Action finished) => Run(introEvent, finished);

        public void RunRandom(Action finished)
        {
            if (unused.Count == 0)
                unused.AddRange(events);

            int index = UnityEngine.Random.Range(0, unused.Count);
            RoadEventData roadEvent = unused[index];
            unused.RemoveAt(index);
            Run(roadEvent, finished);
        }

        public void Close() => view.Hide();

        private void Run(RoadEventData roadEvent, Action finished)
        {
            onFinished = finished;
            objectiveChanged.Raise(Loc.T("Оберіть, як вчинити"));
            view.ShowEvent(roadEvent, CanChoose, OnChoice);
        }

        private static bool CanChoose(RoadEventChoice choice) =>
            choice.requiredItem == null || GameContext.Inventory.Count(choice.requiredItem) > 0;

        private void OnChoice(RoadEventChoice choice)
        {
            Apply(choice);
            // Наслідок міг загасити Вогника, тоді гра вже закрила діалог і показала кінець.
            if (GameContext.Flame.State != Flame.FlameState.Extinguished)
                view.ShowOutcome(choice.outcome, OnOutcomeClosed);
        }

        private void OnOutcomeClosed()
        {
            view.Hide();
            onFinished?.Invoke();
        }

        private static void Apply(RoadEventChoice choice)
        {
            if (choice.requiredItem != null)
                GameContext.Inventory.TryRemove(choice.requiredItem, 1);
            if (choice.rewardItem != null)
                choice.rewardItem.Give();

            if (choice.flameChange > 0f)
                GameContext.Flame.Restore(choice.flameChange);
            else if (choice.flameChange < 0f)
                GameContext.Flame.Drain(-choice.flameChange);

            if (choice.temperatureChange != 0f)
                GameContext.Temperature.AddHeat(choice.temperatureChange);

            if (choice.powerChange > 0f)
                GameContext.Power.Restore(choice.powerChange);
            else if (choice.powerChange < 0f)
                GameContext.Power.TryConsume(-choice.powerChange);
        }
    }
}
