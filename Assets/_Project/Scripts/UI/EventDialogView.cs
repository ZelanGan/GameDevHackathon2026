using System;
using System.Collections.Generic;
using NightTrolleybus.Core;
using NightTrolleybus.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Вікно події: текст і кнопки вибору. Логіки гри не містить.</summary>
    public class EventDialogView : MonoBehaviour
    {
        private const float ButtonHeight = 70f;
        private const float ButtonSpacing = 14f;

        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private RectTransform buttonsRoot;
        [SerializeField] private Sprite square;
        [SerializeField] private Font font;

        private readonly List<GameObject> buttons = new List<GameObject>();

        private void Awake() => panel.SetActive(false);

        public void ShowEvent(RoadEventData roadEvent, Func<RoadEventChoice, bool> canChoose, Action<RoadEventChoice> onChoice)
        {
            panel.SetActive(true);
            titleLabel.text = roadEvent.Title;
            bodyLabel.text = roadEvent.Description;
            ClearButtons();

            foreach (RoadEventChoice choice in roadEvent.Choices)
            {
                bool available = canChoose(choice);
                string text = available || choice.requiredItem == null
                    ? choice.text
                    : $"{choice.text} (потрібно: {choice.requiredItem.DisplayName})";
                RoadEventChoice captured = choice;
                AddButton(text, available, () => onChoice(captured));
            }
        }

        public void ShowOutcome(string outcome, Action onClosed)
        {
            bodyLabel.text = outcome;
            ClearButtons();
            AddButton("Далі", true, () => onClosed());
        }

        public void Hide()
        {
            ClearButtons();
            panel.SetActive(false);
        }

        private void ClearButtons()
        {
            // Destroy спрацьовує в кінці кадру, а стара кнопка не має отримати ще одне натискання.
            foreach (GameObject button in buttons)
            {
                button.SetActive(false);
                Destroy(button);
            }

            buttons.Clear();
        }

        private void AddButton(string text, bool available, Action onClick)
        {
            var image = new GameObject("ChoiceButton").AddComponent<Image>();
            image.transform.SetParent(buttonsRoot, false);
            image.sprite = square;
            image.color = available ? new Color(0.95f, 0.65f, 0.3f) : new Color(0.3f, 0.3f, 0.3f);
            image.rectTransform.anchorMin = new Vector2(0f, 1f);
            image.rectTransform.anchorMax = new Vector2(1f, 1f);
            image.rectTransform.pivot = new Vector2(0.5f, 1f);
            image.rectTransform.sizeDelta = new Vector2(0f, ButtonHeight);
            image.rectTransform.anchoredPosition = new Vector2(0f, -buttons.Count * (ButtonHeight + ButtonSpacing));

            var button = image.gameObject.AddComponent<Button>();
            button.interactable = available;
            button.onClick.AddListener(() =>
            {
                GameContext.Audio?.Play(SoundId.UiClick);
                onClick();
            });

            var label = new GameObject("Label").AddComponent<Text>();
            label.transform.SetParent(image.transform, false);
            label.font = font;
            label.fontSize = 26;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.black;
            label.text = text;
            label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            buttons.Add(image.gameObject);
        }
    }
}
