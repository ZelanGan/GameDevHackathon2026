using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace NightTrolleybus.Stops
{
    /// <summary>Зарядна станція в депо: один раз за зупинку поповнює енергію тролейбуса. Синє світло показує, що заряд є.</summary>
    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class PowerCharger : MonoBehaviour, IInteractable
    {
        [SerializeField] private float amount = 40f;
        [SerializeField] private Light2D beacon;
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private StringEventChannel noticeRaised;

        private bool used;

        public string Prompt => used ? "Зарядна станція порожня" : $"Зарядити тролейбус (+{Mathf.RoundToInt(amount)} енергії)";

        private void Update()
        {
            if (!used)
                beacon.intensity = 0.8f + Mathf.PingPong(Time.time * pulseSpeed, 1f) * 0.8f;
        }

        public void Interact()
        {
            if (used)
                return;

            used = true;
            beacon.enabled = false;
            GetComponent<SpriteRenderer>().color = new Color(0.4f, 0.4f, 0.45f);
            GameContext.Power?.Restore(amount);
            noticeRaised.Raise("Тролейбус заряджено");
            GameContext.Audio?.Play(SoundId.PowerUp);
        }
    }
}
