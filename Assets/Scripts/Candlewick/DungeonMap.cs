using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Candlewick
{
    public class Room
    {
        public RectInt Rect;
        public bool Entered, IsStart;
        public int WavesLeft;
        public int WaveIndex;
        public float WaveDelay;
        public Altar Altar;
        public bool Cleared => IsStart || (Entered && WavesLeft <= 0);
        public Vector2 Center => Rect.center;
    }

    /// <summary>Room-and-corridor layout, wall collision, line of sight and the chase flow field.</summary>
    public class DungeonMap
    {
        const int GridSize = 3, CellSize = 19, Border = 2;
        public readonly int Width, Height;
        public readonly List<Room> Rooms = new List<Room>();
        readonly bool[] floor;
        readonly int[] dist;
        readonly Queue<int> queue = new Queue<int>();
        Vector2Int flowOrigin = new Vector2Int(-1, -1);

        public DungeonMap()
        {
            Width = Height = GridSize * CellSize + Border * 2;
            floor = new bool[Width * Height];
            dist = new int[Width * Height];
            Generate();
        }

        void Generate()
        {
            for (int gy = 0; gy < GridSize; gy++)
                for (int gx = 0; gx < GridSize; gx++)
                {
                    int w = Random.Range(10, CellSize - 3), h = Random.Range(9, CellSize - 3);
                    int x = Border + gx * CellSize + Random.Range(1, CellSize - w - 1);
                    int y = Border + gy * CellSize + Random.Range(1, CellSize - h - 1);
                    var room = new Room { Rect = new RectInt(x, y, w, h) };
                    Rooms.Add(room);
                    Carve(room.Rect);
                }

            // Random spanning tree over the 3x3 grid, plus two loops so the player is never cornered.
            var connected = new HashSet<int> { Random.Range(0, Rooms.Count) };
            var edges = new List<Vector2Int>();
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (i % GridSize != GridSize - 1) edges.Add(new Vector2Int(i, i + 1));
                if (i + GridSize < Rooms.Count) edges.Add(new Vector2Int(i, i + GridSize));
            }
            Shuffle(edges);
            var unused = new List<Vector2Int>();
            bool grew = true;
            var pending = new List<Vector2Int>(edges);
            while (grew)
            {
                grew = false;
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    var e = pending[i];
                    bool a = connected.Contains(e.x), b = connected.Contains(e.y);
                    if (a == b) continue;
                    Corridor(Rooms[e.x], Rooms[e.y]);
                    connected.Add(e.x);
                    connected.Add(e.y);
                    pending.RemoveAt(i);
                    grew = true;
                }
            }
            unused.AddRange(pending);
            for (int i = 0; i < Mathf.Min(2, unused.Count); i++) Corridor(Rooms[unused[i].x], Rooms[unused[i].y]);
        }

        void Carve(RectInt r)
        {
            for (int y = r.yMin; y < r.yMax; y++)
                for (int x = r.xMin; x < r.xMax; x++)
                    floor[y * Width + x] = true;
        }

        void Corridor(Room a, Room b)
        {
            var from = Vector2Int.FloorToInt(a.Center);
            var to = Vector2Int.FloorToInt(b.Center);
            int minX = Mathf.Min(from.x, to.x), maxX = Mathf.Max(from.x, to.x);
            int minY = Mathf.Min(from.y, to.y), maxY = Mathf.Max(from.y, to.y);
            Carve(new RectInt(minX - 1, from.y - 1, maxX - minX + 3, 3));
            Carve(new RectInt(to.x - 1, minY - 1, 3, maxY - minY + 3));
        }

        public bool IsFloor(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && floor[y * Width + x];
        public bool IsFloor(Vector2 p) => IsFloor(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

        public Room RoomAt(Vector2 p)
        {
            var cell = Vector2Int.FloorToInt(p);
            foreach (var room in Rooms) if (room.Rect.Contains(cell)) return room;
            return null;
        }

        bool Overlaps(Vector2 p, float r)
        {
            int x0 = Mathf.FloorToInt(p.x - r), x1 = Mathf.FloorToInt(p.x + r);
            int y0 = Mathf.FloorToInt(p.y - r), y1 = Mathf.FloorToInt(p.y + r);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (!IsFloor(x, y)) return true;
            return false;
        }

        /// <summary>Moves a body axis by axis so it slides along walls instead of sticking to them.</summary>
        public Vector2 Move(Vector2 p, Vector2 delta, float r)
        {
            int steps = Mathf.CeilToInt(delta.magnitude / 0.3f);
            if (steps == 0) return p;
            Vector2 step = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                if (!Overlaps(new Vector2(p.x + step.x, p.y), r)) p.x += step.x;
                if (!Overlaps(new Vector2(p.x, p.y + step.y), r)) p.y += step.y;
            }
            return p;
        }

        public bool LineOfSight(Vector2 a, Vector2 b)
        {
            float d = Vector2.Distance(a, b);
            int steps = Mathf.CeilToInt(d / 0.25f);
            for (int i = 1; i < steps; i++)
                if (!IsFloor(Vector2.Lerp(a, b, i / (float)steps))) return false;
            return true;
        }

        public void UpdateFlow(Vector2 target)
        {
            var cell = Vector2Int.FloorToInt(target);
            if (cell == flowOrigin || !IsFloor(cell.x, cell.y)) return;
            flowOrigin = cell;
            for (int i = 0; i < dist.Length; i++) dist[i] = int.MaxValue;
            int start = cell.y * Width + cell.x;
            dist[start] = 0;
            queue.Clear();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int c = queue.Dequeue(), x = c % Width, y = c / Width, next = dist[c] + 1;
                Visit(x + 1, y, next);
                Visit(x - 1, y, next);
                Visit(x, y + 1, next);
                Visit(x, y - 1, next);
            }
        }

        void Visit(int x, int y, int d)
        {
            if (!IsFloor(x, y)) return;
            int c = y * Width + x;
            if (dist[c] <= d) return;
            dist[c] = d;
            queue.Enqueue(c);
        }

        /// <summary>Direction along the shortest walkable route towards the flow target.</summary>
        public Vector2 FlowDir(Vector2 p)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
            int best = IsFloor(x, y) ? dist[y * Width + x] : int.MaxValue;
            var dir = Vector2.zero;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0 || !IsFloor(x + dx, y + dy)) continue;
                    if (dx != 0 && dy != 0 && (!IsFloor(x + dx, y) || !IsFloor(x, y + dy))) continue;
                    int d = dist[(y + dy) * Width + x + dx];
                    if (d >= best) continue;
                    best = d;
                    dir = new Vector2(x + dx + 0.5f, y + dy + 0.5f) - p;
                }
            return dir.normalized;
        }

        public Vector2 RandomPointIn(Room room, Vector2 awayFrom, float minDist)
        {
            for (int i = 0; i < 30; i++)
            {
                var p = new Vector2(Random.Range(room.Rect.xMin + 1f, room.Rect.xMax - 1f), Random.Range(room.Rect.yMin + 1f, room.Rect.yMax - 1f));
                if (Vector2.Distance(p, awayFrom) >= minDist) return p;
            }
            return room.Center;
        }

        public void BuildTilemap(Transform parent)
        {
            var grid = new GameObject("Grid").AddComponent<Grid>();
            grid.transform.SetParent(parent, false);
            var tilemapGo = new GameObject("Tiles");
            tilemapGo.transform.SetParent(grid.transform, false);
            var tilemap = tilemapGo.AddComponent<Tilemap>();
            var renderer = tilemapGo.AddComponent<TilemapRenderer>();
            renderer.sharedMaterial = Art.Lit;
            renderer.sortingOrder = 0;

            var floorTile = NewTile(Art.Floor);
            var floorAlt = NewTile(Art.FloorAlt);
            var wallTop = NewTile(Art.WallTop);
            var wallFace = NewTile(Art.WallFace);

            for (int y = -6; y < Height + 6; y++)
                for (int x = -8; x < Width + 8; x++)
                {
                    TileBase tile;
                    if (IsFloor(x, y)) tile = (x * 7 + y * 13 + (x ^ y)) % 5 == 0 ? floorAlt : floorTile;
                    else tile = IsFloor(x, y - 1) ? wallFace : wallTop;
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
        }

        static Tile NewTile(Sprite sprite)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            return tile;
        }

        static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
