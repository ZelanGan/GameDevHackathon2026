using NightTrolleybus.Core.Events;
using NightTrolleybus.Flame;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Шкали полум'я, температури й енергії з підписами та назва стану. Лише слухає події.</summary>
    public class HudView : MonoBehaviour
    {
        private const string FlameTitle = "Вогник";
        private const string TemperatureTitle = "Тепло в салоні";
        private const string PowerTitle = "Енергія тролейбуса";

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

        private Text flameBarLabel;
        private Text temperatureBarLabel;
        private Text powerBarLabel;

        private void Awake()
        {
            flameBarLabel = CreateBarLabel(flameFill);
            temperatureBarLabel = CreateBarLabel(temperatureFill);
            powerBarLabel = CreateBarLabel(powerFill);
        }

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

        private void OnFlameChanged(float normalized) => SetBar(flameFill, flameBarLabel, FlameTitle, normalized);

        private void OnStateChanged(FlameState state)
        {
            FlameStateSettings settings = config.GetSettings(state);
            flameStateLabel.text = $"Стан Вогника: {settings.displayName}";
            flameFill.color = settings.color;
        }

        private void OnTemperatureChanged(float normalized) => SetBar(temperatureFill, temperatureBarLabel, TemperatureTitle, normalized);

        private void OnPowerChanged(float normalized) => SetBar(powerFill, powerBarLabel, PowerTitle, normalized);

        private static void SetBar(Image fill, Text label, string title, float normalized)
        {
            fill.fillAmount = normalized;
            label.text = $"{title}: {Mathf.RoundToInt(normalized * 100f)}%";
        }

        // Підпис лежить поверх шкали, а контур лишає його читабельним і на кольорі, і на темному тлі.
        private static Text CreateBarLabel(Image fill)
        {
            var label = new GameObject("BarLabel").AddComponent<Text>();
            label.transform.SetParent(fill.transform.parent, false);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.raycastTarget = false;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);

            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return label;
        }
    }
}
