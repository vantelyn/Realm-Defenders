using UnityEngine;

namespace Game.Combat
{

/// <summary>
/// Componente que puede reducir o anular el dano entrante. Implementado por
/// units con bloqueo (Warrior con su Guard frontal) o por animales con concha
/// (Turtle). Devuelve un multiplicador del dano: 0 = inmunidad total, 1 = sin
/// reduccion, 0.4 = recibe 40% del dano original.
///
/// El receiver chequea blocked = (mult <= 0): si el bloqueo es total no se
/// aplica HP loss, hit flash ni OnDamaged event.
/// </summary>
public interface IDamageBlocker
{
    float GetDamageMultiplier(Vector2 incomingHitDirection);
}
}
