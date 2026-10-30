using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Candlewick
{
    public enum RoomKind { Stairs, Dorm, Kitchen, Shower, Corridor, Lobby, Roof }

    public class Room
    {
        public RectInt Rect; // The room's interior.
        public int FloorY;   // The solid row the room's occupants stand on.
        public RoomKind Kind;
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
    /// A student dormitory seen in cross-section: identical storeys of dorm rooms, kitchens, showers and corridors,
    /// joined by doors, with a stairwell at each end running up to a flat roof.
    /// Also provides collision, line of sight and the chase flow field.
    /// </summary>
    public class DungeonMap
    {
        public const float KillY = 3.5f; // Falling below this means the player fell into a pit.
        const int Margin = 12, WallThickness = 2, SlabThickness = 2, GroundRow = 9, Storeys = 4, StoreyHeight = 7, RoofHeight = 7;
        const int DoorHeight = 3, ShaftWidth = 8, StairWidth = 4, StairRise = 3, SkyAbove = 10, MinRoomWidth = 8;
        const float BodyStep = 0.2f;
        public static readonly Color NightSky = new Color(0.1f, 0.12f, 0.3f);

        public readonly int Width, Height;
        public readonly List<Room> Rooms = new List<Room>();
        public readonly List<Vector2> DoorSpots = new List<Vector2>(); // Doorway centres, at floor level.
        public Room StartRoom { get; private set; }
        public Vector2 StartWindow { get; private set; } // Where the last ray of the sun comes in.

        readonly bool[] open, oneWay, sky;
        readonly int[] dist, roofAt;
        readonly Queue<int> queue = new Queue<int>();
        readonly byte[] wallTiles;
        readonly int interiorWidth;
        readonly List<RoomKind> planKinds = new List<RoomKind>();
        readonly List<int> planWidths = new List<int>();
        Vector2Int flowOrigin = new Vector2Int(-1, -1);
        Tilemap skyMap;

        /// <summary>Generates a new dormitory; a higher size makes the building longer.</summary>
        public DungeonMap(int size)
        {
            // Long enough for at least one dorm room in each half of the plan.
            interiorWidth = 120 + size * 10 + Random.Range(0, 9);
            Width = Margin * 2 + interiorWidth;
            Height = FloorRow(Storeys) + RoofHeight + SkyAbove;

            open = new bool[Width * Height];
            oneWay = new bool[Width * Height];
            sky = new bool[Width * Height];
            wallTiles = new byte[Width * Height];
            dist = new int[Width * Height];
            roofAt = new int[Width];

            Generate();
        }

        static int FloorRow(int storey) => GroundRow + storey * (StoreyHeight + SlabThickness);

        void Generate()
        {
            for (int x = 0; x < Width; x++) roofAt[x] = GroundRow;
            int roofRow = FloorRow(Storeys);
            for (int x = Margin - WallThickness; x < Margin + interiorWidth + WallThickness; x++) roofAt[x] = roofRow;
            // A low parapet at each end of the roof.
            for (int w = 1; w <= WallThickness; w++)
            {
                roofAt[Margin - w] = roofRow + 1;
                roofAt[Margin + interiorWidth - 1 + w] = roofRow + 1;
            }

            MakePlan();
            for (int s = 0; s < Storeys; s++) BuildStorey(s);

            var roof = new RectInt(Margin, roofRow + 1, interiorWidth, RoofHeight);
            Rooms.Add(new Room { Rect = roof, FloorY = roofRow, Kind = RoomKind.Roof });
            Carve(roof, true);
            for (int y = roof.yMin; y < roof.yMax; y++)
                for (int x = roof.xMin; x < roof.xMax; x++)
                    sky[y * Width + x] = true;

            CutStairs(Margin);
            CutStairs(Margin + interiorWidth - ShaftWidth);
            foreach (var room in Rooms) room.Stand = FindStand(room);

            // The ray lands in a dorm room near the middle of the building, not too far from anything.
            float best = float.MaxValue;
            var centre = new Vector2(Width / 2f, FloorRow(Storeys / 2));
            foreach (var room in Rooms)
            {
                if (room.Kind != RoomKind.Dorm) continue;
                float d = Vector2.Distance(room.Center, centre) * Random.Range(1f, 1.3f);
                if (d >= best) continue;
                best = d;
                StartRoom = room;
            }
            StartWindow = WindowSpot(StartRoom);
        }

        // One plan serves every storey, so walls line up and kitchens and showers stack, as in a real building.
        // It mirrors around the centre: corridor, dorm rooms, kitchen, then the shared showers in the middle.
        void MakePlan()
        {
            int span = interiorWidth - ShaftWidth * 2 - WallThickness * 2;
            int showers = Random.Range(MinRoomWidth, 11);
            int kitchen = Random.Range(11, 14);
            int corridor = Random.Range(12, 17);
            int half = (span - showers) / 2 - WallThickness;
            int used = corridor + WallThickness + kitchen + WallThickness;

            var dorms = new List<int>();
            while (true)
            {
                int dorm = Random.Range(10, 13);
                if (used + dorm + WallThickness > half) break;
                dorms.Add(dorm);
                used += dorm + WallThickness;
            }
            // Whatever is left lengthens the corridors, keeping both halves equal.
            corridor += half - used + WallThickness;

            AddToPlan(RoomKind.Corridor, corridor);
            foreach (int dorm in dorms) AddToPlan(RoomKind.Dorm, dorm);
            AddToPlan(RoomKind.Kitchen, kitchen);
            AddToPlan(RoomKind.Shower, showers);
            AddToPlan(RoomKind.Kitchen, kitchen);
            for (int i = dorms.Count - 1; i >= 0; i--) AddToPlan(RoomKind.Dorm, dorms[i]);
            int total = -WallThickness;
            foreach (int w in planWidths) total += w + WallThickness;
            AddToPlan(RoomKind.Corridor, span - total - WallThickness);
        }

        void AddToPlan(RoomKind kind, int width)
        {
            planKinds.Add(kind);
            planWidths.Add(width);
        }

        void BuildStorey(int storey)
        {
            int floor = FloorRow(storey);
            int x = Margin;
            AddRoom(RoomKind.Stairs, x, ShaftWidth, floor);
            x += ShaftWidth;
            CutDoor(x, floor);
            x += WallThickness;

            int centre = planKinds.IndexOf(RoomKind.Shower);
            for (int i = 0; i < planKinds.Count; i++)
            {
                var kind = planKinds[i];
                int width = planWidths[i];
                // On the ground floor the kitchens and showers give way to one big lobby.
                if (storey == 0 && i == centre - 1)
                {
                    kind = RoomKind.Lobby;
                    width = planWidths[i] + planWidths[i + 1] + planWidths[i + 2] + WallThickness * 2;
                    i += 2;
                }
                AddRoom(kind, x, width, floor);
                x += width;
                CutDoor(x, floor);
                x += WallThickness;
            }
            AddRoom(RoomKind.Stairs, x, ShaftWidth, floor);
        }

        void AddRoom(RoomKind kind, int x, int width, int floor)
        {
            var rect = new RectInt(x, floor + 1, width, StoreyHeight);
            Rooms.Add(new Room { Rect = rect, FloorY = floor, Kind = kind });
            Carve(rect, true);
            byte tile = kind == RoomKind.Kitchen ? (byte)1 : kind == RoomKind.Shower ? (byte)2 : (byte)0;
            for (int y = rect.yMin; y < rect.yMax; y++)
                for (int c = rect.xMin; c < rect.xMax; c++)
                    wallTiles[y * Width + c] = tile;

            // The top bunk doubles as a platform.
            if (kind == RoomKind.Dorm)
                for (int c = 0; c < BunkWidth; c++)
                    oneWay[(floor + 2) * Width + x + 1 + c] = true;
        }

        const int BunkWidth = 3;

        void CutDoor(int wallX, int floor)
        {
            Carve(new RectInt(wallX, floor + 1, WallThickness, DoorHeight), true);
            DoorSpots.Add(new Vector2(wallX + WallThickness / 2f, floor + 1f));
        }

        // Each stairwell climbs every storey on planks within jump height, through a hole in each slab,
        // with one plank bridging the hole so the floor above stays walkable. S drops back down.
        void CutStairs(int shaftX)
        {
            int x = shaftX + (ShaftWidth - StairWidth) / 2;
            for (int s = 0; s < Storeys; s++)
            {
                int floor = FloorRow(s), above = FloorRow(s + 1);
                Carve(new RectInt(x, floor + StoreyHeight + 1, StairWidth, SlabThickness), true);
                for (int y = floor + StairRise; y < above; y += StairRise) AddPlank(x, y);
                AddPlank(x, above);
            }
        }

        void AddPlank(int x, int y)
        {
            for (int i = 0; i < StairWidth; i++) oneWay[y * Width + x + i] = true;
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
            skyMap = NewTilemap(grid2.transform, "Sky", 0, Art.Glow);
            SetSkyTint(NightSky);
            var planks = NewTilemap(grid2.transform, "Platforms", 1, Art.Lit);

            var backWall = NewTile(Art.Floor);
            var backWallAlt = NewTile(Art.FloorAlt);
            var stone = NewTile(Art.WallTop);
            var ledge = NewTile(Art.WallFace);
            var plank = NewTile(Art.Plank);
            var kitchen = NewTile(Art.TileKitchen);
            var shower = NewTile(Art.TileShower);
            var night = NewTile(Art.Sky);
            var stars = NewTile(Art.SkyStars);

            for (int y = -6; y < Height + 6; y++)
                for (int x = -8; x < Width + 8; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (IsOpen(x, y))
                    {
                        if (sky[y * Width + x]) skyMap.SetTile(cell, (x * 31 + y * 17) % 23 == 0 ? stars : night);
                        else
                        {
                            byte kind = wallTiles[y * Width + x];
                            background.SetTile(cell, kind == 1 ? kitchen : kind == 2 ? shower : (x * 7 + y * 13 + (x ^ y)) % 5 == 0 ? backWallAlt : backWall);
                        }
                        if (IsOneWay(x, y)) planks.SetTile(cell, plank);
                    }
                    else if (y <= RoofOf(x))
                        background.SetTile(cell, IsOpen(x, y + 1) ? ledge : stone);
                    else
                        skyMap.SetTile(cell, (x * 31 + y * 17) % 23 == 0 ? stars : night);
                }

            // The bottom of every pit fades into gloom instead of showing a floor.
            var gloom = Art.Spr("PitGloom", parent, Art.Square, Art.Glow, new Color(0.05f, 0.01f, 0.1f, 0.96f), 3);
            gloom.transform.localScale = new Vector3(Width, KillY + 1f, 1f);
            gloom.transform.localPosition = new Vector3(Width / 2f, (KillY + 1f) / 2f, 0f);

            // The moon hangs over the roof.
            var moon = Art.Spr("Moon", parent, Art.Disk, Art.Glow, new Color(0.86f, 0.9f, 1f), 0, 4f);
            moon.transform.localPosition = new Vector3(Width * Random.Range(0.25f, 0.75f), Height - 5f, 0f);

            AddDecor(parent);
        }

        /// <summary>The sky art is pale so it can be tinted anywhere from night to sunrise.</summary>
        public void SetSkyTint(Color tint)
        {
            if (skyMap != null) skyMap.color = tint;
        }

        bool InMap(int x) => x >= 0 && x < Width;
        int RoofOf(int x) => InMap(x) ? roofAt[x] : GroundRow;

        // Furniture sits against the back wall and never affects collision.
        void AddDecor(Transform parent)
        {
            var root = new GameObject("Decor").transform;
            root.SetParent(parent, false);
            foreach (var room in Rooms)
            {
                float floor = room.FloorY + 1f, top = room.Rect.yMax;
                float left = room.Rect.xMin, right = room.Rect.xMax, middle = (left + right) / 2f;
                switch (room.Kind)
                {
                    case RoomKind.Dorm:
                        // The bunk's top mattress lines up with its plank.
                        PlaceDecor(root, Art.Bunk, Art.Lit, Color.white, new Vector2(left + 1f + BunkWidth / 2f, floor + 1.5f), new Vector2(BunkWidth + 0.6f, 2.6f));
                        PlaceDecor(root, Art.Wardrobe, Art.Lit, Color.white, new Vector2(right - 1.2f, floor + 1.6f), new Vector2(1.6f, 3.2f));
                        PlaceDecor(root, Art.Desk, Art.Lit, Color.white, new Vector2(middle + 0.8f, floor + 0.7f), new Vector2(2f, 1.5f));
                        var window = WindowSpot(room);
                        Window(root, window.x, top);
                        Poster(root, right - 3.2f, floor + 3.6f);
                        break;
                    case RoomKind.Kitchen:
                        PlaceDecor(root, Art.Fridge, Art.Lit, Color.white, new Vector2(left + 1.3f, floor + 1.5f), new Vector2(1.5f, 3f));
                        PlaceDecor(root, Art.Stove, Art.Lit, Color.white, new Vector2(left + 3.2f, floor + 0.9f), new Vector2(1.8f, 1.8f));
                        PlaceDecor(root, Art.Stove, Art.Lit, Color.white, new Vector2(left + 5.1f, floor + 0.9f), new Vector2(1.8f, 1.8f));
                        Window(root, right - 3f, top);
                        break;
                    case RoomKind.Shower:
                        for (float x = left + 1.2f; x < right - 0.6f; x += 2.2f)
                            PlaceDecor(root, Art.Shower, Art.Lit, Color.white, new Vector2(x, floor + 1.9f), new Vector2(1.8f, 3.6f));
                        break;
                    case RoomKind.Corridor:
                        for (float x = left + 2f; x < right - 1.5f; x += 4.5f)
                        {
                            Door(root, x, floor);
                            if (x + 2.2f < right - 1f) Poster(root, x + 2.2f, floor + 3f);
                        }
                        break;
                    case RoomKind.Lobby:
                        PlaceDecor(root, Art.Desk, Art.Lit, new Color(0.85f, 0.8f, 0.7f), new Vector2(middle, floor + 0.9f), new Vector2(3.2f, 1.8f));
                        Door(root, left + 2.2f, floor);
                        Door(root, right - 2.2f, floor);
                        for (int i = -1; i <= 1; i++) Poster(root, middle + i * 1.5f, floor + 3.8f);
                        break;
                    case RoomKind.Stairs:
                        Window(root, room.Rect.xMin + 1f, top);
                        break;
                }
            }
        }

        static Vector2 WindowSpot(Room room) => new Vector2((room.Rect.xMin + room.Rect.xMax) / 2f + 0.8f, room.Rect.yMax - 2f);

        static void Door(Transform root, float x, float floor) =>
            PlaceDecor(root, Art.Pillar, Art.Lit, Color.white, new Vector2(x, floor + 1.5f), new Vector2(1.5f, 3f));

        static void Window(Transform root, float x, float top) =>
            PlaceDecor(root, Art.Window, Art.Glow, new Color(0.45f, 0.62f, 1f, 0.75f), new Vector2(x, top - 2f), new Vector2(1.4f, 2.4f));

        static void Poster(Transform root, float x, float y)
        {
            var colour = Random.value < 0.5f ? new Color(0.95f, 0.85f, 0.6f) : new Color(0.65f, 0.8f, 0.95f);
            PlaceDecor(root, Art.Banner, Art.Lit, colour, new Vector2(x, y), new Vector2(1.2f, 1.2f));
        }

        static void PlaceDecor(Transform root, Sprite sprite, Material material, Color color, Vector2 position, Vector2 size)
        {
            var go = Art.Spr(sprite.name, root, sprite, material, color, 0);
            go.GetComponent<SpriteRenderer>().sortingOrder = 1;
            go.transform.localPosition = position;
            // Sprites differ in native size, so scale each to the size asked for.
            Vector2 native = sprite.bounds.size;
            go.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
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
