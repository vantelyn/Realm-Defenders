using UnityEngine;
using Game.AI;
using Game.Projectiles;

namespace Game.Enemies
{

/// <summary>
/// Gnoll enemigo ranged. Lanza huesos en arco hacia el target desde stopDistanceOpportunity
/// (o stopDistanceStrategic contra estructuras). En lugar del DetectAndDamageTargets melee,
/// el animation event del clip Throw llama a SpawnBone() que instancia el proyectil.
/// </summary>
public class GnollEnemy : BaseEnemyAI
{
    [Header("Ranged")]
    [SerializeField] private GameObject bonePrefab;
    [SerializeField] private Transform boneSpawnPoint;

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
        // Fallback: hacia donde mira
        return transform.position + (transform.localScale.x > 0 ? Vector3.right : Vector3.left) * 3f;
    }

    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
        var g = GetComponent<Game.Audio.AttackGruntSfx>(); if (g != null) g.PlayGrunt();
    }

    /// <summary>
    /// Llamado por el animation event en el clip Gnoll_Throw a mitad del swing.
    /// </summary>
    public void SpawnBone()
    {
        if (bonePrefab == null) return;
        Vector3 spawnPos = boneSpawnPoint != null ? boneSpawnPoint.position : transform.position;
        GameObject go = Instantiate(bonePrefab, spawnPos, Quaternion.identity);
        GnollBone bone = go.GetComponent<GnollBone>();
        if (bone != null) bone.Launch(pendingTargetPos);
    }
}
}
