using System.Collections;
using NightTrolleybus.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Player
{
    /// <summary>Ближній бій: удар поточною зброєю туди, куди дивиться гравець.</summary>
    [RequireComponent(typeof(PlayerMotor2D))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer swingVisual;

        private readonly Collider2D[] hits = new Collider2D[8];
        private PlayerMotor2D motor;
        private PlayerLoadout loadout;
        private InputAction attackAction;
        private float cooldownTimer;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor2D>();
            loadout = FindAnyObjectByType<PlayerLoadout>();
            attackAction = InputSystem.actions.FindAction("Player/Attack", true);
            swingVisual.enabled = false;
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;
            // Натискання по інвентарю чи іншому інтерфейсу не має бути ударом.
            bool overInterface = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!motor.IsLocked && !overInterface && cooldownTimer <= 0f && attackAction.WasPressedThisFrame())
                Attack();
        }

        public void Attack()
        {
            WeaponData weapon = loadout.Weapon;
            cooldownTimer = weapon.Cooldown;

            Vector2 direction = motor.FacingDirection;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            StartCoroutine(ShowSwing(direction, angle, weapon.Range));

            Vector2 center = (Vector2)transform.position + direction * (weapon.Range / 2f);
            int count = Physics2D.OverlapBox(center, new Vector2(weapon.Range, 1.6f), angle, ContactFilter2D.noFilter, hits);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].gameObject != gameObject && hits[i].TryGetComponent(out Health target))
                    target.TakeDamage(weapon.Damage);
            }
        }

        private IEnumerator ShowSwing(Vector2 direction, float angle, float range)
        {
            Transform swing = swingVisual.transform;
            swing.localPosition = direction * (range / 2f);
            swing.rotation = Quaternion.Euler(0f, 0f, angle);
            swingVisual.enabled = true;
            yield return new WaitForSeconds(0.1f);
            swingVisual.enabled = false;
        }
    }
}
