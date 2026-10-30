using UnityEditor;
using UnityEngine;

namespace Candlewick
{
    /// <summary>Draws a schematic of a generated dormitory in the editor, without entering Play mode or touching the scene.</summary>
    public class MapPreviewWindow : EditorWindow
    {
        static readonly Color Solid = new Color(0.12f, 0.11f, 0.16f);
        static readonly Color Plank = new Color(0.95f, 0.65f, 0.25f);
        static readonly Color StartMark = new Color(0.25f, 0.95f, 0.35f);
        static readonly Color CandleSpot = new Color(1f, 0.85f, 0.2f);

        static readonly RoomKind[] Kinds =
        {
            RoomKind.Dorm, RoomKind.Corridor, RoomKind.Kitchen, RoomKind.Shower, RoomKind.Study,
            RoomKind.Lounge, RoomKind.Laundry, RoomKind.Storage, RoomKind.Lobby, RoomKind.Stairs, RoomKind.Roof,
        };

        static Color KindColor(RoomKind kind)
        {
            switch (kind)
            {
                case RoomKind.Dorm: return new Color(0.42f, 0.62f, 0.95f);
                case RoomKind.Corridor: return new Color(0.5f, 0.5f, 0.55f);
                case RoomKind.Kitchen: return new Color(0.9f, 0.5f, 0.25f);
                case RoomKind.Shower: return new Color(0.3f, 0.82f, 0.85f);
                case RoomKind.Study: return new Color(0.35f, 0.72f, 0.4f);
                case RoomKind.Lounge: return new Color(0.9f, 0.45f, 0.65f);
                case RoomKind.Laundry: return new Color(0.68f, 0.5f, 0.95f);
                case RoomKind.Storage: return new Color(0.58f, 0.42f, 0.28f);
                case RoomKind.Lobby: return new Color(0.92f, 0.86f, 0.55f);
                case RoomKind.Stairs: return new Color(0.82f, 0.82f, 0.82f);
                default: return new Color(0.14f, 0.2f, 0.42f);
            }
        }

        int floor = 1, seed;
        readonly System.Collections.Generic.Dictionary<RoomKind, int> counts = new System.Collections.Generic.Dictionary<RoomKind, int>();
        Texture2D preview;
        DungeonMap map;

        [MenuItem("Tools/Candlewick/Map Preview")]
        static void Open() => GetWindow<MapPreviewWindow>("Map Preview");

        void OnEnable()
        {
            seed = Random.Range(0, int.MaxValue);
            Generate();
        }

        void OnDisable()
        {
            if (preview != null) DestroyImmediate(preview);
        }

        void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                floor = EditorGUILayout.IntPopup("Day", floor, new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }, new[] { 1, 2, 3, 4, 5 });
                seed = EditorGUILayout.IntField("Seed", seed);
                if (GUILayout.Button("Same seed", GUILayout.Width(90))) Generate();
                if (GUILayout.Button("Generate", GUILayout.Width(90)))
                {
                    seed = Random.Range(0, int.MaxValue);
                    Generate();
                }
            }

            if (map == null || preview == null) Generate();
            EditorGUILayout.LabelField($"{map.Width} x {map.Height} cells, {map.Rooms.Count} rooms");
            using (new EditorGUILayout.HorizontalScope())
            {
                Legend("Start", StartMark);
                Legend("Candle or soul spot", CandleSpot);
                Legend("Landings and bunks", Plank);
                GUILayout.FlexibleSpace();
            }
            // Two rows, so every room type stays readable in a narrow window.
            for (int row = 0; row < 2; row++)
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = row * 6; i < Mathf.Min(Kinds.Length, (row + 1) * 6); i++)
                    {
                        counts.TryGetValue(Kinds[i], out int count);
                        Legend($"{Kinds[i]} ({count})", KindColor(Kinds[i]));
                    }
                    GUILayout.FlexibleSpace();
                }

            Rect area = GUILayoutUtility.GetRect(10f, 10000f, 10f, 10000f);
            GUI.DrawTexture(area, preview, ScaleMode.ScaleToFit);
        }

        static void Legend(string label, Color color)
        {
            Rect swatch = GUILayoutUtility.GetRect(12f, 12f, GUILayout.Width(12f), GUILayout.Height(16f));
            EditorGUI.DrawRect(new Rect(swatch.x, swatch.y + 3f, 12f, 12f), color);
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
        }

        void Generate()
        {
            // The generator draws from Unity's shared random stream, so leave that as we found it.
            var state = Random.state;
            Random.InitState(seed);
            map = new DungeonMap(floor);
            Random.state = state;
            Paint();
            Repaint();
        }

        void Paint()
        {
            if (preview != null) DestroyImmediate(preview);
            preview = new Texture2D(map.Width, map.Height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };

            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                    preview.SetPixel(x, y, Solid);

            counts.Clear();
            foreach (var room in map.Rooms)
            {
                counts.TryGetValue(room.Kind, out int count);
                counts[room.Kind] = count + 1;
                var color = KindColor(room.Kind);
                for (int y = room.Rect.yMin; y < room.Rect.yMax; y++)
                    for (int x = room.Rect.xMin; x < room.Rect.xMax; x++)
                        preview.SetPixel(x, y, color);
            }

            // Doorways and stair holes are open cells outside any room.
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    if (map.IsOneWay(x, y)) preview.SetPixel(x, y, Plank);
                    else if (map.IsOpen(x, y) && map.RoomAt(new Vector2(x + 0.5f, y + 0.5f)) == null) preview.SetPixel(x, y, KindColor(RoomKind.Stairs));
                    else if (!map.IsOpen(x, y)) preview.SetPixel(x, y, Solid);
                }

            foreach (var room in map.Rooms)
                if (room != map.StartRoom && room.Kind != RoomKind.Stairs && room.Kind != RoomKind.Roof) Mark(room.Stand, CandleSpot);
            Mark(map.StartRoom.Stand, StartMark);
            preview.Apply();
        }

        void Mark(Vector2 at, Color color)
        {
            int cx = Mathf.FloorToInt(at.x), cy = Mathf.FloorToInt(at.y);
            for (int y = cy - 1; y <= cy + 1; y++)
                for (int x = cx - 1; x <= cx + 1; x++)
                    if (x >= 0 && y >= 0 && x < map.Width && y < map.Height) preview.SetPixel(x, y, color);
        }
    }
}
