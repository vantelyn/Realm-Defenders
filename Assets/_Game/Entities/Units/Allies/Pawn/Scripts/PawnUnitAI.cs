using UnityEngine;
using Game.Combat;
using Game.Targeting;
using Game.Units;

namespace Game.AI
{

[RequireComponent(typeof(PawnUnit))]
public class PawnUnitAI : BaseUnitAI
{
    public enum AIState { Idle, ChaseEnemy, AttackEnemy, MoveToTree, Chop, MoveToResource, ReturnHome, Flee, Commanded }

    [Header("Pawn References")]
    [SerializeField] private TargetDetector treeDetector;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.0f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float chaseHysteresis = 0.5f;

    [Header("Bear Flee")]
    [Tooltip("Al recibir dano, busca osos en este radio para decidir si huir.")]
    [SerializeField] private float bearDamageScanRadius = 3f;
    [SerializeField] private LayerMask bearLayer;
    [Tooltip("Mientras este flag este activo, el Pawn huye y no engancha enemigos/arboles/recursos.")]
    [SerializeField] private float fleeSafeDistance = 4.5f;
    [SerializeField] private float fleeMaxDuration = 5f;

    [Header("Chopping")]
    [SerializeField] private float chopRange = 1.0f;
    [SerializeField] private float chopCooldown = 1.2f;

    [Header("Resource Pickup")]
    [SerializeField] private float resourceOpportunityRadius = 2.0f;

    private PawnUnit pawn;

    private AIState state = AIState.Idle;
    private Transform currentEnemy;
    private Transform currentTree;
    private Transform currentResource;
    private Vector3 commandPoint;
    private bool isManualCommand;
    private float lastAttackTime = -999f;

    private DamageReceiverPlayer damageReceiver;
    private Transform fleeFrom;
    private float fleeExpireTime = -1f;

    public AIState State => state;

    protected override void Awake()
    {
        base.Awake();
        pawn = GetComponent<PawnUnit>();
        damageReceiver = GetComponent<DamageReceiverPlayer>();
        if (damageReceiver != null) damageReceiver.OnDamaged += HandleDamaged;
    }

