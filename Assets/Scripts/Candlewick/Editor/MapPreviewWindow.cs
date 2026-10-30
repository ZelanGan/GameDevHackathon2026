using UnityEditor;
using UnityEngine;

namespace Candlewick
{
    /// <summary>Draws a schematic of a generated dormitory in the editor, without entering Play mode or touching the scene.</summary>
    public class MapPreviewWindow : EditorWindow
    {
        static readonly Color Solid = new Color(0.12f, 0.11f, 0.16f);
        static readonly Color Interior = new Color(0.36f, 0.33f, 0.44f);
        static readonly Color Rampart = new Color(0.2f, 0.28f, 0.5f);
        static readonly Color Plank = new Color(0.9f, 0.6f, 0.25f);
        static readonly Color Pit = new Color(0.55f, 0.1f, 0.7f);
        static readonly Color StartMark = new Color(0.25f, 0.95f, 0.35f);
        static readonly Color CandleSpot = new Color(1f, 0.85f, 0.2f);

        int floor = 1, seed;
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
                floor = EditorGUILayout.IntPopup("Floor", floor, new[] { "1", "2", "3" }, new[] { 1, 2, 3 });
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
                Legend("Planks", Plank);
                Legend("Pit", Pit);
                Legend("Rampart", Rampart);
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
                    preview.SetPixel(x, y, CellColor(x, y));

            foreach (var room in map.Rooms)
                if (room != map.StartRoom) Mark(room.Stand, CandleSpot);
            Mark(map.StartRoom.Stand, StartMark);
            preview.Apply();
        }

        Color CellColor(int x, int y)
        {
            if (!map.IsOpen(x, y)) return Solid;
            if (map.IsOneWay(x, y)) return Plank;
            if (y < map.Rooms[0].FloorY && map.IsOpen(x, Mathf.CeilToInt(DungeonMap.KillY))) return Pit;
            return map.IsOpenSky(x, y) ? Rampart : Interior;
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
