using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MonkUnit))]
public class MonkUnitAI : MonoBehaviour, IUnitAI
{
    public enum AIState { Idle, MoveToAlly, Heal, Flee, ReturnHome, MoveToBuilding }

    [Header("References")]
    [SerializeField] private EnemyDetector enemyDetector;
    [SerializeField] private AllyDetector allyDetector;

    [Header("Home Zone")]
    [SerializeField] private float homeRadius = 1.5f;
    [SerializeField] private float maxTravelDistance = 10f;

    [Header("Healing")]
    [SerializeField] private float healRange = 3f;
    [SerializeField] private float healCooldown = 2f;

    [Header("Flee")]
    [SerializeField] private float enemyDangerDistance = 2.5f;
    [SerializeField] private float fleeDistance = 3.5f;

    [Header("Patrol")]
    [SerializeField] private float patrolStepRadius = 1.0f;
    [SerializeField] private float patrolPauseMin = 2.5f;
    [SerializeField] private float patrolPauseMax = 5.0f;
    [SerializeField] private float patrolArriveDistance = 0.15f;

    [Header("Repath")]
    [SerializeField] private float repathInterval = 0.25f;

    [Header("Buildings")]
    [SerializeField] private float buildingSearchRadius = 12f;
    [SerializeField] private LayerMask buildingSearchLayerMask;
    [SerializeField] private string buildingTag = "Building";

    private MonkUnit monk;
    private PlayerUnit unit;
    private NavMeshAgent agent;
    private Rigidbody2D rb;
    private DamageReceiverPlayer selfHealth;

    private AIState state = AIState.Idle;
    private Vector3 homePosition;
    private PlayerUnit currentHealTarget;
    private Building targetBuilding;
    private float lastHealTime = -999f;
    private float lastRepathTime = -999f;

    private float nextPatrolTime = -1f;
    private bool hasPatrolDestination;

    public AIState State => state;

    private void Awake()
    {
        monk = GetComponent<MonkUnit>();
        unit = GetComponent<PlayerUnit>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody2D>();
        selfHealth = GetComponent<DamageReceiverPlayer>();

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

    public void SetHome(Vector3 position) { homePosition = position; }

    public void Enable()
    {
        unit.SetMode(PlayerUnit.ControlMode.AI);

        if (!monk.IsGarrisoned)
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

        if (rb != null && !monk.IsGarrisoned)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }

        currentHealTarget = null;
        targetBuilding = null;
        unit.SetMode(PlayerUnit.ControlMode.Player);
    }

