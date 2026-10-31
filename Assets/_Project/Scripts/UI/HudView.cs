using NightTrolleybus.Core.Events;
using NightTrolleybus.Flame;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Шкали полум'я, температури й енергії та назва стану. Лише слухає події.</summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] private FlameConfig config;

        [Header("Канали подій")]
        [SerializeField] private FloatEventChannel flameChanged;
        [SerializeField] private FlameStateEventChannel flameStateChanged;
        [SerializeField] private FloatEventChannel temperatureChanged;
        [SerializeField] private FloatEventChannel powerChanged;

        [Header("Елементи")]
        [SerializeField] private Image flameFill;
        [SerializeField] private Image temperatureFill;
        [SerializeField] private Image powerFill;
        [SerializeField] private Text flameStateLabel;

        private void OnEnable()
        {
            flameChanged.Raised += OnFlameChanged;
            flameStateChanged.Raised += OnStateChanged;
            temperatureChanged.Raised += OnTemperatureChanged;
            powerChanged.Raised += OnPowerChanged;
        }

        private void OnDisable()
        {
            flameChanged.Raised -= OnFlameChanged;
            flameStateChanged.Raised -= OnStateChanged;
            temperatureChanged.Raised -= OnTemperatureChanged;
            powerChanged.Raised -= OnPowerChanged;
        }

        private void OnFlameChanged(float normalized) => flameFill.fillAmount = normalized;

        private void OnStateChanged(FlameState state)
        {
            FlameStateSettings settings = config.GetSettings(state);
            flameStateLabel.text = settings.displayName;
            flameFill.color = settings.color;
        }

        private void OnTemperatureChanged(float normalized) => temperatureFill.fillAmount = normalized;

        private void OnPowerChanged(float normalized) => powerFill.fillAmount = normalized;
    }
}
