using System;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Player
{
    /// <summary>Здоров'я гравця або ворога.</summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("Необов'язковий канал для інтерфейсу, значення 0..1")]
        [SerializeField] private FloatEventChannel healthChanged;

        public event Action Died;

        public float Current { get; private set; }

        private void Awake() => Restore(maxHealth);

        public void SetMax(float value)
        {
            maxHealth = value;
            Restore(value);
        }

        public void RestoreFull() => Restore(maxHealth);

        public void TakeDamage(float amount)
        {
            if (Current <= 0f)
                return;

            Restore(Current - amount);
            if (Current <= 0f)
                Died?.Invoke();
        }

        private void Restore(float value)
        {
            Current = Mathf.Clamp(value, 0f, maxHealth);
            if (healthChanged != null)
                healthChanged.Raise(Current / maxHealth);
        }
    }
}
