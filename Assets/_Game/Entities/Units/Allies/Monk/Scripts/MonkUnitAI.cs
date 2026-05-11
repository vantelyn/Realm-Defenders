using UnityEngine;
using Game.Buildings;
using Game.Combat;
using Game.Targeting;
using Game.Units;

namespace Game.AI
{

[RequireComponent(typeof(MonkUnit))]
public class MonkUnitAI : BaseUnitAI
{
    public enum AIState { Idle, MoveToAlly, Heal, Flee, ReturnHome, MoveToBuilding, MoveToResource }

    [Header("Monk References")]
    [SerializeField] private AllyDetector allyDetector;

    [Header("Healing")]
    [SerializeField] private float healCooldown = 2f;

    [Header("Flee")]
    [SerializeField] private float enemyDangerDistance = 2.5f;
    [SerializeField] private float fleeDistance = 3.5f;

    [Header("Buildings")]
    [SerializeField] private BuildingGarrisonHelper garrison = new BuildingGarrisonHelper();

    private MonkUnit monk;

    private AIState state = AIState.Idle;
    private PlayerUnit currentHealTarget;
    private Transform currentResource;
    private float lastHealTime = -999f;

    public AIState State => state;

    protected override void Awake()
    {
        base.Awake();
        monk = GetComponent<MonkUnit>();
    }

    protected override void CleanDetectors()
    {
        if (allyDetector != null) allyDetector.CleanNulls();
    }

    protected override void ResetStateOnEnable() => TransitionTo(AIState.Idle);

    protected override void ClearTargetsOnDisable()
    {
        currentHealTarget = null;
        currentResource = null;
        garrison.ClearTarget();
    }

    protected override void TickStates()
    {
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
            case AIState.MoveToResource: TickMoveToResource(); break;
        }
    }

    private void TickIdle()
    {
        ResumeAgent();

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

        Transform resource = FindClosestResource();
        if (resource != null)
        {
            currentResource = resource;
            TransitionTo(AIState.MoveToResource);
            return;
        }

        if (garrison.SeekBuildings)
        {
            Building freeBuilding = garrison.FindFreeBuilding(transform.position);
            if (freeBuilding != null)
            {
                garrison.SetTarget(freeBuilding);
                TransitionTo(AIState.MoveToBuilding);
                return;
            }
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

        RepathTo(currentHealTarget.transform.position);
    }

    private void TickHeal()
    {
        if (currentHealTarget == null || IsFullHealth(currentHealTarget))
        {
            currentHealTarget = null;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        StopAgent();

        float dist = Vector2.Distance(transform.position, currentHealTarget.transform.position);
        if (dist > monk.HealRange)
        {
            ResumeAgent();
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
        ResumeAgent();

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

        Vector3 target = transform.position + (Vector3)(awayDir * (fleeDistance - distanceNow + 1f));
        if (UnityEngine.AI.NavMesh.SamplePosition(target, out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
        {
            RepathTo(hit.position);
        }
    }

    private void TickReturnHome()
    {
        ResumeAgent();

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

        RepathTo(homePosition);
    }

    private void TickMoveToResource()
    {
        // Flee y healing tienen prioridad
        if (ShouldFlee())
        {
            currentResource = null;
            TransitionTo(AIState.Flee);
            return;
        }

        PlayerUnit patient = FindHealTarget();
        if (patient != null)
        {
            currentResource = null;
            currentHealTarget = patient;
            TransitionTo(AIState.MoveToAlly);
            return;
        }

        if (currentResource == null)
        {
            TransitionTo(AIState.Idle);
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
            TransitionTo(AIState.Idle);
            return;
        }

        RepathTo(currentResource.position);
    }

        private void TickMoveToBuilding()
    {
        if (ShouldFlee())
        {
            garrison.ClearTarget();
            TransitionTo(AIState.Flee);
            return;
        }

        PlayerUnit patient = FindHealTarget();
        if (patient != null)
        {
            garrison.ClearTarget();
            currentHealTarget = patient;
            TransitionTo(AIState.MoveToAlly);
            return;
        }

        if (!garrison.IsTargetValid())
        {
            garrison.ClearTarget();
            TransitionTo(AIState.Idle);
            return;
        }

        if (garrison.IsAtDoor(transform.position))
        {
            garrison.TryEnter(monk);
            return;
        }

        RepathTo(garrison.GetDoorPosition());
    }

    private void TickGarrisoned()
    {
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

    private bool ShouldFlee()
    {
        Transform enemy = FindClosestEnemy();
        if (enemy == null) return false;
        return Vector2.Distance(transform.position, enemy.position) < enemyDangerDistance;
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
                DamageReceiverPlayer hp = ally.Health;
                if (hp == null || hp.IsAtFullHealth) continue;

                if (hp.HealthRatio < bestRatio)
                {
                    bestRatio = hp.HealthRatio;
                    best = ally;
                }
            }
        }

        if (best != null) return best;

        if (monk.Health != null && !monk.Health.IsAtFullHealth) return monk;
        return null;
    }

    private bool IsFullHealth(PlayerUnit p)
    {
        DamageReceiverPlayer hp = p.Health;
        return hp == null || hp.IsAtFullHealth;
    }

    private void TransitionTo(AIState next)
    {
        if (state == AIState.Idle && next != AIState.Idle) hasPatrolDestination = false;
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.stoppingDistance = 0f;
        state = next;
        lastRepathTime = -999f;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.4f);
        if (Application.isPlaying && monk != null) Gizmos.DrawWireSphere(transform.position, monk.HealRange);
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, enemyDangerDistance);
    }
}
}
