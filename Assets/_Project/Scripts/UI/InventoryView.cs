using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Items;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Панель інвентаря: клітинки з предметами й назва вибраного.</summary>
    public class InventoryView : MonoBehaviour
    {
        private const float SlotSize = 76f;
        private const float SlotSpacing = 8f;
        private const float SelectedScale = 1.18f;
        private const float SelectedLift = 8f;

        // Рамка кольорова сама, тому ці кольори лише підфарбовують її.
        private static readonly Color SlotColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        private static readonly Color SlotHover = new Color(1f, 0.95f, 0.8f, 1f);
        private static readonly Color SlotSelected = new Color(1f, 0.75f, 0.3f, 1f);
        private static readonly Color Warm = new Color(1f, 0.85f, 0.5f);

        [SerializeField] private Inventory inventory;
        [SerializeField] private VoidEventChannel inventoryChanged;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Sprite frame;
        [SerializeField] private Font font;

        private Image[] slotFrames;
        private Image[] slotIcons;
        private Text titleLabel;
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
                titleLabel.text = string.Empty;
                useButton.gameObject.SetActive(false);
                return;
            }

            // Квиток лише для обміну, тому кнопка з'являється тільки для предметів, які можна застосувати.
            useButton.gameObject.SetActive(inventory.Items[inventory.SelectedIndex].IsUsable);
            useLabel.text = Loc.T("Q: використати");

            // Під курсором видно назву іншої клітинки, інакше вибраний предмет, який витратить Q.
            bool peeksOther = hoveredIndex >= 0 && hoveredIndex < count && hoveredIndex != inventory.SelectedIndex;
            titleLabel.text = inventory.Items[peeksOther ? hoveredIndex : inventory.SelectedIndex].DisplayName;
        }

        private void Build()
        {
            int capacity = inventory.Capacity;
            float rowWidth = capacity * SlotSize + (capacity - 1) * SlotSpacing;

            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 20f);
            panel.sizeDelta = new Vector2(rowWidth, SlotSize + 96f);

            slotFrames = new Image[capacity];
            slotIcons = new Image[capacity];
            for (int i = 0; i < capacity; i++)
                BuildSlot(i);

            titleLabel = CreateText("Title", 28, FontStyle.Bold, Warm, 10f, 36f);
            BuildUseButton();
        }

        private void BuildUseButton()
        {
            var image = new GameObject("UseButton").AddComponent<Image>();
            image.transform.SetParent(panel, false);
            image.sprite = frame;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(260f, 44f);
            rect.anchoredPosition = new Vector2(0f, SlotSize + 50f);

            useButton = image.gameObject.AddComponent<Button>();
            ColorBlock colors = useButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = SlotSelected;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(1f, 0.85f, 0.5f);
            useButton.colors = colors;
            useButton.onClick.AddListener(inventory.UseSelected);

            useLabel = CreateSlotText("Label", image.transform, string.Empty, 22, Color.white);
            useLabel.alignment = TextAnchor.MiddleCenter;
            Inset(useLabel.rectTransform, 2f);
            useButton.gameObject.SetActive(false);
        }

        private void BuildSlot(int index)
        {
            var slotFrame = new GameObject($"Slot_{index}").AddComponent<Image>();
            slotFrame.transform.SetParent(panel, false);
            slotFrame.sprite = frame;
            slotFrame.type = Image.Type.Sliced;
            slotFrame.color = SlotColor;
            RectTransform frameRect = slotFrame.rectTransform;
            frameRect.anchorMin = frameRect.anchorMax = Vector2.zero;
            frameRect.pivot = Vector2.zero;
            frameRect.sizeDelta = new Vector2(SlotSize, SlotSize);
            frameRect.anchoredPosition = new Vector2(index * (SlotSize + SlotSpacing), 0f);

            var icon = new GameObject("Icon").AddComponent<Image>();
            icon.transform.SetParent(slotFrame.transform, false);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Inset(icon.rectTransform, 13f);

            Text number = CreateSlotText("Number", slotFrame.transform, (index + 1).ToString(), 18, Color.white);
            number.alignment = TextAnchor.UpperLeft;
            Inset(number.rectTransform, 7f);

            var slot = slotFrame.gameObject.AddComponent<InventorySlotView>();
            slot.Index = index;
            slot.Hovered += OnSlotHovered;
            slot.Unhovered += OnSlotUnhovered;
            slot.Clicked += OnSlotClicked;

            slotFrames[index] = slotFrame;
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
