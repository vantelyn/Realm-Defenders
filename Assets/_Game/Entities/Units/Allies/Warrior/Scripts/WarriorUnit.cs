using UnityEngine;
using Game.Combat;

namespace Game.Units
{

public class WarriorUnit : PlayerUnit, IDamageBlocker
{
    private const int BlockIndex = 2;

    [Header("Block")]
    [SerializeField] private float blockAngle = 180f;

    private bool isBlocking;
    private Vector2 blockDir = Vector2.right;

    public bool IsBlocking => isBlocking;
    public override bool IsBusy => base.IsBusy || isBlocking;
    public override bool HasSecondary => true;

    public override void PrimaryAttack(Vector2 worldAimDirection)
    {
        if (isAttacking) return;
        if (isBlocking) EndBlock(); // si estaba bloqueando y ataca, cortar bloqueo
        attackDir = LastMovementDir.normalized;

        int dirIndex = GetDirectionIndex(attackDir);
        int attackIndex = Random.Range(0, 2);

        animator.SetInteger("attackDirection", dirIndex);
        animator.SetInteger("attackIndex", attackIndex);
        animator.SetTrigger("doAttack");
    }

    public override void SecondaryAction(Vector2 worldAimDirection, PlayerUnit hoveredUnit)
    {
        BeginBlock();
    }

    public override void EndSecondaryAction()
    {
        EndBlock();
    }

    private void BeginBlock()
    {
        if (isAttacking) return;
        if (isBlocking) return;

        blockDir = LastMovementDir.sqrMagnitude > 0.01f
            ? LastMovementDir.normalized
            : (transform.localScale.x > 0 ? Vector2.right : Vector2.left);

        int dirIndex = GetDirectionIndex(blockDir);
        animator.SetInteger("attackDirection", dirIndex);
        animator.SetInteger("attackIndex", BlockIndex);
        animator.SetBool("isBlocking", true);
        animator.SetTrigger("doAttack");

        isBlocking = true;
        canMove = false;
        movementInput = Vector2.zero;
        if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
    }

    private void EndBlock()
    {
        if (!isBlocking) return;
        isBlocking = false;
        animator.SetBool("isBlocking", false);
        canMove = true;
    }

    protected override void OnBecameUnselected()
    {
        // Si pierde la seleccion mientras bloquea, terminar bloqueo limpiamente
        if (isBlocking) EndBlock();
        base.OnBecameUnselected();
    }

    public float GetDamageMultiplier(Vector2 incomingHitDirection)
    {
        if (!isBlocking) return 1f;
        Vector2 fromAttacker = -incomingHitDirection.normalized;
        float angle = Vector2.Angle(blockDir.normalized, fromAttacker);
        // Bloqueo frontal total. Lateral/trasero: sin reduccion.
        return angle <= blockAngle * 0.5f ? 0f : 1f;
    }

    // Animation Events legacy del clip de bloqueo. Mantener vacios como defensa
    // por si Unity llama un evento residual desde algun re-import.
    public void StartBlock() { }
    public void EndBlockEvent() { }
}
}
