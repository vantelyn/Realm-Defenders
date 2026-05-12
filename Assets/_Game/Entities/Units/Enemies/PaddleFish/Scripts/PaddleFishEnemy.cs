using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Paddle Fish enemigo melee acuatico. Un solo clip de Attack;
/// flip lateral via localScale para left/right.
/// </summary>
public class PaddleFishEnemy : BaseEnemyAI
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }
}
}
