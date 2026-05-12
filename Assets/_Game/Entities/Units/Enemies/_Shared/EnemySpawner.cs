using System.Collections;
using UnityEngine;

using Game.Managers;

namespace Game.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefabs normales")]
        [SerializeField] private GameObject[] normalPrefabs;

        [Header("Prefab Boss")]
        [SerializeField] private GameObject bossPrefab;

        [Header("Threat")]
        [Tooltip("Por debajo de esta amenaza no se spawnea nada.")]
        [SerializeField] private float minThreatToSpawn = 5f;

        [Tooltip("A partir de esta amenaza pueden aparecer bosses.")]
        [SerializeField] private float bossThreat = 90f;

        [Tooltip("Amenaza máxima usada para calcular la velocidad máxima.")]
        [SerializeField] private float maxThreat = 100f;

        [Header("Spawn Rate")]
        [SerializeField] private float baseDelay = 20f;
        [SerializeField] private float minDelay = 1f;

        [Header("Bosses")]
        [Tooltip("Máximo de bosses vivos en toda la escena.")]
        [SerializeField] private int maxBossesAlive = 4;

        [Tooltip("Probabilidad de spawnear un boss cuando la amenaza supera el umbral.")]
        [Range(0f, 1f)]
        [SerializeField] private float bossSpawnChance = 0.25f;

        private static int aliveBosses = 0;

        private void Start()
        {
            if (normalPrefabs == null || normalPrefabs.Length == 0)
            {
                Debug.LogWarning($"[{name}] EnemySpawner sin prefabs normales asignados.");
                return;
            }

            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                float threat = ThreatManager.GetTotalThreat();

                if (threat < minThreatToSpawn)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                float wait = GetCurrentDelay(threat);
                yield return new WaitForSeconds(wait);

                threat = ThreatManager.GetTotalThreat();

                if (threat < minThreatToSpawn)
                    continue;

                SpawnNormalEnemy();

                if (threat >= bossThreat)
                {
                    TrySpawnBoss();
                }
            }
        }

        private void SpawnNormalEnemy()
        {
            GameObject prefab = normalPrefabs[Random.Range(0, normalPrefabs.Length)];
            Instantiate(prefab, transform.position, Quaternion.identity);
        }

        private void TrySpawnBoss()
        {
            if (bossPrefab == null)
                return;

            if (aliveBosses >= maxBossesAlive)
                return;

            if (Random.value > bossSpawnChance)
                return;

            GameObject boss = Instantiate(bossPrefab, transform.position, Quaternion.identity);

            aliveBosses++;

            BossLifetimeTracker tracker = boss.AddComponent<BossLifetimeTracker>();
            tracker.Init();
        }

        private float GetCurrentDelay(float threat)
        {
            float t = Mathf.InverseLerp(minThreatToSpawn, maxThreat, threat);
            return Mathf.Lerp(baseDelay, minDelay, t);
        }

        public static void NotifyBossDestroyed()
        {
            aliveBosses = Mathf.Max(0, aliveBosses - 1);
        }
    }

    public class BossLifetimeTracker : MonoBehaviour
    {
        private bool initialized = false;

        public void Init()
        {
            initialized = true;
        }

        private void OnDestroy()
        {
            if (initialized)
            {
                EnemySpawner.NotifyBossDestroyed();
            }
        }
    }
}