using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Spider enemigo melee ligero y rapido. Un solo clip de Attack;
/// flip lateral via localScale para left/right.
/// </summary>
public class SpiderEnemy : BaseEnemyAI
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }
}
}
