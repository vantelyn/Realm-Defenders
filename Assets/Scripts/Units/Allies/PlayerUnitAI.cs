using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerUnit))]
public class PlayerUnitAI : MonoBehaviour, IUnitAI
{
    public enum AIState { Idle, Chase, Attack, ReturnHome, MoveToBuilding }

    [Header("References")]
    [SerializeField] private EnemyDetector detector;

    [Header("Home Zone")]
    [SerializeField] private float homeRadius = 1.5f;
    [SerializeField] private float maxChaseDistance = 10f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float chaseHysteresis = 0.5f;

    [Header("Patrol")]
    [SerializeField] private float patrolStepRadius = 1.0f;
    [SerializeField] private float patrolPauseMin = 2.5f;
    [SerializeField] private float patrolPauseMax = 5.0f;
    [SerializeField] private float patrolArriveDistance = 0.15f;

    [Header("Repath")]
    [SerializeField] private float repathInterval = 0.25f;

    [Header("Buildings")]
    [SerializeField] private bool seekBuildings = false;
    [SerializeField] private float buildingSearchRadius = 12f;
    [SerializeField] private LayerMask buildingSearchLayerMask;
    [SerializeField] private string buildingTag = "Building";

    private PlayerUnit unit;
    private NavMeshAgent agent;
    private Rigidbody2D rb;

    private AIState state = AIState.Idle;
    private Vector3 homePosition;
    private Transform currentTarget;
    private Building targetBuilding;
    private float lastAttackTime = -999f;
    private float lastRepathTime = -999f;

    private float nextPatrolTime = -1f;
    private bool hasPatrolDestination;

    public AIState State => state;

    private float EffectiveAttackRange => attackRange * unit.RangeMultiplier;

