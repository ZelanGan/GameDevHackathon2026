using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Candlewick
{
    public class Room
    {
        public RectInt Rect; // The room's interior.
        public int FloorY;   // The solid row the room's occupants stand on.
        public bool Entered, IsStart;
        public int WavesLeft;
        public int WaveIndex;
        public float WaveDelay;
        public Altar Altar;
        public Vector2 Stand; // A safe place for a body to stand in this room.
        public bool Cleared => IsStart || (Entered && WavesLeft <= 0);
        public Vector2 Center => Rect.center;
    }

    /// <summary>
    /// A castle seen in cross-section: storeys of rooms split by walls with doorways, joined by plank stairwells,
    /// with towers and open ramparts on the roof. Also provides collision, line of sight and the chase flow field.
    /// </summary>
    public class DungeonMap
    {
        public const float KillY = 3.5f; // Falling below this means the player fell into a pit.
        const int Margin = 12, WallThickness = 2, SlabThickness = 2, GroundRow = 9, PitFloor = 2;
        const int DoorHeight = 4, StairWidth = 4, StairRise = 3, TowerExtra = 6, SkyAbove = 10, WallClearance = 4;
        const float BodyStep = 0.2f;

        public readonly int Width, Height;
        public readonly List<Room> Rooms = new List<Room>();
        public Room StartRoom { get; private set; }
        public Room GateRoom { get; private set; }
        public Vector2 GatePos { get; private set; }

        readonly bool[] open, oneWay, sky;
        readonly int[] dist, roofAt;
        readonly Queue<int> queue = new Queue<int>();
        readonly int columns, storeys;
        readonly int[] columnX, columnWidth, storeyFloor, storeyHeight;
        readonly Room[,] grid;
        readonly HashSet<int>[,] reserved; // Floor columns that features must leave clear.
        readonly bool[] isRampart;
        Vector2Int flowOrigin = new Vector2Int(-1, -1);

        /// <summary>Generates a new castle; deeper floors are wider and taller.</summary>
        public DungeonMap(int depth)
        {
            columns = 4 + depth;
            storeys = 3 + depth / 2;
            columnX = new int[columns];
            columnWidth = new int[columns];
            storeyFloor = new int[storeys];
            storeyHeight = new int[storeys];
            grid = new Room[storeys, columns];
            reserved = new HashSet<int>[storeys, columns];
            isRampart = new bool[columns];

            int x = Margin;
            for (int c = 0; c < columns; c++)
            {
                columnX[c] = x;
                columnWidth[c] = Random.Range(16, 25);
                x += columnWidth[c] + WallThickness;
            }
            Width = x - WallThickness + Margin;

            int y = GroundRow;
            for (int s = 0; s < storeys; s++)
            {
                storeyFloor[s] = y;
                storeyHeight[s] = Random.Range(7, 10);
                y += storeyHeight[s] + SlabThickness;
            }
            Height = y + TowerExtra + SkyAbove;

            open = new bool[Width * Height];
            oneWay = new bool[Width * Height];
            sky = new bool[Width * Height];
            dist = new int[Width * Height];
            roofAt = new int[Width];

            Generate();
        }

        void Generate()
        {
            ChooseRoof();
            CarveRooms();
            StartRoom = grid[storeys - 1, 0];
            GateRoom = grid[0, columns - 1];
            ConnectRooms();
            foreach (var room in Rooms) AddFeatures(room);
            foreach (var room in Rooms) room.Stand = FindStand(room);
            int gateColumn = GateRoom.Rect.xMax - 5;
            GatePos = new Vector2(gateColumn + 0.5f, GateRoom.FloorY + 2.5f);
        }

        // The roof alternates enclosed towers with ramparts open to the sky. The start is always a tower.
        void ChooseRoof()
        {
            for (int c = 1; c < columns; c++) isRampart[c] = !isRampart[c - 1] ? Random.value < 0.7f : Random.value < 0.35f;

            int top = storeys - 1;
            for (int x = 0; x < Width; x++) roofAt[x] = GroundRow;
            int wallTop = storeyFloor[top] + storeyHeight[top] + SlabThickness;
            for (int c = 0; c < columns; c++)
            {
                int roof = isRampart[c] ? storeyFloor[top] : wallTop + TowerExtra;
                for (int x = columnX[c]; x < columnX[c] + columnWidth[c]; x++) roofAt[x] = roof;
                // Walls stand at least full height either side of a rampart, or as tall as the tower they belong to.
                for (int w = 1; w <= WallThickness; w++)
                {
                    int left = columnX[c] - w, right = columnX[c] + columnWidth[c] - 1 + w;
                    int wall = isRampart[c] ? wallTop : roof;
                    roofAt[left] = Mathf.Max(roofAt[left], wall);
                    roofAt[right] = Mathf.Max(roofAt[right], wall);
                }
            }
        }

        void CarveRooms()
        {
            for (int s = 0; s < storeys; s++)
                for (int c = 0; c < columns; c++)
                {
                    int interior = storeyHeight[s];
                    bool tower = s == storeys - 1 && !isRampart[c];
                    if (tower) interior += TowerExtra;
                    var rect = new RectInt(columnX[c], storeyFloor[s] + 1, columnWidth[c], interior);
                    var room = new Room { Rect = rect, FloorY = storeyFloor[s] };
                    grid[s, c] = room;
                    reserved[s, c] = new HashSet<int>();
                    Rooms.Add(room);
                    Carve(rect, true);
                    if (s == storeys - 1 && isRampart[c])
                        for (int y = rect.yMin; y < rect.yMax; y++)
                            for (int x = rect.xMin; x < rect.xMax; x++)
                                sky[y * Width + x] = true;
                }
        }

        // A random walk over the room grid builds a spanning tree; a few extra links add loops to flee through.
        void ConnectRooms()
        {
            var visited = new bool[storeys, columns];
            var stack = new Stack<Vector2Int>();
            var start = new Vector2Int(0, storeys - 1);
            visited[start.y, start.x] = true;
            stack.Push(start);
            var neighbours = new List<Vector2Int>();
            while (stack.Count > 0)
            {
                var at = stack.Peek();
                neighbours.Clear();
                foreach (var n in Adjacent(at))
                    if (!visited[n.y, n.x]) neighbours.Add(n);
                if (neighbours.Count == 0) { stack.Pop(); continue; }
                var next = neighbours[Random.Range(0, neighbours.Count)];
                Link(at, next);
                visited[next.y, next.x] = true;
                stack.Push(next);
            }

            int extras = columns * storeys / 3;
            for (int i = 0; i < extras; i++)
            {
                var a = new Vector2Int(Random.Range(0, columns), Random.Range(0, storeys));
                var options = new List<Vector2Int>(Adjacent(a));
                Link(a, options[Random.Range(0, options.Count)]);
            }
        }

        IEnumerable<Vector2Int> Adjacent(Vector2Int at)
        {
            if (at.x > 0) yield return new Vector2Int(at.x - 1, at.y);
            if (at.x < columns - 1) yield return new Vector2Int(at.x + 1, at.y);
            if (at.y > 0) yield return new Vector2Int(at.x, at.y - 1);
            if (at.y < storeys - 1) yield return new Vector2Int(at.x, at.y + 1);
        }

        void Link(Vector2Int a, Vector2Int b)
        {
            if (a.y == b.y) CutDoor(Mathf.Min(a.x, b.x), a.y);
            else CutStairwell(a.x, Mathf.Min(a.y, b.y));
        }

        void CutDoor(int leftColumn, int storey)
        {
            int wallX = columnX[leftColumn] + columnWidth[leftColumn];
            Carve(new RectInt(wallX, storeyFloor[storey] + 1, WallThickness, DoorHeight), true);
        }

        // Planks climb from a room to a hole in its ceiling, each within jump height, and one plank
        // bridges the hole so the upper room stays walkable. S drops back down through them.
        void CutStairwell(int column, int lower)
        {
            var room = grid[lower, column];
            int x = Random.Range(room.Rect.xMin + WallClearance, room.Rect.xMax - WallClearance - StairWidth);
            int floorBelow = storeyFloor[lower], floorAbove = storeyFloor[lower + 1];
            Carve(new RectInt(x, floorBelow + storeyHeight[lower] + 1, StairWidth, SlabThickness), true);
            for (int y = floorBelow + StairRise; y < floorAbove; y += StairRise) AddPlank(x, y);
            AddPlank(x, floorAbove);
            for (int i = -1; i <= StairWidth; i++)
            {
                reserved[lower, column].Add(x + i);
                reserved[lower + 1, column].Add(x + i);
            }
        }

        void AddPlank(int x, int y)
        {
            for (int i = 0; i < StairWidth; i++) oneWay[y * Width + x + i] = true;
        }

        void AddFeatures(Room room)
        {
            if (room == StartRoom || room == GateRoom) return;
            int storey = StoreyOf(room), column = ColumnOf(room);
            switch (Random.Range(0, 4))
            {
                case 0 when storey == 0:
                    AddBrokenFloor(room, reserved[storey, column]);
                    break;
                case 1:
                    AddCrates(room, reserved[storey, column]);
                    break;
                case 2:
                    AddGallery(room, reserved[storey, column]);
                    break;
            }
        }

        // Ground-floor flagstones have collapsed into the pit below.
        void AddBrokenFloor(Room room, HashSet<int> keepClear)
        {
            int x = room.Rect.xMin + WallClearance;
            while (x < room.Rect.xMax - WallClearance - 3)
            {
                int gap = Random.Range(2, 4);
                if (IsFree(keepClear, x - 1, gap + 2))
                {
                    Carve(new RectInt(x, PitFloor, gap, room.FloorY - PitFloor + 1), true);
                    x += gap + Random.Range(4, 8);
                }
                else x++;
            }
        }

        void AddCrates(Room room, HashSet<int> keepClear)
        {
            int count = Random.Range(1, 4);
            for (int i = 0; i < count; i++)
            {
                int width = Random.Range(2, 4), height = Random.Range(1, 3);
                int x = Random.Range(room.Rect.xMin + WallClearance, room.Rect.xMax - WallClearance - width);
                if (!IsFree(keepClear, x - 1, width + 2)) continue;
                Carve(new RectInt(x, room.FloorY + 1, width, height), false);
                for (int c = x - 1; c < x + width + 1; c++) keepClear.Add(c);
            }
        }

        void AddGallery(Room room, HashSet<int> keepClear)
        {
            int y = room.FloorY + StairRise;
            int width = Random.Range(4, 8);
            int x = Random.Range(room.Rect.xMin, room.Rect.xMax - width);
            if (!IsFree(keepClear, x, width)) return;
            for (int i = 0; i < width; i++) oneWay[y * Width + x + i] = true;
        }

        static bool IsFree(HashSet<int> keepClear, int x, int width)
        {
            for (int i = 0; i < width; i++)
                if (keepClear.Contains(x + i)) return false;
            return true;
        }

        int StoreyOf(Room room)
        {
            for (int s = 0; s < storeys; s++) if (storeyFloor[s] == room.FloorY) return s;
            return 0;
        }

        int ColumnOf(Room room)
        {
            for (int c = 0; c < columns; c++) if (columnX[c] == room.Rect.xMin) return c;
            return 0;
        }

        void Carve(RectInt r, bool isOpen)
        {
            for (int y = r.yMin; y < r.yMax; y++)
                for (int x = r.xMin; x < r.xMax; x++)
                    open[y * Width + x] = isOpen;
        }

        Vector2 FindStand(Room room)
        {
            int middle = room.Rect.xMin + room.Rect.width / 2;
            for (int offset = 0; offset < room.Rect.width; offset++)
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    int x = middle + offset * sign;
                    if (x <= room.Rect.xMin || x >= room.Rect.xMax - 1 || !IsFlatAndClear(x, room.FloorY)) continue;
                    return new Vector2(x + 0.5f, room.FloorY + 1.34f);
                }
            return new Vector2(middle + 0.5f, room.FloorY + 1.34f);
        }

        bool IsFlatAndClear(int x, int floorY)
        {
            for (int c = x - 1; c <= x + 1; c++)
            {
                if (IsOpen(c, floorY)) return false;
                for (int y = floorY + 1; y <= floorY + 4; y++)
                    if (!IsOpen(c, y) || IsOneWay(c, y)) return false;
            }
            return true;
        }

        public bool IsOpen(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && open[y * Width + x];
        public bool IsOpen(Vector2 p) => IsOpen(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        public bool IsOneWay(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && oneWay[y * Width + x];
        public bool IsOpenSky(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && sky[y * Width + x];

        /// <summary>Whether a walker could stand on the cell containing this point.</summary>
        public bool IsStandable(Vector2 p)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
            return !IsOpen(x, y) || IsOneWay(x, y);
        }

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
                    if (!IsOpen(x, y)) return true;
            return false;
        }

        /// <summary>Moves a flying body axis by axis so it slides along walls instead of sticking to them.</summary>
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

        /// <summary>Moves a body under gravity: it lands on ground and on one-way platforms, unless dropping through them.</summary>
        public Vector2 MoveWalker(Vector2 p, Vector2 delta, float r, bool dropThrough, out bool grounded, out bool bumped)
        {
            grounded = bumped = false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) / BodyStep));
            Vector2 step = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                if (!Overlaps(new Vector2(p.x + step.x, p.y), r)) p.x += step.x;
                if (grounded || bumped) continue;

                float next = p.y + step.y;
                if (step.y > 0f)
                {
                    if (Overlaps(new Vector2(p.x, next), r)) bumped = true;
                    else p.y = next;
                }
                else if (Overlaps(new Vector2(p.x, next), r) || (!dropThrough && LandsOnPlatform(p, next, r)))
                {
                    p.y = Mathf.Floor(p.y - r + 0.0001f) + r;
                    grounded = true;
                }
                else p.y = next;
            }
            return p;
        }

        bool LandsOnPlatform(Vector2 p, float nextY, float r)
        {
            float feet = p.y - r, nextFeet = nextY - r;
            int top = Mathf.FloorToInt(feet + 0.0001f);
            if (nextFeet >= top) return false;
            int x0 = Mathf.FloorToInt(p.x - r), x1 = Mathf.FloorToInt(p.x + r);
            for (int x = x0; x <= x1; x++)
                if (IsOneWay(x, top - 1)) return true;
            return false;
        }

        public bool LineOfSight(Vector2 a, Vector2 b)
        {
            float d = Vector2.Distance(a, b);
            int steps = Mathf.CeilToInt(d / 0.25f);
            for (int i = 1; i < steps; i++)
                if (!IsOpen(Vector2.Lerp(a, b, i / (float)steps))) return false;
            return true;
        }

        public void UpdateFlow(Vector2 target)
        {
            var cell = Vector2Int.FloorToInt(target);
            if (cell == flowOrigin || !IsOpen(cell.x, cell.y)) return;
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
            if (!IsOpen(x, y)) return;
            int c = y * Width + x;
            if (dist[c] <= d) return;
            dist[c] = d;
            queue.Enqueue(c);
        }

        /// <summary>Direction along the shortest flying route towards the flow target.</summary>
        public Vector2 FlowDir(Vector2 p)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
            int best = IsOpen(x, y) ? dist[y * Width + x] : int.MaxValue;
            var dir = Vector2.zero;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0 || !IsOpen(x + dx, y + dy)) continue;
                    if (dx != 0 && dy != 0 && (!IsOpen(x + dx, y) || !IsOpen(x, y + dy))) continue;
                    int d = dist[(y + dy) * Width + x + dx];
                    if (d >= best) continue;
                    best = d;
                    dir = new Vector2(x + dx + 0.5f, y + dy + 0.5f) - p;
                }
            return dir.normalized;
        }

        /// <summary>A spawn point near the room's floor, so whatever appears there stays within the flame's reach.</summary>
        public Vector2 RandomPointIn(Room room, Vector2 awayFrom, float minDist)
        {
            for (int i = 0; i < 60; i++)
            {
                var p = new Vector2(Random.Range(room.Rect.xMin + 1f, room.Rect.xMax - 1f), room.FloorY + Random.Range(1.9f, 3.6f));
                if (Vector2.Distance(p, awayFrom) >= minDist && !Overlaps(p, 0.8f)) return p;
            }
            return room.Stand + Vector2.up;
        }

        public void BuildTilemap(Transform parent)
        {
            var grid2 = new GameObject("Grid").AddComponent<Grid>();
            grid2.transform.SetParent(parent, false);
            var background = NewTilemap(grid2.transform, "Tiles", 0, Art.Lit);
            var skyMap = NewTilemap(grid2.transform, "Sky", 0, Art.Glow);
            var planks = NewTilemap(grid2.transform, "Platforms", 1, Art.Lit);

            var backWall = NewTile(Art.Floor);
            var backWallAlt = NewTile(Art.FloorAlt);
            var stone = NewTile(Art.WallTop);
            var ledge = NewTile(Art.WallFace);
            var plank = NewTile(Art.Plank);
            var night = NewTile(Art.Sky);
            var stars = NewTile(Art.SkyStars);

            for (int y = -6; y < Height + 6; y++)
                for (int x = -8; x < Width + 8; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    bool merlon = InMap(x) && y == roofAt[x] + 1 && x % 4 < 2 && x >= Margin - WallThickness && x < Width - Margin + WallThickness;
                    if (IsOpen(x, y))
                    {
                        if (sky[y * Width + x]) skyMap.SetTile(cell, (x * 31 + y * 17) % 23 == 0 ? stars : night);
                        else background.SetTile(cell, (x * 7 + y * 13 + (x ^ y)) % 5 == 0 ? backWallAlt : backWall);
                        if (IsOneWay(x, y)) planks.SetTile(cell, plank);
                    }
                    else if (merlon || y <= RoofOf(x))
                        background.SetTile(cell, IsOpen(x, y + 1) ? ledge : stone);
                    else
                        skyMap.SetTile(cell, (x * 31 + y * 17) % 23 == 0 ? stars : night);
                }

            // The bottom of every pit fades into gloom instead of showing a floor.
            var gloom = Art.Spr("PitGloom", parent, Art.Square, Art.Glow, new Color(0.05f, 0.01f, 0.1f, 0.96f), 3);
            gloom.transform.localScale = new Vector3(Width, KillY + 1f, 1f);
            gloom.transform.localPosition = new Vector3(Width / 2f, (KillY + 1f) / 2f, 0f);

            var moon = Art.Spr("Moon", parent, Art.Disk, Art.Glow, new Color(0.86f, 0.9f, 1f), 0, 4f);
            moon.transform.localPosition = new Vector3(Width * Random.Range(0.25f, 0.75f), Height - 5f, 0f);

            AddDecor(parent);
        }

        bool InMap(int x) => x >= 0 && x < Width;
        int RoofOf(int x) => InMap(x) ? roofAt[x] : GroundRow;

        // Pillars, windows and banners sit on the back wall; they never affect collision.
        void AddDecor(Transform parent)
        {
            var root = new GameObject("Decor").transform;
            root.SetParent(parent, false);
            foreach (var room in Rooms)
            {
                if (sky[room.Rect.yMin * Width + room.Rect.xMin]) continue;
                int top = room.Rect.yMax;
                for (int x = room.Rect.xMin + Random.Range(3, 6); x < room.Rect.xMax - 3; x += Random.Range(5, 9))
                {
                    if (!IsOpen(x, top - 3)) continue;
                    switch (Random.Range(0, 4))
                    {
                        case 0:
                            PlaceDecor(root, Art.Pillar, Art.Lit, Color.white, new Vector2(x + 0.5f, room.FloorY + 1f + room.Rect.height / 2f), new Vector2(1.2f, room.Rect.height));
                            break;
                        case 1:
                            PlaceDecor(root, Art.Window, Art.Glow, new Color(0.45f, 0.62f, 1f, 0.75f), new Vector2(x + 0.5f, top - 2.6f), new Vector2(1.6f, 2.8f));
                            break;
                        case 2:
                            var colour = Random.value < 0.5f ? new Color(0.72f, 0.1f, 0.12f) : new Color(0.2f, 0.25f, 0.7f);
                            PlaceDecor(root, Art.Banner, Art.Lit, colour, new Vector2(x + 0.5f, top - 2.2f), new Vector2(1.5f, 3f));
                            break;
                    }
                }
            }
        }

        static void PlaceDecor(Transform root, Sprite sprite, Material material, Color color, Vector2 position, Vector2 size)
        {
            var go = Art.Spr(sprite.name, root, sprite, material, color, 0);
            go.GetComponent<SpriteRenderer>().sortingOrder = 1;
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        static Tilemap NewTilemap(Transform parent, string name, int order, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return tilemap;
        }

        static Tile NewTile(Sprite sprite)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            return tile;
        }
    }
}
