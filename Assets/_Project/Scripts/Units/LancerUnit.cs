using UnityEngine;

namespace Game.Units
{

public class LancerUnit : PlayerUnit
{
    private struct AttackAnim
    {
        public int index;
        public Vector2 idealDir;
        public bool flipIfLeft;
    }

    private static readonly AttackAnim[] AttackAnims = new AttackAnim[]
    {
        new AttackAnim { index = 0, idealDir = new Vector2( 1f,  0f), flipIfLeft = false },
        new AttackAnim { index = 1, idealDir = new Vector2(-1f,  0f), flipIfLeft = false },
        new AttackAnim { index = 2, idealDir = new Vector2( 0f,  1f), flipIfLeft = false },
        new AttackAnim { index = 3, idealDir = new Vector2( 0f, -1f), flipIfLeft = false },
        new AttackAnim { index = 4, idealDir = new Vector2( 1f,  1f).normalized, flipIfLeft = true },
        new AttackAnim { index = 5, idealDir = new Vector2( 1f, -1f).normalized, flipIfLeft = true },
    };

    public override bool HasSecondary => false;

    public override void PrimaryAttack(Vector2 worldAimDirection)
    {
        if (isAttacking) return;

        attackDir = LastMovementDir.normalized;
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
}
}
