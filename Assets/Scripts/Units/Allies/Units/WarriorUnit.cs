using UnityEngine;

public class WarriorUnit : PlayerUnit, IDamageBlocker
{
    private const int BlockIndex = 2;

    [Header("Block")]
    [SerializeField] private float blockAngle = 180f;

    private bool isBlocking;
    private Vector2 blockDir = Vector2.right;

    public override bool HasSecondary => true;

    public override void PrimaryAttack(Vector2 worldAimDirection)
    {
        if (isAttacking) return;
        attackDir = LastMovementDir.normalized;

        int dirIndex = GetDirectionIndex(attackDir);
        int attackIndex = Random.Range(0, 2);

        animator.SetInteger("attackDirection", dirIndex);
        animator.SetInteger("attackIndex", attackIndex);
        animator.SetTrigger("doAttack");
    }

    public override void SecondaryAction(Vector2 worldAimDirection, PlayerUnit hoveredUnit)
    {
        if (isAttacking) return;

        blockDir = LastMovementDir.sqrMagnitude > 0.01f
            ? LastMovementDir.normalized
            : (transform.localScale.x > 0 ? Vector2.right : Vector2.left);

        int dirIndex = GetDirectionIndex(blockDir);
        animator.SetInteger("attackDirection", dirIndex);
        animator.SetInteger("attackIndex", BlockIndex);
        animator.SetTrigger("doAttack");
    }

    public bool TryBlock(Vector2 incomingHitDirection)
    {
        if (!isBlocking) return false;
        Vector2 fromAttacker = -incomingHitDirection.normalized;
        float angle = Vector2.Angle(blockDir.normalized, fromAttacker);
        return angle <= blockAngle * 0.5f;
    }

    // Animation Events del clip de bloqueo
    public void StartBlock() { isBlocking = true; }
    public void EndBlock() { isBlocking = false; }
}