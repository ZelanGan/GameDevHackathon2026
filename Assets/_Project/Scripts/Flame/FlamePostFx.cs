using NightTrolleybus.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NightTrolleybus.Flame
{
    /// <summary>Посилює віньєтку, коли Вогник слабшає.</summary>
    [RequireComponent(typeof(Volume))]
    public class FlamePostFx : MonoBehaviour
    {
        [SerializeField] private FlameConfig config;
        [SerializeField] private FlameStateEventChannel flameStateChanged;
        [SerializeField] private float transitionSpeed = 2f;

        private Vignette vignette;
        private float targetIntensity;

        private void Awake()
        {
            // Volume.profile створює копію, тому асет профілю під час гри не змінюється.
            if (!GetComponent<Volume>().profile.TryGet(out vignette))
                Debug.LogError("У профілі Volume немає ефекту Vignette", this);
        }

        private void OnEnable() => flameStateChanged.Raised += OnStateChanged;

        private void OnDisable() => flameStateChanged.Raised -= OnStateChanged;

        private void Start()
        {
            if (GameContext.Flame != null)
                OnStateChanged(GameContext.Flame.State);
        }

        private void OnStateChanged(FlameState state) => targetIntensity = config.GetSettings(state).vignette;

        private void Update()
        {
            if (vignette == null)
                return;

            vignette.intensity.Override(Mathf.Lerp(vignette.intensity.value, targetIntensity, transitionSpeed * Time.deltaTime));
        }
    }
}
