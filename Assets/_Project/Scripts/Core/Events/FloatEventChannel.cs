using UnityEngine;

namespace NightTrolleybus.Core.Events
{
    /// <summary>Канал події з дробовим значенням, наприклад шкала 0..1.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Channels/Float", fileName = "FloatChannel")]
    public class FloatEventChannel : EventChannel<float>
    {
    }
}
