using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Enemies
{

/// <summary>
/// Genera ovejas peri�dicamente dentro de un radio alrededor del spawner.
/// Se mantiene un l�mite m�ximo de ovejas vivas generadas por este spawner.
/// </summary>
public class SheepSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject sheepPrefab;

    [Header("Timing")]
    [SerializeField] private float minSpawnTime = 8f;
    [SerializeField] private float maxSpawnTime = 20f;

    [Header("Position")]
    [Tooltip("Radio alrededor del spawner en el que aparecen las ovejas.")]
    [SerializeField] private float spawnRadius = 4f;

    [Tooltip("Si est� activo, ajusta la posici�n a un punto v�lido del NavMesh.")]
    [SerializeField] private bool snapToNavMesh = true;

    [Header("Population Cap")]
    [Tooltip("M�ximo de ovejas vivas generadas por este spawner. 0 = sin l�mite.")]
    [SerializeField] private int maxAlive = 6;

    private int aliveCount = 0;

    private void Start()
    {
        if (sheepPrefab == null)
        {
            Debug.LogWarning($"[{name}] SheepSpawner sin prefab asignado.");
            return;
        }
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float wait = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(wait);

            if (maxAlive > 0 && aliveCount >= maxAlive) continue;

            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        Vector3 pos = GetRandomPosition();
        GameObject sheep = Instantiate(sheepPrefab, pos, Quaternion.identity);

        aliveCount++;
        SheepLifetimeTracker tracker = sheep.AddComponent<SheepLifetimeTracker>();
        tracker.Init(this);
    }

    public void NotifySheepDestroyed()
    {
        aliveCount = Mathf.Max(0, aliveCount - 1);
    }

    private Vector3 GetRandomPosition()
    {
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 candidate = transform.position + new Vector3(offset.x, offset.y, 0f);

        if (snapToNavMesh && NavMesh.SamplePosition(candidate, out NavMeshHit hit, spawnRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return candidate;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.5f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}

/// <summary>
/// Componente auxiliar que notifica al spawner cuando la oveja es destruida,
/// para que el conteo de ovejas vivas sea correcto.
/// </summary>
public class SheepLifetimeTracker : MonoBehaviour
{
    private SheepSpawner spawner;

    public void Init(SheepSpawner owner) { spawner = owner; }

    private void OnDestroy()
    {
        if (spawner != null) spawner.NotifySheepDestroyed();
    }
}
}
