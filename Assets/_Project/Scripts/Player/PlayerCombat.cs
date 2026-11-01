using System.Collections;
using NightTrolleybus.Core;
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
        private const float SwingSeconds = 0.16f;
        private const float SwingArc = 75f;
        private const float HeldAngle = 55f;
        private const float SlashHeight = 2.2f;
        private const int OrderBehindPlayer = 2;
        private static readonly Vector2 HandOffset = new Vector2(0.3f, -0.15f);

        [Tooltip("Слід удару")]
        [SerializeField] private SpriteRenderer swingVisual;
        [Tooltip("Зброя в руці")]
        [SerializeField] private SpriteRenderer weaponVisual;

        private readonly Collider2D[] hits = new Collider2D[8];
        private PlayerMotor2D motor;
        private PlayerLoadout loadout;
        private InputAction attackAction;
        private float cooldownTimer;
        private int weaponOrder;
        private bool isSwinging;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor2D>();
            loadout = FindAnyObjectByType<PlayerLoadout>();
            attackAction = InputSystem.actions.FindAction("Player/Attack", true);
            swingVisual.enabled = false;
            weaponOrder = weaponVisual.sortingOrder;
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;
            weaponVisual.sprite = loadout.Weapon.Sprite;
            if (!isSwinging)
                HoldWeapon();

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
            GameContext.Audio?.Play(SoundId.Swing);

            Vector2 center = (Vector2)transform.position + direction * (weapon.Range / 2f);
            int count = Physics2D.OverlapBox(center, new Vector2(weapon.Range, 1.6f), angle, ContactFilter2D.noFilter, hits);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].gameObject != gameObject && hits[i].TryGetComponent(out Health target))
                {
                    target.TakeDamage(weapon.Damage);
                    GameContext.Audio?.Play(SoundId.Hit);
                }
            }
        }

        // Спиною до камери герой затуляє зброю собою.
        private void HoldWeapon()
        {
            Vector2 direction = motor.FacingDirection;
            float side = direction.x < -0.1f ? -1f : 1f;
            Transform held = weaponVisual.transform;
            held.localPosition = new Vector2(HandOffset.x * side, HandOffset.y);
            held.rotation = Quaternion.Euler(0f, 0f, side > 0f ? HeldAngle : 180f - HeldAngle);
            weaponVisual.flipY = side < 0f;
            weaponVisual.sortingOrder = direction.y > 0.5f ? OrderBehindPlayer : weaponOrder;
        }

        private IEnumerator ShowSwing(Vector2 direction, float angle, float range)
        {
            isSwinging = true;
            // Зброя йде згори вниз, тож у лівий бік дуга дзеркальна.
            float arc = direction.x < 0f ? -SwingArc : SwingArc;
            Transform weapon = weaponVisual.transform;
            weapon.localPosition = direction * 0.25f;
            weaponVisual.flipY = direction.x < 0f;
            weaponVisual.sortingOrder = weaponOrder;

            Transform swing = swingVisual.transform;
            swing.rotation = Quaternion.Euler(0f, 0f, angle);
            swing.localScale = new Vector3(range / swingVisual.sprite.bounds.size.x, SlashHeight, 1f);
            swingVisual.flipY = direction.x < 0f;
            swingVisual.enabled = true;

            for (float time = 0f; time < SwingSeconds; time += Time.deltaTime)
            {
                float progress = time / SwingSeconds;
                weapon.rotation = Quaternion.Euler(0f, 0f, angle + Mathf.Lerp(arc, -arc, progress));
                swing.localPosition = direction * Mathf.Lerp(range * 0.35f, range * 0.6f, progress);
                swingVisual.color = new Color(1f, 1f, 1f, 1f - progress);
                yield return null;
            }

            swingVisual.enabled = false;
            isSwinging = false;
        }
    }
}
