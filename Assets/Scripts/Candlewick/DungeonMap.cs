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
        const int Border = 2, MinLeaf = 14, MaxLeaf = 26, MinRooms = 8, MinRoomSize = 8, MaxRoomSize = 20;
        public readonly int Width, Height;
        public readonly List<Room> Rooms = new List<Room>();
        readonly bool[] floor;
        readonly int[] dist;
        readonly Queue<int> queue = new Queue<int>();
        Vector2Int flowOrigin = new Vector2Int(-1, -1);

        /// <summary>Generates a new castle floor; deeper floors are larger.</summary>
        public DungeonMap(int depth)
        {
            Width = 58 + depth * 8 + Random.Range(0, 9);
            Height = 46 + depth * 6 + Random.Range(0, 9);
            floor = new bool[Width * Height];
            dist = new int[Width * Height];
            do Generate();
            while (Rooms.Count < MinRooms || !AllRoomsReachable());
        }

        void Generate()
        {
            System.Array.Clear(floor, 0, floor.Length);
            Rooms.Clear();
            Split(new RectInt(Border, Border, Width - Border * 2, Height - Border * 2));

            // Sibling links alone make a tree; a few extra corridors add loops so the player is never cornered.
            int loops = Random.Range(2, 2 + Rooms.Count / 3);
            for (int i = 0; i < loops; i++)
            {
                var room = Rooms[Random.Range(0, Rooms.Count)];
                Room partner = null;
                float best = float.MaxValue;
                foreach (var other in Rooms)
                {
                    if (other == room) continue;
                    float d = Vector2.Distance(room.Center, other.Center) * Random.Range(1f, 1.8f);
                    if (d >= best) continue;
                    best = d;
                    partner = other;
                }
                Corridor(room, partner);
            }
        }

        // Binary space partition: each area becomes a room or splits in two, and the halves get linked.
        List<Room> Split(RectInt area)
        {
            bool canSplitX = area.width >= MinLeaf * 2, canSplitY = area.height >= MinLeaf * 2;
            bool mustSplit = area.width > MaxLeaf || area.height > MaxLeaf;
            if ((!canSplitX && !canSplitY) || (!mustSplit && Random.value < 0.15f))
                return new List<Room> { MakeRoom(area) };

            bool splitX = canSplitX && (!canSplitY || (area.width == area.height ? Random.value < 0.5f : area.width > area.height));
            List<Room> a, b;
            if (splitX)
            {
                int cut = Random.Range(MinLeaf, area.width - MinLeaf + 1);
                a = Split(new RectInt(area.xMin, area.yMin, cut, area.height));
                b = Split(new RectInt(area.xMin + cut, area.yMin, area.width - cut, area.height));
            }
            else
            {
                int cut = Random.Range(MinLeaf, area.height - MinLeaf + 1);
                a = Split(new RectInt(area.xMin, area.yMin, area.width, cut));
                b = Split(new RectInt(area.xMin, area.yMin + cut, area.width, area.height - cut));
            }

            LinkClosest(a, b);
            a.AddRange(b);
            return a;
        }

        Room MakeRoom(RectInt leaf)
        {
            int w = Random.Range(Mathf.Max(MinRoomSize, leaf.width * 6 / 10), Mathf.Min(leaf.width - 1, MaxRoomSize + 1));
            int h = Random.Range(Mathf.Max(MinRoomSize, leaf.height * 6 / 10), Mathf.Min(leaf.height - 1, MaxRoomSize + 1));
            int x = Random.Range(leaf.xMin + 1, leaf.xMax - w);
            int y = Random.Range(leaf.yMin + 1, leaf.yMax - h);
            var room = new Room { Rect = new RectInt(x, y, w, h) };
            Rooms.Add(room);
            CarveShape(room.Rect);
            return room;
        }

        // Every shape keeps the room's center and top middle open: altars and the gate stand there.
        void CarveShape(RectInt r)
        {
            bool roomy = r.width >= 11 && r.height >= 11;
            switch (Random.Range(0, 5))
            {
                case 1: // Notched corners
                    Carve(r, true);
                    int notch = Random.Range(2, 4);
                    Carve(new RectInt(r.xMin, r.yMin, notch, notch), false);
                    Carve(new RectInt(r.xMax - notch, r.yMin, notch, notch), false);
                    Carve(new RectInt(r.xMin, r.yMax - notch, notch, notch), false);
                    Carve(new RectInt(r.xMax - notch, r.yMax - notch, notch, notch), false);
                    break;
                case 2 when roomy: // Cross
                    Carve(new RectInt(r.xMin, r.yMin + r.height / 4, r.width, r.height / 2), true);
                    Carve(new RectInt(r.xMin + r.width / 4, r.yMin, r.width / 2, r.height), true);
                    break;
                case 3: // Round hall
                    for (int y = r.yMin; y < r.yMax; y++)
                        for (int x = r.xMin; x < r.xMax; x++)
                        {
                            float dx = (x + 0.5f - r.center.x) / (r.width / 2f), dy = (y + 0.5f - r.center.y) / (r.height / 2f);
                            if (dx * dx + dy * dy <= 1f) floor[y * Width + x] = true;
                        }
                    break;
                case 4 when roomy: // Pillared hall
                    Carve(r, true);
                    for (int y = r.yMin + 2; y < r.yMax - 3; y += 3)
                        for (int x = r.xMin + 2; x < r.xMax - 2; x += 3)
                            if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), r.center) > 3f)
                                floor[y * Width + x] = false;
                    break;
                default:
                    Carve(r, true);
                    break;
            }
        }

        void Carve(RectInt r, bool open = true)
        {
            for (int y = r.yMin; y < r.yMax; y++)
                for (int x = r.xMin; x < r.xMax; x++)
                    floor[y * Width + x] = open;
        }

        void LinkClosest(List<Room> a, List<Room> b)
        {
            Room bestA = null, bestB = null;
            float best = float.MaxValue;
            foreach (var ra in a)
                foreach (var rb in b)
                {
                    float d = Vector2.Distance(ra.Center, rb.Center);
                    if (d >= best) continue;
                    best = d;
                    bestA = ra;
                    bestB = rb;
                }
            Corridor(bestA, bestB);
        }

        void Corridor(Room a, Room b)
        {
            var from = Vector2Int.FloorToInt(a.Center);
            var to = Vector2Int.FloorToInt(b.Center);
            int width = Random.Range(2, 4);
            // The bend can fall on either side, so hallways don't all turn the same way.
            int bendY = Random.value < 0.5f ? from.y : to.y;
            int bendX = bendY == from.y ? to.x : from.x;
            int minX = Mathf.Min(from.x, to.x), maxX = Mathf.Max(from.x, to.x);
            int minY = Mathf.Min(from.y, to.y), maxY = Mathf.Max(from.y, to.y);
            Carve(new RectInt(minX - 1, bendY - 1, maxX - minX + width, width));
            Carve(new RectInt(bendX - 1, minY - 1, width, maxY - minY + width));
        }

        bool AllRoomsReachable()
        {
            flowOrigin = new Vector2Int(-1, -1);
            UpdateFlow(Rooms[0].Center);
            foreach (var room in Rooms)
                if (WalkDistance(room.Center) == int.MaxValue) return false;
            return true;
        }

        /// <summary>Steps along the floor from the last flow target; int.MaxValue if unreachable.</summary>
        public int WalkDistance(Vector2 p)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
            return IsFloor(x, y) ? dist[y * Width + x] : int.MaxValue;
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
                if (Vector2.Distance(p, awayFrom) >= minDist && !Overlaps(p, 0.8f)) return p;
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
    }
}
