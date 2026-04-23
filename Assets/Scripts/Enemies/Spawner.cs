using System.Collections;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefabs a generar")]
    public GameObject[] prefabs;

    [Header("Configuración")]
    public int totalToSpawn = 5;
    public float minSpawnTime = 5f;
    public float maxSpawnTime = 20f;

    private int spawnedCount = 0;

    void Start()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("No hay prefabs asignados en el spawner.");
            return;
        }

        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (spawnedCount < totalToSpawn)
        {
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);

            GameObject prefabAleatorio = prefabs[Random.Range(0, prefabs.Length)];
            Instantiate(prefabAleatorio, transform.position, Quaternion.identity);

            spawnedCount++;
        }
    }
}