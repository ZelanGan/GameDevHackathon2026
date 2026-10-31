using System;
using UnityEngine;

namespace NightTrolleybus.Core.Events
{
    /// <summary>Канал події без параметрів.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Channels/Void", fileName = "VoidChannel")]
    public class VoidEventChannel : ScriptableObject
    {
        public event Action Raised;

        public void Raise() => Raised?.Invoke();
    }
}
