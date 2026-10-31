using NightTrolleybus.Core;
using UnityEngine;

namespace NightTrolleybus.Items.Effects
{
    /// <summary>Відновлює силу полум'я Вогника.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Item Effects/Restore Flame", fileName = "RestoreFlame")]
    public class RestoreFlameEffect : ItemEffect
    {
        [SerializeField] private float amount = 10f;

        public override string Description => $"+{Mathf.RoundToInt(amount)} до сили Вогника";

        public override void Apply()
        {
            GameContext.Flame?.Restore(amount);
            GameContext.Audio?.Play(SoundId.FlameFed);
        }
    }
}
