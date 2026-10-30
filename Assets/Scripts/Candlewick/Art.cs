using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Candlewick
{
    /// <summary>Palette, materials and the game's sprites, loaded from Assets/Resources/Sprites.</summary>
    public static class Art
    {
        public static readonly Color Background = new Color(0.035f, 0.03f, 0.06f);
        public static readonly Color Wax = new Color(0.96f, 0.88f, 0.72f);
        public static readonly Color Sunray = new Color(1f, 0.8f, 0.36f);
        public static readonly Color WaxDark = new Color(0.45f, 0.42f, 0.46f);
        public static readonly Color Ember = new Color(1f, 0.45f, 0.08f);
        public static readonly Color Flame = new Color(1f, 0.78f, 0.28f);
        public static readonly Color Core = new Color(1f, 0.96f, 0.75f);
        public static readonly Color Blood = new Color(0.9f, 0.14f, 0.08f);
        public static readonly Color Gloom = new Color(0.32f, 0.12f, 0.5f);

        public static Material Lit, Glow;
        public static Sprite Soft, Disk, Square, Moth, Snuffer, Creeper, Ghost, Soul, Child, Cape, Bunk, Desk, Wardrobe, Fridge, Stove, Shower, TileKitchen, TileShower, TileStairs, Landing, StairFlight, Washer, Sofa, Tv, Shelf, Boxes, Floor, FloorAlt, WallTop, WallFace, Plank, Sky, SkyStars, Window, Banner, Pillar, Arrow;

        public static void Init()
        {
            if (Lit != null && Soft != null) return;

            Lit = Resources.Load<Material>("CandlewickLit");
            if (Lit == null) Lit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            Glow = Resources.Load<Material>("CandlewickGlow");
            if (Glow == null) Glow = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

            Soft = Load("soft");
            Disk = Load("disk");
            Square = Load("square");
            Arrow = Load("arrow");
            Moth = Load("moth");
            Snuffer = Load("snuffer");
            Creeper = Load("creeper");
            Ghost = Load("ghost");
            Soul = Load("soul");
            Child = Load("child");
            Cape = Load("cape");
            Bunk = Load("bunk");
            Desk = Load("desk");
            Wardrobe = Load("wardrobe");
            Fridge = Load("fridge");
            Stove = Load("stove");
            Shower = Load("shower");
            TileKitchen = Load("tile_kitchen");
            TileShower = Load("tile_shower");
            TileStairs = Load("tile_stairs");
            Landing = Load("landing");
            StairFlight = Load("stair_flight");
            Washer = Load("washer");
            Sofa = Load("sofa");
            Tv = Load("tv");
            Shelf = Load("shelf");
            Boxes = Load("boxes");

            // Side view: Floor is the dormitory wall behind the action, WallTop is solid rock, WallFace a walkable ledge.
            Floor = Load("wall_back");
            FloorAlt = Load("wall_back_alt");
            WallTop = Load("stone");
            WallFace = Load("ledge");
            Plank = Load("plank");
            Sky = Load("sky");
            SkyStars = Load("sky_stars");
            Window = Load("window");
            Banner = Load("banner");
            Pillar = Load("pillar");
        }

        static Sprite Load(string name)
        {
            var sprite = Resources.Load<Sprite>("Sprites/" + name);
            if (sprite == null) Debug.LogError($"Missing sprite Assets/Resources/Sprites/{name}.png");
            return sprite;
        }

        public static GameObject Spr(string name, Transform parent, Sprite sprite, Material mat, Color color, int order, float scale = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        public static Light2D AddLight(GameObject go, Color color, float radius, float intensity)
        {
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.2f;
            light.falloffIntensity = 0.45f;
            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
            light.targetSortingLayers = ids;
            return light;
        }
    }
}
