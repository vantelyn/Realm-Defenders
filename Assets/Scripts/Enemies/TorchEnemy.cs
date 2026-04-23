using UnityEngine;

/// <summary>
/// Enemigo con antorcha. De momento solo cambia los parámetros del animator.
/// </summary>
public class TorchEnemy : BaseEnemyAI
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetInteger("attackDirection", directionIndex);
        animator.SetTrigger("doAttack");
    }
}