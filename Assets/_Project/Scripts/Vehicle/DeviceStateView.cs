using NightTrolleybus.Core.Events;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NightTrolleybus.Vehicle
{
    public enum ViewedDevice
    {
        Heater,
        Doors
    }

    /// <summary>Показує стан пристрою малюнком і світлом. Сам пристрій живе в сцені Boot, тому поточний стан читаємо під час запуску.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DeviceStateView : MonoBehaviour
    {
        [SerializeField] private ViewedDevice device;
        [SerializeField] private BoolEventChannel stateChanged;
        [SerializeField] private Sprite offSprite;
        [Tooltip("Кілька кадрів дають живий рух, наприклад вогонь у печі")]
        [SerializeField] private Sprite[] onFrames;
        [SerializeField] private float framesPerSecond = 6f;
        [SerializeField] private Light2D stateLight;

        private SpriteRenderer spriteRenderer;
        private bool isOn;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            isOn = device == ViewedDevice.Heater
                ? FindAnyObjectByType<Heater>().IsRunning
                : FindAnyObjectByType<DoorsWindows>().IsOpen;
            Apply();
        }

        private void OnEnable() => stateChanged.Raised += OnStateChanged;

        private void OnDisable() => stateChanged.Raised -= OnStateChanged;

        private void Update()
        {
            if (isOn)
                spriteRenderer.sprite = onFrames[(int)(Time.time * framesPerSecond) % onFrames.Length];
        }

        private void OnStateChanged(bool value)
        {
            isOn = value;
            Apply();
        }

        private void Apply()
        {
            spriteRenderer.sprite = isOn ? onFrames[0] : offSprite;
            if (stateLight != null)
                stateLight.enabled = isOn;
        }
    }
}
