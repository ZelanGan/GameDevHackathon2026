using System;
using UnityEngine;

namespace NightTrolleybus.Flame
{
    /// <summary>Параметри одного стану Вогника.</summary>
    [Serializable]
    public struct FlameStateSettings
    {
        public FlameState state;
        [Tooltip("Мінімальна сила полум'я для цього стану")]
        public float minFlame;
        public string displayName;
        public float lightRadius;
        public float lightIntensity;
        public Color color;
        [Range(0f, 1f)] public float vignette;
        [Tooltip("Швидкість пульсації вогника")]
        public float pulseSpeed;
        [Tooltip("Сила мерехтіння світла")]
        public float flickerAmount;
    }

    /// <summary>Налаштування шкали полум'я. Під час гри цей асет не змінюється.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Config/Flame", fileName = "FlameConfig")]
    public class FlameConfig : ScriptableObject
    {
        [SerializeField] private float maxFlame = 100f;
        [SerializeField] private float startFlame = 85f;
        [Tooltip("Скільки полум'я втрачається за секунду без зовнішніх чинників")]
        [SerializeField] private float baseDrainPerSecond = 0.2f;
        [Tooltip("Запас для переходу в сильніший стан, щоб стан не мерехтів на межі порога")]
        [SerializeField] private float stateHysteresis = 2f;
        [Tooltip("Температура 0..1, нижче якої холод прискорює згасання")]
        [SerializeField] private float coldThreshold = 0.5f;
        [Tooltip("У скільки разів швидше згасає Вогник за нульової температури")]
        [SerializeField] private float maxColdDrainMultiplier = 2f;
        [Tooltip("У скільки разів швидше згасає Вогник за максимального страху")]
        [SerializeField] private float maxFearDrainMultiplier = 1.75f;
        [Tooltip("Від найсильнішого стану до найслабшого")]
        [SerializeField] private FlameStateSettings[] states =
        {
            new FlameStateSettings { state = FlameState.Bright, minFlame = 75f, displayName = "Яскравий", lightRadius = 8f, lightIntensity = 1.2f, color = new Color(1f, 0.78f, 0.4f), vignette = 0.2f, pulseSpeed = 1.5f, flickerAmount = 0.03f },
            new FlameStateSettings { state = FlameState.Stable, minFlame = 45f, displayName = "Стабільний", lightRadius = 6f, lightIntensity = 1f, color = new Color(1f, 0.6f, 0.25f), vignette = 0.3f, pulseSpeed = 2.5f, flickerAmount = 0.05f },
            new FlameStateSettings { state = FlameState.Weak, minFlame = 20f, displayName = "Слабкий", lightRadius = 4f, lightIntensity = 0.8f, color = new Color(1f, 0.4f, 0.15f), vignette = 0.45f, pulseSpeed = 4f, flickerAmount = 0.08f },
            new FlameStateSettings { state = FlameState.Critical, minFlame = 0f, displayName = "Критичний", lightRadius = 2.2f, lightIntensity = 0.55f, color = new Color(0.8f, 0.2f, 0.1f), vignette = 0.6f, pulseSpeed = 7f, flickerAmount = 0.15f },
            new FlameStateSettings { state = FlameState.Extinguished, minFlame = 0f, displayName = "Згас", lightRadius = 0f, lightIntensity = 0f, color = Color.black, vignette = 0.8f, pulseSpeed = 0f, flickerAmount = 0f }
        };

        public float MaxFlame => maxFlame;
        public float StartFlame => startFlame;
        public float BaseDrainPerSecond => baseDrainPerSecond;

        /// <summary>Множник згасання від холоду: 1 у теплі й більше, коли температура нижча за поріг.</summary>
        public float GetColdDrainMultiplier(float normalizedTemperature)
        {
            return Mathf.Lerp(maxColdDrainMultiplier, 1f, Mathf.InverseLerp(0f, coldThreshold, normalizedTemperature));
        }

        public float GetFearDrainMultiplier(float normalizedFear) => Mathf.Lerp(1f, maxFearDrainMultiplier, normalizedFear);

        public FlameStateSettings GetSettings(FlameState state)
        {
            foreach (var settings in states)
            {
                if (settings.state == state)
                    return settings;
            }

            throw new ArgumentOutOfRangeException(nameof(state), state, "У FlameConfig немає налаштувань для цього стану");
        }

        /// <summary>Визначає стан за силою полум'я з урахуванням поточного стану.</summary>
        public FlameState EvaluateState(float flame, FlameState current)
        {
            if (flame <= 0f)
                return FlameState.Extinguished;

            foreach (var settings in states)
            {
                if (settings.state == FlameState.Extinguished)
                    continue;

                float threshold = settings.minFlame + (settings.state > current ? stateHysteresis : 0f);
                if (flame >= threshold)
                    return settings.state;
            }

            return FlameState.Critical;
        }
    }
}
