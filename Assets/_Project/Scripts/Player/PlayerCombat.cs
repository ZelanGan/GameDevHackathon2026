using System.Collections;
using NightTrolleybus.Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Player
{
    /// <summary>Ближній бій: удар поточною зброєю в напрямку погляду.</summary>
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
            if (!motor.IsLocked && cooldownTimer <= 0f && attackAction.WasPressedThisFrame())
                Attack();
        }

        public void Attack()
        {
            WeaponData weapon = loadout.Weapon;
            cooldownTimer = weapon.Cooldown;
            StartCoroutine(ShowSwing(weapon.Range));

            Vector2 center = (Vector2)transform.position + new Vector2(motor.Facing * weapon.Range / 2f, 0f);
            int count = Physics2D.OverlapBox(center, new Vector2(weapon.Range, 1.6f), 0f, ContactFilter2D.noFilter, hits);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].gameObject != gameObject && hits[i].TryGetComponent(out Health target))
                    target.TakeDamage(weapon.Damage);
            }
        }

        private IEnumerator ShowSwing(float range)
        {
            swingVisual.transform.localPosition = new Vector3(motor.Facing * range / 2f, 0f, 0f);
            swingVisual.enabled = true;
            yield return new WaitForSeconds(0.1f);
            swingVisual.enabled = false;
        }
    }
}
