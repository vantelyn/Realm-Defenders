using UnityEngine;

namespace Game.Units
{

public class PawnScript : PlayerUnit
{
    public override bool HasSecondary => true;

    public override void PrimaryAttack(Vector2 worldAimDirection)
    {
        if (isAttacking) return;

        float horizontal = LastMovementDir.x;
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

    public override void SecondaryAction(Vector2 worldAimDirection, PlayerUnit hoveredUnit)
    {
        if (isAttacking) return;
        animator.SetTrigger("doBuild");
    }
}
}
