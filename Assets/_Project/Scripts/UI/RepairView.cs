using System;
using NightTrolleybus.Core;
using NightTrolleybus.Vehicle.Repair;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Вікно міні-гри ремонту: малює стан гри й передає їй натискання мишею.</summary>
    public class RepairView : MonoBehaviour
    {
        private const int CanvasOrder = 20;
        private const float FlashSeconds = 0.35f;
        private const float DoneSeconds = 0.7f;
        private const float ShakePixels = 10f;
        private const float PanelWidth = 860f;
        private const float PanelHeight = 560f;
        private const float TrackWidth = 700f;

        private static readonly Color PanelColor = new Color(0.1f, 0.09f, 0.1f, 0.97f);
        private static readonly Color CellColor = new Color(0.17f, 0.15f, 0.16f);
        private static readonly Color TextColor = new Color(1f, 0.93f, 0.8f);
        private static readonly Color HintColor = new Color(0.75f, 0.7f, 0.62f);
        private static readonly Color GoodColor = new Color(0.4f, 0.9f, 0.5f);
        private static readonly Color BadColor = new Color(1f, 0.3f, 0.25f);
        private static readonly Color AmberColor = new Color(1f, 0.75f, 0.25f);
        private static readonly Color MetalColor = new Color(0.68f, 0.7f, 0.74f);
        private static readonly Color UnlitColor = new Color(0.25f, 0.22f, 0.22f);
        private static readonly Color[] WireColors =
        {
            new Color(0.9f, 0.27f, 0.22f),
            new Color(0.98f, 0.8f, 0.25f),
            new Color(0.3f, 0.6f, 0.95f)
        };

        [SerializeField] private Sprite frame;
        [SerializeField] private Sprite square;
        [SerializeField] private Font font;

        private RepairMinigame minigame;
        private GameObject root;
        private RectTransform panel;
        private Image flash;
        private Text title;
        private Text message;
        private Action refresh;
        private int shownMistakes;
        private float flashTimer;

        public void Show(RepairMinigame game, string titleText, Action close)
        {
            Hide();
            minigame = game;
            shownMistakes = 0;
            flashTimer = 0f;
            BuildWindow(titleText, close);

            switch (game)
            {
                case PolesMinigame poles:
                    BuildPoles(poles);
                    break;
                case FuseMinigame fuse:
                    BuildFuse(fuse);
                    break;
                case WiresMinigame wires:
                    BuildWires(wires);
                    break;
            }

            flash.transform.SetAsLastSibling();
            refresh?.Invoke();
        }

        public void Hide()
        {
            minigame = null;
            refresh = null;
            if (root != null)
                Destroy(root);
        }

        /// <summary>Ненадовго лишає вікно на екрані, щоб гравець побачив результат ремонту.</summary>
        public void ShowDone()
        {
            refresh?.Invoke();
            minigame = null;
            refresh = null;
            flash.color = Color.clear;
            panel.anchoredPosition = Vector2.zero;
            title.color = GoodColor;
            message.color = GoodColor;
            message.text = Loc.T("Відремонтовано!");
            foreach (Button button in root.GetComponentsInChildren<Button>())
                button.interactable = false;
            Destroy(root, DoneSeconds);
        }

        private void Update()
        {
            if (minigame == null)
                return;

            if (minigame.Mistakes != shownMistakes)
            {
                shownMistakes = minigame.Mistakes;
                flashTimer = FlashSeconds;
                GameContext.Audio?.Play(SoundId.Spark);
            }

            refresh?.Invoke();
            UpdateFlash();
        }

        private void UpdateFlash()
        {
            flashTimer = Mathf.Max(0f, flashTimer - Time.deltaTime);
            float strength = flashTimer / FlashSeconds;
            flash.color = new Color(BadColor.r, BadColor.g, BadColor.b, strength * 0.25f);
            panel.anchoredPosition = new Vector2(Mathf.Sin(flashTimer * 70f) * ShakePixels * strength, 0f);
        }

        private void BuildWindow(string titleText, Action close)
        {
            root = new GameObject("RepairWindow", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasOrder;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            Image background = CreateImage("Panel", root.transform, frame, PanelColor, new Vector2(0f, 0f), new Vector2(PanelWidth, PanelHeight));
            background.type = Image.Type.Sliced;
            background.raycastTarget = true;
            panel = background.rectTransform;
            // Рамка всередині напівпрозора, а за вікном не має відволікати салон.
            CreateImage("Backing", panel, square, PanelColor, Vector2.zero, new Vector2(PanelWidth - 12f, PanelHeight - 12f));

            title = CreateText("Title", panel, 40, TextColor, new Vector2(0f, 228f), new Vector2(PanelWidth - 40f, 56f));
            title.text = titleText;
            message = CreateText("Message", panel, 26, HintColor, new Vector2(0f, 172f), new Vector2(PanelWidth - 40f, 44f));

            Button leave = CreateButton("Leave", panel, new Vector2(0f, -240f), new Vector2(280f, 54f));
            Text leaveLabel = CreateText("Label", leave.transform, 24, HintColor, Vector2.zero, new Vector2(280f, 54f));
            leaveLabel.text = Loc.T("Esc: відійти");
            leave.onClick.AddListener(() => close());

            flash = CreateImage("Flash", panel, square, Color.clear, Vector2.zero, new Vector2(PanelWidth, PanelHeight));
        }

        private void BuildPoles(PolesMinigame game)
        {
            const float trackY = 56f;
            CreateImage("Track", panel, square, CellColor, new Vector2(0f, trackY), new Vector2(TrackWidth, 70f));
            float zoneWidth = PolesMinigame.WireZone * 2f * TrackWidth;
            Image zone = CreateImage("Zone", panel, square, new Color(GoodColor.r, GoodColor.g, GoodColor.b, 0.35f), new Vector2(0f, trackY), new Vector2(zoneWidth, 70f));
            CreateImage("Wire", panel, square, AmberColor, new Vector2(0f, trackY), new Vector2(6f, 110f));

            RectTransform pole = CreateImage("Pole", panel, square, MetalColor, new Vector2(0f, trackY - 30f), new Vector2(10f, 76f)).rectTransform;
            CreateImage("PoleHead", pole, square, Color.white, new Vector2(0f, 38f), new Vector2(38f, 10f));

            Image[] lamps = CreateLamps(PolesMinigame.RequiredHits, -48f);

            Button connect = CreateButton("Connect", panel, new Vector2(0f, -140f), new Vector2(420f, 76f));
            Text connectLabel = CreateText("Label", connect.transform, 30, TextColor, Vector2.zero, new Vector2(420f, 76f));
            connectLabel.text = Loc.T("Підключити (Пробіл)");
            connect.onClick.AddListener(() => game.Press(1));

            refresh = () =>
            {
                pole.anchoredPosition = new Vector2((game.Marker - 0.5f) * TrackWidth, trackY - 30f);
                zone.color = Mathf.Abs(game.Marker - 0.5f) <= PolesMinigame.WireZone
                    ? new Color(GoodColor.r, GoodColor.g, GoodColor.b, 0.65f)
                    : new Color(GoodColor.r, GoodColor.g, GoodColor.b, 0.3f);
                for (int i = 0; i < lamps.Length; i++)
                    lamps[i].color = i < game.Hits ? GoodColor : UnlitColor;
                ShowMessage(flashTimer > 0f, "Промах! Спочатку.", "Тисніть, коли штанга над дротом. Тричі поспіль.");
            };
        }

        private void BuildFuse(FuseMinigame game)
        {
            Image display = CreateImage("Display", panel, square, Color.black, new Vector2(0f, 110f), new Vector2(380f, 64f));
            Text required = CreateText("Required", display.transform, 32, AmberColor, Vector2.zero, new Vector2(380f, 64f));

            for (int i = 0; i < game.ChoiceCount; i++)
            {
                int choice = i + 1;
                RectTransform cell = CreateChoice(i, choice, game);
                CreateImage("CapTop", cell, square, MetalColor, new Vector2(0f, 72f), new Vector2(54f, 20f));
                CreateImage("Glass", cell, square, new Color(0.62f, 0.8f, 0.88f, 0.75f), new Vector2(0f, 20f), new Vector2(54f, 84f));
                CreateImage("Filament", cell, square, AmberColor, new Vector2(0f, 20f), new Vector2(2f + i * 3f, 84f));
                CreateImage("CapBottom", cell, square, MetalColor, new Vector2(0f, -32f), new Vector2(54f, 20f));
                Text amps = CreateText("Amps", cell, 28, TextColor, new Vector2(0f, -70f), new Vector2(180f, 36f));
                amps.text = Loc.F("{0} А", game.AmpsOf(i));
            }

            refresh = () =>
            {
                required.text = Loc.F("Потрібен на {0} А", game.Required);
                ShowMessage(flashTimer > 0f, "Іскра! Не той запобіжник.", "Виберіть запобіжник на потрібний струм.");
            };
        }

        private void BuildWires(WiresMinigame game)
        {
            int count = game.WireCount;
            var chips = new Image[count];
            for (int i = 0; i < count; i++)
            {
                int wire = game.WireAt(i);
                chips[i] = CreateImage("Chip", panel, square, WireColors[wire - 1], new Vector2((i - (count - 1) / 2f) * 92f, 116f), new Vector2(56f, 56f));
                Text number = CreateText("Number", chips[i].transform, 30, Color.white, Vector2.zero, new Vector2(56f, 56f));
                number.text = wire.ToString();
            }

            var bridges = new Image[count];
            for (int i = 0; i < count; i++)
            {
                RectTransform cell = CreateChoice(i, i + 1, game);
                Color color = WireColors[i];
                CreateImage("Top", cell, square, color, new Vector2(0f, 62f), new Vector2(18f, 44f));
                CreateImage("Bottom", cell, square, color, new Vector2(0f, -40f), new Vector2(18f, 44f));
                CreateImage("ClampTop", cell, square, MetalColor, new Vector2(0f, 38f), new Vector2(28f, 10f));
                CreateImage("ClampBottom", cell, square, MetalColor, new Vector2(0f, -16f), new Vector2(28f, 10f));
                bridges[i] = CreateImage("Bridge", cell, square, color, new Vector2(0f, 11f), new Vector2(18f, 44f));
            }

            refresh = () =>
            {
                for (int i = 0; i < count; i++)
                {
                    bridges[i].enabled = game.IsConnected(i + 1);
                    Color color = WireColors[game.WireAt(i) - 1];
                    chips[i].color = i < game.Connected ? Color.Lerp(color, Color.black, 0.65f) : color;
                    chips[i].rectTransform.localScale = Vector3.one * (i == game.Connected ? 1.2f : 1f);
                }

                ShowMessage(flashTimer > 0f, "Не той провід! Спочатку.", "З'єднайте проводи в указаному порядку.");
            };
        }

        private void ShowMessage(bool isError, string error, string hint)
        {
            message.text = Loc.T(isError ? error : hint);
            message.color = isError ? BadColor : HintColor;
        }

        /// <summary>Комірка з варіантом: натискається мишею або клавішею зі своїм номером.</summary>
        private RectTransform CreateChoice(int index, int choice, RepairMinigame game)
        {
            Button button = CreateButton("Choice" + choice, panel, new Vector2((index - 1) * 220f, -74f), new Vector2(200f, 230f));
            button.onClick.AddListener(() => game.Press(choice));
            var cell = (RectTransform)button.transform;
            Text key = CreateText("Key", cell, 24, HintColor, new Vector2(-76f, 92f), new Vector2(40f, 36f));
            key.text = choice.ToString();
            return cell;
        }

        private Image[] CreateLamps(int count, float y)
        {
            var lamps = new Image[count];
            for (int i = 0; i < count; i++)
                lamps[i] = CreateImage("Lamp", panel, square, UnlitColor, new Vector2((i - (count - 1) / 2f) * 64f, y), new Vector2(40f, 40f));
            return lamps;
        }

        private Button CreateButton(string objectName, Transform parent, Vector2 position, Vector2 size)
        {
            Image image = CreateImage(objectName, parent, frame, CellColor, position, size);
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.5f, 1.4f, 1.2f);
            button.colors = colors;
            return button;
        }

        private Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color, Vector2 position, Vector2 size)
        {
            var image = new GameObject(objectName).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.anchoredPosition = position;
            return image;
        }

        private Text CreateText(string objectName, Transform parent, int size, Color color, Vector2 position, Vector2 box)
        {
            var label = new GameObject(objectName).AddComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = font;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = box;
            label.rectTransform.anchoredPosition = position;
            return label;
        }
    }
}
