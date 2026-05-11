using UnityEngine;

namespace Game.Combat
{

[System.Serializable]
public class DamageRule
{
    public string layerName;
    public int damage = 1;
    public bool applyForce = true;
    public bool applyHitAnimation = false;
}
}
