using System;
using UnityEngine;

namespace NightTrolleybus.Core.Events
{
    /// <summary>Базовий канал події з параметром: системи спілкуються через асет, а не напряму.</summary>
    public abstract class EventChannel<T> : ScriptableObject
    {
        public event Action<T> Raised;

        public void Raise(T value) => Raised?.Invoke(value);
    }
}