    private void Awake()
    {
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

    private void Start()
    {
        Enable();
    }

    public void SetHome(Vector3 position)
    {
        homePosition = position;
    }

    public void Enable()
    {
        unit.SetMode(PlayerUnit.ControlMode.AI);

        if (!unit.IsGarrisoned)
        {
            if (agent != null)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                }
                agent.enabled = true;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
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

        if (rb != null && !unit.IsGarrisoned)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        currentTarget = null;
        targetBuilding = null;
        unit.SetMode(PlayerUnit.ControlMode.Player);
    }

    private void Update()
    {
        if (unit.Mode != PlayerUnit.ControlMode.AI) return;
        if (detector != null) detector.CleanNulls();

        if (unit.IsGarrisoned)
        {
            TickGarrisoned();
            return;
        }

        switch (state)
        {
            case AIState.Idle: TickIdle(); break;
            case AIState.Chase: TickChase(); break;
            case AIState.Attack: TickAttack(); break;
            case AIState.ReturnHome: TickReturnHome(); break;
            case AIState.MoveToBuilding: TickMoveToBuilding(); break;
        }

        if (agent != null && agent.enabled)
        {
            Vector2 vel = agent.velocity;
            unit.SetMovementInput(vel.sqrMagnitude > 0.01f ? vel.normalized : Vector2.zero);
        }
    }

    private void TickIdle()
    {
        if (agent != null && agent.enabled && agent.isStopped)
        {
            agent.isStopped = false;
        }

        Transform enemy = FindEnemy();
        if (enemy != null)
        {
            currentTarget = enemy;
            hasPatrolDestination = false;
            TransitionTo(AIState.Chase);
            return;
        }

        if (seekBuildings)
        {
            Building freeBuilding = FindFreeBuilding();
            if (freeBuilding != null)
            {
                targetBuilding = freeBuilding;
                hasPatrolDestination = false;
                TransitionTo(AIState.MoveToBuilding);
                return;
            }
        }

        if (DistanceToHome() > homeRadius)
        {
            hasPatrolDestination = false;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        if (hasPatrolDestination)
        {
            if (agent != null && agent.enabled && !agent.pathPending &&
                agent.remainingDistance <= patrolArriveDistance)
            {
                hasPatrolDestination = false;
                ScheduleNextPatrol();
                if (agent.hasPath) agent.ResetPath();
            }
        }
        else
        {
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
    }

    private void TickChase()
    {
        if (currentTarget == null)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        if (agent != null && agent.enabled && agent.isStopped)
        {
            agent.isStopped = false;
        }

        if (DistanceToHome() > maxChaseDistance)
        {
            currentTarget = null;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        float distToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (distToTarget <= EffectiveAttackRange)
        {
            TransitionTo(AIState.Attack);
            return;
        }

        if (Time.time - lastRepathTime > repathInterval && agent != null && agent.enabled)
        {
            agent.SetDestination(currentTarget.position);
            lastRepathTime = Time.time;
        }
    }

    private void TickAttack()
    {
        if (currentTarget == null)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        if (agent != null && agent.enabled)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
        }

        float distToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (distToTarget > EffectiveAttackRange + chaseHysteresis)
        {
            if (agent != null && agent.enabled) agent.isStopped = false;
            TransitionTo(AIState.Chase);
            return;
        }

        if (unit.IsAttacking) return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            Vector2 toTarget = (Vector2)(currentTarget.position - transform.position);
            unit.LastMovementDir = toTarget.normalized;
            unit.PrimaryAttack(toTarget);
            lastAttackTime = Time.time;
        }
    }

    private void TickReturnHome()
    {
        if (agent != null && agent.enabled && agent.isStopped)
        {
            agent.isStopped = false;
        }

        Transform enemy = FindEnemy();
        if (enemy != null)
        {
            currentTarget = enemy;
            TransitionTo(AIState.Chase);
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

    private void TickMoveToBuilding()
    {
        Transform enemy = FindEnemy();
        if (enemy != null)
        {
            targetBuilding = null;
            currentTarget = enemy;
            TransitionTo(AIState.Chase);
            return;
        }

        if (targetBuilding == null || !targetBuilding.HasFreeSlot)
        {
            targetBuilding = null;
            TransitionTo(AIState.Idle);
            return;
        }

        Vector3 door = targetBuilding.DoorPosition;
        float dist = Vector2.Distance(transform.position, door);
        if (dist <= 0.3f)
        {
            targetBuilding.TryEnter(unit);
            targetBuilding = null;
            return;
        }

        if (Time.time - lastRepathTime > repathInterval && agent != null && agent.enabled)
        {
            agent.SetDestination(door);
            lastRepathTime = Time.time;
        }
    }

    private void TickGarrisoned()
    {
        if (agent != null && agent.enabled) agent.enabled = false;

        Transform enemy = FindEnemy();
        if (enemy == null)
        {
            currentTarget = null;
            return;
        }

        float dist = Vector2.Distance(transform.position, enemy.position);
        if (dist > EffectiveAttackRange)
        {
            currentTarget = null;
            return;
        }

        currentTarget = enemy;

        if (unit.IsAttacking) return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            Vector2 toTarget = (Vector2)(enemy.position - transform.position);
            unit.LastMovementDir = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector2.right;
            unit.PrimaryAttack(toTarget);
            lastAttackTime = Time.time;
        }
    }

    private void ScheduleNextPatrol()
    {
        float wait = Random.Range(patrolPauseMin, patrolPauseMax);
        nextPatrolTime = Time.time + wait;
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

    private Building FindFreeBuilding()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, buildingSearchRadius, buildingSearchLayerMask);
        Building best = null;
        float bestDist = float.MaxValue;
        foreach (Collider2D c in hits)
        {
            if (c == null) continue;
            if (!c.CompareTag(buildingTag)) continue;
            Building b = c.GetComponentInParent<Building>();
            if (b == null || !b.HasFreeSlot) continue;
            float d = Vector2.Distance(transform.position, b.DoorPosition);
            if (d < bestDist) { bestDist = d; best = b; }
        }
        return best;
    }

    private void TransitionTo(AIState next)
    {
        if (state == AIState.Idle && next != AIState.Idle)
        {
            hasPatrolDestination = false;
        }
        state = next;
        lastRepathTime = -999f;
    }

    private Transform FindEnemy()
    {
        if (detector == null) return null;
        return detector.FindClosest(transform.position);
    }

    private float DistanceToHome()
    {
        return Vector2.Distance(transform.position, homePosition);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, homeRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, maxChaseDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}