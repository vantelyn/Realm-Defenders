using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public float speed = 7;
    private Rigidbody2D rb2D;
    public Transform target;

    NavMeshAgent navMeshAgent;
    Animator animator;

    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;
    }

    void Update()
    {
        navMeshAgent.SetDestination(target.position);
        AdjustAnimationsAndRotation();
    }

    public void AdjustAnimationsAndRotation()
    {
        bool isRunning = navMeshAgent.velocity.sqrMagnitude > 0.01f;
        animator.SetBool("isRunning", isRunning);

        if (navMeshAgent.desiredVelocity.x > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);

        if (navMeshAgent.desiredVelocity.x < 0.01f)
            transform.localScale = new Vector3(-1, 1, 1);
    }
}
