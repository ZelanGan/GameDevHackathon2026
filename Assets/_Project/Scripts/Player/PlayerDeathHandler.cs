using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Player
{
    /// <summary>Смерть гравця завершує партію: повідомляє про неї, а кінець показує GameManager.</summary>
    [RequireComponent(typeof(Health))]
    public class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel playerDied;

        private Health health;

        private void Awake() => health = GetComponent<Health>();

        private void OnEnable() => health.Died += OnDied;

        private void OnDisable() => health.Died -= OnDied;

        private void OnDied() => playerDied.Raise();
    }
}
