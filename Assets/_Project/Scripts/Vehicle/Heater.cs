using NightTrolleybus.Core;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Обігрівач салону: перетворює енергію на тепло.</summary>
    public class Heater : PowerConsumer
    {
        [SerializeField] private float heatPerSecond = 2f;

        protected override void Tick(float deltaTime) => GameContext.Temperature?.AddHeat(heatPerSecond * deltaTime);
    }
}
