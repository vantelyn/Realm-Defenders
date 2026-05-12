using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Lizard enemigo melee reptil. Un solo clip de Attack;
/// flip lateral via localScale para left/right.
/// </summary>
public class LizardEnemy : BaseEnemyAI
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }
}
}
