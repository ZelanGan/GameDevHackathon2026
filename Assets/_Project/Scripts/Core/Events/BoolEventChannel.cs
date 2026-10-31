using UnityEngine;

namespace NightTrolleybus.Core.Events
{
    /// <summary>Канал події з логічним значенням, наприклад «обігрівач працює».</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Channels/Bool", fileName = "BoolChannel")]
    public class BoolEventChannel : EventChannel<bool>
    {
    }
}
