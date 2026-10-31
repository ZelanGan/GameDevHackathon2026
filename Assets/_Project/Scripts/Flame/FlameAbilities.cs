using System.Collections;
using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Flame
{
    /// <summary>Здібності Вогника: F Спалах, R Підсвічування, G Зігрівання. Кожна витрачає полум'я.</summary>
    public class FlameAbilities : MonoBehaviour
    {
        [Tooltip("Звідки діє Спалах")]
        [SerializeField] private Transform origin;

        [Header("Спалах")]
        [SerializeField] private float flashCost = 15f;
        [SerializeField] private float flashRadius = 6f;
        [SerializeField] private float stunDuration = 3f;
        [SerializeField] private VoidEventChannel flashTriggered;

        [Header("Підсвічування")]
        [SerializeField] private float revealCost = 10f;
        [SerializeField] private float revealDuration = 8f;
        [SerializeField] private BoolEventChannel revealChanged;

        [Header("Зігрівання")]
        [SerializeField] private bool canWarm;
        [SerializeField] private float warmCost = 10f;
        [SerializeField] private float warmHeat = 20f;

        [SerializeField] private StringEventChannel noticeRaised;

        private readonly Collider2D[] hits = new Collider2D[16];
        private bool revealActive;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.fKey.wasPressedThisFrame)
                Flash();
            if (keyboard.rKey.wasPressedThisFrame)
                Reveal();
            if (keyboard.gKey.wasPressedThisFrame)
                Warm();
        }

        public void Flash()
        {
            if (!TrySpend(flashCost))
                return;

            flashTriggered.Raise();
            GameContext.Audio?.Play(SoundId.Flash);
            int count = Physics2D.OverlapCircle(origin.position, flashRadius, ContactFilter2D.noFilter, hits);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].TryGetComponent(out IStunnable stunnable))
                    stunnable.Stun(stunDuration);
            }
        }

        public void Reveal()
        {
            if (revealActive || !TrySpend(revealCost))
                return;

            GameContext.Audio?.Play(SoundId.Reveal);
            StartCoroutine(RevealRoutine());
        }

        public void Warm()
        {
            // Поза салоном гріти нічого, тому полум'я не витрачаємо.
            if (!canWarm || GameContext.Temperature == null)
            {
                noticeRaised.Raise(Loc.T("Тут нічого зігрівати"));
                return;
            }

            if (TrySpend(warmCost))
            {
                GameContext.Temperature.AddHeat(warmHeat);
                GameContext.Audio?.Play(SoundId.FlameFed);
            }
        }

        private IEnumerator RevealRoutine()
        {
            revealActive = true;
            revealChanged.Raise(true);
            yield return new WaitForSeconds(revealDuration);
            revealActive = false;
            revealChanged.Raise(false);
        }

        private bool TrySpend(float cost)
        {
            if (GameContext.Flame != null && GameContext.Flame.TrySpend(cost))
                return true;

            noticeRaised.Raise(Loc.T("Вогнику бракує сили"));
            return false;
        }
    }
}
