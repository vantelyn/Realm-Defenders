using System.Collections.Generic;
using UnityEngine;

public class PawnScript : PlayerUnit
{
    public override bool HasSecondary => true;

    public override void PrimaryAttack(Vector2 worldAimDirection)
    {
        if (isAttacking) return;

        float horizontal = lastMovementDir.x;
        if (Mathf.Abs(horizontal) < 0.01f)
            horizontal = transform.localScale.x > 0 ? 1f : -1f;

        attackDir = new Vector2(Mathf.Sign(horizontal), 0f);

        if ((attackDir.x > 0 && transform.localScale.x < 0) ||
            (attackDir.x < 0 && transform.localScale.x > 0))
        {
            Vector3 s = transform.localScale;
            s.x *= -1;
            transform.localScale = s;
        }

        animator.SetTrigger("doChop");
    }

    public override void SecondaryAction(Vector2 worldAimDirection)
    {
        if (isAttacking) return;
        animator.SetTrigger("doBuild");
    }

    // Animation Event llamado desde PawnChopping
    public void DetectAndDamageTargets()
    {
        Vector2 attackPoint = (Vector2)transform.position + attackDir.normalized * attackRange * 0.5f;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayer);
        filter.useLayerMask = true;
        filter.useTriggers = false;

        List<Collider2D> hits = new List<Collider2D>();
        Physics2D.OverlapCircle(attackPoint, attackRange, filter, hits);

        foreach (Collider2D target in hits)
        {
            if (target == null) continue;
            int layer = target.gameObject.layer;


            Vector2 hitDirection = target.transform.position - transform.position;

            if (layer == LayerMask.NameToLayer("Enemy"))
                target.GetComponent<DamageReceiver>()?.ApplyDamage(1, true, false, hitDirection);
            else if (layer == LayerMask.NameToLayer("Sheep"))
                target.GetComponent<DamageReceiver>()?.ApplyDamage(1, true, false, hitDirection);
            else if (layer == LayerMask.NameToLayer("Tree"))
                target.GetComponent<DamageReceiver>()?.ApplyDamage(1, false, true, hitDirection);
        }
    }
}