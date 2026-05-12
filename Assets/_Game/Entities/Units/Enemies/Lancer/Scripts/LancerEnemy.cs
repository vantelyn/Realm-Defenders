using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Lancer enemigo melee de alcance largo (lanza). Un solo clip de Attack;
/// flip lateral via localScale para left/right.
/// </summary>
public class LancerEnemy : BaseEnemyAI
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }
}
}
