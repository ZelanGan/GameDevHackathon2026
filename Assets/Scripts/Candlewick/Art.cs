using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Candlewick
{
    /// <summary>Palette, materials and every sprite in the game, generated at runtime.</summary>
    public static class Art
    {
        public static readonly Color Background = new Color(0.035f, 0.03f, 0.06f);
        public static readonly Color Wax = new Color(0.96f, 0.88f, 0.72f);
        public static readonly Color WaxDark = new Color(0.45f, 0.42f, 0.46f);
        public static readonly Color Ember = new Color(1f, 0.45f, 0.08f);
        public static readonly Color Flame = new Color(1f, 0.78f, 0.28f);
        public static readonly Color Core = new Color(1f, 0.96f, 0.75f);
        public static readonly Color Blood = new Color(0.9f, 0.14f, 0.08f);
        public static readonly Color Gloom = new Color(0.32f, 0.12f, 0.5f);

        public static Material Lit, Glow;
        public static Sprite Soft, Disk, Square, Moth, Snuffer, Creeper, Ghost, Floor, FloorAlt, WallTop, WallFace, Plank, Sky, SkyStars, Window, Banner, Pillar, Arrow;

        public static void Init()
        {
            if (Lit != null && Soft != null) return;

            Lit = Resources.Load<Material>("CandlewickLit");
            if (Lit == null) Lit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            Glow = Resources.Load<Material>("CandlewickGlow");
            if (Glow == null) Glow = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

            Soft = Make(64, (x, y) => { float d = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y)); return White(d * d); });
            Disk = Make(48, (x, y) => White(Edge(1f - Mathf.Sqrt(x * x + y * y), 0.05f)));
            Square = Make(4, (x, y) => Color.white);
            Moth = Make(64, MothShape);
            Snuffer = Make(64, SnufferShape);
            Creeper = Make(64, CreeperShape);
            Ghost = Make(64, GhostShape);
            Arrow = Make(32, (x, y) => White(Edge(0.85f - (Mathf.Abs(y) * 1.6f + Mathf.Abs(x)), 0.08f)));

            // Side view: Floor is the castle wall behind the action, WallTop is solid rock, WallFace a walkable ledge.
            Floor = Tile(new Color(0.2f, 0.18f, 0.27f), 0.03f, 11, true);
            FloorAlt = Tile(new Color(0.17f, 0.15f, 0.24f), 0.03f, 29, true);
            WallTop = Tile(new Color(0.06f, 0.05f, 0.09f), 0.01f, 47, false);
            WallFace = Tile(new Color(0.62f, 0.52f, 0.66f), 0.06f, 71, false);
            Plank = PlankTile();
            Sky = Tile(new Color(0.03f, 0.04f, 0.11f), 0.004f, 83, false, false);
            SkyStars = Tile(new Color(0.03f, 0.04f, 0.11f), 0.004f, 97, false, true);
            Window = Make(32, WindowShape);
            Banner = Make(32, BannerShape);
            Pillar = Make(32, PillarShape);
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

        // Shapes receive x,y in [-1,1]; +x is the sprite's facing direction.
        static Sprite Make(int size, Func<float, float, Color> shade, FilterMode filter = FilterMode.Bilinear)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                    px[j * size + i] = shade((i + 0.5f) / size * 2f - 1f, (j + 0.5f) / size * 2f - 1f);
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Color WindowShape(float x, float y)
        {
            // Arched top over a straight body, split by a stone mullion.
            float body = y < 0.3f ? 0.8f - Mathf.Abs(x) : 0.8f - Mathf.Sqrt(x * x + (y - 0.3f) * (y - 0.3f) * 1.8f);
            float d = Mathf.Min(body, y + 0.95f);
            Color c = Mathf.Abs(x) < 0.07f || Mathf.Abs(y + 0.25f) < 0.05f ? new Color(0.1f, 0.1f, 0.18f) : Color.white;
            c.a = Edge(d, 0.05f);
            return c;
        }

        static Color BannerShape(float x, float y)
        {
            // Hanging cloth ending in a swallowtail, with a pale stripe.
            float bottom = -1f + 0.55f * (1f - Mathf.Abs(x) / 0.7f);
            float d = Mathf.Min(0.7f - Mathf.Abs(x), Mathf.Min(0.95f - y, bottom < y ? y - bottom : -1f));
            Color c = Mathf.Abs(y - 0.55f) < 0.08f ? new Color(1f, 0.9f, 0.5f) : Color.white;
            c.a = Edge(d, 0.05f);
            return c;
        }

        static Color PillarShape(float x, float y)
        {
            // A fluted shaft between a wider capital and base.
            bool capital = Mathf.Abs(y) > 0.9f;
            float halfWidth = capital ? 0.95f : 0.62f;
            float shade = capital ? 0.42f : 0.26f + 0.08f * Mathf.Cos(x * 14f);
            return new Color(shade, shade * 0.95f, shade * 1.15f, Edge(halfWidth - Mathf.Abs(x), 0.05f));
        }

        static Sprite Tile(Color baseColor, float noise, int seed, bool brickLines, bool star = false)
        {
            var rng = new System.Random(seed);
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float n = ((float)rng.NextDouble() - 0.5f) * noise * 2f;
                    Color c = baseColor + new Color(n, n, n, 0f);
                    if (brickLines && (y % 5 == 0 || (x + (y / 5) * 6) % 12 == 0)) c *= 0.55f;
                    if (!brickLines && !star && (x == 0 || y == 0)) c *= 0.82f;
                    if (star && x == 7 && y == 9) c = new Color(0.85f, 0.88f, 1f);
                    c.a = 1f;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // A thin wooden board along the top of its cell, so the wall behind stays visible.
        static Sprite PlankTile()
        {
            const int size = 16, thickness = 5;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool board = y >= size - thickness;
                    Color c = new Color(0.72f, 0.48f, 0.28f);
                    if (y == size - 1) c = new Color(0.9f, 0.68f, 0.42f);
                    if (y == size - thickness || x == 0) c *= 0.55f;
                    c.a = board ? 1f : 0f;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Color White(float a) => new Color(1f, 1f, 1f, a);
        static float Edge(float d, float width) => Mathf.Clamp01(d / width);

        static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            return 1f - Mathf.Sqrt(dx * dx + dy * dy);
        }

        static Color MothShape(float x, float y)
        {
            float wing = Mathf.Max(Ellipse(x, Mathf.Abs(y), 0.05f, 0.48f, 0.42f, 0.48f), Ellipse(x, Mathf.Abs(y), -0.4f, 0.3f, 0.32f, 0.3f));
            float body = Ellipse(x, y, 0f, 0f, 0.62f, 0.16f);
            float a = Edge(Mathf.Max(wing, body), 0.08f);
            return Color.Lerp(new Color(0.42f, 0.34f, 0.62f), new Color(0.75f, 0.68f, 0.85f), Edge(body, 0.2f)) * new Color(1, 1, 1, 0) + new Color(0, 0, 0, a);
        }

        static Color SnufferShape(float x, float y)
        {
            // Hooded silhouette: wide skirt tapering to a point, seen from the front.
            float halfWidth = Mathf.Lerp(0.82f, 0.08f, (y + 0.9f) / 1.8f);
            float d = Mathf.Min(halfWidth - Mathf.Abs(x), Mathf.Min(y + 0.9f, 0.9f - y));
            float hood = Ellipse(x, y, 0f, 0.05f, 0.34f, 0.3f);
            Color c = hood > 0f ? new Color(0.06f, 0.04f, 0.1f) : new Color(0.44f, 0.3f, 0.58f);
            c.a = Edge(d, 0.07f);
            return c;
        }

        static Color GhostShape(float x, float y)
        {
            // Rounded head over a sheet with a wavy hem; the face is cut out of it.
            float head = Ellipse(x, y, 0f, 0.25f, 0.62f, 0.68f);
            float hem = -0.78f + 0.12f * Mathf.Cos(x * 11f);
            float sheet = Mathf.Min(0.62f - Mathf.Abs(x), Mathf.Min(0.25f - y, y - hem));
            float face = Mathf.Max(Ellipse(Mathf.Abs(x), y, 0.24f, 0.35f, 0.12f, 0.17f), Ellipse(x, y, 0f, 0.02f, 0.13f, 0.11f));
            Color c = face > 0f ? new Color(0.02f, 0.03f, 0.1f) : Color.white;
            c.a = Edge(Mathf.Max(head, sheet), 0.07f);
            return c;
        }

        static Color CreeperShape(float x, float y)
        {
            float angle = Mathf.Atan2(y, x), r = Mathf.Sqrt(x * x + y * y);
            float edge = 0.76f + 0.13f * Mathf.Sin(angle * 7f) + 0.05f * Mathf.Sin(angle * 13f + 1f);
            Color c = Color.Lerp(new Color(0.5f, 0.42f, 0.56f), new Color(0.26f, 0.2f, 0.34f), r);
            c.a = Edge(edge - r, 0.06f);
            return c;
        }
    }
}
