using UnityEngine;
using Game.Buildings;
using Game.Units;

namespace Game.AI
{

[RequireComponent(typeof(PlayerUnit))]
public class PlayerUnitAI : BaseUnitAI
{
    public enum AIState { Idle, Chase, Attack, ReturnHome, MoveToBuilding }

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float chaseHysteresis = 0.5f;

    [Header("Buildings")]
    [SerializeField] private BuildingGarrisonHelper garrison = new BuildingGarrisonHelper();

    private AIState state = AIState.Idle;
    private Transform currentTarget;
    private float lastAttackTime = -999f;

    public AIState State => state;

    private float EffectiveAttackRange => attackRange * unit.RangeMultiplier;

    protected override void ResetStateOnEnable() => TransitionTo(AIState.Idle);

    protected override void ClearTargetsOnDisable()
    {
        currentTarget = null;
        garrison.ClearTarget();
    }

    protected override void TickStates()
    {
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
    }

    private void TickIdle()
    {

        Debug.Log($"[{name}] Idle tick, hasPatrol={hasPatrolDestination}");
        ResumeAgent();

        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentTarget = enemy;
            hasPatrolDestination = false;
            TransitionTo(AIState.Chase);
            return;
        }

        if (garrison.SeekBuildings)
        {
            Building freeBuilding = garrison.FindFreeBuilding(transform.position);
            if (freeBuilding != null)
            {
                garrison.SetTarget(freeBuilding);
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

        TickPatrol();
    }

    private void TickChase()
    {
        if (currentTarget == null)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        ResumeAgent();

        if (DistanceToHome() > maxTravelDistance)
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

        RepathTo(currentTarget.position);
    }

    private void TickAttack()
    {
        if (currentTarget == null)
        {
            TransitionTo(AIState.ReturnHome);
            return;
        }

        StopAgent();

        float distToTarget = Vector2.Distance(transform.position, currentTarget.position);
        if (distToTarget > EffectiveAttackRange + chaseHysteresis)
        {
            ResumeAgent();
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
        ResumeAgent();

        Transform enemy = FindClosestEnemy();
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

        RepathTo(homePosition);
    }

    private void TickMoveToBuilding()
    {
        Debug.Log($"[{name}] MoveToBuilding tick, target={(garrison.HasTarget ? garrison.TargetBuilding.name : "NULL")}, dist={(garrison.HasTarget ? Vector2.Distance(transform.position, garrison.GetDoorPosition()).ToString("F2") : "-")}");
        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            garrison.ClearTarget();
            currentTarget = enemy;
            TransitionTo(AIState.Chase);
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
            garrison.TryEnter(unit);
            return;
        }

        if (agent != null)
        {
            Debug.Log($"[{name}] agent.enabled={agent.enabled}, hasPath={agent.hasPath}, pathStatus={agent.pathStatus}, remaining={agent.remainingDistance:F2}, vel={agent.velocity.magnitude:F2}, stopped={agent.isStopped}");
        }

        RepathTo(garrison.GetDoorPosition());
    }

    private void TickGarrisoned()
    {
        if (agent != null && agent.enabled) agent.enabled = false;

        Transform enemy = FindClosestEnemy();
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

    private void TransitionTo(AIState next)
    {
        Debug.Log($"[{name}] {state} -> {next}");
        if (state == AIState.Idle && next != AIState.Idle) hasPatrolDestination = false;
        state = next;
        lastRepathTime = -999f;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(Application.isPlaying ? homePosition : transform.position, maxTravelDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
}
