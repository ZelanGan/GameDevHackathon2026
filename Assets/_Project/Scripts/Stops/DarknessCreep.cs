using System.Collections.Generic;
using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Stops
{
    /// <summary>Темрява наповзає з кінця локації: посилює страх, народжує мобів і забирає полум'я в собі.</summary>
    public class DarknessCreep : MonoBehaviour
    {
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
        [SerializeField] private float spawnMinY = -10f;
        [SerializeField] private float spawnMaxY = 10f;
        [SerializeField] private float slowSpawnInterval = 25f;
        [SerializeField] private float fastSpawnInterval = 8f;
        [SerializeField] private int maxSpawned = 6;

        [Tooltip("Рівень темряви у діапазоні 0..1")]
        [SerializeField] private FloatEventChannel darknessChanged;

        private readonly List<GameObject> spawned = new List<GameObject>();
        private float elapsed;
        private float spawnTimer;

        public float Level => Mathf.Clamp01(elapsed / duration);
        public float EdgeX => Mathf.Lerp(startEdgeX, endEdgeX, Level);

        private void Start() => spawnTimer = slowSpawnInterval;

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

            spawnTimer = Mathf.Lerp(slowSpawnInterval, fastSpawnInterval, Level);
            spawned.RemoveAll(enemy => enemy == null);
            if (spawned.Count < maxSpawned)
                spawned.Add(Instantiate(enemyPrefab, new Vector3(EdgeX - 1f, Random.Range(spawnMinY, spawnMaxY), 0f), Quaternion.identity));
        }
    }
}