    private void OnDestroy()
    {
        if (damageReceiver != null) damageReceiver.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(int amount, Vector2 hitDirection)
    {
        if (unit.Mode != PlayerUnit.ControlMode.AI) return;
        // Escanear osos en proximidad; si hay, huimos del mas cercano
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, bearDamageScanRadius, bearLayer);
        Transform nearestBear = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            Transform root = hits[i].transform;
            while (root.parent != null) root = root.parent;
            float d = ((Vector2)(root.position - transform.position)).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; nearestBear = root; }
        }
        if (nearestBear != null)
        {
            fleeFrom = nearestBear;
            fleeExpireTime = Time.time + fleeMaxDuration;
            currentEnemy = null;
            currentTree = null;
            currentResource = null;
            TransitionTo(AIState.Flee);
        }
    }

    protected override void CleanDetectors()
    {
        if (treeDetector != null) treeDetector.CleanNulls();
    }

    protected override void ResetStateOnEnable() => TransitionTo(AIState.Idle);

    protected override void ClearTargetsOnDisable()
    {
        currentEnemy = null;
        currentTree = null;
        currentResource = null;
        fleeFrom = null;
        fleeExpireTime = -1f;
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
            case AIState.Flee: TickFlee(); break;
            case AIState.Commanded: TickCommanded(); break;
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

        // Si el recurso del target se ha llenado a mitad de tarea, abortar.
        // Excepcion: comando manual del jugador ignora el cap (despejar terreno).
        if (!isManualCommand && IsHarvestableFull(currentTree))
        {
            currentTree = null;
            TransitionTo(AIState.Idle);
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

        // Limite de travel solo aplica en IA libre.
        if (!isManualCommand && DistanceToHome() > maxTravelDistance)
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

        // Si el recurso del target se ha llenado mientras cortabamos, abortar.
        // Excepcion: comando manual del jugador ignora el cap.
        if (!isManualCommand && IsHarvestableFull(currentTree))
        {
            currentTree = null;
            TransitionTo(AIState.Idle);
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

        if (!isManualCommand && IsResourceFull(currentResource))
        {
            currentResource = null;
            if (currentTree != null) TransitionTo(AIState.MoveToTree);
            else TransitionTo(AIState.Idle);
            return;
        }

        if (currentResource == null)
        {
            if (currentTree != null) TransitionTo(AIState.MoveToTree);
            else TransitionTo(AIState.Idle);
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

    private void TickFlee()
    {
        ResumeAgent();

        if (fleeFrom == null || Time.time >= fleeExpireTime)
        {
            fleeFrom = null;
            TransitionTo(AIState.Idle);
            return;
        }

        float distNow = Vector2.Distance(transform.position, fleeFrom.position);
        if (distNow >= fleeSafeDistance)
        {
            fleeFrom = null;
            TransitionTo(AIState.Idle);
            return;
        }

        Vector2 awayDir = (Vector2)(transform.position - fleeFrom.position);
        if (awayDir.sqrMagnitude < 0.001f) awayDir = Random.insideUnitCircle.normalized;
        awayDir = awayDir.normalized;
        Vector3 target = transform.position + (Vector3)(awayDir * (fleeSafeDistance - distNow + 1f));
        if (UnityEngine.AI.NavMesh.SamplePosition(target, out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
        {
            RepathTo(hit.position);
        }
    }

    private void TickCommanded()
    {
        ResumeAgent();
        RepathTo(commandPoint);
        if (agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath && !agent.pathPending && agent.remainingDistance <= resourceArriveDistance)
        {
            TransitionTo(AIState.Idle);
        }
    }

    // ---- Comandos manuales (RMB) ----

    public override void CommandMoveTo(Vector3 worldPoint)
    {
        currentEnemy = null;
        currentTree = null;
        currentResource = null;
        fleeFrom = null;
        fleeExpireTime = -1f;
        hasPatrolDestination = false;
        commandPoint = worldPoint;
        SetHome(worldPoint);
        isManualCommand = true;
        TransitionTo(AIState.Commanded);
    }

    public override void CommandAttackTarget(Transform target)
    {
        if (target == null) return;
        currentEnemy = target;
        currentTree = null;
        currentResource = null;
        fleeFrom = null;
        fleeExpireTime = -1f;
        hasPatrolDestination = false;
        SetHome(target.position);
        isManualCommand = true;
        TransitionTo(AIState.ChaseEnemy);
    }

    public override void CommandHarvestTarget(Transform target)
    {
        if (target == null) return;
        int layer = target.gameObject.layer;
        int treeLayer = LayerMask.NameToLayer("Tree");
        int sheepLayer = LayerMask.NameToLayer("Sheep");
        int resourcesLayer = LayerMask.NameToLayer("Resources");

        currentEnemy = null;
        fleeFrom = null;
        fleeExpireTime = -1f;
        hasPatrolDestination = false;

        if (layer == treeLayer || layer == sheepLayer)
        {
            currentTree = target;
            currentResource = null;
            SetHome(target.position);
            isManualCommand = true;
            TransitionTo(AIState.MoveToTree);
            return;
        }

        if (layer == resourcesLayer)
        {
            currentResource = target;
            currentTree = null;
            SetHome(target.position);
            isManualCommand = true;
            TransitionTo(AIState.MoveToResource);
            return;
        }
    }

    private void garrison_resetIfAny()
    {
        // El Pawn no garrisonea de manera autonoma; no hace falta resetear nada.
    }


    private Transform FindClosestTree()
    {
        // El TreeDetector del Pawn detecta layer Tree y layer Sheep:
        // arboles dropean Wood, ovejas dropean Meat al morir.
        // Stumps comparten layer Tree pero tienen tag Stump; los ignoramos
        // (solo se destruyen con click izq manual del jugador).
        if (treeDetector == null) return null;

        var inv = Game.Managers.InventoryManager.Instance;
        bool woodFull = inv != null && inv.IsWoodFull;
        bool meatFull = inv != null && inv.IsMeatFull;
        if (woodFull && meatFull) return null;

        int treeLayer  = LayerMask.NameToLayer("Tree");
        int sheepLayer = LayerMask.NameToLayer("Sheep");

        Transform best = null;
        float bestSqr = float.MaxValue;
        foreach (Transform tr in treeDetector.Detected)
        {
            if (tr == null) continue;
            if (tr.CompareTag("Stump")) continue;

            int layer = tr.gameObject.layer;
            if (layer == treeLayer  && woodFull) continue;
            if (layer == sheepLayer && meatFull) continue;

            float sqr = ((Vector2)(tr.position - transform.position)).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = tr; }
        }
        return best;
    }

    /// <summary>True si el target del TreeDetector (arbol u oveja) tiene su recurso al maximo
    /// y por tanto no tiene sentido seguir cortando/cazandolo. Por layer (Tree -> Wood, Sheep -> Meat).</summary>
    private bool IsHarvestableFull(Transform t)
    {
        if (t == null) return false;
        var inv = Game.Managers.InventoryManager.Instance;
        if (inv == null) return false;
        int layer = t.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Tree"))  return inv.IsWoodFull;
        if (layer == LayerMask.NameToLayer("Sheep")) return inv.IsMeatFull;
        return false;
    }


    private Transform FindResourceWithinRadius(float radius)
    {
        // Reusa FindClosestResource de BaseUnitAI: ya filtra tipos al maximo
        // y prioriza por escasez. Aqui solo aplicamos el corte por radio.
        Transform candidate = FindClosestResource();
        if (candidate == null) return null;
        float dist = Vector2.Distance(transform.position, candidate.position);
        return dist <= radius ? candidate : null;
    }

    private void TransitionTo(AIState next)
    {
        if (state == AIState.Idle && next != AIState.Idle) hasPatrolDestination = false;
        if (next != AIState.ChaseEnemy && next != AIState.AttackEnemy && next != AIState.Chop && agent != null && agent.enabled && agent.isOnNavMesh)
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(0f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, chopRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, resourceOpportunityRadius);
    }
}
}