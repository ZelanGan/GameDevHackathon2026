using System.Collections.Generic;
using System.Linq;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Items;
using NightTrolleybus.Player;
using NightTrolleybus.Stops;
using NightTrolleybus.Stops.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using static NightTrolleybus.EditorTools.BuilderTools;

namespace NightTrolleybus.EditorTools
{
    /// <summary>Збирає сцени зупинок у вигляді зверху. Вже зібрані сцени без явної команди не перезаписує.</summary>
    public static class StopSceneBuilder
    {
        private const string LevelName = "StopLevel";
        private const string EnemyPrefabPath = Root + "/Prefabs/Enemies/Shade.prefab";
        private const string WraithPrefabPath = Root + "/Prefabs/Enemies/Wraith.prefab";
        private const string DepotPath = Root + "/Scenes/Stops/Stop_Depot.unity";
        private const string MarketPath = Root + "/Scenes/Stops/Stop_Market.unity";
        private const string GunShopPath = Root + "/Scenes/Stops/Stop_GunShop.unity";

        // Межі локації в юнітах. Вхід ліворуч, темрява наповзає праворуч.
        private const int MinX = 0;
        private const int MaxX = 60;
        private const int MinY = -12;
        private const int MaxY = 12;

        private static bool overwrite;
        private static string wallSprite;

        [MenuItem("Night Trolleybus/Scenes/Rebuild All Stops (overwrites scenes)")]
        public static void RebuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Префаб теж заново: у нього змінився колайдер і зник вплив гравітації.
            AssetDatabase.DeleteAsset(EnemyPrefabPath);
            AssetDatabase.DeleteAsset(WraithPrefabPath);
            overwrite = true;
            try
            {
                BuildDepot();
                BuildMarket();
                BuildGunShop();
            }
            finally
            {
                overwrite = false;
            }
        }

        [MenuItem("Night Trolleybus/Scenes/Build Stop Depot Scene")]
        public static void BuildDepot()
        {
            if (!BeginScene(DepotPath, "Depot", out GameObject player, out Transform level))
                return;

            // Депо: електрика й запчастини, кілька кімнат і небагато ворогів. Планування будує генератор.
            // Станція стоїть у кадрі біля входу й світиться сама, щоб її не треба було шукати в темряві.
            GameObject charger = CreateArt("PowerCharger", null, new Vector3(7f, 2.5f, 0f), "Top_Charger", 1, Color.white, UnlitMaterial);
            AddFittedCollider(charger, true);
            var beacon = charger.AddComponent<Light2D>();
            beacon.lightType = Light2D.LightType.Point;
            beacon.color = new Color(0.35f, 0.75f, 1f);
            beacon.pointLightOuterRadius = 3.5f;
            beacon.intensity = 1.2f;
            Wire(charger.AddComponent<PowerCharger>(), ("noticeRaised", Channel<StringEventChannel>("BusNoticeRaised")), ("beacon", beacon));

            var pickups = new List<GameObject>
            {
                Pickup("Match", Match, 8f, -3f),
                Pickup("Candle", Candle, 14f, 8f),
                Pickup("Battery_A", Battery, 25f, -8f),
                Pickup("Ticket", Ticket, 33f, 9f)
            };

            // Прихована кімната в кутку: прохід до неї відкривається лише під Підсвічуванням.
            Wall(level, 52, 6, 60, 7);
            Wall(level, 52, 7, 53, 8);
            Wall(level, 52, 10, 53, 12);
            GameObject hiddenWall = CreateTiled("HiddenWall", null, new Vector3(52.5f, 9f, 0f), "Top_HiddenWall", new Vector2(1f, 2f), Color.white, 1);
            hiddenWall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 2f);
            MakeRevealable(hiddenWall, true);
            Pickup("Battery_Hidden", Battery, 56f, 9.5f);
            GameObject hiddenCandle = Pickup("Candle_Hidden", Candle, 44f, 9f);
            MakeRevealable(hiddenCandle);
            pickups.Add(hiddenCandle);

