using UnityEngine;
using UnityEngine.AI;
using Game.Core;

namespace Game.UI
{

public class UnitSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory inventory;

    [Header("Spawn Point")]
    [Tooltip("Punto central donde aparecen las unidades. Si es null, se usa el transform del propio spawner.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Radio alrededor del spawnPoint en el que aparecen las unidades al azar.")]
    [SerializeField] private float spawnRadius = 1.5f;

    [Tooltip("Si est� activo, ajusta la posici�n a un punto v�lido del NavMesh (evita spawns sobre agua o dentro de edificios).")]
    [SerializeField] private bool snapToNavMesh = true;

    public bool TrySpawn(UnitRecipe recipe)
    {
        if (recipe == null || recipe.unitPrefab == null) return false;
        if (inventory == null) return false;

        if (!recipe.TryPay(inventory)) return false;

        Vector3 pos = GetRandomSpawnPosition();
        Instantiate(recipe.unitPrefab, pos, Quaternion.identity);
        return true;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector2 offset = Random.insideUnitCircle * spawnRadius;
        Vector3 candidate = center + new Vector3(offset.x, offset.y, 0f);

        if (snapToNavMesh && NavMesh.SamplePosition(candidate, out NavMeshHit hit, spawnRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return candidate;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(center, spawnRadius);
    }
}
}
