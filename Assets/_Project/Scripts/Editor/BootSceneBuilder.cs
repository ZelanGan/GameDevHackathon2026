using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Flame;
using NightTrolleybus.Items;
using NightTrolleybus.Narrative;
using NightTrolleybus.Passengers;
using NightTrolleybus.Route;
using NightTrolleybus.Save;
using NightTrolleybus.UI;
using NightTrolleybus.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static NightTrolleybus.EditorTools.BuilderTools;

namespace NightTrolleybus.EditorTools
{
    /// <summary>Збирає сцену Boot із системами й загальним інтерфейсом та сцену RouteMap.</summary>
    public static class BootSceneBuilder
    {
        private const string BootPath = Root + "/Scenes/Boot.unity";
        private const string MapPath = Root + "/Scenes/RouteMap.unity";
        private const string BootMarker = "GameRoot";
        private const string MapMarker = "RouteMapRoot";

        [MenuItem("Night Trolleybus/Scenes/Build Boot Scene")]
        public static void BuildBoot()
        {
            if (IsAlreadyBuilt(BootPath, BootMarker))
                return;

            // Нова сцена вивантажує незбережені асети, тому створюємо її до них.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var noticeRaised = Channel<StringEventChannel>("BusNoticeRaised");
            var flameChanged = Channel<FloatEventChannel>("FlameChanged");
            var flameState = Channel<FlameStateEventChannel>("FlameStateChanged");
            var extinguished = Channel<VoidEventChannel>("FlameExtinguished");
            var temperatureChanged = Channel<FloatEventChannel>("TemperatureChanged");
            var powerChanged = Channel<FloatEventChannel>("PowerChanged");
            var fearChanged = Channel<FloatEventChannel>("FearChanged");
            var inBusChanged = Channel<BoolEventChannel>("InBusChanged");
            var heaterRunning = Channel<BoolEventChannel>("HeaterRunningChanged");
            var doorsOpen = Channel<BoolEventChannel>("DoorsOpenChanged");
            var cabinLightRunning = Channel<BoolEventChannel>("CabinLightRunningChanged");
            var inventoryChanged = Channel<VoidEventChannel>("InventoryChanged");
            var gameStateChanged = Channel<StringEventChannel>("GameStateChanged");

            var root = new GameObject(BootMarker);

            var flameObject = Child("FlameSystem", root);
            Wire(flameObject.AddComponent<FlameSystem>(),
                ("config", FlameSettings), ("flameChanged", flameChanged), ("flameStateChanged", flameState),
                ("flameExtinguished", extinguished), ("temperatureChanged", temperatureChanged),
                ("fearChanged", fearChanged), ("inBusChanged", inBusChanged),
                ("fuelReceived", Channel<VoidEventChannel>("FuelReceived")));
            Wire(Child("FearSystem", root).AddComponent<FearSystem>(), ("fearChanged", fearChanged));
            Wire(Child("TemperatureSystem", root).AddComponent<TemperatureSystem>(),
                ("config", GetOrCreate<TemperatureConfig>(Root + "/Data/Config/TemperatureConfig.asset")),
                ("temperatureChanged", temperatureChanged), ("doorsOpenChanged", doorsOpen));
            Wire(Child("PowerSystem", root).AddComponent<PowerSystem>(),
                ("config", GetOrCreate<PowerConfig>(Root + "/Data/Config/PowerConfig.asset")), ("powerChanged", powerChanged),
                ("powerDepleted", Channel<VoidEventChannel>("PowerDepleted")), ("powerRestored", Channel<VoidEventChannel>("PowerRestored")));
            Wire(Child("VehicleFaults", root).AddComponent<VehicleFaults>(), ("faultsChanged", Channel<VoidEventChannel>("FaultsChanged")));

            var heater = Child("Heater", root).AddComponent<Heater>();
            SetFloat(heater, "powerPerSecond", 0.7f);
            Wire(heater, ("runningChanged", heaterRunning));
            var cabinLight = Child("CabinLight", root).AddComponent<CabinLight>();
            SetFloat(cabinLight, "powerPerSecond", 0.25f);
            Wire(cabinLight, ("runningChanged", cabinLightRunning));
            var doors = Child("DoorsWindows", root).AddComponent<DoorsWindows>();
            Wire(doors, ("doorsOpenChanged", doorsOpen));

            var inventory = Child("Inventory", root).AddComponent<Inventory>();
            Wire(inventory, ("inventoryChanged", inventoryChanged), ("noticeRaised", noticeRaised));
            var loadout = Child("PlayerLoadout", root).AddComponent<PlayerLoadout>();
            Wire(loadout, ("startingWeapon", Pipe));
            var roster = Child("PassengerRoster", root).AddComponent<PassengerRoster>();
            var route = Child("RouteManager", root).AddComponent<RouteManager>();

            var database = GetOrCreate<GameDatabase>(Root + "/Data/GameDatabase.asset");
            WireArray(database, "items", new Object[] { Match, Candle, Battery, Ticket });
            WireArray(database, "weapons", new Object[] { Pipe, Crowbar, FireAxe });
            WireArray(database, "passengers", BusSceneBuilder.PassengerPool());
            AssetDatabase.SaveAssets();

            var saveSystem = Child("SaveSystem", root).AddComponent<SaveSystem>();
            Wire(saveSystem,
                ("database", database), ("route", route), ("inventory", inventory), ("loadout", loadout),
                ("roster", roster), ("heater", heater), ("cabinLight", cabinLight), ("doors", doors));

            var volumeObject = Child("Global Volume", root);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = GetOrCreateVolumeProfile();
            Wire(volumeObject.AddComponent<FlamePostFx>(), ("config", FlameSettings), ("flameStateChanged", flameState));

            CreateAudio(root, gameStateChanged);

            var eventSystem = Child("EventSystem", root);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var manager = Child("GameManager", root);
            var sceneLoader = manager.AddComponent<SceneLoader>();
            var gameManager = manager.AddComponent<GameManager>();

            GameObject hud = CreateHud(root, inventory, flameChanged, flameState, temperatureChanged, powerChanged,
                fearChanged, heaterRunning, doorsOpen, cabinLightRunning, inventoryChanged, noticeRaised, gameStateChanged);

            var director = Child("FlameDialogueDirector", root).AddComponent<FlameDialogueDirector>();
            WireArray(director, "lines", NarrativeContent.Lines());
            Wire(director,
                ("route", route), ("bubbleRaised", Channel<StringEventChannel>("FlameBubbleRaised")),
                ("flameStateChanged", flameState), ("fuelReceived", Channel<VoidEventChannel>("FuelReceived")),
                ("temperatureChanged", temperatureChanged), ("fearChanged", fearChanged),
                ("flashTriggered", Channel<VoidEventChannel>("FlashTriggered")),
                ("powerDepleted", Channel<VoidEventChannel>("PowerDepleted")),
                ("faultsChanged", Channel<VoidEventChannel>("FaultsChanged")),
                ("inBusChanged", inBusChanged), ("gameStateChanged", gameStateChanged));

            CreateBubble(hud);
            EventDialogView dialog = CreateEventDialog(hud);
            var eventRunner = Child("RoadEventRunner", root).AddComponent<RoadEventRunner>();
            WireArray(eventRunner, "events", NarrativeContent.Events());
            Wire(eventRunner, ("view", dialog), ("introEvent", NarrativeContent.Intro()),
                ("objectiveChanged", Channel<StringEventChannel>("ObjectiveChanged")));

            GameMenuView menu = CreateMenu(root);
            CutsceneView cutscene = CreateCutscene(root, "Cutscene", "katscena");
            CutsceneView endingCutscene = CreateCutscene(root, "EndingCutscene", "final_cutscene");
            GameObject endPanel = CreatePanel("EndPanel", hud.transform, string.Empty);
            Text endLabel = endPanel.GetComponentInChildren<Text>();
            endLabel.alignment = TextAnchor.LowerCenter;
            PlaceColumn(endLabel.rectTransform, 300f, 380f);
            Button restart = CreateButton("RestartButton", endPanel.transform, "У головне меню");

            Wire(gameManager,
                ("sceneLoader", sceneLoader), ("route", route), ("saveSystem", saveSystem), ("roadEvents", eventRunner),
                ("flameExtinguished", extinguished), ("arrivedAtNode", Channel<VoidEventChannel>("ArrivedAtNode")),
                ("stopCompleted", Channel<VoidEventChannel>("StopCompleted")), ("inBusChanged", inBusChanged),
                ("gameStateChanged", gameStateChanged), ("noticeRaised", noticeRaised),
                ("objectiveChanged", Channel<StringEventChannel>("ObjectiveChanged")),
                ("menu", menu), ("cutscene", cutscene), ("endingCutscene", endingCutscene), ("endPanel", endPanel), ("endLabel", endLabel), ("restartButton", restart));

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), BootPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Сцену зібрано: {BootPath}");
        }

