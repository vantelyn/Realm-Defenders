using UnityEngine;

[RequireComponent(typeof(PawnScript))]
public class PawnUnitAI : BaseUnitAI
{
    public enum AIState { Idle, ChaseEnemy, AttackEnemy, MoveToTree, Chop, MoveToResource, ReturnHome }

    [Header("Pawn References")]
    [SerializeField] private EnemyDetector treeDetector;
    [SerializeField] private EnemyDetector resourceDetector;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float chaseHysteresis = 0.5f;

    [Header("Chopping")]
    [SerializeField] private float chopRange = 1.0f;
    [SerializeField] private float chopCooldown = 1.2f;

    [Header("Resource Pickup")]
    [SerializeField] private float resourceOpportunityRadius = 2.0f;
    [SerializeField] private float resourceArriveDistance = 0.3f;

    private PawnScript pawn;

    private AIState state = AIState.Idle;
    private Transform currentEnemy;
    private Transform currentTree;
    private Transform currentResource;
    private float lastAttackTime = -999f;

    public AIState State => state;

    protected override void Awake()
    {
        base.Awake();
        pawn = GetComponent<PawnScript>();
    }

    protected override void CleanDetectors()
    {
        if (treeDetector != null) treeDetector.CleanNulls();
        if (resourceDetector != null) resourceDetector.CleanNulls();
    }

    protected override void ResetStateOnEnable() => TransitionTo(AIState.Idle);

    protected override void ClearTargetsOnDisable()
    {
        currentEnemy = null;
        currentTree = null;
        currentResource = null;
    }

    protected override void TickStates()
    {
        switch (state)
        {
            case AIState.Idle: TickIdle(); break;
            case AIState.ChaseEnemy: TickChaseEnemy(); break;
            case AIState.AttackEnemy: TickAttackEnemy(); break;
            case AIState.MoveToTree: TickMoveToTree(); break;
            case AIState.Chop: TickChop(); break;
            case AIState.MoveToResource: TickMoveToResource(); break;
            case AIState.ReturnHome: TickReturnHome(); break;
        }
    }

    private void TickIdle()
    {
        ResumeAgent();

        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentEnemy = enemy;
            TransitionTo(AIState.ChaseEnemy);
            return;
        }

        Transform tree = FindClosestTree();
        if (tree != null)
        {
            currentTree = tree;
            TransitionTo(AIState.MoveToTree);
            return;
        }

        Transform resource = FindClosestResource();
        if (resource != null)
        {
            currentResource = resource;
            TransitionTo(AIState.MoveToResource);
            return;
        }

        if (DistanceToHome() > homeRadius)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        TickPatrol();
    }

    private void TickChaseEnemy()
    {
        if (currentEnemy == null)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        ResumeAgent();

        if (DistanceToHome() > maxTravelDistance)
        {
            currentEnemy = null;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        float dist = Vector2.Distance(transform.position, currentEnemy.position);
        if (dist <= attackRange)
        {
            TransitionTo(AIState.AttackEnemy);
            return;
        }

        RepathTo(currentEnemy.position);
    }

    private void TickAttackEnemy()
    {
        if (currentEnemy == null)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        StopAgent();

        float dist = Vector2.Distance(transform.position, currentEnemy.position);
        if (dist > attackRange + chaseHysteresis)
        {
            ResumeAgent();
            TransitionTo(AIState.ChaseEnemy);
            return;
        }

        if (unit.IsAttacking) return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            Vector2 toTarget = (Vector2)(currentEnemy.position - transform.position);
            unit.LastMovementDir = toTarget.normalized;
            unit.PrimaryAttack(toTarget);
            lastAttackTime = Time.time;
        }
    }

    private void TickMoveToTree()
    {
        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentEnemy = enemy;
            currentTree = null;
            TransitionTo(AIState.ChaseEnemy);
            return;
        }

        Transform opportunisticResource = FindResourceWithinRadius(resourceOpportunityRadius);
        if (opportunisticResource != null)
        {
            currentResource = opportunisticResource;
            TransitionTo(AIState.MoveToResource);
            return;
        }

        if (currentTree == null)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        ResumeAgent();

        if (DistanceToHome() > maxTravelDistance)
        {
            currentTree = null;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        float dist = Vector2.Distance(transform.position, currentTree.position);
        if (dist <= chopRange)
        {
            TransitionTo(AIState.Chop);
            return;
        }

        RepathTo(currentTree.position);
    }

    private void TickChop()
    {
        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentEnemy = enemy;
            currentTree = null;
            TransitionTo(AIState.ChaseEnemy);
            return;
        }

        if (currentTree == null)
        {
            TransitionTo(AIState.Idle);
            return;
        }

        StopAgent();

        float dist = Vector2.Distance(transform.position, currentTree.position);
        if (dist > chopRange + chaseHysteresis)
        {
            ResumeAgent();
            TransitionTo(AIState.MoveToTree);
            return;
        }

        if (unit.IsAttacking) return;

        if (Time.time - lastAttackTime >= chopCooldown)
        {
            Vector2 toTarget = (Vector2)(currentTree.position - transform.position);
            unit.LastMovementDir = toTarget.normalized;
            unit.PrimaryAttack(toTarget);
            lastAttackTime = Time.time;
        }
    }

    private void TickMoveToResource()
    {
        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentEnemy = enemy;
            currentResource = null;
            TransitionTo(AIState.ChaseEnemy);
            return;
        }

        if (currentResource == null)
        {
            if (currentTree != null) TransitionTo(AIState.MoveToTree);
            else TransitionTo(AIState.Idle);
            return;
        }

        ResumeAgent();

        if (DistanceToHome() > maxTravelDistance)
        {
            currentResource = null;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        float dist = Vector2.Distance(transform.position, currentResource.position);
        if (dist <= resourceArriveDistance)
        {
            currentResource = null;
            if (currentTree != null) TransitionTo(AIState.MoveToTree);
            else TransitionTo(AIState.Idle);
            return;
        }

        RepathTo(currentResource.position);
    }

    private void TickReturnHome()
    {
        ResumeAgent();

        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentEnemy = enemy;
            TransitionTo(AIState.ChaseEnemy);
            return;
        }

        if (DistanceToHome() <= homeRadius)
        {
            TransitionTo(AIState.Idle);
            return;
        }

        RepathTo(homePosition);
    }

    private Transform FindClosestTree()
    {
        if (treeDetector == null) return null;
        return treeDetector.FindClosest(transform.position);
    }

    private Transform FindClosestResource()
    {
        if (resourceDetector == null) return null;
        return resourceDetector.FindClosest(transform.position);
    }

    private Transform FindResourceWithinRadius(float radius)
    {
        if (resourceDetector == null) return null;
        Transform candidate = resourceDetector.FindClosest(transform.position);
        if (candidate == null) return null;
        float dist = Vector2.Distance(transform.position, candidate.position);
        return dist <= radius ? candidate : null;
    }

    private void TransitionTo(AIState next)
    {
        if (state == AIState.Idle && next != AIState.Idle) hasPatrolDestination = false;
        state = next;
        lastRepathTime = -999f;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(0f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, chopRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, resourceOpportunityRadius);
    }
}