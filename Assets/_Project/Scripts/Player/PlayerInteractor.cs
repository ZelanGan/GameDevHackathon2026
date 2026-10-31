using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Player
{
    /// <summary>Знаходить найближчий об'єкт взаємодії, показує над ним кнопку E й виконує дію.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        private const float BadgeGap = 0.25f;
        private const float LabelGap = 0.25f;
        private const int BadgeOrder = 20;

        [SerializeField] private float radius = 1.2f;
        [SerializeField] private Sprite keyBadge;
        [SerializeField] private Material badgeMaterial;
        [SerializeField] private Font labelFont;

        private readonly Collider2D[] hits = new Collider2D[8];
        private ContactFilter2D filter;
        private InputAction interactAction;
        private GameObject badge;
        private TextMesh label;

        private void Awake()
        {
            filter = ContactFilter2D.noFilter;
            interactAction = InputSystem.actions.FindAction("Player/Interact", true);
            CreateBadge();
        }

        private void Update()
        {
            IInteractable nearest = FindNearest(out Collider2D target);
            ShowBadge(nearest, target);

            if (nearest != null && interactAction.WasPressedThisFrame())
                nearest.Interact();
        }

        private IInteractable FindNearest(out Collider2D target)
        {
            IInteractable nearest = null;
            target = null;
            float bestDistance = float.MaxValue;
            int count = Physics2D.OverlapCircle(transform.position, radius, filter, hits);

            for (int i = 0; i < count; i++)
            {
                if (!hits[i].TryGetComponent(out IInteractable interactable))
                    continue;

                float distance = Vector2.Distance(transform.position, hits[i].transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = interactable;
                    target = hits[i];
                }
            }

            return nearest;
        }

        private void ShowBadge(IInteractable nearest, Collider2D target)
        {
            bool visible = nearest != null && nearest.CanInteract;
            badge.SetActive(visible);
            if (!visible)
                return;

            Bounds bounds = target.bounds;
            badge.transform.position = new Vector3(bounds.center.x, bounds.max.y + BadgeGap, 0f);
            label.text = nearest.Label ?? string.Empty;
        }

        private void CreateBadge()
        {
            // Дочірній об'єкт зникає разом зі сценою гравця, хоч кнопка й стоїть над чужим об'єктом.
            badge = new GameObject("InteractBadge");
            badge.transform.SetParent(transform, false);
            var spriteRenderer = badge.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = keyBadge;
            spriteRenderer.sharedMaterial = badgeMaterial;
            spriteRenderer.sortingOrder = BadgeOrder;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(badge.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, LabelGap, 0f);
            label = labelObject.AddComponent<TextMesh>();
            label.font = labelFont;
            label.fontSize = 48;
            label.characterSize = 0.05f;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.color = Color.white;
            var labelRenderer = labelObject.GetComponent<MeshRenderer>();
            labelRenderer.sharedMaterial = labelFont.material;
            labelRenderer.sortingOrder = BadgeOrder;

            badge.SetActive(false);
        }
    }
}
