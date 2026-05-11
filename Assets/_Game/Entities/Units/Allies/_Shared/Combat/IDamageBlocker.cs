using UnityEngine;

namespace Game.Combat
{

public interface IDamageBlocker
{
    bool TryBlock(Vector2 incomingHitDirection);
}
}
