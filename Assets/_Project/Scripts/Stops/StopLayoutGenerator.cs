using System.Collections.Generic;
using UnityEngine;

namespace NightTrolleybus.Stops
{
    public enum StopInterior
    {
        Rooms,
        Aisles
    }

    /// <summary>Під час входу на зупинку випадково будує стіни, меблі й деталі та розкладає предмети й ворогів. Увесь вільний простір лишається досяжним.</summary>
    public class StopLayoutGenerator : MonoBehaviour
    {
        private const int Width = 60;
        private const int Height = 24;
        private const int BottomY = -12;
        private const int EntranceEnd = 7;
        private const int PlaceAttempts = 200;
        private const int GapHeight = 4;
        private const float WallShadowThickness = 0.1f;
        private const float WallShadowOverlap = 0.6f;
        private static readonly Vector2Int StartCell = new Vector2Int(3, 12);

        [Tooltip("Неактивний зразок твердого об'єкта: спрайт, колайдер і дочірня тінь")]
        [SerializeField] private GameObject solidTemplate;
        [SerializeField] private StopInterior interior;
        [SerializeField] private Sprite interiorSprite;
        [SerializeField] private int firstWallMin = 12;
        [SerializeField] private int firstWallMax = 18;
        [SerializeField] private int spacingMin = 10;
        [SerializeField] private int spacingMax = 17;
        [SerializeField] private int lastWallX = 54;

        [Header("Деталі")]
        [SerializeField] private Sprite[] props;
        [SerializeField] private int propsMin = 10;
        [SerializeField] private int propsMax = 16;
        [SerializeField] private Sprite[] decals;
        [SerializeField] private int decalCount = 60;

        [Header("Що розкласти")]
        [Tooltip("Лишаються в першій частині локації, ближче до входу")]
        [SerializeField] private Transform[] nearEntrance;
        [SerializeField] private Transform[] pickups;
        [SerializeField] private Transform[] enemies;
        [Tooltip("Ділянки в клітинках, яких генератор не чіпає: зарядна станція, прихована кімната")]
        [SerializeField] private RectInt[] reserved;

        private readonly bool[,] solid = new bool[Width, Height];
        private readonly bool[,] occupied = new bool[Width, Height];
        private readonly bool[,] visited = new bool[Width, Height];
        private readonly Queue<Vector2Int> queue = new Queue<Vector2Int>();
        private readonly List<Vector2> placed = new List<Vector2>();
        private int firstWallX;

        private void Awake()
        {
            foreach (RectInt area in reserved)
                Mark(solid, area, true);

            if (interior == StopInterior.Rooms)
                BuildRooms();
            else
                BuildAisles();

            PlaceNearEntrance();
            PlaceProps();
            Scatter(pickups, EntranceEnd, 5f);
            Scatter(enemies, firstWallMin + 2, 4f);
            PlaceDecals();
        }

        private void BuildRooms()
        {
            int x = Random.Range(firstWallMin, firstWallMax + 1);
            firstWallX = x;
            while (x <= lastWallX)
            {
                BuildWallWithGaps(x);
                x += Random.Range(spacingMin, spacingMax + 1);
            }
        }

        private void BuildWallWithGaps(int x)
        {
            int firstGap = Random.Range(1, Height - GapHeight - 1);
            bool hasSecondGap = Random.value < 0.5f;
            int secondGap = Random.Range(1, Height - GapHeight - 1);

            int runStart = -1;
            for (int y = 0; y <= Height; y++)
            {
                bool isGap = y == Height || IsInGap(y, firstGap) || (hasSecondGap && IsInGap(y, secondGap)) || solid[x, y];
                if (!isGap && runStart < 0)
                    runStart = y;
                if (isGap && runStart >= 0)
                {
                    var run = new RectInt(x, runStart, 1, y - runStart);
                    Mark(solid, run, true);
                    CreateSolid(interiorSprite, run, true);
                    runStart = -1;
                }
            }
        }

        private static bool IsInGap(int y, int gapStart) => y >= gapStart && y < gapStart + GapHeight;

        private void BuildAisles()
        {
            firstWallX = firstWallMin;
            for (int x = Random.Range(firstWallMin, firstWallMax + 1); x <= lastWallX; x += Random.Range(spacingMin, spacingMax + 1))
            {
                int runs = Random.Range(1, 4);
                for (int i = 0; i < runs; i++)
                {
                    int length = Random.Range(4, 10);
                    var run = new RectInt(x, Random.Range(1, Height - length - 1), 1, length);
                    if (TryBlock(run))
                        CreateSolid(interiorSprite, run, true);
                }
            }
        }

        private void PlaceNearEntrance()
        {
            foreach (Transform item in nearEntrance)
            {
                RectInt area = Footprint(item.GetComponent<SpriteRenderer>().bounds.size);
                for (int attempt = 0; attempt < PlaceAttempts; attempt++)
                {
                    area.position = new Vector2Int(Random.Range(EntranceEnd + 1, firstWallX - area.width), Random.Range(1, Height - area.height - 1));
                    if (!IsFree(area) || TooClose(Center(area), 4f))
                        continue;

                    item.position = Center(area);
                    Mark(occupied, area, true);
                    placed.Add(Center(area));
                    break;
                }
            }
        }

