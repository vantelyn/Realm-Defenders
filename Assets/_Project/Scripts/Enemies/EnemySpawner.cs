using System.Collections;
using UnityEngine;
using Game.Targeting;

namespace Game.Enemies
{

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs a generar")]
    public GameObject[] prefabs;

    [Header("Spawn rate")]
    [Tooltip("Tiempo base de espera entre spawns (en segundos). Se divide por la amenaza normalizada (0..1) + 1, de forma que a threat=0 el tiempo es el base, a threat=max el tiempo es base/2... pero usamos interpolacion directa contra minDelay.")]
    public float baseDelay = 20f;

    [Tooltip("Tiempo m�nimo de espera cuando la amenaza es m�xima.")]
    public float minDelay = 1f;

    [Tooltip("Valor de amenaza en el que la frecuencia es maxima. Por encima de esto no acelera m�s.")]
    public float maxThreat = 100f;

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
        while (true)
        {
            yield return new WaitForSeconds(GetCurrentDelay());
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Instantiate(prefab, transform.position, Quaternion.identity);
        }
    }

    private float GetCurrentDelay()
    {
        float t = Mathf.Clamp01(ThreatRegistry.GetTotalThreat() / maxThreat);
        return Mathf.Lerp(baseDelay, minDelay, t);
    }
}
}
