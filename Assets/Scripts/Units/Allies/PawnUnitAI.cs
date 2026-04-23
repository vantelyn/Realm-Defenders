using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PawnScript))]
public class PawnUnitAI : MonoBehaviour, IUnitAI
{
    public enum AIState { Idle, ChaseEnemy, AttackEnemy, MoveToTree, Chop, MoveToResource, ReturnHome }

    [Header("References")]
    [SerializeField] private EnemyDetector enemyDetector;
    [SerializeField] private EnemyDetector treeDetector;
    [SerializeField] private EnemyDetector resourceDetector;

    [Header("Home Zone")]
    [SerializeField] private float homeRadius = 1.5f;
    [SerializeField] private float maxTravelDistance = 10f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float chaseHysteresis = 0.5f;

    [Header("Chopping")]
    [SerializeField] private float chopRange = 1.0f;
    [SerializeField] private float chopCooldown = 1.2f;

    [Header("Resource Pickup")]
    [SerializeField] private float resourceOpportunityRadius = 2.0f; // mientras va a árbol, recoge si hay recurso a menos de esto
    [SerializeField] private float resourceArriveDistance = 0.3f;    // cerca suficiente para que el Resource Collector lo coja

    [Header("Patrol")]
    [SerializeField] private float patrolStepRadius = 1.0f;
    [SerializeField] private float patrolPauseMin = 2.5f;
    [SerializeField] private float patrolPauseMax = 5.0f;
    [SerializeField] private float patrolArriveDistance = 0.15f;

    [Header("Repath")]
    [SerializeField] private float repathInterval = 0.25f;

    private PawnScript pawn;
    private PlayerUnit unit;
    private NavMeshAgent agent;
    private Rigidbody2D rb;

    private AIState state = AIState.Idle;
    private Vector3 homePosition;
    private Transform currentEnemy;
    private Transform currentTree;
    private Transform currentResource;
    private float lastAttackTime = -999f;
    private float lastRepathTime = -999f;

    private float nextPatrolTime = -1f;
    private bool hasPatrolDestination;

    public AIState State => state;

    private void Awake()
    {
        pawn = GetComponent<PawnScript>();
        unit = GetComponent<PlayerUnit>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        homePosition = transform.position;
    }

    private void Start() { Enable(); }

    public void SetHome(Vector3 position) { homePosition = position; }

    public void Enable()
    {
        unit.SetMode(PlayerUnit.ControlMode.AI);
        if (agent != null)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                transform.position = hit.position;
            agent.enabled = true;
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        TransitionTo(AIState.Idle);
    }

