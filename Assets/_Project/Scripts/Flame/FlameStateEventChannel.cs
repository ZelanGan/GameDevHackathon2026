using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Flame
{
    /// <summary>Канал зміни стану Вогника.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Channels/Flame State", fileName = "FlameStateChannel")]
    public class FlameStateEventChannel : EventChannel<FlameState>
    {
    }
}
