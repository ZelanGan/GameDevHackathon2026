using System.Collections.Generic;
using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Flame
{
    /// <summary>Рахує страх Вогника із суми джерел. Страх наростає й спадає плавно.</summary>
    public class FearSystem : MonoBehaviour, IFear
    {
        [Tooltip("Наскільки змінюється страх за секунду")]
        [SerializeField] private float changePerSecond = 0.5f;
        [Tooltip("Страх у діапазоні 0..1")]
        [SerializeField] private FloatEventChannel fearChanged;

        private readonly Dictionary<object, float> sources = new Dictionary<object, float>();

        public float Normalized { get; private set; }

        private void Awake() => GameContext.Register(this);

        private void OnDestroy() => GameContext.Unregister(this);

        public void SetSource(object source, float amount) => sources[source] = amount;

        public void RemoveSource(object source) => sources.Remove(source);

        private void Update()
        {
            float target = 0f;
            foreach (float amount in sources.Values)
                target += amount;

            float fear = Mathf.MoveTowards(Normalized, Mathf.Clamp01(target), changePerSecond * Time.deltaTime);
            if (Mathf.Approximately(fear, Normalized))
                return;

            Normalized = fear;
            fearChanged.Raise(Normalized);
        }
    }
}
