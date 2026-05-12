using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Thief enemigo melee agil. Un solo clip de Attack;
/// flip lateral via localScale para left/right.
/// </summary>
public class ThiefEnemy : BaseEnemyAI
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }
}
}
