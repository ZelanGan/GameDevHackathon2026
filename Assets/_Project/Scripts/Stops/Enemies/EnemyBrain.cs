using NightTrolleybus.Core;
using NightTrolleybus.Flame;
using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Stops.Enemies
{
    /// <summary>Моб темряви: іде прямо до гравця, б'є поруч, лякає Вогника, після Спалаху стоїть оглушений.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class EnemyBrain : MonoBehaviour, IStunnable
    {
        [SerializeField] private EnemyData data;

        private Rigidbody2D body;
        private Health health;
        private Transform player;
        private Health playerHealth;
        private float stunTimer;
        private float attackTimer;
        private Vector2 moveDirection;

        public bool IsStunned => stunTimer > 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        private void Start()
        {
            health.SetMax(data.MaxHealth);

            GameObject playerObject = GameObject.FindWithTag("Player");
            player = playerObject.transform;
            playerHealth = playerObject.GetComponent<Health>();
            // Моб не штовхає гравця, а завдає шкоди на відстані удару.
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), playerObject.GetComponent<Collider2D>());
        }

        private void OnDestroy()
        {
            health.Died -= OnDied;
            GameContext.Fear?.RemoveSource(this);
        }

        public void Stun(float duration) => stunTimer = duration;

        private void Update()
        {
            attackTimer -= Time.deltaTime;
            moveDirection = Vector2.zero;

            Vector2 toPlayer = player.position - transform.position;
            float distance = toPlayer.magnitude;
            UpdateFear(distance);

            if (IsStunned)
            {
                stunTimer -= Time.deltaTime;
                return;
            }

            if (distance <= data.AttackRange)
            {
                if (attackTimer <= 0f)
                {
                    attackTimer = data.AttackInterval;
                    playerHealth.TakeDamage(data.AttackDamage);
                }
            }
            else if (distance <= data.DetectionRange)
            {
                moveDirection = toPlayer / distance;
            }
        }

        private void FixedUpdate() => body.linearVelocity = moveDirection * data.Speed;

        private void UpdateFear(float distance)
        {
            if (GameContext.Fear == null)
                return;

            if (distance <= data.FearRange)
                GameContext.Fear.SetSource(this, data.Fear);
            else
                GameContext.Fear.RemoveSource(this);
        }

        private void OnDied() => Destroy(gameObject);
    }
}
