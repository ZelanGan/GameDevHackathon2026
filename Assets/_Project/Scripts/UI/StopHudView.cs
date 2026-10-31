using NightTrolleybus.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Частина інтерфейсу для зупинки: здоров'я, темрява й страх.</summary>
    public class StopHudView : MonoBehaviour
    {
        [SerializeField] private FloatEventChannel healthChanged;
        [SerializeField] private FloatEventChannel darknessChanged;
        [SerializeField] private FloatEventChannel fearChanged;
        [SerializeField] private Text healthLabel;
        [SerializeField] private Text darknessLabel;

        private float fear;
        private float darkness;

        private void OnEnable()
        {
            healthChanged.Raised += OnHealthChanged;
            darknessChanged.Raised += OnDarknessChanged;
            fearChanged.Raised += OnFearChanged;
        }

        private void OnDisable()
        {
            healthChanged.Raised -= OnHealthChanged;
            darknessChanged.Raised -= OnDarknessChanged;
            fearChanged.Raised -= OnFearChanged;
        }

        private void OnHealthChanged(float normalized) => healthLabel.text = $"Здоров'я: {Mathf.RoundToInt(normalized * 100f)}%";

        private void OnDarknessChanged(float normalized)
        {
            darkness = normalized;
            RefreshDarkness();
        }

        private void OnFearChanged(float normalized)
        {
            fear = normalized;
            RefreshDarkness();
        }

        private void RefreshDarkness() =>
            darknessLabel.text = $"Темрява: {Mathf.RoundToInt(darkness * 100f)}%   Страх: {Mathf.RoundToInt(fear * 100f)}%";
    }
}
