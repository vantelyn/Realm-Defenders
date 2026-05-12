using UnityEngine;
using Game.AI;
using Game.Projectiles;

namespace Game.Enemies
{

/// <summary>
/// HarpoonFish: enemigo ranged que lanza arpones en linea casi recta. Mismo patron
/// que GnollEnemy: cachea posicion del target en PerformAttack, animation event en
/// el clip Throw llama a SpawnHarpoon que instancia y lanza el proyectil.
/// </summary>
public class HarpoonFishEnemy : BaseEnemyAI
{
    [Header("Ranged")]
    [SerializeField] private GameObject harpoonPrefab;
    [SerializeField] private Transform harpoonSpawnPoint;

    private Vector3 pendingTargetPos;

    protected override void PerformAttack()
    {
        pendingTargetPos = GetCurrentTargetPosition();
        base.PerformAttack();
    }

    private Vector3 GetCurrentTargetPosition()
    {
        if (currentOpportunityTarget != null) return currentOpportunityTarget.position;
        if (currentStrategicTarget != null) return currentStrategicTarget.Transform.position;
        return transform.position + (transform.localScale.x > 0 ? Vector3.right : Vector3.left) * 3f;
    }

    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }

    /// <summary>Animation event en HarpoonFish_Throw.</summary>
    public void SpawnHarpoon()
    {
        if (harpoonPrefab == null) return;
        Vector3 spawnPos = harpoonSpawnPoint != null ? harpoonSpawnPoint.position : transform.position;
        GameObject go = Instantiate(harpoonPrefab, spawnPos, Quaternion.identity);
        Harpoon h = go.GetComponent<Harpoon>();
        if (h != null) h.Launch(pendingTargetPos);
    }
}
}
