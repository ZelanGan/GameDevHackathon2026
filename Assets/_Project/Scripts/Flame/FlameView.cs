using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NightTrolleybus.Flame
{
    /// <summary>Показує стан Вогника через світло, колір і пульсацію. Стан гри не змінює.</summary>
    public class FlameView : MonoBehaviour
    {
        [SerializeField] private FlameConfig config;
        [SerializeField] private FlameStateEventChannel flameStateChanged;
        [SerializeField] private Light2D flameLight;
        [SerializeField] private SpriteRenderer flameSprite;
        [Tooltip("Швидкість переходу між станами")]
        [SerializeField] private float transitionSpeed = 3f;
        [SerializeField] private float pulseScaleAmount = 0.1f;
        [SerializeField] private VoidEventChannel flashTriggered;
        [Tooltip("У скільки разів яскравішає світло в момент Спалаху")]
        [SerializeField] private float flashBoost = 4f;
        [SerializeField] private float flashFadeSpeed = 4f;

        private float flashAmount;

        private FlameStateSettings target;
        private float radius;
        private float intensity;
        private Color color;
        private Vector3 baseScale;

        private void Awake() => baseScale = flameSprite.transform.localScale;

        private void OnEnable()
        {
            flameStateChanged.Raised += OnStateChanged;
            flashTriggered.Raised += OnFlash;
        }

        private void OnDisable()
        {
            flameStateChanged.Raised -= OnStateChanged;
            flashTriggered.Raised -= OnFlash;
        }

        private void OnFlash() => flashAmount = 1f;

        private void Start()
        {
            // Сцена може завантажитися пізніше за систему, тому беремо поточний стан напряму.
            FlameState state = GameContext.Flame != null ? GameContext.Flame.State : FlameState.Stable;
            target = config.GetSettings(state);
            radius = target.lightRadius;
            intensity = target.lightIntensity;
            color = target.color;
        }

        private void OnStateChanged(FlameState state) => target = config.GetSettings(state);

        private void Update()
        {
            float step = transitionSpeed * Time.deltaTime;
            radius = Mathf.Lerp(radius, target.lightRadius, step);
            intensity = Mathf.Lerp(intensity, target.lightIntensity, step);
            color = Color.Lerp(color, target.color, step);

            float noise = Mathf.PerlinNoise(Time.time * 8f, 0f) * 2f - 1f;
            flameLight.pointLightOuterRadius = radius * (1f + noise * target.flickerAmount);
            flameLight.pointLightInnerRadius = flameLight.pointLightOuterRadius * 0.2f;
            flashAmount = Mathf.MoveTowards(flashAmount, 0f, flashFadeSpeed * Time.deltaTime);
            flameLight.intensity = intensity * (1f + noise * target.flickerAmount) * Mathf.Lerp(1f, flashBoost, flashAmount);
            flameLight.color = color;

            flameSprite.color = color;
            float pulse = 1f + Mathf.Sin(Time.time * target.pulseSpeed) * pulseScaleAmount;
            // Коли радіус наближається до нуля, вогник плавно зникає.
            flameSprite.transform.localScale = baseScale * (pulse * Mathf.Clamp01(radius));
        }
    }
}