        private void PlaceProps()
        {
            int count = Random.Range(propsMin, propsMax + 1);
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = props[Random.Range(0, props.Length)];
                RectInt area = Footprint(sprite.bounds.size);
                for (int attempt = 0; attempt < PlaceAttempts; attempt++)
                {
                    area.position = new Vector2Int(Random.Range(EntranceEnd + 1, Width - area.width), Random.Range(0, Height - area.height));
                    if (!TryBlock(area))
                        continue;

                    CreateSolid(sprite, area, false);
                    break;
                }
            }
        }

        private void Scatter(Transform[] items, int minX, float spacing)
        {
            foreach (Transform item in items)
            {
                for (int attempt = 0; attempt < PlaceAttempts; attempt++)
                {
                    var area = new RectInt(Random.Range(minX, Width), Random.Range(0, Height), 1, 1);
                    if (!IsFree(area) || TooClose(Center(area), spacing))
                        continue;

                    item.position = Center(area);
                    Mark(occupied, area, true);
                    placed.Add(Center(area));
                    break;
                }
            }
        }

        private void PlaceDecals()
        {
            Material material = solidTemplate.GetComponent<SpriteRenderer>().sharedMaterial;
            for (int i = 0; i < decalCount; i++)
            {
                var decal = new GameObject("Decal").AddComponent<SpriteRenderer>();
                decal.transform.SetParent(transform, false);
                decal.transform.position = new Vector3(Random.Range(0f, Width), BottomY + Random.Range(0f, Height), 0f);
                decal.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0, 4) * 90f);
                decal.sprite = decals[Random.Range(0, decals.Length)];
                decal.sharedMaterial = material;
                decal.flipX = Random.value < 0.5f;
                decal.sortingOrder = -9;
            }
        }

        private void CreateSolid(Sprite sprite, RectInt area, bool isTiled)
        {
            GameObject instance = Instantiate(solidTemplate, transform);
            instance.transform.position = Center(area);

            var spriteRenderer = instance.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.drawMode = isTiled ? SpriteDrawMode.Tiled : SpriteDrawMode.Simple;
            Vector2 size = isTiled ? area.size : (Vector2)sprite.bounds.size;
            if (isTiled)
                spriteRenderer.size = size;

            instance.GetComponent<BoxCollider2D>().size = size;

            // Тінь кидають лише стіни. Вона тонка посередині, щоб ближня до світла половина стіни лишалася видимою.
            Transform shadow = instance.transform.GetChild(0);
            shadow.gameObject.SetActive(isTiled);
            // Біля зовнішньої стіни тінь заходить у неї, інакше в куті світло прослизає в щілину між тінями.
            float below = area.yMin == 0 ? WallShadowOverlap : 0f;
            float above = area.yMax == Height ? WallShadowOverlap : 0f;
            shadow.localScale = new Vector3(WallShadowThickness, size.y + below + above, 1f);
            shadow.localPosition = new Vector3(0f, (above - below) / 2f, 0f);
            instance.SetActive(true);
        }

        /// <summary>Займає ділянку лише тоді, коли після цього весь вільний простір лишається досяжним від входу.</summary>
        private bool TryBlock(RectInt area)
        {
            if (!IsFree(area))
                return false;

            Mark(solid, area, true);
            if (IsEverythingReachable())
                return true;

            Mark(solid, area, false);
            return false;
        }

        private bool IsFree(RectInt area)
        {
            if (area.xMin < 0 || area.yMin < 0 || area.xMax > Width || area.yMax > Height)
                return false;

            foreach (Vector2Int cell in area.allPositionsWithin)
            {
                if (solid[cell.x, cell.y] || occupied[cell.x, cell.y])
                    return false;
            }

            return true;
        }

        private bool IsEverythingReachable()
        {
            System.Array.Clear(visited, 0, visited.Length);
            queue.Clear();
            queue.Enqueue(StartCell);
            visited[StartCell.x, StartCell.y] = true;
            int reached = 1;

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                reached += Visit(cell.x + 1, cell.y) + Visit(cell.x - 1, cell.y) + Visit(cell.x, cell.y + 1) + Visit(cell.x, cell.y - 1);
            }

            int free = 0;
            foreach (bool isSolid in solid)
            {
                if (!isSolid)
                    free++;
            }

            return reached == free;
        }

        private int Visit(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height || solid[x, y] || visited[x, y])
                return 0;

            visited[x, y] = true;
            queue.Enqueue(new Vector2Int(x, y));
            return 1;
        }

        private bool TooClose(Vector2 point, float spacing)
        {
            foreach (Vector2 other in placed)
            {
                if ((other - point).sqrMagnitude < spacing * spacing)
                    return true;
            }

            return false;
        }

        private static void Mark(bool[,] grid, RectInt area, bool value)
        {
            foreach (Vector2Int cell in area.allPositionsWithin)
                grid[cell.x, cell.y] = value;
        }

        private static RectInt Footprint(Vector2 size) =>
            new RectInt(0, 0, Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y));

        private static Vector3 Center(RectInt area) =>
            new Vector3(area.x + area.width / 2f, BottomY + area.y + area.height / 2f, 0f);
    }
}
