using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Items;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Панель інвентаря: клітинки з предметами, опис і кнопка «Використати».</summary>
    public class InventoryView : MonoBehaviour
    {
        private const float SlotSize = 76f;
        private const float SlotSpacing = 8f;
        private const float SelectedScale = 1.18f;
        private const float SelectedLift = 8f;

        private static readonly Color SlotColor = new Color(0.08f, 0.07f, 0.09f, 0.85f);
        private static readonly Color SlotHover = new Color(0.32f, 0.24f, 0.16f, 0.95f);
        private static readonly Color SlotSelected = new Color(0.95f, 0.62f, 0.25f, 1f);
        private static readonly Color Warm = new Color(1f, 0.85f, 0.5f);

        [SerializeField] private Inventory inventory;
        [SerializeField] private VoidEventChannel inventoryChanged;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Sprite square;
        [SerializeField] private Font font;

        private Image[] slotFrames;
        private Image[] slotIcons;
        private Text titleLabel;
        private Text descriptionLabel;
        private Button useButton;
        private Text useLabel;
        private int hoveredIndex = -1;

        private void Awake() => Build();

        private void OnEnable() => inventoryChanged.Raised += Refresh;

        private void OnDisable() => inventoryChanged.Raised -= Refresh;

        private void Start() => Refresh();

        private void OnSlotHovered(int index)
        {
            hoveredIndex = index;
            Refresh();
        }

        private void OnSlotUnhovered(int index)
        {
            if (hoveredIndex == index)
                hoveredIndex = -1;
            Refresh();
        }

        private void OnSlotClicked(int index, int clickCount)
        {
            GameContext.Audio?.Play(SoundId.UiClick);
            inventory.Select(index);
            // Подвійне натискання одразу використовує предмет.
            if (clickCount >= 2 && index == inventory.SelectedIndex && index < inventory.Items.Count)
                inventory.UseSelected();
        }

        private void Refresh()
        {
            int count = inventory.Items.Count;
            for (int i = 0; i < slotFrames.Length; i++)
            {
                bool hasItem = i < count;
                bool isSelected = hasItem && i == inventory.SelectedIndex;
                slotIcons[i].enabled = hasItem;
                if (hasItem)
                    slotIcons[i].sprite = inventory.Items[i].Icon;

                // Вибрану клітинку видно здалеку: вона більша, піднята й має яскраву рамку.
                slotFrames[i].color = isSelected ? SlotSelected : i == hoveredIndex && hasItem ? SlotHover : SlotColor;
                slotFrames[i].rectTransform.localScale = Vector3.one * (isSelected ? SelectedScale : 1f);
                slotFrames[i].rectTransform.anchoredPosition = new Vector2(i * (SlotSize + SlotSpacing), isSelected ? SelectedLift : 0f);
            }

            // Вибрана клітинка має лежати поверх сусідів, бо вона більша за них.
            if (count > 0)
                slotFrames[inventory.SelectedIndex].transform.SetAsLastSibling();

            if (count == 0)
            {
                titleLabel.text = $"Інвентар порожній (0/{inventory.Capacity})";
                descriptionLabel.text = "Предмети підбираються дотиком. Вибір: колесо миші, Tab або 1-6";
                useButton.interactable = false;
                useLabel.text = "Немає що використати";
                return;
            }

            ItemData selected = inventory.Items[inventory.SelectedIndex];
            titleLabel.text = $"Вибрано: {selected.DisplayName}";

            // Опис під курсором не замінює вибраний предмет, а лише підказує про іншу клітинку.
            bool peeksOther = hoveredIndex >= 0 && hoveredIndex < count && hoveredIndex != inventory.SelectedIndex;
            descriptionLabel.text = peeksOther
                ? $"Клітинка {hoveredIndex + 1}: {inventory.Items[hoveredIndex].DisplayName} — {inventory.Items[hoveredIndex].Description}"
                : $"{selected.Description}   |   Вибір: колесо миші, Tab або 1-6";

            useButton.interactable = selected.IsUsable;
            useLabel.text = selected.IsUsable ? $"Q — використати: {selected.DisplayName}" : "Цей предмет лише для обміну";
        }

        private void Build()
        {
            int capacity = inventory.Capacity;
            float rowWidth = capacity * SlotSize + (capacity - 1) * SlotSpacing;

            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 20f);
            panel.sizeDelta = new Vector2(rowWidth, SlotSize + 148f);

            slotFrames = new Image[capacity];
            slotIcons = new Image[capacity];
            for (int i = 0; i < capacity; i++)
                BuildSlot(i);

            titleLabel = CreateText("Title", 28, FontStyle.Bold, Warm, 118f, 36f);
            descriptionLabel = CreateText("Description", 22, FontStyle.Normal, Color.white, 88f, 30f);

            var image = new GameObject("UseButton").AddComponent<Image>();
            image.transform.SetParent(panel, false);
            image.sprite = square;
            image.color = Color.white;
            PlaceRow(image.rectTransform, SlotSize + 38f, 44f, 0.7f);

            useButton = image.gameObject.AddComponent<Button>();
            ColorBlock colors = useButton.colors;
            colors.normalColor = new Color(0.72f, 0.42f, 0.14f, 0.95f);
            colors.highlightedColor = SlotSelected;
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = new Color(1f, 0.8f, 0.45f);
            colors.disabledColor = new Color(0.14f, 0.14f, 0.14f, 0.8f);
            useButton.colors = colors;
            useButton.onClick.AddListener(inventory.UseSelected);

            useLabel = new GameObject("Label").AddComponent<Text>();
            useLabel.transform.SetParent(image.transform, false);
            useLabel.font = font;
            useLabel.fontSize = 22;
            useLabel.alignment = TextAnchor.MiddleCenter;
            useLabel.color = Color.white;
            useLabel.raycastTarget = false;
            RectTransform labelRect = useLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            AddOutline(useLabel);
        }

        private void BuildSlot(int index)
        {
            var frame = new GameObject($"Slot_{index}").AddComponent<Image>();
            frame.transform.SetParent(panel, false);
            frame.sprite = square;
            frame.color = SlotColor;
            RectTransform frameRect = frame.rectTransform;
            frameRect.anchorMin = frameRect.anchorMax = Vector2.zero;
            frameRect.pivot = Vector2.zero;
            frameRect.sizeDelta = new Vector2(SlotSize, SlotSize);
            frameRect.anchoredPosition = new Vector2(index * (SlotSize + SlotSpacing), 0f);

            var inner = new GameObject("Inner").AddComponent<Image>();
            inner.transform.SetParent(frame.transform, false);
            inner.sprite = square;
            inner.color = new Color(0.03f, 0.03f, 0.05f, 0.95f);
            inner.raycastTarget = false;
            Inset(inner.rectTransform, 5f);

            var icon = new GameObject("Icon").AddComponent<Image>();
            icon.transform.SetParent(frame.transform, false);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Inset(icon.rectTransform, 13f);

            Text number = CreateSlotText("Number", frame.transform, (index + 1).ToString(), 18, Color.white);
            number.alignment = TextAnchor.UpperLeft;
            Inset(number.rectTransform, 7f);

            var slot = frame.gameObject.AddComponent<InventorySlotView>();
            slot.Index = index;
            slot.Hovered += OnSlotHovered;
            slot.Unhovered += OnSlotUnhovered;
            slot.Clicked += OnSlotClicked;

            slotFrames[index] = frame;
            slotIcons[index] = icon;
        }

        private Text CreateSlotText(string objectName, Transform parent, string text, int size, Color color)
        {
            var label = new GameObject(objectName).AddComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = font;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            label.text = text;
            label.raycastTarget = false;
            AddOutline(label);
            return label;
        }

        private Text CreateText(string objectName, int size, FontStyle style, Color color, float y, float height)
        {
            var label = new GameObject(objectName).AddComponent<Text>();
            label.transform.SetParent(panel, false);
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            PlaceRow(label.rectTransform, SlotSize + y, height, 1f);
            AddOutline(label);
            return label;
        }

        private void PlaceRow(RectTransform rect, float y, float height, float widthFactor)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(panel.sizeDelta.x * widthFactor, height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        private static void Inset(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void AddOutline(Text label)
        {
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }
}
