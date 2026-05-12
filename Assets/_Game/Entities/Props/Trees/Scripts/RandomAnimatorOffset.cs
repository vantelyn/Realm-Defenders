using UnityEngine;

public class RandomAnimationVariation : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        float randomOffset = Random.Range(0f, 1f);
        float randomSpeed = Random.Range(0.25f, 0.8f);

        animator.Play(0, 0, randomOffset);
        animator.speed = randomSpeed;
    }
}