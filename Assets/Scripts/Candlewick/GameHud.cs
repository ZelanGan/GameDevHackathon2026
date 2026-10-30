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
        const float GaugeHeight = 300f;
        Text waxLabel, candlesLabel, rescueLabel, modeLabel, statLabel, timeLabel, promptLabel, toastLabel;
        float toastTime;
        int shownBuffHash = -1;
        List<BuffInfo> currentChoices;
        System.Action redraw;

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

            candlesLabel = Label(hudRoot, "", 38, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1200f, 60f));
            rescueLabel = Label(hudRoot, "", 30, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(1200f, 44f));
            rescueLabel.color = new Color(0.55f, 1f, 0.65f);
            timeLabel = Label(hudRoot, "", 26, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-40f, -30f), new Vector2(400f, 110f));
            timeLabel.color = Muted;
            modeLabel = Label(hudRoot, "", 28, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(130f, -68f), new Vector2(600f, 40f));
            statLabel = Label(hudRoot, "", 26, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(130f, -108f), new Vector2(600f, 40f));
            promptLabel = Label(hudRoot, "", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1100f, 50f));
            toastLabel = Label(hudRoot, "", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -135f), new Vector2(1100f, 60f));
            toastLabel.color = Art.Flame;

            buffRow = Rect("Buffs", hudRoot);
            Place(buffRow, new Vector2(0f, 1f), new Vector2(130f, -150f), new Vector2(700f, 56f));

            panelRoot = Rect("Panels", canvas);
            Stretch(panelRoot);
        }

        void BuildGauge()
        {
            var gauge = Rect("WaxGauge", hudRoot);
            // A bright frame keeps the gauge readable against the dark level even when it is nearly empty.
            Place(gauge, new Vector2(0f, 1f), new Vector2(40f, -70f), new Vector2(64f, GaugeHeight));
            var frame = Rect("Frame", gauge).gameObject.AddComponent<Image>();
            frame.sprite = Art.Square;
            frame.color = new Color(0.8f, 0.72f, 0.58f);
            frame.rectTransform.anchorMin = Vector2.zero;
            frame.rectTransform.anchorMax = Vector2.one;
            frame.rectTransform.offsetMin = new Vector2(-4f, -4f);
            frame.rectTransform.offsetMax = new Vector2(4f, 4f);
            var back = Rect("Back", gauge).gameObject.AddComponent<Image>();
            back.sprite = Art.Square;
            back.color = new Color(0.1f, 0.09f, 0.14f);
            back.rectTransform.anchorMin = Vector2.zero;
            back.rectTransform.anchorMax = Vector2.one;
            back.rectTransform.offsetMin = back.rectTransform.offsetMax = Vector2.zero;

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

            waxLabel = Label(hudRoot, "", 30, TextAnchor.UpperCenter, new Vector2(0f, 1f), new Vector2(-8f, -70f - GaugeHeight - 12f), new Vector2(160f, 42f));
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
            waxFlame.rectTransform.anchoredPosition = new Vector2(0f, 6f + fraction * (GaugeHeight - 12f) + 26f);
            waxFlame.rectTransform.localScale = Vector3.one * flicker * (player.EffectiveMode == FlameMode.Flare ? 1.35f : player.EffectiveMode == FlameMode.Dim ? 0.6f : 1f);
            waxFlame.color = player.Alive ? (player.SnuffedFor > 0f ? new Color(0.6f, 0.4f, 0.9f) : Art.Flame) : Color.clear;
            waxLabel.text = $"{Mathf.CeilToInt(player.Wax)}/{Mathf.RoundToInt(player.MaxWax)}";
            waxLabel.color = fraction < 0.25f ? Art.Blood : TextColor;
            statLabel.text = Loc.T("Damage", "Шкода") + $" x{player.DamageMultiplier:0.0}   " + Loc.T("Speed", "Швидкість") + $" x{player.SpeedMultiplier:0.0}";

            candlesLabel.text = game.CurrentGate.IsOpen
                ? Loc.T($"The gate is open! Candles Lit: {game.LitThisNight}", $"Ворота відчинено! Запалено свічок: {game.LitThisNight}")
                : Loc.T($"Candles Lit: {game.LitThisNight}/{GameManager.CandlesToOpen} to open the gate", $"Запалено свічок: {game.LitThisNight}/{GameManager.CandlesToOpen} до відкриття воріт");
            candlesLabel.color = game.CurrentGate.IsOpen ? new Color(0.75f, 0.88f, 1f) : TextColor;
            rescueLabel.text = Loc.T("Rescued", "Врятовано") + $": {game.Rescued}/{GameManager.TotalCaptives}"
                + (game.FollowerCount > 0 ? "   " + Loc.T("With you", "З тобою") + $": {game.FollowerCount}" : "");
            int rekindles = game.RekindlesLeft();
            timeLabel.text = Loc.T("Floor", "Поверх") + $" {game.Depth}/{GameManager.FloorCount}   {FormatTime(game.SurvivalTime)}"
                + (rekindles > 0 ? "\n" + Loc.T("Rekindles", "Відродження") + $": {rekindles}" : "");

            if (player.SnuffedFor > 0f) { modeLabel.text = Loc.T("SNUFFED", "ПРИГАШЕНО"); modeLabel.color = new Color(0.72f, 0.5f, 1f); }
            else if (player.Mode == FlameMode.Flare) { modeLabel.text = Loc.T("FLARE", "СПАЛАХ"); modeLabel.color = Art.Ember; }
            else if (player.Mode == FlameMode.Dim) { modeLabel.text = Loc.T("DIM", "ПРИГЛУШЕНО"); modeLabel.color = Muted; }
            else { modeLabel.text = Loc.T("NORMAL", "ЗВИЧАЙНЕ"); modeLabel.color = TextColor; }

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
            redraw = null;
        }

        void ToggleLanguage()
        {
            Loc.Toggle();
            var screen = redraw;
            HidePanels();
            screen?.Invoke();
        }

        public void ShowTitle()
        {
            redraw = ShowTitle;
            var panel = Panel(1f);
            Label(panel, "CANDLEWICK", 118, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(1400f, 150f)).color = Art.Flame;
            Label(panel, Loc.T("Rescue the captive candles and lead them out of the castle.", "Врятуй полонені свічки й виведи їх із замку."), 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 235f), new Vector2(1200f, 60f)).color = Muted;
            AddButton(panel, Loc.T("Start Game", "Почати гру"), new Vector2(0f, 120f), new Vector2(380f, 86f), game.StartRun);

            string controls = Loc.T(
                "<b>A / D</b>  Run     <b>W / Space</b>  Jump     <b>S</b>  Drop through a platform     <b>Mouse</b>  Aim     <b>Hold Left Click</b>  Breathe fire\n" +
                "<b>Right Click / Shift</b>  Toggle Flare: burns 3 wax/s for speed, light and a roaring wide flame\n" +
                "<b>Q</b>  Toggle Dim: barely melts, but you are slow, weak and half blind\n" +
                "The more wax you have, the harder your flame hits. The less you have, the faster you run.\n" +
                "<b>E</b>  Free a caged candle (5 wax) or light an altar candle (15 wax) once its room is clear\n\n" +
                "Each floor holds 3 caged candles: the green arrow finds them. Freed ones follow you and count as rescued when you pass the gate.\n" +
                "Light 3 altar candles to open the gate, and clear all 3 floors to escape. Falling into a pit costs 15 wax.\n" +
                "Ghosts pass through walls, snuff out your followers and can't be killed: fire only scatters them, and they stay out of candlelight.\n" +
                "Enemies drop wax. Lit candles grant a buff, restore your wax nearby, and rekindle you once if your flame dies.",
                "<b>A / D</b>  Біг     <b>W / Пробіл</b>  Стрибок     <b>S</b>  Зістрибнути з платформи     <b>Миша</b>  Приціл     <b>Утримуй ліву кнопку</b>  Дихати вогнем\n" +
                "<b>Права кнопка / Shift</b>  Режим «Спалах»: 3 воску/с за швидкість, світло й широке полум'я\n" +
                "<b>Q</b>  Приглушений режим: майже не таєш, але ти повільний, слабкий і погано бачиш\n" +
                "Що більше воску, то сильніше б'є полум'я. Що менше воску, то швидше ти бігаєш.\n" +
                "<b>E</b>  Звільнити свічку з клітки (5 воску) або запалити свічку на вівтарі (15 воску), коли кімнату зачищено\n\n" +
                "На кожному поверсі є 3 свічки в клітках: їх показує зелена стрілка. Звільнені йдуть за тобою й вважаються врятованими за воротами.\n" +
                "Запали 3 свічки на вівтарях, щоб відчинити ворота, і пройди всі 3 поверхи, щоб утекти. Падіння в яму коштує 15 воску.\n" +
                "Привиди проходять крізь стіни, гасять твоїх супутників, і їх не вбити: вогонь лише розвіює їх, а світла свічок вони уникають.\n" +
                "З ворогів падає віск. Запалена свічка дає посилення, відновлює віск поруч і один раз відроджує, якщо ти згаснеш.");
            var help = Label(panel, controls, 30, TextAnchor.UpperCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(1760f, 400f));
            help.supportRichText = true;
            help.color = TextColor;
        }

        public void ShowBuffChoice(List<BuffInfo> choices)
        {
            HidePanels();
            currentChoices = choices;
            redraw = () => ShowBuffChoice(choices);
            var panel = Panel(0.82f);
            Label(panel, Loc.T("The candle burns again", "Свічка знову горить"), 62, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1300f, 90f)).color = Art.Flame;
            Label(panel, Loc.T("Choose a blessing", "Обери благословення"), 32, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1000f, 50f)).color = Muted;
            for (int i = 0; i < choices.Count; i++)
            {
                var info = choices[i];
                float x = (i - (choices.Count - 1) / 2f) * 480f;
                var type = info.Type;
                var button = AddButton(panel, "", new Vector2(x, -30f), new Vector2(440f, 300f), () => game.ChooseBuff(type));
                var rect = button.GetComponent<RectTransform>();
                Label(rect, $"{i + 1}", 26, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(100f, 36f)).color = Muted;
                Label(rect, info.Glyph, 64, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(120f, 80f)).color = info.Color;
                Label(rect, info.Name, 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(420f, 50f)).color = TextColor;
                var description = Label(rect, info.Description, 27, TextAnchor.UpperCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -84f), new Vector2(390f, 90f));
                description.color = Muted;
            }
        }

        public void ShowFloorCleared()
        {
            redraw = ShowFloorCleared;
            var panel = Panel(0.86f);
            Label(panel, Loc.T("One floor closer to freedom", "На поверх ближче до волі"), 78, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1400f, 110f)).color = Art.Flame;
            Label(panel, Loc.T($"Floor {game.Depth}/{GameManager.FloorCount} behind you.  Rescued: {game.Rescued}/{GameManager.TotalCaptives}.  Time: {FormatTime(game.SurvivalTime)}", $"Поверх {game.Depth}/{GameManager.FloorCount} позаду.  Врятовано: {game.Rescued}/{GameManager.TotalCaptives}.  Час: {FormatTime(game.SurvivalTime)}"), 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(1400f, 60f)).color = TextColor;
            Label(panel, Loc.T("More ghosts wait below. You keep your buffs and regain some wax.", "Унизу чекає більше привидів. Посилення залишаються, а частина воску відновлюється."), 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(1400f, 50f)).color = Muted;
            AddButton(panel, Loc.T("Descend  [Enter]", "Спуститися  [Enter]"), new Vector2(0f, -100f), new Vector2(440f, 86f), game.NextFloor);
        }

        public void ShowEscaped()
        {
            redraw = ShowEscaped;
            var panel = Panel(0.9f);
            string ending = game.Rescued == GameManager.TotalCaptives
                ? Loc.T("You saved every candle", "Ти врятував усі свічки")
                : game.Rescued == 0
                    ? Loc.T("You escaped, alone", "Ти втік, але сам")
                    : Loc.T("You escaped the castle", "Ти втік із замку");
            Label(panel, ending, 82, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(1400f, 110f)).color = new Color(0.75f, 0.88f, 1f);
            Label(panel, Loc.T("Rescued", "Врятовано") + $": {game.Rescued}/{GameManager.TotalCaptives}", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, Loc.T("Escape time", "Час утечі") + $": {FormatTime(game.SurvivalTime)}", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, Loc.T("Shadows burned", "Спалено тіней") + $": {game.Kills}", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(1000f, 50f)).color = Muted;
            AddButton(panel, Loc.T("Play Again  [Enter]", "Грати знову  [Enter]"), new Vector2(0f, -130f), new Vector2(440f, 86f), game.StartRun);
        }

        public void ShowGameOver()
        {
            HidePanels();
            redraw = ShowGameOver;
            var panel = Panel(0.9f);
            Label(panel, Loc.T("Your flame went out", "Твоє полум'я згасло"), 82, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(1400f, 110f)).color = Art.Blood;
            Label(panel, Loc.T("Rescued", "Врятовано") + $": {game.Rescued}/{GameManager.TotalCaptives}", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, Loc.T("Survived", "Протримався") + $": {FormatTime(game.SurvivalTime)}", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, Loc.T("Reached floor", "Досягнутий поверх") + $" {game.Depth}/{GameManager.FloorCount}   " + Loc.T("Shadows burned", "Спалено тіней") + $": {game.Kills}", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(1000f, 50f)).color = Muted;
            AddButton(panel, Loc.T("Try Again  [Enter]", "Спробувати ще  [Enter]"), new Vector2(0f, -130f), new Vector2(440f, 86f), game.StartRun);
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

            // Every menu screen offers the switch, named in the language it switches to.
            var language = AddButton(rect, Loc.T("Українська", "English"), Vector2.zero, new Vector2(260f, 64f), ToggleLanguage);
            Place(language.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(260f, 64f));
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
