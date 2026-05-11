using UnityEngine;
using Game.AI;
using Game.Combat;

namespace Game.Enemies
{

/// <summary>
/// Enemigo Troll. Mismo patron de ataque que Torch (attackDirection + doAttack),
/// pero intercepta la muerte de su DamageReceiver para reproducir la animacion
/// doDie antes de destruirse.
/// </summary>
public class TrollEnemy : BaseEnemyAI
{
    [Header("Death")]
    [SerializeField] private float dieAnimationDuration = 0.85f;

    private DamageReceiver damageReceiver;
    private bool isDying;

    protected override void Awake()
    {
        base.Awake();
        damageReceiver = GetComponent<DamageReceiver>();
        if (damageReceiver != null)
        {
            damageReceiver.autoDestroyOnDeath = false;
            damageReceiver.OnDying += HandleDying;
        }
    }

    private void OnDestroy()
    {
        if (damageReceiver != null) damageReceiver.OnDying -= HandleDying;
    }

    protected override void Update()
    {
        if (isDying) return;
        base.Update();
    }

    protected override void FixedUpdate()
    {
        if (isDying) return;
        base.FixedUpdate();
    }

    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetInteger("attackDirection", directionIndex);
        animator.SetTrigger("doAttack");
    }

    private void HandleDying()
    {
        if (isDying) return;
        isDying = true;

        // Cancelar cualquier Invoke pendiente (FinishAttack, etc.)
        CancelInvoke();

        // Parar movimiento y desactivar agent para evitar mas pathfinding
        canMove = false;
        if (agent != null)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                if (agent.hasPath) agent.ResetPath();
                agent.isStopped = true;
            }
            agent.enabled = false;
        }

        // Disparar animacion de muerte
        if (animator != null)
        {
            animator.SetBool("isRunning", false);
            animator.SetTrigger("doDie");
        }

        // Destruir tras la animacion
        Invoke(nameof(FinalizeDeath), dieAnimationDuration);
    }

    private void FinalizeDeath()
    {
        Destroy(gameObject);
    }
}
}
