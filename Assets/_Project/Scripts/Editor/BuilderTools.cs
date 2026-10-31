using System.IO;
using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Flame;
using NightTrolleybus.Items;
using NightTrolleybus.Items.Effects;
using NightTrolleybus.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace NightTrolleybus.EditorTools
{
    /// <summary>Спільні кроки збирачів сцен. Наявні асети не перезаписує.</summary>
    public static class BuilderTools
    {
        public const string Root = "Assets/_Project";
        private const string LitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        private const string UnlitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private const string SpritesPath = Root + "/Art/Sprites/Placeholders";

        public static Material LitMaterial => AssetDatabase.LoadAssetAtPath<Material>(LitMaterialPath);
        public static Material UnlitMaterial => AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterialPath);
        public static Sprite Square => GetOrCreateSprite("Square", false);
        public static Sprite Circle => GetOrCreateSprite("Circle", true);
        public static FlameConfig FlameSettings => GetOrCreate<FlameConfig>(Root + "/Data/Config/FlameConfig.asset");

        public static T Channel<T>(string channelName) where T : ScriptableObject =>
            GetOrCreate<T>($"{Root}/Data/Channels/{channelName}.asset");

        public static bool IsAlreadyBuilt(string scenePath, string marker)
        {
            if (!File.Exists(scenePath) || !File.ReadAllText(scenePath).Contains($"m_Name: {marker}"))
                return false;

            Debug.LogError($"Сцена вже зібрана: {scenePath}. Щоб зібрати заново, приберіть із неї об'єкт {marker}.");
            return true;
        }

        public static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static void Wire(Object target, params (string field, Object value)[] references)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in references)
                serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        public static void WireArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        public static void SetFloat(Object target, string field, float value) =>
            Edit(target, serialized => serialized.FindProperty(field).floatValue = value);

        public static void SetString(Object target, string field, string value) =>
            Edit(target, serialized => serialized.FindProperty(field).stringValue = value);

        public static void SetBool(Object target, string field, bool value) =>
            Edit(target, serialized => serialized.FindProperty(field).boolValue = value);

        public static void SetEnum(Object target, string field, int value) =>
            Edit(target, serialized => serialized.FindProperty(field).enumValueIndex = value);

        public static void SetInt(Object target, string field, int value) =>
            Edit(target, serialized => serialized.FindProperty(field).intValue = value);

        private static void Edit(Object target, System.Action<SerializedObject> change)
        {
            var serialized = new SerializedObject(target);
            change(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        public static GameObject CreateSprite(string objectName, Transform parent, Vector3 position, Vector3 scale, Color color, int order, Material material = null)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = position;
            spriteObject.transform.localScale = scale;

            var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Square;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = order;
            spriteRenderer.sharedMaterial = material != null ? material : LitMaterial;
            return spriteObject;
        }

        /// <summary>Об'єкт із намальованим спрайтом у натуральну величину: 32 пікселі на юніт.</summary>
        public static GameObject CreateArt(string objectName, Transform parent, Vector3 position, string spriteName, int order, Color? color = null, Material material = null)
        {
            GameObject art = CreateSprite(objectName, parent, position, Vector3.one, color ?? Color.white, order, material);
            art.GetComponent<SpriteRenderer>().sprite = PixelArt.Get(spriteName);
            return art;
        }

        /// <summary>Те саме, але колайдер охоплює весь спрайт.</summary>
        public static BoxCollider2D AddFittedCollider(GameObject art, bool isTrigger)
        {
            var box = art.AddComponent<BoxCollider2D>();
            box.size = art.GetComponent<SpriteRenderer>().sprite.bounds.size;
            box.isTrigger = isTrigger;
            return box;
        }

        /// <summary>Тло, що повторює спрайт плиткою на заданий розмір у юнітах.</summary>
        public static GameObject CreateTiled(string objectName, Transform parent, Vector3 position, string spriteName, Vector2 size, Color color, int order)
        {
            GameObject art = CreateArt(objectName, parent, position, spriteName, order, color);
            var spriteRenderer = art.GetComponent<SpriteRenderer>();
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            spriteRenderer.size = size;
            return art;
        }

        public static GameObject CreatePickup(string objectName, ItemData item, Vector3 position)
        {
            GameObject pickup = CreateArt(objectName, null, position, $"Item_{item.Id}", 3);
            AddFittedCollider(pickup, true);
            Wire(pickup.AddComponent<ItemPickup>(), ("item", item));
            return pickup;
        }

        public static void AddFrameLoop(GameObject target, float framesPerSecond, params string[] spriteNames)
        {
            var loop = target.AddComponent<SpriteFrameLoop>();
            var frames = new Object[spriteNames.Length];
            for (int i = 0; i < spriteNames.Length; i++)
                frames[i] = PixelArt.Get(spriteNames[i]);
            WireArray(loop, "frames", frames);
            SetFloat(loop, "framesPerSecond", framesPerSecond);
        }

        /// <summary>Тіло гравця зі спрайтом і анімацією ходи.</summary>
        public static void CreatePlayerBody(GameObject player, string spritePrefix = "")
        {
            Sprite idle = PixelArt.Get($"{spritePrefix}Player_Idle");
            GameObject body = CreateArt("Body", player.transform, Vector3.zero, $"{spritePrefix}Player_Idle", 5);
            var view = player.AddComponent<PlayerSpriteView>();
            Wire(view,
                ("motor", player.GetComponent<PlayerMotor2D>()), ("body", player.GetComponent<Rigidbody2D>()),
                ("spriteRenderer", body.GetComponent<SpriteRenderer>()), ("idleFrame", idle));
            WireArray(view, "walkFrames", new Object[] { PixelArt.Get($"{spritePrefix}Player_Walk_A"), idle, PixelArt.Get($"{spritePrefix}Player_Walk_B"), idle });

            // Спиною водія видно лише згори: у салоні він ходить тільки вбік.
            if (spritePrefix.Length == 0)
                return;

            Sprite backIdle = PixelArt.Get($"{spritePrefix}Player_Back_Idle");
            Wire(view, ("backIdleFrame", backIdle));
            WireArray(view, "backWalkFrames", new Object[] { PixelArt.Get($"{spritePrefix}Player_Back_Walk_A"), backIdle, PixelArt.Get($"{spritePrefix}Player_Back_Walk_B"), backIdle });
        }

        public static void AddInteractor(GameObject player) =>
            Wire(player.AddComponent<PlayerInteractor>(),
                ("keyBadge", PixelArt.Get("Key_E")), ("badgeMaterial", UnlitMaterial),
                ("labelFont", Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")));

        public static void CreateFlame(Transform parent)
        {
            var flame = new GameObject("Flame");
            flame.transform.SetParent(parent, false);
            flame.transform.localPosition = new Vector3(0.7f, 0.9f, 0f);

            var flameLight = flame.AddComponent<Light2D>();
            flameLight.lightType = Light2D.LightType.Point;

            // Свічка окремо: розмір змінює лише вогник, а темніє вона разом зі світлом, яке він дає.
            CreateArt("Candle", flame.transform, Vector3.zero, "Flame_Candle", 9);
            // Чаша перекриває низ свічки, тому свічка наче стоїть у ній.
            CreateArt("Bowl", flame.transform, new Vector3(0f, -0.3125f, 0f), "Flame_Bowl", 11);
            GameObject sprite = CreateArt("Sprite", flame.transform, Vector3.zero, "Flame_A", 10, Color.white, UnlitMaterial);
            var spriteRenderer = sprite.GetComponent<SpriteRenderer>();
            AddFrameLoop(sprite, 7f, "Flame_A", "Flame_B", "Flame_A", "Flame_C");

            Wire(flame.AddComponent<FlameView>(),
                ("config", FlameSettings), ("flameStateChanged", Channel<FlameStateEventChannel>("FlameStateChanged")),
                ("flameLight", flameLight), ("flameSprite", spriteRenderer),
                ("flashTriggered", Channel<VoidEventChannel>("FlashTriggered")));
        }

        public static void CreateAbilities(GameObject host, Transform origin, bool canWarm)
        {
            var abilities = host.AddComponent<FlameAbilities>();
            Wire(abilities,
                ("origin", origin), ("noticeRaised", Channel<StringEventChannel>("BusNoticeRaised")),
                ("flashTriggered", Channel<VoidEventChannel>("FlashTriggered")),
                ("revealChanged", Channel<BoolEventChannel>("RevealChanged")));
            SetBool(abilities, "canWarm", canWarm);
        }

        public static void CreateGlobalLight(float intensity = 0.03f)
        {
            var globalLight = new GameObject("Global Light 2D").AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = intensity;
        }

        public static Camera CreateMainCamera()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            return camera;
        }

        public static Text CreateLabel(string objectName, Transform parent, float y, float height, string text)
        {
            var label = new GameObject(objectName).AddComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 26;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(32f, y);
            labelRect.sizeDelta = new Vector2(860f, height);
            return label;
        }

        public static ItemData GetOrCreateItem(string assetName, string id, string displayName, ItemEffect effect)
        {
            string path = $"{Root}/Data/Items/{assetName}.asset";
            bool exists = File.Exists(path);
            var item = GetOrCreate<ItemData>(path);
            // Значок оновлюємо і в наявних предметах: раніше там стояв порожній квадрат.
            Wire(item, ("icon", PixelArt.Get($"Item_{id}")));
            if (exists)
                return item;

            SetString(item, "id", id);
            SetString(item, "displayName", displayName);
            WireArray(item, "effects", effect != null ? new Object[] { effect } : new Object[0]);
            return item;
        }

        public static RestoreFlameEffect FlameEffect(string assetName, float amount)
        {
            var effect = GetOrCreate<RestoreFlameEffect>($"{Root}/Data/Items/Effects/RestoreFlame_{assetName}.asset");
            SetFloat(effect, "amount", amount);
            return effect;
        }

        public static ItemData Match => GetOrCreateItem("Match", "match", "Сірник", FlameEffect("Match", 8f));
        public static ItemData Candle => GetOrCreateItem("Candle", "candle", "Свічка", FlameEffect("Candle", 25f));
        public static ItemData Ticket => GetOrCreateItem("Ticket", "ticket", "Старий квиток", null);

        public static ItemData Battery
        {
            get
            {
                var effect = GetOrCreate<RestorePowerEffect>($"{Root}/Data/Items/Effects/RestorePower_Battery.asset");
                SetFloat(effect, "amount", 25f);
                return GetOrCreateItem("Battery", "battery", "Акумулятор", effect);
            }
        }

        public static WeaponData GetOrCreateWeapon(string assetName, string id, string displayName, float damage, float cooldown, float range)
        {
            string path = $"{Root}/Data/Items/{assetName}.asset";
            if (File.Exists(path))
                return AssetDatabase.LoadAssetAtPath<WeaponData>(path);

            var weapon = GetOrCreate<WeaponData>(path);
            SetString(weapon, "id", id);
            SetString(weapon, "displayName", displayName);
            SetFloat(weapon, "damage", damage);
            SetFloat(weapon, "cooldown", cooldown);
            SetFloat(weapon, "range", range);
            return weapon;
        }

        public static WeaponData Pipe => GetOrCreateWeapon("Weapon_Pipe", "pipe", "Обрізок труби", 25f, 0.4f, 1.4f);
        public static WeaponData Crowbar => GetOrCreateWeapon("Weapon_Crowbar", "crowbar", "Лом", 40f, 0.5f, 1.6f);
        public static WeaponData FireAxe => GetOrCreateWeapon("Weapon_FireAxe", "fire_axe", "Пожежна сокира", 60f, 0.7f, 1.9f);

        public static VolumeProfile GetOrCreateVolumeProfile()
        {
            const string path = "Assets/Settings/FlameVolumeProfile.asset";
            if (File.Exists(path))
                return AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.color.Override(Color.black);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.6f);
            AssetDatabase.AddObjectToAsset(vignette, profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static Sprite GetOrCreateSprite(string spriteName, bool isCircle)
        {
            string path = $"{SpritesPath}/{spriteName}.png";
            if (!File.Exists(path))
            {
                EnsureFolder(SpritesPath);
                const int size = 32;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool filled = !isCircle || Vector2.Distance(new Vector2(x, y), center) <= size / 2f;
                        texture.SetPixel(x, y, filled ? Color.white : Color.clear);
                    }
                }

                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = size;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
