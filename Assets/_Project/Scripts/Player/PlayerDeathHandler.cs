using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Player
{
    /// <summary>Падіння гравця не є поразкою: він повертається до входу, а Вогник втрачає силу.</summary>
    [RequireComponent(typeof(Health))]
    public class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private float flameLoss = 15f;
        [SerializeField] private StringEventChannel noticeRaised;

        private Health health;

        private void Awake() => health = GetComponent<Health>();

        private void OnEnable() => health.Died += OnDied;

        private void OnDisable() => health.Died -= OnDied;

        private void OnDied()
        {
            GameContext.Flame?.Drain(flameLoss);
            transform.position = respawnPoint.position;
            health.RestoreFull();
            noticeRaised.Raise(Loc.T("Ви впали. Вогник витратив силу, щоб повернути вас."));
        }
    }
}
