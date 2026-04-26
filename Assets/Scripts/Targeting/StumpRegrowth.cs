using UnityEngine;

namespace Game.Targeting
{

/// <summary>
/// Se coloca en el prefab del Stump. Pasado un tiempo aleatorio entre min/max,
/// instancia el prefab del �rbol en su misma posici�n y se autodestruye.
/// </summary>
public class StumpRegrowth : MonoBehaviour
{
    [Header("Regrowth")]
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private float minRegrowthTime = 20f;
    [SerializeField] private float maxRegrowthTime = 40f;

    private float timer;
    private bool regrown;

    private void Start()
    {
        timer = Random.Range(minRegrowthTime, maxRegrowthTime);
    }

    private void Update()
    {
        if (regrown) return;

        timer -= Time.deltaTime;
        if (timer <= 0f) Regrow();
    }

    private void Regrow()
    {
        regrown = true;

        if (treePrefab != null)
        {
            Instantiate(treePrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
}
