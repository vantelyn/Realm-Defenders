using System.Collections.Generic;
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
        attackDir = lastMovementDir.normalized;

        int dirIndex = GetDirectionIndex(attackDir);
        int attackIndex = Random.Range(0, 2);

        animator.SetInteger("attackDirection", dirIndex);
        animator.SetInteger("attackIndex", attackIndex);
        animator.SetTrigger("doAttack");
    }

    public override void SecondaryAction(Vector2 worldAimDirection)
    {
        if (isAttacking) return;

        blockDir = lastMovementDir.sqrMagnitude > 0.01f
            ? lastMovementDir.normalized
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

    // Animation Event de los clips de ataque
    public void DetectAndDamageTargets()
    {
        Vector2 attackPoint = (Vector2)transform.position + attackDir.normalized * attackRange * 0.5f;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayer);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        List<Collider2D> hitTargets = new List<Collider2D>();
        Physics2D.OverlapCircle(attackPoint, attackRange, filter, hitTargets);

        foreach (Collider2D target in hitTargets)
        {
            if (target == null) continue;
            Vector2 hitDirection = target.transform.position - transform.position;
            int layer = target.gameObject.layer;

            if (layer == LayerMask.NameToLayer("Enemy"))
                target.GetComponent<DamageReceiver>()?.ApplyDamage(1, true, false, hitDirection);
            else if (layer == LayerMask.NameToLayer("Sheep"))
                target.GetComponent<DamageReceiver>()?.ApplyDamage(1, true, false, hitDirection);
            else if (layer == LayerMask.NameToLayer("Tree"))
                target.GetComponent<DamageReceiver>()?.ApplyDamage(1, false, true, hitDirection);
        }
    }
}