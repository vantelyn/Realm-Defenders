using System.Collections;
using UnityEngine;

public class TorchSpawner : MonoBehaviour
{
    [Header("Prefab a generar")]
    public GameObject torchPrefab;

    [Header("Configuración")]
    public int totalToSpawn = 5;
    public float minSpawnTime = 5f;
    public float maxSpawnTime = 20f;

    private int spawnedCount = 0;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (spawnedCount < totalToSpawn)
        {
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);

            Instantiate(torchPrefab, transform.position, Quaternion.identity);
            spawnedCount++;
        }
    }
}