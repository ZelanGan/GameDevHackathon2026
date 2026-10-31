using NightTrolleybus.Core.Events;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Світильник у сцені салону. Показує стан пристрою, який живе в сцені Boot.</summary>
    [RequireComponent(typeof(Light2D))]
    public class CabinLightView : MonoBehaviour
    {
        [SerializeField] private BoolEventChannel runningChanged;

        private Light2D cabinLight;

        private void Awake() => cabinLight = GetComponent<Light2D>();

        private void OnEnable() => runningChanged.Raised += OnRunningChanged;

        private void OnDisable() => runningChanged.Raised -= OnRunningChanged;

        private void Start() => cabinLight.enabled = FindAnyObjectByType<CabinLight>().IsRunning;

        private void OnRunningChanged(bool running) => cabinLight.enabled = running;
    }
}
