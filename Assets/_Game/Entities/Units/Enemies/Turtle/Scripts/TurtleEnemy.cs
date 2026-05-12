using UnityEngine;
using Game.AI;

namespace Game.Enemies
{

/// <summary>
/// Turtle enemigo tanque ultra-resistente con bloqueo proactivo. Hereda toda
/// la logica de Guard de GuardingEnemyBase. El AnimatorController tiene
/// Guard_In + Guard_Out en lugar de un Guard loop unico (controlados por
/// el bool isGuarding via transiciones secuenciales en el AnimatorController).
/// </summary>
public class TurtleEnemy : GuardingEnemyBase
{
    protected override void ApplyAttackAnimatorParams(int directionIndex)
    {
        animator.SetTrigger("doAttack");
    }
}
}
