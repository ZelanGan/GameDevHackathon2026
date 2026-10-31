using System;
using System.Collections.Generic;
using NightTrolleybus.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Головне меню й меню паузи. Лише показує сторінки й повідомляє, що обрав гравець.</summary>
    public class GameMenuView : MonoBehaviour
    {
        private const float ButtonWidth = 460f;
        private const float ButtonHeight = 72f;
        private const float ButtonSpacing = 16f;
        private const float NeutralFlameStrength = 0.55f;

        private static readonly Color ButtonColor = new Color(0.1f, 0.08f, 0.1f, 0.88f);
        private static readonly Color ButtonHover = new Color(0.95f, 0.62f, 0.25f, 1f);
        private static readonly Color ButtonPressed = new Color(1f, 0.8f, 0.45f, 1f);
        private static readonly Color ButtonDisabled = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        private static readonly Color Warm = new Color(1f, 0.78f, 0.4f);

        [SerializeField] private Sprite background;
        [SerializeField] private Sprite square;
        [SerializeField] private Sprite glow;
        [SerializeField] private Sprite[] flameFrames;
        [SerializeField] private Font font;

        public event Action StartRequested;
        public event Action ContinueRequested;
        public event Action ResumeRequested;
        public event Action ExitToMenuRequested;

        private readonly List<GameObject> pageItems = new List<GameObject>();
        private GameObject root;
        private GameObject art;
        private GameObject dim;
        private RectTransform pageRoot;
        private Image flameImage;
        private Image glowImage;
        private Image darknessImage;
        private RectTransform startButtonRect;
        private RectTransform quitButtonRect;
        private float flameStrength = NeutralFlameStrength;
        private Action redraw;
        private bool hasSave;

        private void Awake()
        {
            BuildFrame();
            root.SetActive(false);
        }

        private void OnEnable() => GameSettings.LanguageChanged += OnLanguageChanged;

        private void OnDisable() => GameSettings.LanguageChanged -= OnLanguageChanged;

        private void OnLanguageChanged() => redraw?.Invoke();

        public void ShowMain(bool saveExists)
        {
            hasSave = saveExists;
            art.SetActive(true);
            dim.SetActive(false);
            root.SetActive(true);
            ShowMainPage();
        }

        public void ShowPause()
        {
            art.SetActive(false);
            dim.SetActive(true);
            root.SetActive(true);
            ShowPausePage();
        }

        public void Hide()
        {
            ClearPage();
            root.SetActive(false);
        }

        private void Update()
        {
            if (!art.activeInHierarchy)
                return;

            // Час не масштабуємо: у меню гра стоїть, а Вогник живе.
            flameImage.sprite = flameFrames[(int)(Time.unscaledTime * 7f) % flameFrames.Length];

            flameStrength = Mathf.MoveTowards(flameStrength, FindCursorStrength(), Time.unscaledDeltaTime * 2.5f);
            float pulse = 0.85f + Mathf.PerlinNoise(Time.unscaledTime * 2.5f, 0f) * 0.3f;
            glowImage.color = new Color(1f, 0.62f, 0.25f, Mathf.Lerp(0f, 0.9f, flameStrength) * pulse);
            glowImage.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(120f, 1150f, flameStrength);
            flameImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.25f, 2.1f, flameStrength);
            flameImage.color = Color.Lerp(new Color(0.55f, 0.15f, 0.08f), new Color(1f, 0.85f, 0.45f), flameStrength);
            // Згасаючий Вогник забирає з собою весь кадр, разом із теплим вікном на заставці.
            darknessImage.color = new Color(0f, 0f, 0.02f, Mathf.Lerp(0.88f, 0f, flameStrength));

            UpdateKeyboardSelection();
        }

        // Вибір потрібен лише для клавіатури: під мишею він лишав би підсвіченою ще одну кнопку.
        private static void UpdateKeyboardSelection()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero)
                eventSystem.SetSelectedGameObject(null);
        }

        /// <summary>Сила Вогника за курсором: 1 біля «Почати», 0 біля «Вийти». Поза головною сторінкою горить рівно.</summary>
        private float FindCursorStrength()
        {
            if (startButtonRect == null || quitButtonRect == null || Mouse.current == null)
                return NeutralFlameStrength;

            Vector2 cursor = Mouse.current.position.ReadValue();
            float toStart = Vector2.Distance(cursor, startButtonRect.position);
            float toQuit = Vector2.Distance(cursor, quitButtonRect.position);
            return toQuit / Mathf.Max(toStart + toQuit, 1f);
        }

        private void ShowMainPage()
        {
            redraw = ShowMainPage;
            BeginPage(new Vector2(0f, 0.5f), new Vector2(130f, 90f), TextAnchor.UpperLeft);
            AddTitle(MenuText.Get("title"), MenuText.Get("subtitle"));
            startButtonRect = AddButton(MenuText.Get("start"), true, OnStartPressed);
            AddButton(MenuText.Get("continue"), hasSave, () => ContinueRequested?.Invoke());
            AddButton(MenuText.Get("settings"), true, () => ShowSettingsPage(ShowMainPage));
            quitButtonRect = AddButton(MenuText.Get("quit"), true, Quit);
            FinishPage();
        }

        private void ShowPausePage()
        {
            redraw = ShowPausePage;
            BeginPage(new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), TextAnchor.UpperCenter);
            AddTitle(MenuText.Get("pause"), null);
            AddButton(MenuText.Get("continue"), true, () => ResumeRequested?.Invoke());
            AddButton(MenuText.Get("settings"), true, () => ShowSettingsPage(ShowPausePage));
            AddButton(MenuText.Get("exitToMenu"), true, ShowExitConfirm);
            FinishPage();
        }

        private void ShowSettingsPage(Action back)
        {
            redraw = () => ShowSettingsPage(back);
            bool isMain = art.activeSelf;
            BeginPage(isMain ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f), isMain ? new Vector2(130f, 90f) : new Vector2(0f, 150f),
                isMain ? TextAnchor.UpperLeft : TextAnchor.UpperCenter);
            AddTitle(MenuText.Get("settings"), null);
            AddVolumeSlider("volume", GameSettings.Volume, value => GameSettings.Volume = value);
            AddVolumeSlider("musicVolume", GameSettings.MusicVolume, value => GameSettings.MusicVolume = value);
            AddButton($"{MenuText.Get("language")}: {MenuText.CurrentLanguageName}", true, NextLanguage);
            AddNote(MenuText.Get("languageNote"));
            AddButton(MenuText.Get("back"), true, back);
            FinishPage();
        }

        private void OnStartPressed()
        {
            if (hasSave)
                ShowConfirm("confirmNew", () => StartRequested?.Invoke(), ShowMainPage);
            else
                StartRequested?.Invoke();
        }

        private void ShowExitConfirm() => ShowConfirm("confirmExit", () => ExitToMenuRequested?.Invoke(), ShowPausePage);

        private void ShowConfirm(string textKey, Action yes, Action no)
        {
            redraw = () => ShowConfirm(textKey, yes, no);
            bool isMain = art.activeSelf;
            BeginPage(isMain ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f), isMain ? new Vector2(130f, 40f) : new Vector2(0f, 90f),
                isMain ? TextAnchor.UpperLeft : TextAnchor.UpperCenter);
            AddNote(MenuText.Get(textKey), 28, 110f);
            AddButton(MenuText.Get("yes"), true, yes);
            AddButton(MenuText.Get("no"), true, no);
            FinishPage();
        }

        private static void NextLanguage()
        {
            int count = Enum.GetValues(typeof(GameLanguage)).Length;
            GameSettings.Language = (GameLanguage)(((int)GameSettings.Language + 1) % count);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private float nextY;
        private TextAnchor pageAlignment;

        private void BeginPage(Vector2 anchor, Vector2 position, TextAnchor alignment)
        {
            ClearPage();
            pageRoot.anchorMin = pageRoot.anchorMax = anchor;
            pageRoot.pivot = new Vector2(anchor.x, 1f);
            pageRoot.anchoredPosition = position + new Vector2(0f, 200f);
            pageRoot.sizeDelta = new Vector2(860f, 10f);
            pageAlignment = alignment;
            nextY = 0f;
        }

        private void FinishPage()
        {
            foreach (GameObject item in pageItems)
            {
                if (item.TryGetComponent(out Button button) && button.interactable)
                {
                    EventSystem.current?.SetSelectedGameObject(item);
                    return;
                }
            }
        }

        private void ClearPage()
        {
            // Destroy спрацьовує в кінці кадру, а стара кнопка не має отримати ще одне натискання.
            foreach (GameObject item in pageItems)
            {
                item.SetActive(false);
                Destroy(item);
            }

            pageItems.Clear();
            startButtonRect = null;
            quitButtonRect = null;
        }

        private void AddTitle(string title, string subtitle)
        {
            Text titleLabel = AddText(title, 76, 110f, FontStyle.Bold, Warm);
            var shadow = titleLabel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.45f, 0.15f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(4f, -4f);

            if (subtitle != null)
                AddText(subtitle, 30, 56f, FontStyle.Italic, new Color(0.9f, 0.86f, 0.78f));

            nextY -= 24f;
        }

        private void AddNote(string text, int size = 22, float height = 44f) =>
            AddText(text, size, height, FontStyle.Normal, new Color(0.8f, 0.78f, 0.72f));

        private Text AddText(string text, int size, float height, FontStyle style, Color color)
        {
            var label = new GameObject("Text").AddComponent<Text>();
            label.transform.SetParent(pageRoot, false);
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = pageAlignment;
            label.color = color;
            label.text = text;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            PlaceRow(label.rectTransform, 860f, height);
            pageItems.Add(label.gameObject);
            return label;
        }

        private RectTransform AddButton(string text, bool available, Action onClick)
        {
            var image = new GameObject("MenuButton").AddComponent<Image>();
            image.transform.SetParent(pageRoot, false);
            image.sprite = square;
            image.color = Color.white;
            PlaceRow(image.rectTransform, ButtonWidth, ButtonHeight);

            var button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHover;
            colors.selectedColor = ButtonHover;
            colors.pressedColor = ButtonPressed;
            colors.disabledColor = ButtonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.interactable = available;
            button.onClick.AddListener(() =>
            {
                GameContext.Audio?.Play(SoundId.UiClick);
                onClick();
            });

            var label = new GameObject("Label").AddComponent<Text>();
            label.transform.SetParent(image.transform, false);
            label.font = font;
            label.fontSize = 32;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = available ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            label.text = text;
            label.raycastTarget = false;
            Stretch(label.rectTransform);

            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);

            pageItems.Add(image.gameObject);
            nextY -= ButtonSpacing;
            return image.rectTransform;
        }

        private void AddVolumeSlider(string textKey, float current, Action<float> apply)
        {
            AddText($"{MenuText.Get(textKey)}: {Mathf.RoundToInt(current * 100f)}%", 30, 48f, FontStyle.Normal, Color.white);
            var volumeLabel = pageItems[pageItems.Count - 1].GetComponent<Text>();

            var track = new GameObject("VolumeSlider").AddComponent<Image>();
            track.transform.SetParent(pageRoot, false);
            track.sprite = square;
            track.color = ButtonColor;
            PlaceRow(track.rectTransform, ButtonWidth, 26f);

            var fill = new GameObject("Fill").AddComponent<Image>();
            fill.transform.SetParent(track.transform, false);
            fill.sprite = square;
            fill.color = ButtonHover;
            fill.raycastTarget = false;
            Stretch(fill.rectTransform);

            var handle = new GameObject("Handle").AddComponent<Image>();
            handle.transform.SetParent(track.transform, false);
            handle.sprite = square;
            handle.color = Color.white;
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.sizeDelta = new Vector2(18f, 12f);

            var slider = track.gameObject.AddComponent<Slider>();
            slider.targetGraphic = handle;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(current);
            slider.onValueChanged.AddListener(value =>
            {
                apply(value);
                volumeLabel.text = $"{MenuText.Get(textKey)}: {Mathf.RoundToInt(value * 100f)}%";
            });

            pageItems.Add(track.gameObject);
            nextY -= ButtonSpacing + 8f;
        }

        private void PlaceRow(RectTransform rect, float width, float height)
        {
            float pivotX = pageRoot.pivot.x;
            rect.anchorMin = rect.anchorMax = new Vector2(pivotX, 1f);
            rect.pivot = new Vector2(pivotX, 1f);
            rect.anchoredPosition = new Vector2(0f, nextY);
            rect.sizeDelta = new Vector2(width, height);
            nextY -= height;
        }

        private void BuildFrame()
        {
            root = new GameObject("MenuRoot", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            Stretch((RectTransform)root.transform);

            // Сцени за меню немає, тому суцільний фон закриває і порожній кадр, і шкали.
            art = new GameObject("Art", typeof(RectTransform));
            art.transform.SetParent(root.transform, false);
            Stretch((RectTransform)art.transform);

            Image backgroundImage = CreateImage("Background", art.transform, background, Color.white);
            Stretch(backgroundImage.rectTransform);
            darknessImage = CreateImage("Darkness", art.transform, square, Color.clear);
            Stretch(darknessImage.rectTransform);

            // Вогник сидить у єдиному вікні тролейбуса на малюнку.
            var flameAnchor = new Vector2(0.7f, 0.385f);
            glowImage = CreateImage("Glow", art.transform, glow, new Color(1f, 0.62f, 0.25f, 0.7f));
            Place(glowImage.rectTransform, flameAnchor, new Vector2(460f, 460f));
            flameImage = CreateImage("Flame", art.transform, flameFrames[0], new Color(1f, 0.72f, 0.32f));
            Place(flameImage.rectTransform, flameAnchor, new Vector2(64f, 80f));

            var vignette = CreateImage("LeftShade", art.transform, square, new Color(0f, 0f, 0f, 0.35f));
            vignette.rectTransform.anchorMin = Vector2.zero;
            vignette.rectTransform.anchorMax = new Vector2(0.42f, 1f);
            vignette.rectTransform.offsetMin = Vector2.zero;
            vignette.rectTransform.offsetMax = Vector2.zero;

            Image dimImage = CreateImage("Dim", root.transform, square, new Color(0f, 0f, 0f, 0.82f));
            dimImage.raycastTarget = true;
            Stretch(dimImage.rectTransform);
            dim = dimImage.gameObject;

            pageRoot = new GameObject("Page", typeof(RectTransform)).GetComponent<RectTransform>();
            pageRoot.SetParent(root.transform, false);
        }

        private static Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color)
        {
            var image = new GameObject(objectName).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