        [MenuItem("Night Trolleybus/Scenes/Build Route Map Scene")]
        public static void BuildMap()
        {
            if (IsAlreadyBuilt(MapPath, MapMarker))
                return;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMainCamera();

            var canvasObject = new GameObject(MapMarker);
            canvasObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            AddScaler(canvasObject);
            canvasObject.AddComponent<GraphicRaycaster>();

            var mapRoot = new GameObject("Map").AddComponent<RectTransform>();
            mapRoot.SetParent(canvasObject.transform, false);
            mapRoot.anchoredPosition = Vector2.zero;

            var view = canvasObject.AddComponent<RouteMapView>();
            Wire(view, ("root", mapRoot), ("square", Square), ("font", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")));

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), MapPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Сцену зібрано: {MapPath}");
        }

        private static GameObject Child(string objectName, GameObject parent)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static void AddScaler(GameObject canvasObject)
        {
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        private static GameObject CreateHud(GameObject root, Inventory inventory, FloatEventChannel flameChanged, FlameStateEventChannel flameState,
            FloatEventChannel temperatureChanged, FloatEventChannel powerChanged, FloatEventChannel fearChanged,
            BoolEventChannel heaterRunning, BoolEventChannel doorsOpen, BoolEventChannel cabinLightRunning,
            VoidEventChannel inventoryChanged, StringEventChannel noticeRaised, StringEventChannel gameStateChanged)
        {
            var hud = Child("HUD", root);
            var canvas = hud.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            AddScaler(hud);
            hud.AddComponent<GraphicRaycaster>();

            var gaugesRoot = new GameObject("Gauges", typeof(RectTransform)).GetComponent<RectTransform>();
            gaugesRoot.SetParent(hud.transform, false);
            StretchFull(gaugesRoot);
            var inventoryPanel = new GameObject("InventoryPanel", typeof(RectTransform)).GetComponent<RectTransform>();
            inventoryPanel.SetParent(hud.transform, false);
            Text noticeLabel = CreateNoticeLabel(hud.transform);

            Wire(hud.AddComponent<InventoryView>(),
                ("inventory", inventory), ("inventoryChanged", inventoryChanged), ("panel", inventoryPanel),
                ("frame", PixelArt.GetSliced("UI_Frame", 4)), ("font", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")));
            Wire(hud.AddComponent<MessageHudView>(),
                ("noticeRaised", noticeRaised), ("noticeLabel", noticeLabel),
                ("objectiveChanged", Channel<StringEventChannel>("ObjectiveChanged")),
                ("objectiveLabel", CreateObjectiveLabel(hud.transform)));

            var ridePanel = new GameObject("RidePanel", typeof(RectTransform));
            ridePanel.transform.SetParent(hud.transform, false);
            StretchFull((RectTransform)ridePanel.transform);
            Text faultsLabel = CreateLabel("FaultsLabel", ridePanel.transform, -212f, 40f, string.Empty);
            Text repairLabel = CreateLabel("RepairLabel", ridePanel.transform, -252f, 170f, string.Empty);
            Wire(hud.AddComponent<BusHudView>(),
                ("faultsChanged", Channel<VoidEventChannel>("FaultsChanged")),
                ("repairTextChanged", Channel<StringEventChannel>("RepairTextChanged")),
                ("faultsLabel", faultsLabel), ("repairLabel", repairLabel));

            var stopPanel = new GameObject("StopPanel", typeof(RectTransform));
            stopPanel.transform.SetParent(hud.transform, false);
            StretchFull((RectTransform)stopPanel.transform);

            Wire(hud.AddComponent<HudView>(),
                ("config", FlameSettings), ("flameChanged", flameChanged), ("flameStateChanged", flameState),
                ("temperatureChanged", temperatureChanged), ("powerChanged", powerChanged), ("fearChanged", fearChanged),
                ("healthChanged", Channel<FloatEventChannel>("PlayerHealthChanged")),
                ("speedChanged", Channel<FloatEventChannel>("BusSpeedChanged")), ("tripChanged", Channel<FloatEventChannel>("TripChanged")),
                ("gaugesRoot", gaugesRoot), ("ridePanel", ridePanel.transform), ("stopPanel", stopPanel.transform),
                ("frame", PixelArt.GetSliced("UI_Frame", 4)), ("square", Square),
                ("flameIcon", PixelArt.Get("UI_IconFlame")), ("heatIcon", PixelArt.Get("UI_IconHeat")), ("powerIcon", PixelArt.Get("UI_IconPower")),
                ("fearIcon", PixelArt.Get("UI_IconFear")), ("healthIcon", PixelArt.Get("UI_IconHealth")),
                ("trendArrow", PixelArt.Get("UI_Arrow")), ("trolleyIcon", PixelArt.Get("UI_Trolley")), ("stopIcon", PixelArt.Get("UI_StopMark")),
                ("dial", PixelArt.Get("UI_Dial")), ("font", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")));

            var switcher = hud.AddComponent<HudPanelSwitcher>();
            Wire(switcher,
                ("gameStateChanged", gameStateChanged), ("ridePanel", ridePanel), ("stopPanel", stopPanel),
                ("inventoryPanel", inventoryPanel.gameObject));
            WireArray(switcher, "hiddenOnMap", new Object[] { gaugesRoot.gameObject });
            return hud;
        }

        private static Text CreateNoticeLabel(Transform parent)
        {
            Text label = CreateLabel("NoticeLabel", parent, -150f, 40f, string.Empty);
            StyleCentered(label);
            label.rectTransform.anchoredPosition = new Vector2(0f, -150f);
            return label;
        }

        /// <summary>Напис посередині вгорі: під смугою шляху, де його не перекривають шкали.</summary>
        private static void StyleCentered(Text label)
        {
            label.alignment = TextAnchor.UpperCenter;
            label.color = new Color(1f, 0.93f, 0.8f);
            RectTransform rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(1100f, rect.sizeDelta.y);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private static AudioClip Sfx(string fileName) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/SFX/{fileName}.wav");

        private static AudioClip Music(string fileName) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>($"{Root}/Audio/Music/{fileName}.wav");

        private static void CreateAudio(GameObject root, StringEventChannel gameStateChanged)
        {
            // Слухач живе в Boot, бо меню показується ще без жодної камери.
            GameObject audioObject = Child("GameAudio", root);
            audioObject.AddComponent<AudioListener>();
            var audio = audioObject.AddComponent<GameAudio>();
            Wire(audio,
                ("engineLoop", Sfx("Generated/engine_loop")), ("windLoop", Sfx("white_noise_long")), ("darknessLoop", Sfx("ghost_long")),
                ("calmMusic", Music("music_calm")), ("rideMusic", Music("music_ride")), ("stopMusic", Music("music_stop")),
                ("busSpeedChanged", Channel<FloatEventChannel>("BusSpeedChanged")),
                ("darknessChanged", Channel<FloatEventChannel>("DarknessChanged")),
                ("gameStateChanged", gameStateChanged));

            (SoundId id, string file, float volume)[] sounds =
            {
                (SoundId.UiClick, "controller_button_press", 0.6f),
                (SoundId.DeviceSwitch, "controller_button_press_2", 0.7f),
                (SoundId.DoorOpen, "industrial_door_open", 0.8f),
                (SoundId.DoorClose, "industrial_door_close", 0.8f),
                (SoundId.DoorKnock, "door_knock", 1f),
                (SoundId.FlameFed, "fire_lighting", 0.9f),
                (SoundId.Flash, "grow_big", 0.9f),
                (SoundId.Reveal, "white_noise_short", 0.45f),
                (SoundId.Lose, "lose", 1f),
                (SoundId.Victory, "Generated/victory", 0.8f),
                (SoundId.Lock, "lock_lock", 0.8f),
                (SoundId.Spark, "Generated/spark", 0.6f),
                (SoundId.Thump, "Generated/thump", 0.9f),
                (SoundId.LineFlicker, "white_noise_short", 0.35f),
                (SoundId.RepairDone, "Generated/repair_ok", 0.6f),
                (SoundId.PowerUp, "Generated/power_up", 0.6f),
                (SoundId.Pickup, "Generated/pickup", 0.5f),
                (SoundId.Swing, "Generated/swing", 0.5f),
                (SoundId.Hit, "Generated/hit", 0.8f),
                (SoundId.Hurt, "Generated/hurt", 0.6f),
                (SoundId.Step, "Generated/step", 0.18f),
                (SoundId.EnemyAppear, "ghost", 0.7f),
            };

            var serialized = new SerializedObject(audio);
            SerializedProperty array = serialized.FindProperty("sounds");
            array.arraySize = sounds.Length;
            for (int i = 0; i < sounds.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").enumValueIndex = (int)sounds[i].id;
                element.FindPropertyRelative("clip").objectReferenceValue = Sfx(sounds[i].file);
                element.FindPropertyRelative("volume").floatValue = sounds[i].volume;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameMenuView CreateMenu(GameObject root)
        {
            GameObject canvasObject = Child("Menu", root);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            AddScaler(canvasObject);
            canvasObject.AddComponent<GraphicRaycaster>();

            var menu = canvasObject.AddComponent<GameMenuView>();
            Wire(menu,
                ("background", PixelArt.Get("MenuBackground")), ("square", Square), ("glow", PixelArt.Get("MenuGlow")),
                ("candle", PixelArt.Get("Flame_Candle")), ("bowl", PixelArt.Get("Flame_Bowl")),
                ("font", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")));
            WireArray(menu, "flameFrames", new Object[] { PixelArt.Get("Flame_A"), PixelArt.Get("Flame_B"), PixelArt.Get("Flame_A"), PixelArt.Get("Flame_C") });
            return menu;
        }

        private static CutsceneView CreateCutscene(GameObject root, string objectName, string videoName)
        {
            GameObject canvasObject = Child(objectName, root);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            AddScaler(canvasObject);
            canvasObject.AddComponent<GraphicRaycaster>();

            var cutscene = canvasObject.AddComponent<CutsceneView>();
            Wire(cutscene,
                ("clip", AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>($"{Root}/Video/{videoName}.mp4")),
                ("square", Square), ("font", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")));
            return cutscene;
        }

        private static void CreateBubble(GameObject hud)
        {
            var bubble = new GameObject("FlameBubble").AddComponent<Image>();
            bubble.transform.SetParent(hud.transform, false);
            bubble.sprite = PixelArt.GetSliced("UI_Paper", 4);
            bubble.type = Image.Type.Sliced;
            bubble.color = Color.white;
            bubble.raycastTarget = false;
            bubble.rectTransform.sizeDelta = new Vector2(600f, 110f);

            var label = new GameObject("Label").AddComponent<Text>();
            label.transform.SetParent(bubble.transform, false);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 26;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.2f, 0.1f, 0.05f);
            label.raycastTarget = false;
            StretchFull(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(16f, 8f);
            label.rectTransform.offsetMax = new Vector2(-16f, -8f);

            Wire(hud.AddComponent<FlameBubbleView>(),
                ("bubbleRaised", Channel<StringEventChannel>("FlameBubbleRaised")), ("bubble", bubble.rectTransform), ("label", label));
        }

        private static EventDialogView CreateEventDialog(GameObject hud)
        {
            GameObject panel = CreatePanel("EventPanel", hud.transform, string.Empty);
            // Суцільний фон: шкали, мапа й репліки під подією лише відволікають.
            panel.GetComponent<Image>().color = Color.black;
            Text titleLabel = panel.GetComponentInChildren<Text>();
            titleLabel.alignment = TextAnchor.UpperCenter;
            PlaceColumn(titleLabel.rectTransform, 340f, 80f);

            var body = new GameObject("Body").AddComponent<Text>();
            body.transform.SetParent(panel.transform, false);
            body.font = titleLabel.font;
            body.fontSize = 30;
            body.alignment = TextAnchor.UpperCenter;
            body.color = Color.white;
            body.raycastTarget = false;
            PlaceColumn(body.rectTransform, 250f, 240f);

            var buttons = new GameObject("Buttons", typeof(RectTransform)).GetComponent<RectTransform>();
            buttons.SetParent(panel.transform, false);
            PlaceColumn(buttons, -10f, 260f);

            var view = hud.AddComponent<EventDialogView>();
            Wire(view, ("panel", panel), ("titleLabel", titleLabel), ("bodyLabel", body), ("buttonsRoot", buttons),
                ("square", Square), ("font", titleLabel.font));
            return view;
        }

        /// <summary>Розміщує елемент у центральній колонці: y рахується від центру екрана до верхнього краю.</summary>
        private static void PlaceColumn(RectTransform rect, float y, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(1200f, height);
        }

        private static Text CreateObjectiveLabel(Transform parent)
        {
            Text label = CreateLabel("ObjectiveLabel", parent, -28f, 110f, string.Empty);
            label.fontSize = 34;
            label.fontStyle = FontStyle.Bold;
            StyleCentered(label);
            label.color = new Color(1f, 0.85f, 0.4f);
            label.rectTransform.anchoredPosition = new Vector2(0f, -68f);
            return label;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreatePanel(string objectName, Transform parent, string text)
        {
            var panel = new GameObject(objectName).AddComponent<Image>();
            panel.transform.SetParent(parent, false);
            panel.sprite = Square;
            panel.color = new Color(0f, 0f, 0f, 0.92f);
            StretchFull(panel.rectTransform);

            var label = new GameObject("Label").AddComponent<Text>();
            label.transform.SetParent(panel.transform, false);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 44;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
            StretchFull(label.rectTransform);
            return panel.gameObject;
        }

        private static Button CreateButton(string objectName, Transform parent, string text)
        {
            var image = new GameObject(objectName).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = Square;
            image.color = new Color(0.95f, 0.65f, 0.3f);
            image.rectTransform.sizeDelta = new Vector2(320f, 80f);
            image.rectTransform.anchoredPosition = new Vector2(0f, -140f);
            var button = image.gameObject.AddComponent<Button>();

            var label = new GameObject("Label").AddComponent<Text>();
            label.transform.SetParent(image.transform, false);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 32;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.black;
            label.text = text;
            label.raycastTarget = false;
            StretchFull(label.rectTransform);
            return button;
        }
    }
}
