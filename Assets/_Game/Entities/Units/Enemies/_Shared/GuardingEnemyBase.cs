using UnityEngine;
using Game.Combat;

namespace Game.AI
{

/// <summary>
/// Base para enemigos melee con bloqueo proactivo. Activa el Guard durante el
/// approach al target (cuando dist >= guardEngageDistance) y lo desactiva al
/// entrar a attackRange o al atacar. Bloquea dano desde el frente (blockAngle/2
/// a cada lado de blockDir, que se actualiza a la direccion del target).
///
/// Las subclases solo necesitan implementar ApplyAttackAnimatorParams.
/// El AnimatorController debe tener un bool parameter "isGuarding".
/// </summary>
public abstract class GuardingEnemyBase : BaseEnemyAI, IDamageBlocker
{
    [Header("Guard")]
    [Tooltip("Distancia minima al objetivo para mantener el Guard activado durante el approach.")]
    [SerializeField] protected float guardEngageDistance = 4f;
    [Tooltip("Angulo total cubierto por el Guard centrado en blockDir (180 = frente completo).")]
    [SerializeField] protected float blockAngle = 180f;

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

        if (dist <= attackRange) { SetGuard(false); return; }

        if (dist >= guardEngageDistance)
        {
            if (toTarget.sqrMagnitude > 0.0001f) blockDir = toTarget.normalized;
            SetGuard(true);
        }
        else
        {
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

    public bool TryBlock(Vector2 incomingHitDirection)
    {
        if (!isGuarding) return false;
        Vector2 fromAttacker = -incomingHitDirection.normalized;
        float angle = Vector2.Angle(blockDir.normalized, fromAttacker);
        return angle <= blockAngle * 0.5f;
    }
}
}