    public void Disable()
    {
        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.enabled = false;
        }
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }
        currentEnemy = null;
        currentTree = null;
        currentResource = null;
        unit.SetMode(PlayerUnit.ControlMode.Player);
    }

    private void Update()
    {
        if (unit.Mode != PlayerUnit.ControlMode.AI) return;

        if (enemyDetector != null) enemyDetector.CleanNulls();
        if (treeDetector != null) treeDetector.CleanNulls();
        if (resourceDetector != null) resourceDetector.CleanNulls();

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

        if (agent != null && agent.enabled)
        {
            Vector2 vel = agent.velocity;
            unit.SetMovementInput(vel.sqrMagnitude > 0.01f ? vel.normalized : Vector2.zero);
        }
    }

    // ---------- States ----------

    private void TickIdle()
    {
        if (agent != null && agent.enabled && agent.isStopped) agent.isStopped = false;

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

        if (agent != null && agent.enabled && agent.isStopped) agent.isStopped = false;

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

        if (Time.time - lastRepathTime > repathInterval && agent != null && agent.enabled)
        {
            agent.SetDestination(currentEnemy.position);
            lastRepathTime = Time.time;
        }
    }

    private void TickAttackEnemy()
    {
        if (currentEnemy == null)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        if (agent != null && agent.enabled)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
        }

        float dist = Vector2.Distance(transform.position, currentEnemy.position);
        if (dist > attackRange + chaseHysteresis)
        {
            if (agent != null && agent.enabled) agent.isStopped = false;
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
        // Prioridad máxima: enemigos
        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentEnemy = enemy;
            currentTree = null;
            TransitionTo(AIState.ChaseEnemy);
            return;
        }

        // Interrupción oportunista: recurso muy cerca
        Transform opportunisticResource = FindResourceWithinRadius(resourceOpportunityRadius);
        if (opportunisticResource != null)
        {
            currentResource = opportunisticResource;
            // Guardamos el árbol actual, volveremos a él después
            TransitionTo(AIState.MoveToResource);
            return;
        }

        if (currentTree == null)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        if (agent != null && agent.enabled && agent.isStopped) agent.isStopped = false;

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

        if (Time.time - lastRepathTime > repathInterval && agent != null && agent.enabled)
        {
            agent.SetDestination(currentTree.position);
            lastRepathTime = Time.time;
        }
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

        if (agent != null && agent.enabled)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
        }

        float dist = Vector2.Distance(transform.position, currentTree.position);
        if (dist > chopRange + chaseHysteresis)
        {
            if (agent != null && agent.enabled) agent.isStopped = false;
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
        // Prioridad máxima: enemigos
        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentEnemy = enemy;
            currentResource = null;
            TransitionTo(AIState.ChaseEnemy);
            return;
        }

        // Si el recurso desapareció (ya recogido o destruido), volvemos al estado previo
        if (currentResource == null)
        {
            if (currentTree != null) TransitionTo(AIState.MoveToTree);
            else TransitionTo(AIState.Idle);
            return;
        }

        if (agent != null && agent.enabled && agent.isStopped) agent.isStopped = false;

        if (DistanceToHome() > maxTravelDistance)
        {
            currentResource = null;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        float dist = Vector2.Distance(transform.position, currentResource.position);
        if (dist <= resourceArriveDistance)
        {
            // El ResourceCollector en el Pawn habrá recogido el recurso vía OnTriggerEnter2D.
            // Soltamos el target y volvemos a la decisión general.
            currentResource = null;
            if (currentTree != null) TransitionTo(AIState.MoveToTree);
            else TransitionTo(AIState.Idle);
            return;
        }

        if (Time.time - lastRepathTime > repathInterval && agent != null && agent.enabled)
        {
            agent.SetDestination(currentResource.position);
            lastRepathTime = Time.time;
        }
    }

    private void TickReturnHome()
    {
        if (agent != null && agent.enabled && agent.isStopped) agent.isStopped = false;

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

        if (Time.time - lastRepathTime > repathInterval && agent != null && agent.enabled)
        {
            agent.SetDestination(homePosition);
            lastRepathTime = Time.time;
        }
    }

    // ---------- Patrol ----------

    private void TickPatrol()
    {
        if (hasPatrolDestination)
        {
            if (agent != null && agent.enabled && !agent.pathPending &&
                agent.remainingDistance <= patrolArriveDistance)
            {
                hasPatrolDestination = false;
                ScheduleNextPatrol();
                if (agent.hasPath) agent.ResetPath();
            }
            return;
        }

        if (nextPatrolTime < 0f) ScheduleNextPatrol();

        if (Time.time >= nextPatrolTime)
        {
            if (TryPickPatrolPoint(out Vector3 dest))
            {
                if (agent != null && agent.enabled)
                {
                    agent.SetDestination(dest);
                    hasPatrolDestination = true;
                }
            }
            else
            {
                nextPatrolTime = Time.time + 1f;
            }
        }
    }

    private void ScheduleNextPatrol()
    {
        nextPatrolTime = Time.time + Random.Range(patrolPauseMin, patrolPauseMax);
    }

    private bool TryPickPatrolPoint(out Vector3 dest)
    {
        Vector2 offset = Random.insideUnitCircle * patrolStepRadius;
        Vector3 candidate = homePosition + new Vector3(offset.x, offset.y, 0f);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolStepRadius, NavMesh.AllAreas))
        {
            dest = hit.position;
            return true;
        }
        dest = Vector3.zero;
        return false;
    }

    // ---------- Helpers ----------

    private Transform FindClosestEnemy()
    {
        if (enemyDetector == null) return null;
        return enemyDetector.FindClosest(transform.position);
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

    private float DistanceToHome() => Vector2.Distance(transform.position, homePosition);

    private void TransitionTo(AIState next)
    {
        if (state == AIState.Idle && next != AIState.Idle) hasPatrolDestination = false;
        state = next;
        lastRepathTime = -999f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, homeRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(0f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, chopRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, resourceOpportunityRadius);
    }
}