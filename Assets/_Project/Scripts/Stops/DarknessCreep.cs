using System.Collections.Generic;
using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Stops.Enemies;
using UnityEngine;

namespace NightTrolleybus.Stops
{
    /// <summary>Темрява наповзає з кінця локації: посилює страх, забирає полум'я й періодично насилає мобів на гравця.</summary>
    public class DarknessCreep : MonoBehaviour
    {
        private const int SpawnAttempts = 12;
        private const float RetryDelay = 1f;

        [Tooltip("Чорна стіна, лівий край якої є межею темряви")]
        [SerializeField] private Transform wall;
        [SerializeField] private float wallHalfWidth = 30f;
        [SerializeField] private float startEdgeX = 62f;
        [SerializeField] private float endEdgeX = 5f;
        [Tooltip("За скільки секунд темрява доходить до входу")]
        [SerializeField] private float duration = 150f;

        [Header("Вплив")]
        [SerializeField] private Transform player;
        [Range(0f, 1f)]
        [SerializeField] private float fearAtFullLevel = 0.5f;
        [Tooltip("Скільки полум'я за секунду забирає темрява, коли гравець усередині")]
        [SerializeField] private float flameDrainInside = 5f;

        [Header("Моби")]
        [SerializeField] private GameObject enemyPrefab;
        [Tooltip("Секунди між мобами на початку зупинки")]
        [SerializeField] private float slowSpawnInterval = 28f;
        [Tooltip("Секунди між мобами, коли темрява вже дійшла до входу")]
        [SerializeField] private float fastSpawnInterval = 14f;
        [SerializeField] private int maxAlive = 5;
        [Tooltip("Моб з'являється за межами кадру, але не надто далеко від гравця")]
        [SerializeField] private float spawnMinDistance = 9.5f;
        [SerializeField] private float spawnMaxDistance = 14f;
        [SerializeField] private float enemyRadius = 0.5f;
        [SerializeField] private Vector2 levelMin = new Vector2(1f, -11f);
        [SerializeField] private Vector2 levelMax = new Vector2(59f, 11f);

        [Tooltip("Рівень темряви у діапазоні 0..1")]
        [SerializeField] private FloatEventChannel darknessChanged;

        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly Collider2D[] overlapHits = new Collider2D[4];
        private readonly RaycastHit2D[] lineHits = new RaycastHit2D[8];
        private ContactFilter2D solidFilter;
        private float elapsed;
        private float spawnTimer;

        public float Level => Mathf.Clamp01(elapsed / duration);
        public float EdgeX => Mathf.Lerp(startEdgeX, endEdgeX, Level);

        private void Start()
        {
            spawnTimer = slowSpawnInterval;
            solidFilter = new ContactFilter2D { useTriggers = false };
        }

        private void OnDestroy() => GameContext.Fear?.RemoveSource(this);

        private void Update()
        {
            elapsed += Time.deltaTime;
            wall.position = new Vector3(EdgeX + wallHalfWidth, wall.position.y, wall.position.z);
            darknessChanged.Raise(Level);
            GameContext.Fear?.SetSource(this, Level * fearAtFullLevel);

            if (player.position.x >= EdgeX)
                GameContext.Flame?.Drain(flameDrainInside * Time.deltaTime);

            UpdateSpawning();
        }

        private void UpdateSpawning()
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
                return;

            spawned.RemoveAll(enemy => enemy == null);
            if (spawned.Count >= maxAlive)
            {
                spawnTimer = RetryDelay;
                return;
            }

            if (!TryFindSpawnPoint(out Vector2 point))
            {
                spawnTimer = RetryDelay;
                return;
            }

            GameObject enemy = Instantiate(enemyPrefab, point, Quaternion.identity);
            enemy.GetComponent<EnemyBrain>().Alert();
            GameContext.Audio?.Play(SoundId.EnemyAppear);
            spawned.Add(enemy);
            spawnTimer = Mathf.Lerp(slowSpawnInterval, fastSpawnInterval, Level);
        }

        private bool TryFindSpawnPoint(out Vector2 point)
        {
            for (int i = 0; i < SpawnAttempts; i++)
            {
                float distance = Random.Range(spawnMinDistance, spawnMaxDistance);
                point = (Vector2)player.position + Random.insideUnitCircle.normalized * distance;

                bool insideLevel = point.x >= levelMin.x && point.x <= Mathf.Min(levelMax.x, EdgeX - 1f) &&
                                   point.y >= levelMin.y && point.y <= levelMax.y;
                if (insideLevel && IsFree(point) && CanReachPlayer(point))
                    return true;
            }

            point = default;
            return false;
        }

        private bool IsFree(Vector2 point) => Physics2D.OverlapCircle(point, enemyRadius, solidFilter, overlapHits) == 0;

        // Моби йдуть по прямій, тому між ними й гравцем не має бути стіни.
        private bool CanReachPlayer(Vector2 point)
        {
            int count = Physics2D.Linecast(point, player.position, solidFilter, lineHits);
            for (int i = 0; i < count; i++)
            {
                Transform hit = lineHits[i].transform;
                if (hit != player && hit.GetComponent<EnemyBrain>() == null)
                    return false;
            }

            return true;
        }
    }
}
