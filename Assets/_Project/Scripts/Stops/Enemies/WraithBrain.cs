using NightTrolleybus.Core;
using NightTrolleybus.Flame;
using NightTrolleybus.Player;
using NightTrolleybus.Route;
using UnityEngine;

namespace NightTrolleybus.Stops.Enemies
{
    /// <summary>Привид: тримається на відстані й тягне гравця променем. Промінь спершу лише сповільнює, а якщо триває довго, завдає шкоди.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public class WraithBrain : MonoBehaviour, IStunnable
    {
        private const int FirstStop = 3;

        /// <summary>Перші зупинки дають звикнути до гри, тому привиди приходять лише згодом.</summary>
        public static bool IsAllowed
        {
            get
            {
                RouteManager route = FindAnyObjectByType<RouteManager>();
                return route == null || route.StopsVisited >= FirstStop;
            }
        }

        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private float speed = 2f;

        [Header("Відстань")]
        [SerializeField] private float attackRange = 6.5f;
        [Tooltip("Ближче за цю відстань привид відступає")]
        [SerializeField] private float keepAwayDistance = 4f;

        [Header("Промінь")]
        [Range(0.1f, 1f)]
        [SerializeField] private float slowFactor = 0.45f;
        [Tooltip("Скільки секунд промінь лише сповільнює")]
        [SerializeField] private float chargeSeconds = 1.6f;
        [SerializeField] private float damage = 8f;
        [SerializeField] private float damageInterval = 0.8f;
        [Tooltip("Пауза після перерваної атаки")]
        [SerializeField] private float cooldownSeconds = 1.5f;
        [Range(0f, 1f)]
        [SerializeField] private float fear = 0.3f;
        [SerializeField] private SpriteRenderer beam;
        [SerializeField] private Color chargingColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color damagingColor = new Color(1f, 0.35f, 0.4f, 1f);
        [Tooltip("Скільки разів за секунду хвиля променя перевертається")]
        [SerializeField] private float beamWaveRate = 10f;

        private readonly RaycastHit2D[] lineHits = new RaycastHit2D[8];
        private Rigidbody2D body;
        private Health health;
        private Transform player;
        private Health playerHealth;
        private PlayerMotor2D playerMotor;
        private ContactFilter2D solidFilter;
        private Vector2 moveDirection;
        private bool isAttacking;
        private float attackTime;
        private float damageTimer;
        private float cooldownTimer;
        private float stunTimer;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            health.Died += OnDied;
            solidFilter = new ContactFilter2D { useTriggers = false };
            beam.enabled = false;
        }

        private void Start()
        {
            health.SetMax(maxHealth);

            GameObject playerObject = GameObject.FindWithTag("Player");
            player = playerObject.transform;
            playerHealth = playerObject.GetComponent<Health>();
            playerMotor = playerObject.GetComponent<PlayerMotor2D>();
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), playerObject.GetComponent<Collider2D>());
        }

        private void OnDestroy()
        {
            health.Died -= OnDied;
            StopAttack();
        }

        public void Stun(float duration)
        {
            stunTimer = duration;
            InterruptAttack();
        }

        private void Update()
        {
            moveDirection = Vector2.zero;
            cooldownTimer -= Time.deltaTime;

            if (stunTimer > 0f)
            {
                stunTimer -= Time.deltaTime;
                return;
            }

            Vector2 toPlayer = player.position - transform.position;
            float distance = toPlayer.magnitude;
            bool canHit = distance <= attackRange && HasLineOfSight();

            if (isAttacking)
            {
                if (canHit)
                    UpdateAttack(toPlayer, distance);
                else
                    InterruptAttack();
                return;
            }

            if (distance < keepAwayDistance)
                moveDirection = -toPlayer / distance;
            else if (!canHit)
                moveDirection = toPlayer / distance;
            else if (cooldownTimer <= 0f)
                StartAttack();
        }

        private void FixedUpdate() => body.linearVelocity = moveDirection * speed;

        private void StartAttack()
        {
            isAttacking = true;
            attackTime = 0f;
            damageTimer = 0f;
            beam.enabled = true;
            playerMotor.AddSlow(this, slowFactor);
            GameContext.Fear?.SetSource(this, fear);
            GameContext.Audio?.Play(SoundId.EnemyAppear);
        }

        private void UpdateAttack(Vector2 toPlayer, float distance)
        {
            attackTime += Time.deltaTime;
            bool isDamaging = attackTime >= chargeSeconds;

            Transform beamTransform = beam.transform;
            beamTransform.position = (Vector2)transform.position + toPlayer / 2f;
            beamTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg);
            // Промінь товстішає в міру наближення шкоди, тож гравець бачить, скільки часу лишилося.
            float thickness = Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(attackTime / chargeSeconds));
            beamTransform.localScale = new Vector3(1f, thickness, 1f);
            // Хвиля повторюється вздовж променя, а не розтягується, і так здається, що вона біжить.
            beam.size = new Vector2(distance, beam.sprite.bounds.size.y);
            beam.flipY = Mathf.FloorToInt(attackTime * beamWaveRate) % 2 == 0;
            beam.color = isDamaging ? damagingColor : chargingColor;

            if (!isDamaging)
                return;

            damageTimer -= Time.deltaTime;
            if (damageTimer > 0f)
                return;

            damageTimer = damageInterval;
            playerHealth.TakeDamage(damage);
            GameContext.Audio?.Play(SoundId.Hurt);
        }

        private void InterruptAttack()
        {
            if (!isAttacking)
                return;

            StopAttack();
            cooldownTimer = cooldownSeconds;
        }

        private void StopAttack()
        {
            isAttacking = false;
            if (beam != null)
                beam.enabled = false;
            if (playerMotor != null)
                playerMotor.RemoveSlow(this);
            GameContext.Fear?.RemoveSource(this);
        }

        private bool HasLineOfSight()
        {
            int count = Physics2D.Linecast(transform.position, player.position, solidFilter, lineHits);
            for (int i = 0; i < count; i++)
            {
                Transform hit = lineHits[i].transform;
                if (hit != transform && hit != player && hit.GetComponent<IStunnable>() == null)
                    return false;
            }

            return true;
        }

        private void OnDied() => Destroy(gameObject);
    }
}
