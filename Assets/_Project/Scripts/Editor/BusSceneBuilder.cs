using System.IO;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Items;
using NightTrolleybus.Passengers;
using NightTrolleybus.Player;
using NightTrolleybus.Route;
using NightTrolleybus.Vehicle;
using NightTrolleybus.Vehicle.Repair;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static NightTrolleybus.EditorTools.BuilderTools;

namespace NightTrolleybus.EditorTools
{
    /// <summary>Збирає сцену Bus із плейсхолдерів. Вже зібрану сцену не перезаписує.</summary>
    public static class BusSceneBuilder
    {
        private const string BusScenePath = Root + "/Scenes/Bus.unity";
        private const string InteriorName = "BusInterior";

        [MenuItem("Night Trolleybus/Scenes/Build Bus Scene")]
        public static void Build()
        {
            if (IsAlreadyBuilt(BusScenePath, InteriorName))
                return;

            // Нова сцена вивантажує незбережені асети, тому створюємо її до них.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var speedChanged = Channel<FloatEventChannel>("BusSpeedChanged");
            var noticeRaised = Channel<StringEventChannel>("BusNoticeRaised");
            var faultsChanged = Channel<VoidEventChannel>("FaultsChanged");
            var busConfig = GetOrCreate<BusConfig>(Root + "/Data/Config/BusConfig.asset");
            Object[] passengerPool = PassengerPool();
            AssetDatabase.SaveAssets();

            var logic = new GameObject("BusLogic");
            var bus = logic.AddComponent<BusDriveController>();
            Wire(bus, ("config", busConfig), ("speedChanged", speedChanged));
            Wire(logic.AddComponent<BusIncidentSpawner>(), ("bus", bus), ("noticeRaised", noticeRaised));
            var trip = logic.AddComponent<TripController>();
            Wire(trip, ("bus", bus), ("tripChanged", Channel<FloatEventChannel>("TripChanged")),
                ("arrivedAtNode", Channel<VoidEventChannel>("ArrivedAtNode")), ("objectiveChanged", Channel<StringEventChannel>("ObjectiveChanged")));

            CreateGlobalLight();
            CreateOutside(speedChanged);
            Transform[] passengerSeats = CreateInterior();

            GameObject player = CreatePlayer();
            var motor = player.GetComponent<PlayerMotor2D>();
            CreateDevices(bus, motor, trip);

            var passengerManager = logic.AddComponent<PassengerManager>();
            Wire(passengerManager, ("bus", bus), ("trip", trip), ("sprite", PixelArt.Get("Passenger")), ("material", LitMaterial), ("noticeRaised", noticeRaised));
            WireArray(passengerManager, "pool", passengerPool);
            WireArray(passengerManager, "seats", passengerSeats);

            var flameSeat = new GameObject("FlameSeat");
            flameSeat.transform.position = new Vector3(1.8f, -1.6f, 0f);
            CreateFlame(flameSeat.transform);
            CreateAbilities(logic, flameSeat.transform, true);

            CreateFollowCameras(player.transform);

            var repairHost = logic.AddComponent<RepairHost>();
            Wire(repairHost, ("playerMotor", motor), ("repairTextChanged", Channel<StringEventChannel>("RepairTextChanged")));
            CreateRepairStation("PolesStation", new Vector3(5.4f, 1.6f, 0f), FaultType.PolesOff, "Підключити штанги", repairHost, faultsChanged);
            CreateRepairStation("FuseStation", new Vector3(2.9f, 0.4f, 0f), FaultType.FuseBlown, "Замінити запобіжник", repairHost, faultsChanged);
            CreateRepairStation("WiresStation", new Vector3(-4.4f, 0.4f, 0f), FaultType.WiresLoose, "З'єднати проводи", repairHost, faultsChanged);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), BusScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Сцену зібрано: {BusScenePath}");
        }

        public static Object[] PassengerPool() => new Object[]
        {
            GetOrCreatePassenger("OldWoman", "Бабуся зі свічкою", new Color(0.7f, 0.65f, 0.55f), Candle, 0f, 0f),
            GetOrCreatePassenger("Worker", "Втомлений електрик", new Color(0.45f, 0.55f, 0.65f), Battery, 0f, 0f),
            GetOrCreatePassenger("Stranger", "Мовчазний незнайомець", new Color(0.2f, 0.2f, 0.25f), null, 0.3f, 0.5f),
            GetOrCreatePassenger("Shadow", "Тінь у пальті", new Color(0.05f, 0.05f, 0.08f), null, 0.5f, 1f)
        };

