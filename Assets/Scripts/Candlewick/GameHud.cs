using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Candlewick
{
    /// <summary>Builds the whole uGUI layer in code: wax gauge, counters, buff icons and menu screens.</summary>
    public class GameHud : MonoBehaviour
    {
        static readonly Color PanelColor = new Color(0.025f, 0.02f, 0.05f, 0.95f);
        static readonly Color TextColor = new Color(0.92f, 0.85f, 0.74f);
        static readonly Color Muted = new Color(0.62f, 0.58f, 0.68f);

        GameManager game;
        Font font;
        RectTransform canvas, hudRoot, buffRow, panelRoot;
        Image waxFill, waxFlame;
        Text waxLabel, candlesLabel, modeLabel, timeLabel, promptLabel, toastLabel;
        float toastTime;
        int shownBuffHash = -1;
        List<BuffInfo> currentChoices;

        public void Build(GameManager manager)
        {
            game = manager;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("HUD");
            var c = canvasGo.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvas = canvasGo.GetComponent<RectTransform>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem").AddComponent<EventSystem>();
                es.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            hudRoot = Rect("Gameplay", canvas);
            Stretch(hudRoot);
            BuildGauge();

            candlesLabel = Label(hudRoot, "", 38, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 60f));
            timeLabel = Label(hudRoot, "", 26, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(400f, 110f));
            timeLabel.color = Muted;
            modeLabel = Label(hudRoot, "", 28, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(210f, 30f), new Vector2(600f, 40f));
            promptLabel = Label(hudRoot, "", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1100f, 50f));
            toastLabel = Label(hudRoot, "", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1100f, 60f));
            toastLabel.color = Art.Flame;

            buffRow = Rect("Buffs", hudRoot);
            Place(buffRow, new Vector2(0f, 0f), new Vector2(210f, 80f), new Vector2(700f, 56f));

            panelRoot = Rect("Panels", canvas);
            Stretch(panelRoot);
        }

        void BuildGauge()
        {
            var gauge = Rect("WaxGauge", hudRoot);
            Place(gauge, new Vector2(0f, 0f), new Vector2(80f, 215f), new Vector2(64f, 340f));
            var back = gauge.gameObject.AddComponent<Image>();
            back.sprite = Art.Square;
            back.color = new Color(0.1f, 0.09f, 0.14f, 0.92f);

            waxFill = Rect("Fill", gauge).gameObject.AddComponent<Image>();
            waxFill.sprite = Art.Square;
            waxFill.type = Image.Type.Filled;
            waxFill.fillMethod = Image.FillMethod.Vertical;
            waxFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            waxFill.color = Art.Wax;
            var fillRect = waxFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(6f, 6f);
            fillRect.offsetMax = new Vector2(-6f, -6f);

            waxFlame = Rect("Flame", gauge).gameObject.AddComponent<Image>();
            waxFlame.sprite = Art.Soft;
            waxFlame.color = Art.Flame;
            waxFlame.rectTransform.sizeDelta = new Vector2(58f, 84f);
            waxFlame.rectTransform.anchorMin = waxFlame.rectTransform.anchorMax = new Vector2(0.5f, 0f);

            waxLabel = Label(hudRoot, "", 30, TextAnchor.UpperCenter, new Vector2(0f, 0f), new Vector2(32f, 150f), new Vector2(160f, 42f));
        }

        public void Refresh()
        {
            bool inPlay = game.State != GameState.Title;
            hudRoot.gameObject.SetActive(inPlay);
            Cursor.visible = game.State != GameState.Playing;
            if (!inPlay) { HandleChoiceKeys(); return; }

            var player = game.Player;
            float fraction = Mathf.Clamp01(player.Wax / player.MaxWax);
            waxFill.fillAmount = fraction;
            waxFill.color = fraction < 0.25f ? Color.Lerp(Art.Blood, Art.Wax, Mathf.PingPong(Time.time * 3f, 1f)) : Art.Wax;
            float flicker = 0.9f + Mathf.PerlinNoise(Time.time * 9f, 1f) * 0.25f;
            waxFlame.rectTransform.anchoredPosition = new Vector2(0f, 6f + fraction * 328f + 26f);
            waxFlame.rectTransform.localScale = Vector3.one * flicker * (player.EffectiveMode == FlameMode.Flare ? 1.35f : player.EffectiveMode == FlameMode.Dim ? 0.6f : 1f);
            waxFlame.color = player.Alive ? (player.SnuffedFor > 0f ? new Color(0.6f, 0.4f, 0.9f) : Art.Flame) : Color.clear;
            waxLabel.text = $"{Mathf.CeilToInt(player.Wax)}/{Mathf.RoundToInt(player.MaxWax)}";

            candlesLabel.text = game.CurrentGate.IsOpen
                ? $"The gate is open! Candles Lit: {game.LitThisNight}"
                : $"Candles Lit: {game.LitThisNight}/{GameManager.CandlesToOpen} to open the gate";
            candlesLabel.color = game.CurrentGate.IsOpen ? new Color(0.75f, 0.88f, 1f) : TextColor;
            int rekindles = game.RekindlesLeft();
            timeLabel.text = $"Floor {game.Depth}/{GameManager.FloorCount}   {FormatTime(game.SurvivalTime)}" + (rekindles > 0 ? $"\nRekindles: {rekindles}" : "");

            if (player.SnuffedFor > 0f) { modeLabel.text = "SNUFFED"; modeLabel.color = new Color(0.72f, 0.5f, 1f); }
            else if (player.Mode == FlameMode.Flare) { modeLabel.text = "FLARE"; modeLabel.color = Art.Ember; }
            else if (player.Mode == FlameMode.Dim) { modeLabel.text = "DIM"; modeLabel.color = Muted; }
            else { modeLabel.text = "NORMAL"; modeLabel.color = TextColor; }

            promptLabel.text = game.InteractPrompt() ?? "";

            toastTime -= Time.deltaTime;
            toastLabel.text = toastTime > 0f ? toastLabel.text : "";

            RefreshBuffs();
            HandleChoiceKeys();
        }

        void HandleChoiceKeys()
        {
            if (game.State != GameState.Choosing || currentChoices == null || Keyboard.current == null) return;
            var kb = Keyboard.current;
            int pick = kb.digit1Key.wasPressedThisFrame ? 0 : kb.digit2Key.wasPressedThisFrame ? 1 : kb.digit3Key.wasPressedThisFrame ? 2 : -1;
            if (pick >= 0 && pick < currentChoices.Count) game.ChooseBuff(currentChoices[pick].Type);
        }

        void RefreshBuffs()
        {
            int hash = 17;
            foreach (var pair in game.Buffs.Stacks) hash = hash * 31 + (int)pair.Key * 7 + pair.Value;
            if (hash == shownBuffHash) return;
            shownBuffHash = hash;

            for (int i = buffRow.childCount - 1; i >= 0; i--) Destroy(buffRow.GetChild(i).gameObject);
            float x = 0f;
            foreach (var info in BuffSet.All)
            {
                int level = game.Buffs.Level(info.Type);
                if (level == 0) continue;
                var icon = Rect(info.Name, buffRow);
                icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
                icon.pivot = new Vector2(0f, 0.5f);
                icon.anchoredPosition = new Vector2(x, 0f);
                icon.sizeDelta = new Vector2(52f, 52f);
                var image = icon.gameObject.AddComponent<Image>();
                image.sprite = Art.Square;
                image.color = new Color(info.Color.r * 0.3f, info.Color.g * 0.3f, info.Color.b * 0.3f, 0.95f);
                var text = Label(icon, level > 1 ? $"{info.Glyph}{level}" : info.Glyph, 28, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
                text.color = info.Color;
                x += 60f;
            }
        }

        public void Toast(string message)
        {
            toastLabel.text = message;
            toastTime = 2.6f;
        }

        // --- Screens ---------------------------------------------------------------------------

        public void HidePanels()
        {
            for (int i = panelRoot.childCount - 1; i >= 0; i--) Destroy(panelRoot.GetChild(i).gameObject);
            currentChoices = null;
        }

        public void ShowTitle()
        {
            var panel = Panel(1f);
            Label(panel, "CANDLEWICK", 118, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1400f, 150f)).color = Art.Flame;
            Label(panel, "Escape the castle. The ghosts will not stop hunting you.", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 235f), new Vector2(1200f, 60f)).color = Muted;
            AddButton(panel, "Start Game", new Vector2(0f, 120f), new Vector2(380f, 86f), game.StartRun);

            const string controls =
                "<b>WASD</b>  Move          <b>Mouse</b>  Aim          <b>Hold Left Click</b>  Breathe fire\n" +
                "<b>Right Click / Shift</b>  Toggle Flare: burns 3 wax/s for speed, light and a roaring wide flame\n" +
                "<b>Q / Space</b>  Toggle Dim: barely melts, but you are slow, weak and half blind\n" +
                "<b>E</b>  Share 15 wax with an unlit candle once its room is clear\n\n" +
                "Light 3 candles to open the gate on each floor, and get through all 3 floors to escape.\n" +
                "Ghosts pass through walls and can't be killed: fire only scatters them, and they stay out of candlelight.\n" +
                "Enemies drop wax. Lit candles grant a buff, stop your melting nearby, and rekindle you once if your flame dies.";
            var help = Label(panel, controls, 30, TextAnchor.UpperCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(1500f, 400f));
            help.supportRichText = true;
            help.color = TextColor;
        }

        public void ShowBuffChoice(List<BuffInfo> choices)
        {
            HidePanels();
            currentChoices = choices;
            var panel = Panel(0.82f);
            Label(panel, "The candle burns again", 62, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1300f, 90f)).color = Art.Flame;
            Label(panel, "Choose a blessing", 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1000f, 50f)).color = Muted;
            for (int i = 0; i < choices.Count; i++)
            {
                var info = choices[i];
                float x = (i - (choices.Count - 1) / 2f) * 480f;
                var type = info.Type;
                var button = AddButton(panel, "", new Vector2(x, -30f), new Vector2(440f, 300f), () => game.ChooseBuff(type));
                var rect = button.GetComponent<RectTransform>();
                Label(rect, $"{i + 1}", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(100f, 40f)).color = Muted;
                Label(rect, info.Glyph, 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 45f), new Vector2(120f, 80f)).color = info.Color;
                Label(rect, info.Name, 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(420f, 50f)).color = TextColor;
                var description = Label(rect, info.Description, 27, TextAnchor.UpperCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(390f, 80f));
                description.color = Muted;
            }
        }

        public void ShowFloorCleared()
        {
            var panel = Panel(0.86f);
            Label(panel, "One floor closer to freedom", 78, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1400f, 110f)).color = Art.Flame;
            Label(panel, $"Floor {game.Depth}/{GameManager.FloorCount} behind you.  Candles saved: {game.TotalSaved}.  Time: {FormatTime(game.SurvivalTime)}", 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(1400f, 60f)).color = TextColor;
            Label(panel, "More ghosts wait below. You keep your buffs and regain some wax.", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1400f, 50f)).color = Muted;
            AddButton(panel, "Descend  [Enter]", new Vector2(0f, -100f), new Vector2(440f, 86f), game.NextFloor);
        }

        public void ShowEscaped()
        {
            var panel = Panel(0.9f);
            Label(panel, "You escaped the castle", 82, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(1400f, 110f)).color = new Color(0.75f, 0.88f, 1f);
            Label(panel, $"Candles saved: {game.TotalSaved}", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, $"Escape time: {FormatTime(game.SurvivalTime)}", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, $"Shadows burned: {game.Kills}", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(1000f, 50f)).color = Muted;
            AddButton(panel, "Play Again  [Enter]", new Vector2(0f, -130f), new Vector2(440f, 86f), game.StartRun);
        }

        public void ShowGameOver()
        {
            HidePanels();
            var panel = Panel(0.9f);
            Label(panel, "Your flame went out", 82, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(1400f, 110f)).color = Art.Blood;
            Label(panel, $"Candles saved: {game.TotalSaved}", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, $"Survived: {FormatTime(game.SurvivalTime)}", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, $"Reached floor {game.Depth}/{GameManager.FloorCount}   Shadows burned: {game.Kills}", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(1000f, 50f)).color = Muted;
            AddButton(panel, "Try Again  [Enter]", new Vector2(0f, -130f), new Vector2(440f, 86f), game.StartRun);
        }

        static string FormatTime(float seconds) => $"{Mathf.FloorToInt(seconds / 60f)}:{Mathf.FloorToInt(seconds % 60f):00}";

        // --- uGUI helpers ----------------------------------------------------------------------

        RectTransform Panel(float alpha)
        {
            var rect = Rect("Panel", panelRoot);
            Stretch(rect);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Art.Square;
            image.color = new Color(PanelColor.r, PanelColor.g, PanelColor.b, alpha);
            return rect;
        }

        Button AddButton(RectTransform parent, string text, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var rect = Rect("Button", parent);
            Place(rect, new Vector2(0.5f, 0.5f), pos, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Art.Square;
            image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.17f, 0.12f, 0.15f);
            colors.highlightedColor = new Color(0.62f, 0.27f, 0.08f);
            colors.pressedColor = new Color(0.9f, 0.45f, 0.1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            if (text.Length > 0)
                Label(rect, text, 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size).color = TextColor;
            return button;
        }

        Text Label(RectTransform parent, string value, int size, TextAnchor anchor, Vector2 anchorPoint, Vector2 pos, Vector2 box)
        {
            var rect = Rect("Label", parent);
            Place(rect, anchorPoint, pos, box);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = TextColor;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        // The anchor doubles as the pivot, so positions read as offsets from that screen edge.
        static void Place(RectTransform rect, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
