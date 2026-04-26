using UnityEngine;

namespace Game.Projectiles
{

[RequireComponent(typeof(Animator))]
public class HealEffect : MonoBehaviour
{
    private void Start()
    {
        Animator anim = GetComponent<Animator>();
        float duration = anim.GetCurrentAnimatorStateInfo(0).length;
        Destroy(gameObject, duration);
    }
}
}
