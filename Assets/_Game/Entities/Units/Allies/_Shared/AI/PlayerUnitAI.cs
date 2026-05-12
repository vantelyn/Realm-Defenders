using UnityEngine;
using Game.Buildings;
using Game.Units;

namespace Game.AI
{

[RequireComponent(typeof(PlayerUnit))]
public class PlayerUnitAI : BaseUnitAI
{
    public enum AIState { Idle, Chase, Attack, ReturnHome, MoveToBuilding, MoveToResource, Commanded }

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float chaseHysteresis = 0.5f;

    [Header("Buildings")]
    [SerializeField] private BuildingGarrisonHelper garrison = new BuildingGarrisonHelper();

    private AIState state = AIState.Idle;
    private Transform currentTarget;
    private Transform currentResource;
    private Vector3 commandPoint;
    private bool isManualCommand;
    private float currentTargetRadius;
    private float lastAttackTime = -999f;

    public AIState State => state;

    private float EffectiveAttackRange => attackRange * unit.RangeMultiplier;

    protected override void ResetStateOnEnable() => TransitionTo(AIState.Idle);

    protected override void ClearTargetsOnDisable()
    {
        currentTarget = null;
        currentResource = null;
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
            case AIState.MoveToResource: TickMoveToResource(); break;
            case AIState.Commanded: TickCommanded(); break;
        }
    }

    private void TickIdle()
    {

        ResumeAgent();

        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentTarget = enemy;
            currentTargetRadius = GetTargetRadius(enemy);
            hasPatrolDestination = false;
            TransitionTo(AIState.Chase);
            return;
        }

        if (garrison.SeekBuildings && !NoAutoGarrison && !unit.IsSelected)
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

        Transform resource = FindClosestResource();
        if (resource != null)
        {
            currentResource = resource;
            hasPatrolDestination = false;
            TransitionTo(AIState.MoveToResource);
            return;
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

        // Distancia entre bordes (resta los radius del agente y del target)
        float selfRadius = agent != null ? agent.radius : 0f;
        float edgeDist = Vector2.Distance(transform.position, currentTarget.position) - selfRadius - currentTargetRadius;
        if (edgeDist <= EffectiveAttackRange)
        {
            TransitionTo(AIState.Attack);
            return;
        }

        // Setear stoppingDistance para que el agent pare dentro del attackRange, no en el centro del target
        if (agent != null)
        {
            agent.stoppingDistance = selfRadius + currentTargetRadius + EffectiveAttackRange * 0.7f;
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

        float selfRadius = agent != null ? agent.radius : 0f;
        float edgeDist = Vector2.Distance(transform.position, currentTarget.position) - selfRadius - currentTargetRadius;
        if (edgeDist > EffectiveAttackRange + chaseHysteresis)
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
            currentTargetRadius = GetTargetRadius(enemy);
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

    private void TickMoveToResource()
    {
        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            currentResource = null;
            currentTarget = enemy;
            currentTargetRadius = GetTargetRadius(enemy);
            TransitionTo(AIState.Chase);
            return;
        }

        if (!isManualCommand && IsResourceFull(currentResource))
        {
            currentResource = null;
            TransitionTo(AIState.Idle);
            return;
        }

        if (currentResource == null)
        {
            TransitionTo(AIState.Idle);
            return;
        }

        ResumeAgent();

        if (!isManualCommand && DistanceToHome() > maxTravelDistance)
        {
            currentResource = null;
            TransitionTo(AIState.ReturnHome);
            return;
        }

        float dist = Vector2.Distance(transform.position, currentResource.position);
        if (dist <= resourceArriveDistance)
        {
            StopAgent();
            return;
        }

        RepathTo(currentResource.position);
    }

    private void TickCommanded()
    {
        // En el estado Commanded la unidad va al punto sin distraerse con enemigos cercanos.
        // Cuando llega, transit a Idle y la IA libre toma el control.
        ResumeAgent();
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance <= resourceArriveDistance)
        {
            TransitionTo(AIState.Idle);
            return;
        }
        RepathTo(commandPoint);
    }

    // ---- Comandos manuales (RMB) ----

    public override void CommandMoveTo(Vector3 worldPoint)
    {
        currentTarget = null;
        currentResource = null;
        garrison.ClearTarget();
        hasPatrolDestination = false;
        commandPoint = worldPoint;
        SetHome(worldPoint);
        isManualCommand = true;
        TransitionTo(AIState.Commanded);
    }

    public override void CommandAttackTarget(Transform target)
    {
        if (target == null) return;
        currentTarget = target;
        currentTargetRadius = GetTargetRadius(target);
        currentResource = null;
        garrison.ClearTarget();
        hasPatrolDestination = false;
        SetHome(target.position);
        isManualCommand = true;
        TransitionTo(AIState.Chase);
    }

    public override void CommandHarvestTarget(Transform target)
    {
        if (target == null) return;
        int layer = target.gameObject.layer;
        int resourcesLayer = LayerMask.NameToLayer("Resources");
        int sheepLayer = LayerMask.NameToLayer("Sheep");

        // Ovejas: las atacamos como cualquier hostil cuando el jugador lo ordena.
        if (layer == sheepLayer)
        {
            CommandAttackTarget(target);
            return;
        }

        // Otros recursos no recolectables por esta unidad (arboles, etc.): acompaniar al punto.
        if (layer != resourcesLayer)
        {
            CommandMoveTo(target.position);
            return;
        }

        // Drop suelto (Wood/Meat/MoneyBag): recogerlo.
        currentResource = target;
        currentTarget = null;
        garrison.ClearTarget();
        hasPatrolDestination = false;
        SetHome(target.position);
        isManualCommand = true;
        TransitionTo(AIState.MoveToResource);
    }


        private void TickMoveToBuilding()
    {
        Transform enemy = FindClosestEnemy();
        if (enemy != null)
        {
            garrison.ClearTarget();
            currentTarget = enemy;
            currentTargetRadius = GetTargetRadius(enemy);
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
        // (debug log removed)
        if (state == AIState.Idle && next != AIState.Idle) hasPatrolDestination = false;
        if (next != AIState.Chase && next != AIState.Attack && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.stoppingDistance = defaultStoppingDistance;
        }
        if (next == AIState.Idle) isManualCommand = false;
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
