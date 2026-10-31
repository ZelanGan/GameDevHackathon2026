namespace NightTrolleybus.Core.StateMachine
{
    /// <summary>Стан гри: поїздка, зупинка, мапа, пауза, поразка або перемога.</summary>
    public interface IGameState
    {
        void Enter();

        void Exit();
    }
}
