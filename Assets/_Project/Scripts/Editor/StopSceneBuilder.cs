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
        private const string DepotPath = Root + "/Scenes/Stops/Stop_Depot.unity";
        private const string MarketPath = Root + "/Scenes/Stops/Stop_Market.unity";
        private const string GunShopPath = Root + "/Scenes/Stops/Stop_GunShop.unity";

        // Межі локації в юнітах. Вхід ліворуч, темрява наповзає праворуч.
        private const int MinX = 0;
        private const int MaxX = 60;
        private const int MinY = -12;
        private const int MaxY = 12;

        private static bool overwrite;

        [MenuItem("Night Trolleybus/Scenes/Rebuild All Stops (overwrites scenes)")]
        public static void RebuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Префаб теж заново: у нього змінився колайдер і зник вплив гравітації.
            AssetDatabase.DeleteAsset(EnemyPrefabPath);
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
            if (!BeginScene(DepotPath, new Color(0.62f, 0.58f, 0.54f), "TopDepotFloor", out GameObject player, out Transform level))
                return;

            // Депо: електрика й запчастини, кілька кімнат і небагато ворогів.
            Wall(level, 18, -12, 19, -2);
            Wall(level, 18, 2, 19, 12);
            Wall(level, 38, -12, 39, 4);
            Wall(level, 38, 8, 39, 12);
            foreach (Vector2 crate in new[] { new Vector2(10f, 5f), new Vector2(12f, -6f), new Vector2(26f, 6f), new Vector2(30f, -5f), new Vector2(46f, -3f) })
                CreateCrate(level, crate);

            GameObject charger = CreateArt("PowerCharger", null, new Vector3(5f, 6f, 0f), "Top_Charger", 1);
            AddFittedCollider(charger, true);
            Wire(charger.AddComponent<PowerCharger>(), ("noticeRaised", Channel<StringEventChannel>("BusNoticeRaised")));

            Pickup("Match", Match, 8f, -3f);
            Pickup("Candle", Candle, 14f, 8f);
            Pickup("Battery_A", Battery, 25f, -8f);
            Pickup("Ticket", Ticket, 33f, 9f);

            // Прихована кімната в кутку: прохід до неї відкривається лише під Підсвічуванням.
            Wall(level, 52, 6, 60, 7);
            Wall(level, 52, 7, 53, 8);
            Wall(level, 52, 10, 53, 12);
            GameObject hiddenWall = CreateTiled("HiddenWall", null, new Vector3(52.5f, 9f, 0f), "Top_HiddenWall", new Vector2(1f, 2f), Color.white, 1);
            hiddenWall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 2f);
            MakeRevealable(hiddenWall, true);
            Pickup("Battery_Hidden", Battery, 56f, 9.5f);
            MakeRevealable(Pickup("Candle_Hidden", Candle, 44f, 9f));

            FinishScene(DepotPath, player, new[] { new Vector2(24f, 0f), new Vector2(34f, -8f), new Vector2(50f, 0f) });
        }

        [MenuItem("Night Trolleybus/Scenes/Build Stop Market Scene")]
        public static void BuildMarket()
        {
            if (!BeginScene(MarketPath, new Color(0.56f, 0.62f, 0.68f), "TopMarketFloor", out GameObject player, out Transform level))
                return;

            // Супермаркет: ряди полиць, багато предметів, але й багато ворогів.
            for (int x = 12; x < 56; x += 10)
            {
                Wall(level, x, -9, x + 1, -2);
                Wall(level, x, 2, x + 1, 9);
            }

            Pickup("Candle_A", Candle, 7f, 6f);
            Pickup("Match_A", Match, 16f, -6f);
            Pickup("Candle_B", Candle, 18f, 6f);
            Pickup("Match_B", Match, 26f, -5f);
            Pickup("Ticket_A", Ticket, 28f, 7f);
            Pickup("Candle_C", Candle, 37f, -7f);
            Pickup("Ticket_B", Ticket, 46f, 5f);
            MakeRevealable(Pickup("Ticket_Hidden", Ticket, 48f, -10f));
            MakeRevealable(Pickup("Candle_Hidden", Candle, 57f, 10f));

            FinishScene(MarketPath, player, new[] { new Vector2(16f, 0f), new Vector2(25f, 6f), new Vector2(31f, -6f), new Vector2(38f, 0f), new Vector2(47f, 7f), new Vector2(54f, -5f) });
        }

        [MenuItem("Night Trolleybus/Scenes/Build Stop Gun Shop Scene")]
        public static void BuildGunShop()
        {
            if (!BeginScene(GunShopPath, new Color(0.64f, 0.54f, 0.54f), "TopGunShopFloor", out GameObject player, out Transform level))
                return;

            // Магазин зброї: зал із прилавками, а далі склад із небезпечними знахідками.
            Wall(level, 26, -12, 27, -2);
            Wall(level, 26, 2, 27, 12);
            CreateCrate(level, new Vector2(36f, 4f));
            CreateCrate(level, new Vector2(44f, -6f));

            Pickup("Ticket", Ticket, 8f, -6f);
            MakeRevealable(Pickup("Ticket_Hidden", Ticket, 22f, -9f));
            Pickup("Ticket_Store", Ticket, 50f, 8f);

            CreateStand("Stand_Crowbar", new Vector2(13f, 5f), Ticket, 2, Crowbar, null);
            CreateStand("Stand_FireAxe", new Vector2(18f, 5f), Ticket, 4, FireAxe, null);
            CreateStand("Stand_Candle", new Vector2(23f, 5f), Ticket, 1, null, Candle);

            FinishScene(GunShopPath, player, new[] { new Vector2(38f, -2f), new Vector2(50f, 3f) });
        }

        private static bool BeginScene(string path, Color floorColor, string tileName, out GameObject player, out Transform level)
        {
            player = null;
            level = null;
            if (!overwrite && IsAlreadyBuilt(path, LevelName))
                return false;

            // Нова сцена вивантажує незбережені асети, тому створюємо її до них.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateGlobalLight();

            level = new GameObject(LevelName).transform;
            CreateFloor(level, floorColor, tileName);
            Wall(level, MinX - 1, MinY - 1, MaxX + 1, MinY);
            Wall(level, MinX - 1, MaxY, MaxX + 1, MaxY + 1);
            Wall(level, MinX - 1, MinY, MinX, MaxY);
            Wall(level, MaxX, MinY, MaxX + 1, MaxY);

            var respawn = new GameObject("RespawnPoint").transform;
            respawn.SetParent(level, false);
            respawn.position = new Vector3(3f, 0f, 0f);

            player = CreatePlayer(respawn);
            CreateFlame(player.transform);
            player.transform.Find("Flame").localPosition = new Vector3(0.45f, 0.55f, 0f);
            var flameLight = player.GetComponentInChildren<Light2D>();
            flameLight.shadowsEnabled = true;
            flameLight.shadowIntensity = 0.85f;
            CreateAbilities(player, player.transform, false);

            GameObject door = CreateArt("BusDoor", level, new Vector3(0.5f, 0f, 0f), "Top_Door", 1);
            AddFittedCollider(door, true);
            Wire(door.AddComponent<StopExit>(), ("stopCompleted", Channel<VoidEventChannel>("StopCompleted")));
            return true;
        }

        private static void FinishScene(string path, GameObject player, Vector2[] enemies)
        {
            GameObject enemyPrefab = GetOrCreateEnemyPrefab();
            foreach (Vector2 position in enemies)
            {
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
                enemy.transform.position = position;
            }

            CreateDarkness(player.transform, enemyPrefab);
            BusSceneBuilder.CreateFollowCameras(player.transform, 6f, 0f);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Сцену зібрано: {path}");
        }

        private static GameObject Pickup(string objectName, ItemData item, float x, float y) =>
            CreatePickup(objectName, item, new Vector3(x, y, 0f));

        private static void CreateStand(string objectName, Vector2 position, ItemData cost, int count, WeaponData weapon, ItemData item)
        {
            GameObject stand = CreateArt(objectName, null, position, "Top_Counter", 1);
            AddFittedCollider(stand, true);
            var trade = stand.AddComponent<TradeStand>();
            Wire(trade, ("costItem", cost), ("rewardWeapon", weapon), ("rewardItem", item), ("noticeRaised", Channel<StringEventChannel>("BusNoticeRaised")));
            SetInt(trade, "costCount", count);
        }

        private static void CreateFloor(Transform level, Color color, string tileName)
        {
            string tilePath = $"{Root}/Art/Tilesets/{tileName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.color = color;
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = PixelArt.Get("Top_Floor");
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

            GameObject wall = CreateTiled("Wall", level, center, "Top_Wall", size, Color.white, 0);
            wall.AddComponent<BoxCollider2D>().size = size;

            // Тінь будується за формою спрайта, а плитка її не дає, тому тінь кидає окремий прозорий квадрат.
            GameObject shadow = CreateSprite("Shadow", wall.transform, Vector3.zero, new Vector3(size.x, size.y, 1f), Color.clear, 0);
            var caster = shadow.AddComponent<ShadowCaster2D>();
            caster.useRendererSilhouette = true;
            caster.castsShadows = true;
        }

        private static void CreateCrate(Transform level, Vector2 position)
        {
            GameObject crate = CreateArt($"Crate_{position.x}_{position.y}", level, position, "Top_Crate", 2);
            AddFittedCollider(crate, false);
            var caster = crate.AddComponent<ShadowCaster2D>();
            caster.useRendererSilhouette = true;
            caster.castsShadows = true;
        }

        private static GameObject CreatePlayer(Transform respawn)
        {
            var noticeRaised = Channel<StringEventChannel>("BusNoticeRaised");
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = respawn.position;

            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<CircleCollider2D>().radius = 0.38f;
            SetBool(player.AddComponent<PlayerMotor2D>(), "topDown", true);
            Wire(player.AddComponent<PlayerInteractor>(), ("promptChanged", Channel<StringEventChannel>("InteractionPromptChanged")));
            Wire(player.AddComponent<Health>(), ("healthChanged", Channel<FloatEventChannel>("PlayerHealthChanged")));
            Wire(player.AddComponent<PlayerDeathHandler>(), ("respawnPoint", respawn), ("noticeRaised", noticeRaised));

            CreatePlayerBody(player, "Top_");
            GameObject swing = CreateArt("Swing", player.transform, Vector3.zero, "Swing", 6, Color.white, UnlitMaterial);
            Wire(player.AddComponent<PlayerCombat>(), ("swingVisual", swing.GetComponent<SpriteRenderer>()));
            return player;
        }

        private static void MakeRevealable(GameObject target, bool hiddenPassage = false)
        {
            var revealable = target.AddComponent<RevealableObject>();
            Wire(revealable, ("config", FlameSettings), ("revealChanged", Channel<BoolEventChannel>("RevealChanged")));
            SetBool(revealable, "hiddenPassage", hiddenPassage);
        }

        private static void CreateDarkness(Transform player, GameObject enemyPrefab)
        {
            GameObject wall = CreateSprite("DarknessWall", null, new Vector3(92f, 0f, 0f), new Vector3(60f, 60f, 1f), Color.black, 50, UnlitMaterial);
            var darkness = new GameObject("Darkness").AddComponent<DarknessCreep>();
            Wire(darkness, ("wall", wall.transform), ("player", player), ("enemyPrefab", enemyPrefab),
                ("darknessChanged", Channel<FloatEventChannel>("DarknessChanged")));
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
