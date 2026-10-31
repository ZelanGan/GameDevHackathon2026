using System;
using NightTrolleybus.Core.StateMachine;
using NightTrolleybus.Narrative;
using UnityEngine;

namespace NightTrolleybus.Core
{
    /// <summary>Стан, що показує одну сцену змісту й задає плин часу.</summary>
    public class SceneState : IGameState
    {
        private readonly GameManager manager;
        private readonly string sceneName;
        private readonly string stateName;
        private readonly float timeScale;
        private readonly bool inBus;

        public SceneState(GameManager manager, string stateName, string sceneName, float timeScale, bool inBus)
        {
            this.manager = manager;
            this.stateName = stateName;
            this.sceneName = sceneName;
            this.timeScale = timeScale;
            this.inBus = inBus;
        }

        public void Enter()
        {
            Time.timeScale = timeScale;
            manager.EnterScene(stateName, sceneName, inBus);
        }

        public void Exit()
        {
        }
    }

    /// <summary>Пауза: час стоїть, стан під нею лишається незавершеним.</summary>
    public class PauseState : IGameState
    {
        private readonly GameManager manager;
        private float previousTimeScale;

        public PauseState(GameManager manager) => this.manager = manager;

        public void Enter()
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            manager.SetPauseVisible(true);
        }

        public void Exit()
        {
            Time.timeScale = previousTimeScale;
            manager.SetPauseVisible(false);
        }
    }

    /// <summary>Текстова подія: час стоїть, суцільна панель закриває все під нею.</summary>
    public class EventState : IGameState
    {
        private readonly GameManager manager;
        private readonly Action<RoadEventRunner, Action> run;

        public EventState(GameManager manager, Action<RoadEventRunner, Action> run)
        {
            this.manager = manager;
            this.run = run;
        }

        public void Enter()
        {
            Time.timeScale = 0f;
            manager.RunEvent(run);
        }

        public void Exit() => manager.CloseEvent();
    }

    /// <summary>Кінець партії поразкою. Перемога завершується катсценою й поверненням у меню.</summary>
    public class EndState : IGameState
    {
        private readonly GameManager manager;
        private readonly string message;

        public EndState(GameManager manager, string message)
        {
            this.manager = manager;
            this.message = message;
        }

        public void Enter()
        {
            Time.timeScale = 0f;
            manager.ShowEnd(message);
        }

        public void Exit()
        {
        }
    }
}
