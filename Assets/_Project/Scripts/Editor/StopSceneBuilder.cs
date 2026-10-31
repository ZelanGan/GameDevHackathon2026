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
    /// <summary>Збирає сцени зупинок із плейсхолдерів. Вже зібрані сцени не перезаписує.</summary>
    public static class StopSceneBuilder
    {
        private const string LevelName = "StopLevel";
        private const string EnemyPrefabPath = Root + "/Prefabs/Enemies/Shade.prefab";

        [MenuItem("Night Trolleybus/Scenes/Build Stop Depot Scene")]
        public static void BuildDepot()
        {
            string path = Root + "/Scenes/Stops/Stop_Depot.unity";
            if (!BeginScene(path, new Color(0.35f, 0.32f, 0.3f), "DepotGround", new[] { 8f, 22f, 40f }, out GameObject player))
                return;

            // Депо: електрика й запчастини, небагато ворогів.
            GameObject charger = CreateArt("PowerCharger", null, new Vector3(5f, -1.2f, 0f), "Charger", 1);
            AddFittedCollider(charger, true);
            Wire(charger.AddComponent<PowerCharger>(), ("noticeRaised", Channel<StringEventChannel>("BusNoticeRaised")));

            Pickup("Match", Match, 7f);
            Pickup("Candle", Candle, 12f, 1.4f);
            Pickup("Battery_A", Battery, 25f);
            Pickup("Ticket", Ticket, 30f, 1.4f);

            // Прихований прохід: платформа, яку видно лише під Підсвічуванням, веде до схованого акумулятора.
            GameObject hiddenPlatform = CreateArt("HiddenPlatform", null, new Vector3(35.5f, 2.5f, 0f), "HiddenPlank", 1);
            AddFittedCollider(hiddenPlatform, false);
            MakeRevealable(hiddenPlatform);
            MakeRevealable(Pickup("Battery_Hidden", Battery, 36f, 3.5f));
            MakeRevealable(Pickup("Candle_Hidden", Candle, 44f));

            FinishScene(path, player, new[] { 20f, 34f, 50f });
        }

        [MenuItem("Night Trolleybus/Scenes/Build Stop Market Scene")]
        public static void BuildMarket()
        {
            string path = Root + "/Scenes/Stops/Stop_Market.unity";
            if (!BeginScene(path, new Color(0.3f, 0.33f, 0.36f), "MarketGround", new[] { 15f, 27f, 37f, 46f }, out GameObject player))
                return;

            // Супермаркет: багато предметів, але й багато ворогів.
            Pickup("Candle_A", Candle, 7f);
            Pickup("Match_A", Match, 11f, 1.3f);
            Pickup("Candle_B", Candle, 18f, 3.4f);
            Pickup("Match_B", Match, 24f);
            Pickup("Ticket_A", Ticket, 30f, 1.4f);
            Pickup("Candle_C", Candle, 33f);
            Pickup("Ticket_B", Ticket, 42f);
            MakeRevealable(Pickup("Ticket_Hidden", Ticket, 50f));
            MakeRevealable(Pickup("Candle_Hidden", Candle, 55f));

            FinishScene(path, player, new[] { 14f, 22f, 31f, 39f, 47f, 54f });
        }

        [MenuItem("Night Trolleybus/Scenes/Build Stop Gun Shop Scene")]
        public static void BuildGunShop()
        {
            string path = Root + "/Scenes/Stops/Stop_GunShop.unity";
            if (!BeginScene(path, new Color(0.34f, 0.29f, 0.3f), "GunShopGround", new[] { 20f }, out GameObject player))
                return;

            // Магазин зброї: обмін предметів на зброю та кілька знахідок на підході.
            Pickup("Ticket", Ticket, 9f);
            MakeRevealable(Pickup("Ticket_Hidden", Ticket, 26f));

            CreateStand("Stand_Crowbar", 14f, Ticket, 2, Crowbar, null);
            CreateStand("Stand_FireAxe", 17f, Ticket, 4, FireAxe, null);
            CreateStand("Stand_Candle", 23f, Ticket, 1, null, Candle);

            FinishScene(path, player, new[] { 35f, 48f });
        }

        private static bool BeginScene(string path, Color groundColor, string tileName, float[] crates, out GameObject player)
        {
            player = null;
            if (IsAlreadyBuilt(path, LevelName))
                return false;

            // Нова сцена вивантажує незбережені асети, тому створюємо її до них.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateGlobalLight();

            var level = new GameObject(LevelName).transform;
            CreateTerrain(level, groundColor, tileName);
            CreateTiled("Background", level, new Vector3(30f, 3f, 0f), "BrickWall", new Vector2(70f, 12f), groundColor, -10);
            foreach (float x in crates)
                CreateCrate(level, x);

            var respawn = new GameObject("RespawnPoint").transform;
            respawn.SetParent(level, false);
            respawn.position = new Vector3(3f, -1.2f, 0f);

            player = CreatePlayer(respawn);
            CreateFlame(player.transform);
            var flameLight = player.GetComponentInChildren<Light2D>();
            flameLight.shadowsEnabled = true;
            flameLight.shadowIntensity = 0.85f;
            CreateAbilities(player, player.transform, false);

            GameObject door = CreateArt("BusDoor", level, new Vector3(1f, -1f, 0f), "StopDoor", -1);
            AddFittedCollider(door, true);
            Wire(door.AddComponent<StopExit>(), ("stopCompleted", Channel<VoidEventChannel>("StopCompleted")));
            return true;
        }

        private static void FinishScene(string path, GameObject player, float[] enemies)
        {
            GameObject enemyPrefab = GetOrCreateEnemyPrefab();
            foreach (float x in enemies)
            {
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
                enemy.transform.position = new Vector3(x, -1.2f, 0f);
            }

            CreateDarkness(player.transform, enemyPrefab);
            BusSceneBuilder.CreateFollowCameras(player.transform);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            AssetDatabase.SaveAssets();
            Debug.Log($"Сцену зібрано: {path}");
        }

        private static GameObject Pickup(string objectName, ItemData item, float x, float y = -1.7f) =>
            CreatePickup(objectName, item, new Vector3(x, y, 0f));

        private static void CreateStand(string objectName, float x, ItemData cost, int count, WeaponData weapon, ItemData item)
        {
            GameObject stand = CreateArt(objectName, null, new Vector3(x, -1.4f, 0f), "Counter", 1);
            AddFittedCollider(stand, true);
            var trade = stand.AddComponent<TradeStand>();
            Wire(trade, ("costItem", cost), ("rewardWeapon", weapon), ("rewardItem", item), ("noticeRaised", Channel<StringEventChannel>("BusNoticeRaised")));
            SetInt(trade, "costCount", count);
        }

        private static void CreateTerrain(Transform level, Color color, string tileName)
        {
            string tilePath = $"{Root}/Art/Tilesets/{tileName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.color = color;
                tile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = PixelArt.Get("GroundTile");
            EditorUtility.SetDirty(tile);

            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(level, false);
            gridObject.AddComponent<Grid>();

            var mapObject = new GameObject("Ground");
            mapObject.transform.SetParent(gridObject.transform, false);
            var tilemap = mapObject.AddComponent<Tilemap>();
            mapObject.AddComponent<TilemapRenderer>().sharedMaterial = LitMaterial;
            mapObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var tilemapCollider = mapObject.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var compositeCollider = mapObject.AddComponent<CompositeCollider2D>();

            FillTiles(tilemap, tile, -4, 66, -6, -3);   // Земля, верхній край на y = -2.
            FillTiles(tilemap, tile, -4, -3, -2, 8);    // Ліва межа.
            FillTiles(tilemap, tile, 65, 66, -2, 8);    // Права межа.
            FillTiles(tilemap, tile, 10, 13, 0, 0);     // Платформи.
            FillTiles(tilemap, tile, 16, 19, 2, 2);
            FillTiles(tilemap, tile, 28, 31, 0, 0);

            // Без явної генерації зведений колайдер лишається порожнім, і гравець провалюється.
            tilemapCollider.ProcessTilemapChanges();
            compositeCollider.GenerateGeometry();
        }

        private static void FillTiles(Tilemap tilemap, TileBase tile, int minX, int maxX, int minY, int maxY)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }

        private static void CreateCrate(Transform level, float x)
        {
            GameObject crate = CreateArt($"Crate_{x}", level, new Vector3(x, -1.25f, 0f), "Crate", 2);
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
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.6f);
            player.AddComponent<PlayerMotor2D>();
            Wire(player.AddComponent<PlayerInteractor>(), ("promptChanged", Channel<StringEventChannel>("InteractionPromptChanged")));
            Wire(player.AddComponent<Health>(), ("healthChanged", Channel<FloatEventChannel>("PlayerHealthChanged")));
            Wire(player.AddComponent<PlayerDeathHandler>(), ("respawnPoint", respawn), ("noticeRaised", noticeRaised));

            CreatePlayerBody(player);
            GameObject swing = CreateArt("Swing", player.transform, Vector3.zero, "Swing", 6, Color.white, UnlitMaterial);
            Wire(player.AddComponent<PlayerCombat>(), ("swingVisual", swing.GetComponent<SpriteRenderer>()));
            return player;
        }

        private static void MakeRevealable(GameObject target)
        {
            Wire(target.AddComponent<RevealableObject>(), ("config", FlameSettings), ("revealChanged", Channel<BoolEventChannel>("RevealChanged")));
        }

        private static void CreateDarkness(Transform player, GameObject enemyPrefab)
        {
            GameObject wall = CreateSprite("DarknessWall", null, new Vector3(92f, 2f, 0f), new Vector3(60f, 24f, 1f), Color.black, 50, UnlitMaterial);
            var darkness = new GameObject("Darkness").AddComponent<DarknessCreep>();
            Wire(darkness, ("wall", wall.transform), ("player", player), ("enemyPrefab", enemyPrefab),
                ("darknessChanged", Channel<FloatEventChannel>("DarknessChanged")));
        }

        private static GameObject GetOrCreateEnemyPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (existing != null)
                return existing;

            GameObject enemy = CreateArt("Shade", null, Vector3.zero, "Shade_A", 4);
            AddFrameLoop(enemy, 5f, "Shade_A", "Shade_B");
            var body = enemy.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            AddFittedCollider(enemy, false);
            enemy.AddComponent<Health>();
            Wire(enemy.AddComponent<EnemyBrain>(), ("data", GetOrCreate<EnemyData>(Root + "/Data/Enemies/Shade.asset")));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, EnemyPrefabPath);
            Object.DestroyImmediate(enemy);
            return prefab;
        }
    }
}
