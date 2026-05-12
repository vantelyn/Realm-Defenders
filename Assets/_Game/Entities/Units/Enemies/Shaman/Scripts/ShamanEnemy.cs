using UnityEngine;
using Game.AI;
using Game.Projectiles;

namespace Game.Enemies
{

/// <summary>
/// Shaman: enemigo ranged que lanza un proyectil magico con explosion AOE
/// al impactar. Mismo patron de cacheo de target que GnollEnemy / HarpoonFishEnemy.
/// </summary>
public class ShamanEnemy : BaseEnemyAI
{
    [Header("Ranged")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;

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

    /// <summary>Animation event en Shaman_Attack.</summary>
    public void SpawnProjectile()
    {
        if (projectilePrefab == null) return;
        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        ShamanProjectile proj = go.GetComponent<ShamanProjectile>();
        if (proj != null) proj.Launch(pendingTargetPos);
    }
}
}
