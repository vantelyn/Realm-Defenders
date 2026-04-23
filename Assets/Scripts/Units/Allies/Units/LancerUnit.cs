using System.Collections.Generic;
using UnityEngine;

public class LancerUnit : PlayerUnit
{
    // Descripción de cada animación disponible y a qué dirección ideal corresponde.
    // Las diagonales se autorean hacia la derecha; si la dirección apunta a la izquierda,
    // se reflejan poniendo flipIfLeft = true.
    private struct AttackAnim
    {
        public int index;
        public Vector2 idealDir;
        public bool flipIfLeft;
    }

    private static readonly AttackAnim[] AttackAnims = new AttackAnim[]
    {
        new AttackAnim { index = 0, idealDir = new Vector2( 1f,  0f), flipIfLeft = false }, // derecha
        new AttackAnim { index = 1, idealDir = new Vector2(-1f,  0f), flipIfLeft = false }, // izquierda
        new AttackAnim { index = 2, idealDir = new Vector2( 0f,  1f), flipIfLeft = false }, // arriba
        new AttackAnim { index = 3, idealDir = new Vector2( 0f, -1f), flipIfLeft = false }, // abajo
        new AttackAnim { index = 4, idealDir = new Vector2( 1f,  1f).normalized, flipIfLeft = true }, // arriba-derecha (y arriba-izquierda por flip)
        new AttackAnim { index = 5, idealDir = new Vector2( 1f, -1f).normalized, flipIfLeft = true }, // abajo-derecha (y abajo-izquierda por flip)
    };

    public override bool HasSecondary => false;

    public override void PrimaryAttack(Vector2 worldAimDirection)
    {
        if (isAttacking) return;

        attackDir = lastMovementDir.normalized;

        AttackAnim chosen = PickBestAnim(attackDir);

        if (chosen.flipIfLeft)
        {
            bool shouldFaceLeft = attackDir.x < 0f;
            bool isFacingLeft = transform.localScale.x < 0f;
            if (shouldFaceLeft != isFacingLeft)
            {
                Vector3 s = transform.localScale;
                s.x *= -1;
                transform.localScale = s;
            }
        }

        animator.SetInteger("attackDirection", chosen.index);
        animator.SetTrigger("doAttack");
    }

    private AttackAnim PickBestAnim(Vector2 dir)
    {
        // Para las direcciones con flipIfLeft, comparamos contra la versión "espejada" también,
        // de modo que arriba-izquierda también case contra el slot de arriba-derecha.
        AttackAnim best = AttackAnims[0];
        float bestDot = -2f;

        for (int i = 0; i < AttackAnims.Length; i++)
        {
            AttackAnim a = AttackAnims[i];

            float dot = Vector2.Dot(dir, a.idealDir);
            if (a.flipIfLeft)
            {
                Vector2 mirrored = new Vector2(-a.idealDir.x, a.idealDir.y);
                dot = Mathf.Max(dot, Vector2.Dot(dir, mirrored));
            }

            if (dot > bestDot)
            {
                bestDot = dot;
                best = a;
            }
        }
        return best;
    }

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