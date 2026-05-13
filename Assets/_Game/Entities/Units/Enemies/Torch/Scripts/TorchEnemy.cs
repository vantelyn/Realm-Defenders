using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Enemigo con antorcha. De momento solo cambia los par�metros del animator.
/// </summary>
public class TorchEnemy : BaseEnemyAI
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetInteger("attackDirection", directionIndex);
        animator.SetTrigger("doAttack");
        var w = GetComponent<Game.Audio.WhooshSfx>(); if (w != null) w.PlayWhoosh();
    }
}
}
