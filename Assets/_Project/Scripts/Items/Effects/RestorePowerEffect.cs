using NightTrolleybus.Core;
using UnityEngine;

namespace NightTrolleybus.Items.Effects
{
    /// <summary>Відновлює енергію тролейбуса.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Item Effects/Restore Power", fileName = "RestorePower")]
    public class RestorePowerEffect : ItemEffect
    {
        [SerializeField] private float amount = 25f;

        public override string Description => $"+{Mathf.RoundToInt(amount)} до енергії тролейбуса";

        public override void Apply() => GameContext.Power?.Restore(amount);
    }
}
