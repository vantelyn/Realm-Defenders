using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NPC : MonoBehaviour
{
    protected NavMeshAgent navMeshAgent;
    protected Animator animator;
    protected Transform playerTransform;

    [Header("Movement Type")]
    public MovementType movementType;
    public enum MovementType { Static, Path, RandomMovement }

    [Header("Skin")]
    public NPCSkin selectedSkin;
    public RuntimeAnimatorController[] animatorControllers;
    public enum NPCSkin { Blue, Purple, Red, Yellow }

    [Header("Path Movement")]
    public Transform[] pathPoints;
    public float waitTimeInPoint = 3f;
    private int indexPath = 0;

    [Header("Random Movement")]
    public float movementRadius = 5f;
    public float waitTimeRandom = 4f;

    protected Coroutine currentMovementRoutine;

    protected virtual void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false;
            navMeshAgent.updateUpAxis = false;
        }

        ApplySkin();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        ResumeOriginalBehavior();
    }

    protected virtual void Update()
    {
        AdjustAnimationsAndRotation();
    }

    public void AdjustAnimationsAndRotation()
    {
        if (navMeshAgent == null || animator == null)
            return;

        bool isMoving = navMeshAgent.velocity.sqrMagnitude > 0.01f;
        animator.SetBool("isRunning", isMoving);

        if (navMeshAgent.desiredVelocity.x > 0.01f)
            transform.localScale = new Vector3(1f, 1f, 1f);

        if (navMeshAgent.desiredVelocity.x < -0.01f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    void ApplySkin()
    {
        if (animatorControllers != null && animatorControllers.Length > 0)
        {
            int skinIndex = (int)selectedSkin;

            if (animator != null && skinIndex < animatorControllers.Length)
            {
                animator.runtimeAnimatorController = animatorControllers[skinIndex];
            }
        }
    }

    private void ResumeOriginalBehavior()
    {
        ResumeMovementBehavior(movementType);
    }

    private void ResumeMovementBehavior(MovementType type)
    {
        StopCurrentRoutine();

        switch (type)
        {
            case MovementType.Static:
                if (navMeshAgent != null)
                    navMeshAgent.ResetPath();
                break;

            case MovementType.Path:
                StartPathRoutine();
                break;

            case MovementType.RandomMovement:
                StartRandomMovementRoutine();
                break;

            default:
                break;
        }
    }

    private void StartPathRoutine()
    {
        if (pathPoints == null || pathPoints.Length == 0 || navMeshAgent == null)
            return;

        StopCurrentRoutine();
        currentMovementRoutine = StartCoroutine(FollowPath());
    }

    private void StartRandomMovementRoutine()
    {
        StopCurrentRoutine();
        currentMovementRoutine = StartCoroutine(RandomMovementRoutine());
    }

    void StopCurrentRoutine()
    {
        if (currentMovementRoutine != null)
        {
            StopCoroutine(currentMovementRoutine);
            currentMovementRoutine = null;
        }
    }

    protected IEnumerator FollowPath()
    {
        while (true)
        {
            if (pathPoints != null && pathPoints.Length > 0 && navMeshAgent != null)
            {
                Transform targetPoint = pathPoints[indexPath];

                if (targetPoint != null)
                {
                    navMeshAgent.SetDestination(targetPoint.position);

                    yield return WaitUntilDestinationReached();
                    yield return new WaitForSeconds(waitTimeInPoint);

                    indexPath = (indexPath + 1) % pathPoints.Length;
                }
                else
                {
                    indexPath = (indexPath + 1) % pathPoints.Length;
                    yield return null;
                }
            }
            else
            {
                yield return null;
            }
        }
    }

    private IEnumerator WaitUntilDestinationReached()
    {
        if (navMeshAgent == null)
            yield break;

        while (navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.05f)
        {
            yield return null;
        }
    }

    private IEnumerator RandomMovementRoutine()
    {
        while (true)
        {
            if (navMeshAgent != null)
            {
                Vector3 randomDirection = Random.insideUnitSphere * 5f;
                randomDirection += transform.position;
                randomDirection.z = transform.position.z;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
                {
                    navMeshAgent.SetDestination(hit.position);
                    yield return WaitUntilDestinationReached();
                }
            }

            yield return new WaitForSeconds(waitTimeInPoint);
        }
    }

    protected IEnumerator RandomMovement()
    {
        while (true)
        {
            Vector3 randomPos = GetRandomNavMeshPosition();
            navMeshAgent.SetDestination(randomPos);
            yield return WaitUntilDestinationReached();
            yield return new WaitForSeconds(waitTimeRandom);
        }

    }
    private Vector3 GetRandomNavMeshPosition()
    {
        Vector3 randomDirection = Random.insideUnitSphere * movementRadius + transform.position;
        if(NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, movementRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return transform.position;

    }

    protected virtual void OnDisable()
    {
        StopCurrentRoutine();
    }
}