        private static PassengerData GetOrCreatePassenger(string assetName, string displayName, Color color, ItemData gift, float fear, float heatSteal)
        {
            string path = $"{Root}/Data/Passengers/{assetName}.asset";
            if (File.Exists(path))
                return AssetDatabase.LoadAssetAtPath<PassengerData>(path);

            var passenger = GetOrCreate<PassengerData>(path);
            var serialized = new SerializedObject(passenger);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("color").colorValue = color;
            serialized.FindProperty("gift").objectReferenceValue = gift;
            serialized.FindProperty("fear").floatValue = fear;
            serialized.FindProperty("heatStealPerSecond").floatValue = heatSteal;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(passenger);
            return passenger;
        }

        private static void CreateRepairStation(string objectName, Vector3 position, FaultType fault, string actionName, RepairHost host, VoidEventChannel faultsChanged)
        {
            GameObject stationObject = CreateDevice(objectName, null, position, "RepairPanel");
            // Станція висить на стіні, а гравець унизу, тому зона взаємодії тягнеться до підлоги.
            stationObject.GetComponent<BoxCollider2D>().size = new Vector2(0.6f, 3.6f);

            var station = stationObject.AddComponent<RepairStation>();
            Wire(station, ("host", host), ("faultsChanged", faultsChanged), ("indicator", stationObject.GetComponent<SpriteRenderer>()));
            SetEnum(station, "fault", (int)fault);
            SetString(station, "actionName", actionName);
        }

        private static void CreateOutside(FloatEventChannel speedChanged)
        {
            var outside = new GameObject("Outside");
            Random.InitState(42);

            CreateLayer("FarCity", outside.transform, speedChanged, 1.5f, 6f, 14, new Color(0.13f, 0.15f, 0.23f), -30, 4f, 8f);
            CreateLayer("NearCity", outside.transform, speedChanged, 5f, 8f, 12, new Color(0.24f, 0.24f, 0.33f), -25, 2f, 5f);

            CreateSprite("Road", outside.transform, new Vector3(0f, -4f, 0f), new Vector3(120f, 2f, 1f), new Color(0.04f, 0.04f, 0.05f), -20, UnlitMaterial);

            var dashes = new GameObject("RoadDashes");
            dashes.transform.SetParent(outside.transform, false);
            const int dashCount = 16;
            const float dashStep = 5f;
            for (int i = 0; i < dashCount; i++)
            {
                CreateSprite($"Dash_{i}", dashes.transform, new Vector3((i - dashCount / 2f) * dashStep, -4f, 0f), new Vector3(2f, 0.12f, 1f), new Color(0.3f, 0.3f, 0.28f), -19, UnlitMaterial);
            }

            AddParallax(dashes, speedChanged, 14f, dashStep);
        }

        private static void CreateLayer(string layerName, Transform parent, FloatEventChannel speedChanged,
            float scroll, float width, int count, Color color, int order, float minHeight, float maxHeight)
        {
            var layer = new GameObject(layerName);
            layer.transform.SetParent(parent, false);

            for (int i = 0; i < count; i++)
            {
                var segment = new GameObject($"Segment_{i}");
                segment.transform.SetParent(layer.transform, false);
                segment.transform.localPosition = new Vector3((i - count / 2f) * width, 0f, 0f);

                float height = Random.Range(minHeight, maxHeight);
                string[] buildings = { "Building_A", "Building_B", "Building_C" };
                GameObject building = CreateArt("Building", segment.transform, new Vector3(0f, -3f + height / 2f, 0f), buildings[i % buildings.Length], order, color, UnlitMaterial);
                Vector2 artSize = building.GetComponent<SpriteRenderer>().sprite.bounds.size;
                building.transform.localScale = new Vector3(width * 0.85f / artSize.x, height / artSize.y, 1f);
            }

            AddParallax(layer, speedChanged, scroll, width);
        }

        private static void AddParallax(GameObject layer, FloatEventChannel speedChanged, float scroll, float width)
        {
            var parallax = layer.AddComponent<ParallaxLayer>();
            Wire(parallax, ("speedChanged", speedChanged));
            SetFloat(parallax, "scrollAtFullSpeed", scroll);
            SetFloat(parallax, "segmentWidth", width);
        }