    private void Update()
    {
        if (unit.Mode != PlayerUnit.ControlMode.AI) return;

        if (enemyDetector != null) enemyDetector.CleanNulls();
        if (allyDetector != null) allyDetector.CleanNulls();

        if (monk.IsGarrisoned)
        {
            TickGarrisoned();
            return;
        }

        switch (state)
        {
            case AIState.Idle: TickIdle(); break;
            case AIState.MoveToAlly: TickMoveToAlly(); break;
            case AIState.Heal: TickHeal(); break;
            case AIState.Flee: TickFlee(); break;
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
        if (agent != null && agent.enabled && agent.isStopped) agent.isStopped = false;

        if (ShouldFlee())
        {
            TransitionTo(AIState.Flee);
            return;
        }

        PlayerUnit patient = FindHealTarget();
        if (patient != null)
        {
            currentHealTarget = patient;
            TransitionTo(AIState.MoveToAlly);
            return;
        }

        Building freeBuilding = FindFreeBuilding();
        if (freeBuilding != null)
        {
            targetBuilding = freeBuilding;
            TransitionTo(AIState.MoveToBuilding);
            return;
        }

        if (DistanceToHome() > homeRadius)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        TickPatrol();
    }

    private void TickMoveToAlly()
    {
        if (ShouldFlee())
        {
            TransitionTo(AIState.Flee);
            return;
        }

        if (currentHealTarget == null || IsFullHealth(currentHealTarget))
        {
            currentHealTarget = null;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        if (DistanceToHome() > maxTravelDistance)
        {
            currentHealTarget = null;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        float dist = Vector2.Distance(transform.position, currentHealTarget.transform.position);
        if (dist <= monk.HealRange * 0.9f)
        {
            TransitionTo(AIState.Heal);
            return;
        }

        if (Time.time - lastRepathTime > repathInterval && agent != null && agent.enabled)
        {
            agent.SetDestination(currentHealTarget.transform.position);
            lastRepathTime = Time.time;
        }
    }

    private void TickHeal()
    {
        if (currentHealTarget == null || IsFullHealth(currentHealTarget))
        {
            currentHealTarget = null;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        if (agent != null && agent.enabled)
        {
            if (agent.hasPath) agent.ResetPath();
            agent.isStopped = true;
        }

        float dist = Vector2.Distance(transform.position, currentHealTarget.transform.position);
        if (dist > monk.HealRange)
        {
            if (agent != null && agent.enabled) agent.isStopped = false;
            TransitionTo(AIState.MoveToAlly);
            return;
        }

        if (unit.IsAttacking) return;

        if (Time.time - lastHealTime >= healCooldown)
        {
            Vector2 toTarget = (Vector2)(currentHealTarget.transform.position - transform.position);
            unit.LastMovementDir = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector2.right;
            monk.TryHeal(currentHealTarget);
            lastHealTime = Time.time;
        }
    }

    private void TickFlee()
    {
        if (agent != null && agent.enabled && agent.isStopped) agent.isStopped = false;

        Transform enemy = FindClosestEnemy();
        if (enemy == null)
        {
            TransitionTo(AIState.Idle);
            return;
        }

        Vector2 awayDir = (Vector2)(transform.position - enemy.position);
        if (awayDir.sqrMagnitude < 0.001f) awayDir = Random.insideUnitCircle.normalized;
        awayDir = awayDir.normalized;

        float distanceNow = Vector2.Distance(transform.position, enemy.position);
        if (distanceNow >= fleeDistance)
        {
            TransitionTo(AIState.Idle);
            return;
        }

        if (Time.time - lastRepathTime > repathInterval && agent != null && agent.enabled)
        {
            Vector3 target = transform.position + (Vector3)(awayDir * (fleeDistance - distanceNow + 1f));
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            lastRepathTime = Time.time;
        }
    }

    private void TickReturnHome()
    {
        if (agent != null && agent.enabled && agent.isStopped) agent.isStopped = false;

        if (ShouldFlee())
        {
            TransitionTo(AIState.Flee);
            return;
        }

        PlayerUnit patient = FindHealTarget();
        if (patient != null)
        {
            currentHealTarget = patient;
            TransitionTo(AIState.MoveToAlly);
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
        if (ShouldFlee())
        {
            targetBuilding = null;
            TransitionTo(AIState.Flee);
            return;
        }

        PlayerUnit patient = FindHealTarget();
        if (patient != null)
        {
            targetBuilding = null;
            currentHealTarget = patient;
            TransitionTo(AIState.MoveToAlly);
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
            targetBuilding.TryEnter(monk);
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

        if (currentHealTarget == null || IsFullHealth(currentHealTarget))
        {
            currentHealTarget = FindHealTarget();
        }

        if (currentHealTarget == null) return;

        float dist = Vector2.Distance(transform.position, currentHealTarget.transform.position);
        if (dist > monk.HealRange) return;

        if (unit.IsAttacking) return;

        if (Time.time - lastHealTime >= healCooldown)
        {
            Vector2 toTarget = (Vector2)(currentHealTarget.transform.position - transform.position);
            unit.LastMovementDir = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : Vector2.right;
            monk.TryHeal(currentHealTarget);
            lastHealTime = Time.time;
        }
    }

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

    private bool ShouldFlee()
    {
        Transform enemy = FindClosestEnemy();
        if (enemy == null) return false;
        return Vector2.Distance(transform.position, enemy.position) < enemyDangerDistance;
    }

    private Transform FindClosestEnemy()
    {
        if (enemyDetector == null) return null;
        return enemyDetector.FindClosest(transform.position);
    }

    private PlayerUnit FindHealTarget()
    {
        PlayerUnit best = null;
        float bestRatio = 1f;

        if (allyDetector != null)
        {
            foreach (PlayerUnit ally in allyDetector.Detected)
            {
                if (ally == null || ally == monk) continue;
                DamageReceiverPlayer hp = ally.GetComponent<DamageReceiverPlayer>();
                if (hp == null || hp.IsAtFullHealth) continue;

                if (hp.HealthRatio < bestRatio)
                {
                    bestRatio = hp.HealthRatio;
                    best = ally;
                }
            }
        }

        if (best != null) return best;

        if (selfHealth != null && !selfHealth.IsAtFullHealth) return monk;
        return null;
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

    private bool IsFullHealth(PlayerUnit p)
    {
        DamageReceiverPlayer hp = p.GetComponent<DamageReceiverPlayer>();
        return hp == null || hp.IsAtFullHealth;
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
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, healRange);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, enemyDangerDistance);
    }
}