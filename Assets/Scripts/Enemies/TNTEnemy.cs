using UnityEngine;

public class TNTEnemy : BaseEnemyAI
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
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }

    private void UpdateFacingByMovement()
    {
        if (agent == null || !agent.hasPath) return;

        float vx = agent.velocity.x;
        if (Mathf.Abs(vx) > facingVelocityThreshold)
        {
            float sign = vx > 0f ? 1f : -1f;
            transform.localScale = new Vector3(sign, 1f, 1f);
        }
    }

    // Animation Event en el frame de soltar la dinamita
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
        if (currentOpportunityTarget != null) return currentOpportunityTarget.position;
        if (currentStrategicTarget != null) return currentStrategicTarget.Transform.position;
        return transform.position + (Vector3)attackDirectionVector.normalized * attackRange;
    }
}