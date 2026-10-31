using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Flame;
using UnityEngine;

namespace NightTrolleybus.Stops
{
    /// <summary>Прихований предмет існує лише під час Підсвічування й у радіусі світла Вогника, а прихований прохід у цей час відкривається.</summary>
    public class RevealableObject : MonoBehaviour
    {
        [SerializeField] private FlameConfig config;
        [SerializeField] private BoolEventChannel revealChanged;
        [SerializeField] private float fadeSpeed = 4f;
        [Tooltip("Навпаки: це стіна, що видима й заступає шлях, а під Підсвічуванням зникає")]
        [SerializeField] private bool hiddenPassage;

        private SpriteRenderer[] renderers;
        private Collider2D[] colliders;
        private Color[] baseColors;
        private Transform flame;
        private bool revealActive;
        private float alpha;

        private void Awake()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
            colliders = GetComponentsInChildren<Collider2D>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i].color;

            alpha = hiddenPassage ? 1f : 0f;
            Apply();
        }

        private void OnEnable() => revealChanged.Raised += OnRevealChanged;

        private void OnDisable() => revealChanged.Raised -= OnRevealChanged;

        private void Start() => flame = FindAnyObjectByType<FlameView>().transform;

        private void OnRevealChanged(bool active) => revealActive = active;

        private void Update()
        {
            float target = IsLit() != hiddenPassage ? 1f : 0f;
            if (Mathf.Approximately(alpha, target))
                return;

            alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime);
            Apply();
        }

        private bool IsLit()
        {
            if (!revealActive || GameContext.Flame == null)
                return false;

            float radius = config.GetSettings(GameContext.Flame.State).lightRadius;
            return Vector2.Distance(transform.position, flame.position) <= radius;
        }

        private void Apply()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Color color = baseColors[i];
                color.a *= alpha;
                renderers[i].color = color;
            }

            // Колайдери вмикаються, коли об'єкт уже добре видно.
            foreach (Collider2D objectCollider in colliders)
                objectCollider.enabled = alpha >= 0.5f;
        }
    }
}
