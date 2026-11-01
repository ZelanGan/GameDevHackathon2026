using System.Linq;
using NightTrolleybus.Core;
using NightTrolleybus.Route;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Малює мапу маршруту: вузли-кнопки й лінії між ними.</summary>
    public class RouteMapView : MonoBehaviour
    {
        private const float LowPower = 0.5f;

        private static readonly Vector2 NodeSize = new Vector2(190f, 126f);
        private static readonly Vector2 IconSize = new Vector2(170f, 74f);
        private const float LabelHeight = 50f;
        private static readonly Color DimIconColor = new Color(0.55f, 0.55f, 0.6f);

        [SerializeField] private RectTransform root;
        [SerializeField] private Sprite square;
        [Tooltip("Малюнки вузлів у порядку NodeType")]
        [SerializeField] private Sprite[] nodeIcons;
        [SerializeField] private Font font;
        [SerializeField] private float layerSpacing = 280f;
        [SerializeField] private float rowSpacing = 150f;

        private void Start()
        {
            RouteManager route = FindAnyObjectByType<RouteManager>();
            foreach (RouteNode node in route.Nodes)
            {
                foreach (RouteNode next in node.Next)
                    CreateLine(Position(node), Position(next), route.Visited.Contains(node.Id) || route.Current == node);
            }

            foreach (RouteNode node in route.Nodes)
                CreateNode(node, route);
        }

        private Vector2 Position(RouteNode node)
        {
            float x = (node.Layer - 2.5f) * layerSpacing;
            float y = (node.Index - (node.LayerSize - 1) / 2f) * rowSpacing;
            return new Vector2(x, y);
        }

        private void CreateNode(RouteNode node, RouteManager route)
        {
            bool available = route.IsAvailable(node);
            bool visited = route.Visited.Contains(node.Id);

            var image = new GameObject($"Node_{node.Id}").AddComponent<Image>();
            image.transform.SetParent(root, false);
            image.sprite = square;
            image.color = available ? new Color(0.95f, 0.65f, 0.3f) : visited ? new Color(0.45f, 0.35f, 0.25f) : new Color(0.2f, 0.2f, 0.24f);
            // Коли енергії мало, доступне депо видно одразу: воно зелене, а не помаранчеве.
            if (available && node.Type == NodeType.Depot && GameContext.Power.Normalized < LowPower)
                image.color = new Color(0.45f, 0.95f, 0.55f);
            image.rectTransform.sizeDelta = NodeSize;
            image.rectTransform.anchoredPosition = Position(node);

            var button = image.gameObject.AddComponent<Button>();
            button.interactable = available;
            // Недоступний вузол темний сам по собі, а прозорий він пропускав би лінії маршруту.
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                GameContext.Audio?.Play(SoundId.UiClick);
                route.Select(node);
            });

            var icon = new GameObject("Icon").AddComponent<Image>();
            icon.transform.SetParent(image.transform, false);
            icon.sprite = nodeIcons[(int)node.Type];
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            // Малюнок яскравий лише там, куди можна поїхати.
            icon.color = available ? Color.white : DimIconColor;
            icon.rectTransform.sizeDelta = IconSize;
            icon.rectTransform.anchoredPosition = new Vector2(0f, LabelHeight / 2f);

            var label = new GameObject("Label").AddComponent<Text>();
            label.transform.SetParent(image.transform, false);
            label.font = font;
            label.fontSize = 22;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = available ? Color.black : new Color(0.75f, 0.75f, 0.75f);
            label.text = $"{node.Type.DisplayName()}\n<size=15>{node.Type.Benefit()}</size>";
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.rectTransform.sizeDelta = new Vector2(0f, LabelHeight);
            label.rectTransform.anchoredPosition = Vector2.zero;
        }

        private void CreateLine(Vector2 from, Vector2 to, bool bright)
        {
            var line = new GameObject("Line").AddComponent<Image>();
            line.transform.SetParent(root, false);
            line.sprite = square;
            line.raycastTarget = false;
            line.color = bright ? new Color(0.8f, 0.55f, 0.3f) : new Color(0.3f, 0.3f, 0.34f);

            Vector2 delta = to - from;
            line.rectTransform.sizeDelta = new Vector2(delta.magnitude, 4f);
            line.rectTransform.anchoredPosition = (from + to) / 2f;
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }
}
