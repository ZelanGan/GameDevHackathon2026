using UnityEngine;

namespace NightTrolleybus.Core.Events
{
    /// <summary>Канал події з текстом, наприклад підказка взаємодії.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Channels/String", fileName = "StringChannel")]
    public class StringEventChannel : EventChannel<string>
    {
    }
}
