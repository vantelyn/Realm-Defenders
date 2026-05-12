using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Panda enemigo melee simple. Un solo clip de Attack (sin direcciones
/// separadas); el flip lateral via localScale cubre left/right.
/// </summary>
public class PandaEnemy : BaseEnemyAI
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }
}
}