        private static Transform[] CreateInterior()
        {
            var interior = new GameObject(InteriorName).transform;

            AddFittedCollider(CreateArt("Floor", interior, new Vector3(0f, -2.5f, 0f), "BusFloor", 0), false);
            CreateArt("Ceiling", interior, new Vector3(0f, 2.5f, 0f), "BusCeiling", 0);
            AddFittedCollider(CreateArt("LeftEnd", interior, new Vector3(-8.5f, 0f, 0f), "BusEnd", 0), false);
            AddFittedCollider(CreateArt("RightEnd", interior, new Vector3(8.5f, 0f, 0f), "BusEnd", 0), false);

            // Задня стінка одним спрайтом: прозорі отвори в ньому є вікнами.
            CreateArt("Wall", interior, Vector3.zero, "BusWall", -5);

            CreateArt("Seat_Flame", interior, new Vector3(1.8f, -1.5f, 0f), "Seat", -2);

            // Вогник сидить окремо, а пасажири займають решту сидінь.
            float[] passengerSeatPositions = { -5.5f, -1.5f, 4f };
            var seats = new Transform[passengerSeatPositions.Length];
            for (int i = 0; i < seats.Length; i++)
            {
                seats[i] = new GameObject($"PassengerSeat_{i}").transform;
                seats[i].SetParent(interior, false);
                seats[i].localPosition = new Vector3(passengerSeatPositions[i], -1.5f, 0f);
                CreateArt("Seat", seats[i], Vector3.zero, "Seat", -2);
            }

            return seats;
        }

        private static GameObject CreatePlayer()
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = new Vector3(-1f, -1.2f, 0f);

            var body = player.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            player.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.6f);
            SetBool(player.AddComponent<PlayerMotor2D>(), "canJump", false);
            Wire(player.AddComponent<PlayerInteractor>(), ("promptChanged", Channel<StringEventChannel>("InteractionPromptChanged")));

            CreatePlayerBody(player);
            return player;
        }

        private static void CreateDevices(BusDriveController bus, PlayerMotor2D motor, TripController trip)
        {
            var devices = new GameObject("Devices").transform;

            GameObject heaterObject = CreateDevice("HeaterSwitch", devices, new Vector3(-3.2f, -1.5f, 0f), "Heater");
            AddDeviceSwitch(heaterObject, DeviceKind.Heater, "обігрівач");

            var cabinLightObject = new GameObject("CabinLightFixture");
            cabinLightObject.transform.SetParent(devices, false);
            cabinLightObject.transform.position = new Vector3(0f, 1.8f, 0f);
            var cabinLight2D = cabinLightObject.AddComponent<Light2D>();
            cabinLight2D.lightType = Light2D.LightType.Point;
            cabinLight2D.pointLightOuterRadius = 12f;
            cabinLight2D.intensity = 0.4f;
            cabinLight2D.enabled = false;
            Wire(cabinLightObject.AddComponent<CabinLightView>(), ("runningChanged", Channel<BoolEventChannel>("CabinLightRunningChanged")));

            GameObject lightSwitchObject = CreateDevice("LightSwitch", devices, new Vector3(-7f, 0f, 0f), "Switch");
            AddDeviceSwitch(lightSwitchObject, DeviceKind.CabinLight, "світло в салоні");

            GameObject doorsObject = CreateDevice("DoorsSwitch", devices, new Vector3(0f, -1f, 0f), "DoorLever");
            doorsObject.AddComponent<DoorsSwitch>();

            GameObject exitObject = CreateDevice("BusExit", devices, new Vector3(-0.9f, -1.2f, 0f), "ExitDoor");
            // Двері мають світитися навіть у темному салоні, бо це головний орієнтир на зупинці.
            exitObject.GetComponent<SpriteRenderer>().sharedMaterial = UnlitMaterial;
            Wire(exitObject.AddComponent<BusExit>(), ("trip", trip));

            GameObject seatObject = CreateDevice("DriverSeat", devices, new Vector3(7f, -1.4f, 0f), "DriverSeat");
            Wire(seatObject.AddComponent<DriverSeat>(), ("bus", bus), ("playerMotor", motor));
        }

        private static GameObject CreateDevice(string objectName, Transform parent, Vector3 position, string spriteName)
        {
            GameObject device = CreateArt(objectName, parent, position, spriteName, 1);
            AddFittedCollider(device, true);
            return device;
        }

        private static void AddDeviceSwitch(GameObject target, DeviceKind kind, string deviceName)
        {
            var deviceSwitch = target.AddComponent<DeviceSwitch>();
            SetEnum(deviceSwitch, "kind", (int)kind);
            SetString(deviceSwitch, "deviceName", deviceName);
        }

        internal static void CreateFollowCameras(Transform player)
        {
            Camera camera = CreateMainCamera();
            camera.gameObject.AddComponent<CinemachineBrain>();

            var followObject = new GameObject("CM Follow Player");
            followObject.transform.position = new Vector3(0f, 0f, -10f);
            var follow = followObject.AddComponent<CinemachineCamera>();
            follow.Follow = player;
            LensSettings lens = LensSettings.Default;
            lens.OrthographicSize = 5f;
            follow.Lens = lens;

            var composer = followObject.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.TargetOffset = new Vector3(0f, 1.2f, 0f);
            composer.Damping = new Vector3(0.8f, 0.5f, 0f);
        }
    }
}
