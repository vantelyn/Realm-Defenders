using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Gnome enemigo melee ligero. Un solo clip de Attack; flip lateral
/// via localScale para left/right.
/// </summary>
public class GnomeEnemy : BaseEnemyAI
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }
}
}
