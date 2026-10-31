namespace NightTrolleybus.Core.StateMachine
{
    /// <summary>Перемикає стани гри. Пауза лягає поверх поточного стану, не завершуючи його.</summary>
    public class GameStateMachine
    {
        private IGameState pausedState;

        public IGameState Current { get; private set; }
        public bool IsPaused => pausedState != null;

        public void Change(IGameState next)
        {
            Current?.Exit();
            pausedState = null;
            Current = next;
            Current.Enter();
        }

        public void Pause(IGameState pause)
        {
            if (IsPaused)
                return;

            pausedState = Current;
            Current = pause;
            Current.Enter();
        }

        public void Resume()
        {
            if (!IsPaused)
                return;

            Current.Exit();
            Current = pausedState;
            pausedState = null;
        }
    }
}