            StopLayoutGenerator layout = CreateLayout(level, StopInterior.Rooms, wallSprite, pickups, 3, 1,
                new[] { "Top_Crate", "Top_Barrel", "Top_Spool", "Top_Tires", "Top_Workbench" },
                new[] { "Decal_Oil", "Decal_Crack", "Decal_Cable", "Decal_Dust", "Decal_Paper" });
            SetInt(layout, "lastWallX", 48);
            SetReserved(layout, new RectInt(5, 12, 5, 5), new RectInt(52, 18, 8, 6));

            FinishScene(DepotPath, player);
        }

        [MenuItem("Night Trolleybus/Scenes/Build Stop Market Scene")]
        public static void BuildMarket()
        {
            if (!BeginScene(MarketPath, "Market", out GameObject player, out Transform level))
                return;

            // Супермаркет: ряди полиць, багато предметів, але й багато ворогів.
            var pickups = new List<GameObject>
            {
                Pickup("Candle_A", Candle, 7f, 6f),
                Pickup("Match_A", Match, 16f, -6f),
                Pickup("Candle_B", Candle, 18f, 6f),
                Pickup("Match_B", Match, 26f, -5f),
                Pickup("Ticket_A", Ticket, 28f, 7f),
                Pickup("Candle_C", Candle, 37f, -7f),
                Pickup("Ticket_B", Ticket, 46f, 5f)
            };
            foreach ((string pickupName, ItemData item, float x, float y) in new[] { ("Ticket_Hidden", Ticket, 48f, -10f), ("Candle_Hidden", Candle, 57f, 10f) })
            {
                GameObject hidden = Pickup(pickupName, item, x, y);
                MakeRevealable(hidden);
                pickups.Add(hidden);
            }

            StopLayoutGenerator layout = CreateLayout(level, StopInterior.Aisles, "Top_Shelf", pickups, 6, 2,
                new[] { "Top_Cart", "Top_BoxPile", "Top_Checkout", "Top_Crate" },
                new[] { "Decal_Spill", "Decal_Paper", "Decal_Can", "Decal_Crack", "Decal_Dust" });
            SetInt(layout, "firstWallMin", 10);
            SetInt(layout, "firstWallMax", 13);
            SetInt(layout, "spacingMin", 4);
            SetInt(layout, "spacingMax", 7);
            SetInt(layout, "lastWallX", 56);

            FinishScene(MarketPath, player, 18f, 9f);
        }

        [MenuItem("Night Trolleybus/Scenes/Build Stop Gun Shop Scene")]
        public static void BuildGunShop()
        {
            if (!BeginScene(GunShopPath, "GunShop", out GameObject player, out Transform level))
                return;

            // Магазин зброї: зал із прилавками, а далі склад із небезпечними знахідками.
            GameObject hiddenTicket = Pickup("Ticket_Hidden", Ticket, 22f, -9f);
            MakeRevealable(hiddenTicket);
            var pickups = new List<GameObject> { Pickup("Ticket", Ticket, 8f, -6f), hiddenTicket, Pickup("Ticket_Store", Ticket, 50f, 8f) };

            GameObject[] stands =
            {
                CreateStand("Stand_Crowbar", new Vector2(13f, 5f), Ticket, 2, Crowbar, null),
                CreateStand("Stand_FireAxe", new Vector2(18f, 5f), Ticket, 4, FireAxe, null),
                CreateStand("Stand_Candle", new Vector2(23f, 5f), Ticket, 1, null, Candle)
            };

            StopLayoutGenerator layout = CreateLayout(level, StopInterior.Rooms, wallSprite, pickups, 2, 1,
                new[] { "Top_AmmoBox", "Top_Target", "Top_GunRack", "Top_Crate", "Top_Barrel" },
                new[] { "Decal_Shells", "Decal_Crack", "Decal_Dust", "Decal_Paper", "Decal_Oil" });
            // Перша стіна далеко від входу: у залі мають поміститися всі прилавки.
            SetInt(layout, "firstWallMin", 24);
            SetInt(layout, "firstWallMax", 28);
            WireTransforms(layout, "nearEntrance", stands);
            Wire(layout, ("lootBoxSprite", PixelArt.Get("Top_AmmoBox")), ("lootBoxOpenSprite", PixelArt.Get("Top_AmmoBox_Open")),
                ("noticeRaised", Channel<StringEventChannel>("BusNoticeRaised")));
            WireArray(layout, "lootBoxItems", new Object[] { Ticket, Ticket, Match, Candle, null });

            FinishScene(GunShopPath, player);
        }

        private static bool BeginScene(string path, string theme, out GameObject player, out Transform level)
        {
            player = null;
            level = null;
            if (!overwrite && IsAlreadyBuilt(path, LevelName))
                return false;

            // Нова сцена вивантажує незбережені асети, тому створюємо її до них.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Без загального світла за стіною зовсім темно, і ворога там не видно.
            CreateGlobalLight(0f);

            level = new GameObject(LevelName).transform;
            // У кожної зупинки свої підлога й стіни, щоб її можна було впізнати з першого погляду.
            wallSprite = $"Top_Wall_{theme}";
            CreateFloor(level, $"Top{theme}Floor", $"Top_Floor_{theme}");
            Wall(level, MinX - 1, MinY - 1, MaxX + 1, MinY);
            Wall(level, MinX - 1, MaxY, MaxX + 1, MaxY + 1);
            Wall(level, MinX - 1, MinY, MinX, MaxY);
            Wall(level, MaxX, MinY, MaxX + 1, MaxY);

            var respawn = new GameObject("RespawnPoint").transform;
            respawn.SetParent(level, false);
            respawn.position = new Vector3(3f, 0f, 0f);

            player = CreatePlayer(respawn);
            CreateFlame(player.transform);
            // Чаша зі свічкою стоїть на кашкеті водія й менша, ніж на сидінні, щоб не перевищувати голову.
            Transform flameOnHead = player.transform.Find("Flame");
            flameOnHead.localPosition = new Vector3(0f, 0.68f, 0f);
            flameOnHead.localScale = Vector3.one * 0.6f;
            // Світло йде від центра водія: Вогник на голові міг би опинитися за стіною й освітити те, що за нею.
            Object.DestroyImmediate(flameOnHead.GetComponent<Light2D>());
            var flameLight = new GameObject("FlameLight").AddComponent<Light2D>();
            flameLight.transform.SetParent(player.transform, false);
            flameLight.lightType = Light2D.LightType.Point;
            flameLight.shadowsEnabled = true;
            flameLight.shadowIntensity = 1f;
            Wire(flameOnHead.GetComponent<NightTrolleybus.Flame.FlameView>(), ("flameLight", flameLight));
            CreateAbilities(player, player.transform, false);

            GameObject door = CreateArt("BusDoor", level, new Vector3(0.5f, 0f, 0f), "Top_Door", 1);
            AddFittedCollider(door, true);
            Wire(door.AddComponent<StopExit>(), ("stopCompleted", Channel<VoidEventChannel>("StopCompleted")));
            return true;
        }

        private static StopLayoutGenerator CreateLayout(Transform level, StopInterior interior, string interiorSprite,
            List<GameObject> pickups, int enemyCount, int wraithCount, string[] props, string[] decals)
        {
            var layout = new GameObject("Layout").AddComponent<StopLayoutGenerator>();
            layout.transform.SetParent(level, false);

            // Зразок твердого об'єкта: його копії стають стінами й меблями. Тінь кидає окремий прозорий квадрат.
            GameObject template = CreateTiled("SolidTemplate", layout.transform, Vector3.zero, interiorSprite, Vector2.one, Color.white, 1);
            template.AddComponent<BoxCollider2D>().size = Vector2.one;
            GameObject shadow = CreateSprite("Shadow", template.transform, Vector3.zero, Vector3.one, Color.clear, 0);
            ConfigureCaster(shadow);
            template.SetActive(false);

            Wire(layout, ("solidTemplate", template), ("interiorSprite", PixelArt.Get(interiorSprite)));
            SetEnum(layout, "interior", (int)interior);
            WireArray(layout, "props", props.Select(name => (Object)PixelArt.Get(name)).ToArray());
            WireArray(layout, "decals", decals.Select(name => (Object)PixelArt.Get(name)).ToArray());
            WireTransforms(layout, "pickups", pickups);

            GameObject enemyPrefab = GetOrCreateEnemyPrefab();
            var enemies = new List<GameObject>();
            for (int i = 0; i < enemyCount; i++)
                enemies.Add((GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab));
            GameObject wraithPrefab = GetOrCreateWraithPrefab();
            for (int i = 0; i < wraithCount; i++)
                enemies.Add((GameObject)PrefabUtility.InstantiatePrefab(wraithPrefab));
            WireTransforms(layout, "enemies", enemies);
            return layout;
        }

        private static void WireTransforms(Object target, string field, IEnumerable<GameObject> objects) =>
            WireArray(target, field, objects.Select(item => (Object)item.transform).ToArray());

        private static void SetReserved(StopLayoutGenerator layout, params RectInt[] areas)
        {
            var serialized = new SerializedObject(layout);
            SerializedProperty array = serialized.FindProperty("reserved");
            array.arraySize = areas.Length;
            for (int i = 0; i < areas.Length; i++)
                array.GetArrayElementAtIndex(i).rectIntValue = areas[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layout);
        }

        private static void FinishScene(string path, GameObject player, float slowSpawn = 28f, float fastSpawn = 14f)
        {
            GameObject enemyPrefab = GetOrCreateEnemyPrefab();
            CreateDarkness(player.transform, enemyPrefab, slowSpawn, fastSpawn);
            BusSceneBuilder.CreateFollowCameras(player.transform, 4f, 0f);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Сцену зібрано: {path}");
        }

        private static GameObject Pickup(string objectName, ItemData item, float x, float y) =>
            CreatePickup(objectName, item, new Vector3(x, y, 0f));

        private static GameObject CreateStand(string objectName, Vector2 position, ItemData cost, int count, WeaponData weapon, ItemData item)
        {
            GameObject stand = CreateArt(objectName, null, position, "Top_Counter", 1);
            AddFittedCollider(stand, true);
            var trade = stand.AddComponent<TradeStand>();
            Wire(trade, ("costItem", cost), ("rewardWeapon", weapon), ("rewardItem", item), ("noticeRaised", Channel<StringEventChannel>("BusNoticeRaised")));
            SetInt(trade, "costCount", count);
            // Товар лежить на вітрині: зброю видно ще до покупки.
            if (weapon != null)
                CreateArt("Goods", stand.transform, new Vector3(-0.7f, 0.42f, 0f), weapon.name, 2);
            return stand;
        }

        private static void CreateFloor(Transform level, string tileName, string spriteName)
        {
            string tilePath = $"{Root}/Art/Tilesets/{tileName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.color = Color.white;
            tile.sprite = PixelArt.Get(spriteName);
            EditorUtility.SetDirty(tile);

            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(level, false);
            gridObject.AddComponent<Grid>();

            var mapObject = new GameObject("Floor");
            mapObject.transform.SetParent(gridObject.transform, false);
            var tilemap = mapObject.AddComponent<Tilemap>();
            var tilemapRenderer = mapObject.AddComponent<TilemapRenderer>();
            tilemapRenderer.sharedMaterial = LitMaterial;
            tilemapRenderer.sortingOrder = -10;

            for (int x = MinX; x < MaxX; x++)
            {
                for (int y = MinY; y < MaxY; y++)
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        /// <summary>Стіна-прямокутник: спрайт плиткою, колайдер і тінь, щоб світло не проходило крізь неї.</summary>
        private static void Wall(Transform level, int xMin, int yMin, int xMax, int yMax)
        {
            var size = new Vector2(xMax - xMin, yMax - yMin);
            var center = new Vector3(xMin + size.x / 2f, yMin + size.y / 2f, 0f);

            GameObject wall = CreateTiled("Wall", level, center, wallSprite, size, Color.white, 0);
            wall.AddComponent<BoxCollider2D>().size = size;

            // Тінь будується за формою спрайта, а плитка її не дає, тому тінь кидає окремий прозорий квадрат.
            // Він тонкий, щоб ближня до світла половина стіни лишалася видимою, і довший за стіну,
            // щоб на стику зі сусідньою стіною світло не прослизало між тінями.
            Vector3 shadowScale = size.x <= size.y ? new Vector3(0.1f, size.y + 1.2f, 1f) : new Vector3(size.x + 1.2f, 0.1f, 1f);
            GameObject shadow = CreateSprite("Shadow", wall.transform, Vector3.zero, shadowScale, Color.clear, 0);
            ConfigureCaster(shadow);
        }

        private static void ConfigureCaster(GameObject shadow)
        {
            var caster = shadow.AddComponent<ShadowCaster2D>();
            caster.useRendererSilhouette = true;
            caster.castsShadows = true;
            // Обрізання країв росте з масштабом і на довгій стіні лишає діру в тіні.
            caster.trimEdge = 0f;
        }

        private static GameObject CreatePlayer(Transform respawn)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = respawn.position;

            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<CircleCollider2D>().radius = 0.38f;
            SetBool(player.AddComponent<PlayerMotor2D>(), "topDown", true);
            AddInteractor(player);
            Wire(player.AddComponent<Health>(), ("healthChanged", Channel<FloatEventChannel>("PlayerHealthChanged")));
            Wire(player.AddComponent<PlayerDeathHandler>(), ("playerDied", Channel<VoidEventChannel>("PlayerDied")));

            CreatePlayerBody(player, "Top_");
            GameObject swing = CreateArt("Swing", player.transform, Vector3.zero, "Effect_Slash", 7, Color.white, UnlitMaterial);
            GameObject weapon = CreateArt("Weapon", player.transform, Vector3.zero, Bat.name, 6);
            // Малюнки зброї довгі: у натуральну величину вона була б зростом із героя.
            weapon.transform.localScale = Vector3.one * 0.7f;
            Wire(player.AddComponent<PlayerCombat>(), ("swingVisual", swing.GetComponent<SpriteRenderer>()), ("weaponVisual", weapon.GetComponent<SpriteRenderer>()));
            return player;
        }

        private static void MakeRevealable(GameObject target, bool hiddenPassage = false)
        {
            var revealable = target.AddComponent<RevealableObject>();
            Wire(revealable, ("config", FlameSettings), ("revealChanged", Channel<BoolEventChannel>("RevealChanged")));
            SetBool(revealable, "hiddenPassage", hiddenPassage);
        }

        private static void CreateDarkness(Transform player, GameObject enemyPrefab, float slowSpawn, float fastSpawn)
        {
            GameObject wall = CreateSprite("DarknessWall", null, new Vector3(92f, 0f, 0f), new Vector3(60f, 60f, 1f), Color.black, 50, UnlitMaterial);
            var darkness = new GameObject("Darkness").AddComponent<DarknessCreep>();
            Wire(darkness, ("wall", wall.transform), ("player", player), ("enemyPrefab", enemyPrefab), ("rangedPrefab", GetOrCreateWraithPrefab()),
                ("darknessChanged", Channel<FloatEventChannel>("DarknessChanged")));
            SetFloat(darkness, "slowSpawnInterval", slowSpawn);
            SetFloat(darkness, "fastSpawnInterval", fastSpawn);
        }

        private static GameObject GetOrCreateWraithPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(WraithPrefabPath);
            if (existing != null)
                return existing;

            GameObject enemy = CreateArt("Wraith", null, Vector3.zero, "Top_Wraith_A", 4);
            AddFrameLoop(enemy, 4f, "Top_Wraith_A", "Top_Wraith_B");
            var body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            enemy.AddComponent<CircleCollider2D>().radius = 0.5f;
            enemy.AddComponent<Health>();

            // Промінь не світиться сам: у темряві він зникає, тож джерело лишається невидимим.
            var beam = CreateArt("Beam", enemy.transform, Vector3.zero, "Effect_WraithBeam", 6).GetComponent<SpriteRenderer>();
            beam.drawMode = SpriteDrawMode.Tiled;
            Wire(enemy.AddComponent<WraithBrain>(), ("beam", beam));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, WraithPrefabPath);
            Object.DestroyImmediate(enemy);
            return prefab;
        }

        private static GameObject GetOrCreateEnemyPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (existing != null)
                return existing;

            GameObject enemy = CreateArt("Shade", null, Vector3.zero, "Top_Shade_A", 4);
            AddFrameLoop(enemy, 5f, "Top_Shade_A", "Top_Shade_B");
            var body = enemy.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            enemy.AddComponent<CircleCollider2D>().radius = 0.42f;
            enemy.AddComponent<Health>();
            Wire(enemy.AddComponent<EnemyBrain>(), ("data", GetOrCreate<EnemyData>(Root + "/Data/Enemies/Shade.asset")));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            Object.DestroyImmediate(enemy);
            return prefab;
        }
    }
}
