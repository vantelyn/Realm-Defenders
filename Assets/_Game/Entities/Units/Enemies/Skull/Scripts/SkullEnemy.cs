using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Skull enemigo tanque-undead con bloqueo proactivo. Heredando de
/// GuardingEnemyBase, solo necesita el trigger de Attack en el Animator.
/// </summary>
public class SkullEnemy : GuardingEnemyBase
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
        var g = GetComponent<Game.Audio.AttackGruntSfx>(); if (g != null) g.PlayGrunt();
    }
}
}
