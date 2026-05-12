using UnityEngine;
using Game.AI;
using Game.Combat;

namespace Game.Enemies
{

/// <summary>
/// Enemigo tanque con bloqueo proactivo. Cuando el target esta lejos
/// activa el Guard (absorbe dano frontal); cuando llega a attackRange
/// desactiva Guard y golpea.
/// </summary>
public class MinotaurEnemy : BaseEnemyAI, IDamageBlocker
{
    [Header("Guard")]
    [Tooltip("Distancia minima al objetivo para mantener el Guard activado durante el approach.")]
    [SerializeField] private float guardEngageDistance = 4f;
    [Tooltip("Angulo total cubierto por el Guard centrado en blockDir (180 = frente completo).")]
    [SerializeField] private float blockAngle = 180f;

    private bool isGuarding;
    private Vector2 blockDir = Vector2.right;

    public bool IsGuarding => isGuarding;

    protected override void Update()
    {
        base.Update();
        UpdateGuardState();
    }

    private void UpdateGuardState()
    {
        if (isAttacking) { SetGuard(false); return; }

        Vector3? targetPos = null;
        if (currentOpportunityTarget != null) targetPos = currentOpportunityTarget.position;
        else if (currentStrategicTarget != null) targetPos = currentStrategicTarget.Transform.position;

        if (!targetPos.HasValue) { SetGuard(false); return; }

        Vector2 toTarget = (Vector2)(targetPos.Value - transform.position);
        float dist = toTarget.magnitude;

        if (dist <= attackRange)
        {
            SetGuard(false);
            return;
        }

        if (dist >= guardEngageDistance)
        {
            if (toTarget.sqrMagnitude > 0.0001f) blockDir = toTarget.normalized;
            SetGuard(true);
        }
        else
        {
            // Banda entre attackRange y guardEngageDistance: corriendo a contacto, sin guard.
            SetGuard(false);
        }
    }

    private void SetGuard(bool on)
    {
        if (isGuarding == on) return;
        isGuarding = on;
        if (animator != null) animator.SetBool("isGuarding", on);
    }

    protected override void PerformAttack()
    {
        SetGuard(false);
        base.PerformAttack();
    }

    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        // Un solo clip de Attack; el flip lateral via localScale ya cubre left/right.
        animator.SetTrigger("doAttack");
    }

    public bool TryBlock(Vector2 incomingHitDirection)
    {
        if (!isGuarding) return false;
        Vector2 fromAttacker = -incomingHitDirection.normalized;
        float angle = Vector2.Angle(blockDir.normalized, fromAttacker);
        return angle <= blockAngle * 0.5f;
    }
}
}
