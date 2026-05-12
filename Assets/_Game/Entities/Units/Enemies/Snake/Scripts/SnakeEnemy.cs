using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Snake enemigo melee ultrarapido. Un solo clip de Attack;
/// flip lateral via localScale para left/right.
/// </summary>
public class SnakeEnemy : BaseEnemyAI
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }
}
}
