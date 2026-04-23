using UnityEngine;

public class TNTScript : Enemy
{
    [Header("TNT Throwing")]
    [SerializeField] private GameObject dynamitePrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwFlightDuration = 0.8f;
    [SerializeField] private float throwArcHeight = 1.5f;

    [Header("Facing")]
    [SerializeField] private float facingVelocityThreshold = 0.05f;

    protected override void Update()
    {
        base.Update();
        UpdateFacingByMovement();
    }

    // Solo trigger; nos saltamos attackDirection en este enemigo
    protected override void ApplyAttackAnimatorParams(int attackDirection)
    {
        animator.SetTrigger("doAttack");
    }

    private void UpdateFacingByMovement()
    {
        if (navMeshAgent == null || !navMeshAgent.hasPath) return;

        float vx = navMeshAgent.velocity.x;
        if (Mathf.Abs(vx) > facingVelocityThreshold)
        {
            float sign = vx > 0f ? 1f : -1f;
            transform.localScale = new Vector3(sign, 1f, 1f);
        }
    }

    // Llamado desde Animation Event en el frame de suelta
    public void SpawnDynamite()
    {
        if (dynamitePrefab == null) return;

        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position;
        Vector3 targetPos = GetCurrentTargetPosition();

        GameObject go = Instantiate(dynamitePrefab, spawnPos, Quaternion.identity);
        go.GetComponent<Dynamite>()?.Launch(targetPos, throwFlightDuration, throwArcHeight);
    }

    private Vector3 GetCurrentTargetPosition()
    {
        if (currentTargetUnit != null) return currentTargetUnit.position;
        if (motherTransform != null) return motherTransform.position;
        return transform.position + (Vector3)attackDirectionVector.normalized * attackRange;
    }
}