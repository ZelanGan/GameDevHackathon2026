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
        System.Action redraw, subMenuBack;
        Font menuFont;
        Image titleGlow;
        Sprite menuShade;

        public void Build(GameManager manager)
        {
            game = manager;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            menuFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Arial" }, 64);

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
            waxFill.color = Art.Sunray;
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
            bool inPlay = game.State != GameState.Title && game.State != GameState.Intro;
            AnimateMenu();
            hudRoot.gameObject.SetActive(inPlay);
            Cursor.visible = game.State != GameState.Playing;
            if (!inPlay) { HandleChoiceKeys(); return; }

            var player = game.Player;
            float fraction = Mathf.Clamp01(player.Wax / player.MaxWax);
            waxFill.fillAmount = fraction;
            waxFill.color = fraction < 0.25f ? Color.Lerp(Art.Blood, Art.Sunray, Mathf.PingPong(Time.time * 3f, 1f)) : Art.Sunray;
            float flicker = 0.9f + Mathf.PerlinNoise(Time.time * 9f, 1f) * 0.25f;
            waxFlame.rectTransform.anchoredPosition = new Vector2(0f, 6f + fraction * (GaugeHeight - 12f) + 26f);
            waxFlame.rectTransform.localScale = Vector3.one * flicker * (player.EffectiveMode == FlameMode.Flare ? 1.35f : player.EffectiveMode == FlameMode.Dim ? 0.6f : 1f);
            waxFlame.color = player.Alive ? (player.SnuffedFor > 0f ? new Color(0.6f, 0.4f, 0.9f) : Art.Flame) : Color.clear;
            waxLabel.text = $"{Mathf.CeilToInt(player.Wax)}/{Mathf.RoundToInt(player.MaxWax)}";
            waxLabel.color = fraction < 0.25f ? Art.Blood : TextColor;
            statLabel.text = Loc.T("Damage", "Шкода") + $" x{player.DamageMultiplier:0.0}   " + Loc.T("Speed", "Швидкість") + $" x{player.SpeedMultiplier:0.0}";

            candlesLabel.text = DayName(game.Day) + "   " + Loc.T("until sunrise", "до світанку") + $": {FormatTime(game.NightLeft)}";
            candlesLabel.color = game.NightLeft < 60f ? new Color(1f, 0.62f, 0.4f) : TextColor;
            rescueLabel.text = Loc.T("Souls saved", "Врятовано душ") + $": {game.Rescued}/{game.SoulsToday}"
                + (game.FollowerCount > 0 ? "   " + Loc.T("With you", "З тобою") + $": {game.FollowerCount}" : "");
            int rekindles = game.RekindlesLeft();
            timeLabel.text = Loc.T("Candles", "Свічки") + $": {game.LitThisNight}/{game.CandlesToday}"
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
            subMenuBack = null;
        }

        void ToggleLanguage()
        {
            Loc.Toggle();
            var screen = redraw;
            HidePanels();
            screen?.Invoke();
        }

        static string DayName(int day)
        {
            switch (day)
            {
                case 1: return Loc.T("Monday", "Понеділок");
                case 2: return Loc.T("Tuesday", "Вівторок");
                case 3: return Loc.T("Wednesday", "Середа");
                case 4: return Loc.T("Thursday", "Четвер");
                default: return Loc.T("Friday", "П'ятниця");
            }
        }

        /// <summary>The day's name over the intro, without a background so the ray stays visible.</summary>
        public void ShowDayCard()
        {
            redraw = ShowDayCard;
            var card = Rect("DayCard", panelRoot);
            Stretch(card);
            Label(card, DayName(game.Day), 84, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1400f, 120f)).color = Art.Flame;
            Label(card, Loc.T($"Day {game.Day} of {GameManager.DaysInWeek}.  Souls to save: {game.SoulsToday}", $"День {game.Day} з {GameManager.DaysInWeek}.  Душ урятувати: {game.SoulsToday}"), 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(1400f, 60f)).color = TextColor;
        }

        // Called from Refresh while the main menu is open: the title's glow breathes like a flame.
        void AnimateMenu()
        {
            if (titleGlow == null) return;
            float flicker = Mathf.PerlinNoise(Time.time * 1.6f, 4f);
            titleGlow.color = new Color(1f, 0.55f, 0.18f, 0.2f + flicker * 0.22f);
        }

        public void ShowTitle()
        {
            HidePanels();
            redraw = ShowTitle;
            var root = Rect("MainMenu", panelRoot);
            Stretch(root);
            var shade = root.gameObject.AddComponent<Image>();
            shade.sprite = MenuShade();
            shade.color = Color.white;

            titleGlow = Rect("Glow", root).gameObject.AddComponent<Image>();
            titleGlow.sprite = Art.Soft;
            titleGlow.raycastTarget = false;
            Place(titleGlow.rectTransform, new Vector2(0f, 0.5f), new Vector2(-120f, 150f), new Vector2(1300f, 520f));

            var title = Label(root, "CANDLEWICK", 118, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(130f, 300f), new Vector2(1100f, 150f));
            title.font = menuFont;
            title.color = Art.Flame;
            var shadow = title.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.45f, 0.14f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(3f, -4f);

            var subtitle = Label(root, Loc.T("The last ray of the sunset. Five working days. Every soul counts.", "Останній промінь заходу. П'ять робочих днів. Кожна душа важлива."), 32, TextAnchor.UpperLeft, new Vector2(0f, 0.5f), new Vector2(136f, 192f), new Vector2(1000f, 90f));
            subtitle.font = menuFont;
            subtitle.color = new Color(0.9f, 0.82f, 0.72f);

            float y = 90f;
            MenuButton(root, Loc.T("Start Game", "Почати гру"), ref y, game.StartRun);
            MenuButton(root, Loc.T("How to Play", "Як грати"), ref y, () => OpenHowToPlay(ShowTitle));
            MenuButton(root, Loc.T("Settings", "Налаштування"), ref y, () => OpenSettings(ShowTitle));
            MenuButton(root, Loc.T("Quit Game", "Вийти з гри"), ref y, game.QuitGame);
        }

        // Dark on the left where the menu sits, fading out to show the building on the right.
        Sprite MenuShade()
        {
            if (menuShade != null) return menuShade;
            const int width = 128;
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < width; x++)
            {
                float a = Mathf.Clamp01(1.05f - x / (width * 0.62f));
                tex.SetPixel(x, 0, new Color(0.03f, 0.02f, 0.06f, a * a * 0.97f));
            }
            tex.Apply();
            menuShade = Sprite.Create(tex, new Rect(0, 0, width, 1), new Vector2(0.5f, 0.5f));
            return menuShade;
        }

        void MenuButton(RectTransform parent, string text, ref float y, UnityEngine.Events.UnityAction onClick)
        {
            var rect = Rect("MenuButton", parent);
            Place(rect, new Vector2(0f, 0.5f), new Vector2(120f, y), new Vector2(520f, 76f));
            y -= 88f;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Art.Square;
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.05f);
            colors.highlightedColor = new Color(1f, 0.66f, 0.28f, 0.4f);
            colors.pressedColor = new Color(1f, 0.75f, 0.35f, 0.7f);
            colors.selectedColor = colors.normalColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(onClick);
            var label = Label(rect, text, 40, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(480f, 70f));
            label.font = menuFont;
            label.color = new Color(1f, 0.94f, 0.84f);
        }

        void OpenHowToPlay(System.Action back)
        {
            HidePanels();
            subMenuBack = back;
            redraw = () => OpenHowToPlay(back);
            var panel = Panel(0.94f);
            var heading = Label(panel, Loc.T("How to Play", "Як грати"), 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 400f), new Vector2(1200f, 100f));
            heading.font = menuFont;
            heading.color = Art.Flame;
            string controls = Loc.T(
                "<b>A / D</b>  Run     <b>W / Space</b>  Jump     <b>S</b>  Drop through a platform     <b>Mouse</b>  Aim     <b>Hold Left Click</b>  Fire (costs light)\n" +
                "<b>Right Click / Shift</b>  Toggle Flare: burns 3 light/s for speed, reach and a wide flame\n" +
                "<b>Q</b>  Toggle Dim: barely fades, but you are slow, weak and half blind\n" +
                "<b>E</b>  Reach out to a soul (5 light) or light a candle (15 light, when no shadows are near it)\n\n" +
                "Find souls with the pale arrow. They follow you, and are saved when you bring them to a lit candle.\n" +
                "Candles are checkpoints: they restore your light, grant a blessing and rekindle you once if you go out.\n" +
                "Ghosts pass through walls, take your souls and can't be killed: fire only scatters them, and they fear candlelight.\n" +
                "The more light you have, the harder you hit. The less you have, the faster you run. Pits cost 15 light.\n" +
                "At sunrise the day ends: souls still with you are saved, the rest stay lost. Each day is harder than the last.",
                "<b>A / D</b>  Біг     <b>W / Пробіл</b>  Стрибок     <b>S</b>  Зістрибнути з платформи     <b>Миша</b>  Приціл     <b>Утримуй ліву кнопку</b>  Вогонь (витрачає світло)\n" +
                "<b>Права кнопка / Shift</b>  Режим «Спалах»: 3 світла/с за швидкість, дальність і широке полум'я\n" +
                "<b>Q</b>  Приглушений режим: майже не згасаєш, але ти повільний, слабкий і погано бачиш\n" +
                "<b>E</b>  Простягнути руку душі (5 світла) або запалити свічку (15 світла, коли поруч немає тіней)\n\n" +
                "Шукай душі за блідою стрілкою. Вони йдуть за тобою й рятуються, коли ти доводиш їх до запаленої свічки.\n" +
                "Свічки це чекпоінти: відновлюють світло, дають благословення й один раз запалюють тебе знову.\n" +
                "Привиди проходять крізь стіни, забирають душі, і їх не вбити: вогонь лише розвіює їх, а світла свічок вони бояться.\n" +
                "Що більше світла, то сильніше ти б'єш. Що менше, то швидше бігаєш. Яма коштує 15 світла.\n" +
                "Зі сходом сонця день закінчується: душі поруч із тобою рятуються, решта лишається заблуканими. Кожен день важчий за попередній.");
            var help = Label(panel, controls, 28, TextAnchor.UpperCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(1800f, 600f));
            help.supportRichText = true;
            help.color = TextColor;
            AddButton(panel, Loc.T("Back  [Esc]", "Назад  [Esc]"), new Vector2(0f, -400f), new Vector2(400f, 80f), () => CloseSubMenu());
        }

        public void OpenSettings(System.Action back)
        {
            HidePanels();
            subMenuBack = back;
            redraw = () => OpenSettings(back);
            var panel = Panel(0.94f);
            var heading = Label(panel, Loc.T("Settings", "Налаштування"), 72, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1200f, 100f));
            heading.font = menuFont;
            heading.color = Art.Flame;

            Label(panel, Loc.T("Sound", "Звук"), 38, TextAnchor.MiddleRight, new Vector2(0.5f, 0.5f), new Vector2(-470f, 130f), new Vector2(300f, 60f));
            var percent = Label(panel, "", 34, TextAnchor.MiddleLeft, new Vector2(0.5f, 0.5f), new Vector2(440f, 130f), new Vector2(140f, 60f));
            VolumeSlider(panel, new Vector2(40f, 130f), percent);

            Label(panel, Loc.T("Language", "Мова"), 38, TextAnchor.MiddleRight, new Vector2(0.5f, 0.5f), new Vector2(-470f, 10f), new Vector2(300f, 60f));
            AddButton(panel, Loc.T("English", "Українська"), new Vector2(-60f, 10f), new Vector2(420f, 76f), ToggleLanguage);

            AddButton(panel, Loc.T("Back  [Esc]", "Назад  [Esc]"), new Vector2(0f, -170f), new Vector2(400f, 80f), () => CloseSubMenu());
        }

        void VolumeSlider(RectTransform parent, Vector2 pos, Text percent)
        {
            var rect = Rect("Volume", parent);
            Place(rect, new Vector2(0.5f, 0.5f), pos, new Vector2(620f, 34f));
            var back = rect.gameObject.AddComponent<Image>();
            back.sprite = Art.Square;
            back.color = new Color(0.2f, 0.17f, 0.22f);

            var fillArea = Rect("Fill Area", rect);
            Stretch(fillArea);
            var fill = Rect("Fill", fillArea).gameObject.AddComponent<Image>();
            fill.sprite = Art.Square;
            fill.color = Art.Flame;
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = Rect("Handle Area", rect);
            Stretch(handleArea);
            var handle = Rect("Handle", handleArea).gameObject.AddComponent<Image>();
            handle.sprite = Art.Disk;
            handle.color = Art.Core;
            handle.rectTransform.sizeDelta = new Vector2(46f, 46f);

            var slider = rect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Sfx.Volume;
            percent.text = $"{Mathf.RoundToInt(slider.value * 100f)}%";
            slider.onValueChanged.AddListener(value =>
            {
                game.Sound.SetVolume(value);
                percent.text = $"{Mathf.RoundToInt(value * 100f)}%";
            });
        }

        public void ShowPause()
        {
            redraw = ShowPause;
            var panel = Panel(0.72f);
            var heading = Label(panel, Loc.T("Paused", "Пауза"), 84, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 230f), new Vector2(1000f, 110f));
            heading.font = menuFont;
            heading.color = Art.Flame;
            AddButton(panel, Loc.T("Resume  [Esc]", "Продовжити  [Esc]"), new Vector2(0f, 70f), new Vector2(520f, 84f), game.Resume);
            AddButton(panel, Loc.T("Settings", "Налаштування"), new Vector2(0f, -30f), new Vector2(520f, 84f), () => OpenSettings(ShowPause));
            AddButton(panel, Loc.T("Exit to Main Menu", "Вийти в головне меню"), new Vector2(0f, -130f), new Vector2(520f, 84f), game.ReturnToMenu);
        }

        /// <summary>Goes back from an open sub-menu; returns false when none is open.</summary>
        public bool CloseSubMenu()
        {
            if (subMenuBack == null) return false;
            var back = subMenuBack;
            HidePanels();
            back();
            return true;
        }

        public bool InSubMenu => subMenuBack != null;

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

        public void ShowSunrise()
        {
            HidePanels();
            redraw = ShowSunrise;
            var panel = Panel(0.72f);
            bool friday = game.Day >= GameManager.DaysInWeek;
            string ending;
            if (friday)
                ending = game.RescuedThisWeek == GameManager.SoulsInWeek
                    ? Loc.T("Every soul saw the sun", "Кожна душа побачила сонце")
                    : game.RescuedThisWeek == 0
                        ? Loc.T("The week ended in empty halls", "Тиждень скінчився в порожніх коридорах")
                        : Loc.T("The working week is over", "Робочий тиждень закінчено");
            else
                ending = DayName(game.Day) + Loc.T(" is over", ": день закінчено");
            Label(panel, ending, 78, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(1600f, 110f)).color = new Color(1f, 0.78f, 0.5f);
            Label(panel, Loc.T("Souls saved", "Врятовано душ") + $": {game.Rescued}/{game.SoulsToday}", 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, Loc.T("Taken by ghosts", "Забрано привидами") + $": {game.LostSouls}", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1000f, 50f)).color = Muted;
            Label(panel, Loc.T("This week", "За тиждень") + $": {game.RescuedThisWeek}/{GameManager.SoulsInWeek}", 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(1000f, 50f)).color = Muted;
            string next = friday
                ? Loc.T("New week  [Enter]", "Новий тиждень  [Enter]")
                : Loc.T("Next: ", "Далі: ") + DayName(game.Day + 1) + "  [Enter]";
            AddButton(panel, next, new Vector2(0f, -130f), new Vector2(560f, 86f), game.AfterSunrise);
            AddButton(panel, Loc.T("Main Menu", "Головне меню"), new Vector2(0f, -225f), new Vector2(560f, 76f), game.ReturnToMenu);
        }

        public void ShowGameOver()
        {
            HidePanels();
            redraw = ShowGameOver;
            var panel = Panel(0.9f);
            Label(panel, Loc.T("The last ray went out", "Останній промінь згас"), 82, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(1400f, 110f)).color = Art.Blood;
            Label(panel, Loc.T("Souls saved this week", "Врятовано душ за тиждень") + $": {game.RescuedThisWeek}/{GameManager.SoulsInWeek}", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, Loc.T("Survived", "Протримався") + $": {FormatTime(game.SurvivalTime)}", 42, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(1000f, 60f)).color = TextColor;
            Label(panel, DayName(game.Day) + "   " + Loc.T("Shadows burned", "Спалено тіней") + $": {game.Kills}", 30, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(1000f, 50f)).color = Muted;
            AddButton(panel, Loc.T("Try Again  [Enter]", "Спробувати ще  [Enter]"), new Vector2(0f, -130f), new Vector2(440f, 86f), game.StartRun);
            AddButton(panel, Loc.T("Main Menu", "Головне меню"), new Vector2(0f, -225f), new Vector2(440f, 76f), game.ReturnToMenu);
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
