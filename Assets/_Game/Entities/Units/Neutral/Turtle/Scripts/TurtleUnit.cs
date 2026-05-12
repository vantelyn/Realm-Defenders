using UnityEngine;
using UnityEngine.AI;
using Game.Combat;

namespace Game.Units
{

/// <summary>
/// Tortuga. Fauna pasiva como Sheep: wander aleatorio, no agresiva. Cuando
/// recibe dano se mete en su caparazon (activa Guard, anim isGuarding=true)
/// reduciendo el dano entrante parcialmente. No contraataca. Si el jugador
/// (o cualquier atacante) insiste, la tortuga acaba muriendo. Tras unos
/// segundos sin recibir mas golpes, sale del caparazon y vuelve a wander.
///
/// Implementa IDamageBlocker: GetDamageMultiplier devuelve shellDamageMult
/// (default 0.4 = recibe 40% del dano) si esta guardando y el golpe viene
/// dentro de blockAngle. Lateral/trasero: dano completo (no llega a meterse).
/// </summary>
public class TurtleUnit : NPC, IDamageBlocker
{
    [Header("Wander")]
    [SerializeField] private float wanderRadius = 4f;
    [SerializeField] private float wanderIdleMin = 2f;
    [SerializeField] private float wanderIdleMax = 5f;

    [Header("Shell Defense")]
    [Tooltip("Tiempo sin recibir dano tras el cual sale del caparazon y vuelve a Wander.")]
    [SerializeField] private float shellDuration = 3f;
    [Tooltip("Angulo total cubierto por el caparazon centrado en blockDir (180 = frente completo).")]
    [SerializeField] private float blockAngle = 180f;
    [Tooltip("Multiplicador de dano recibido cuando esta en caparazon dentro del angulo. 1=sin reduccion, 0=inmune.")]
    [Range(0f, 1f)]
    [SerializeField] private float shellDamageMult = 0.4f;

    private DamageReceiver damageReceiver;
    private float shellUntil = -1f;
    private bool isGuarding;
    private Vector2 blockDir = Vector2.right;
    private float nextWanderPickTime = 0f;

    public bool IsGuarding => isGuarding;

    protected override void Start()
    {
        movementType = MovementType.Static;
        base.Start();
        damageReceiver = GetComponent<DamageReceiver>();
        if (damageReceiver != null) damageReceiver.OnDamaged += HandleDamaged;
    }

    private void OnDestroy()
    {
        if (damageReceiver != null) damageReceiver.OnDamaged -= HandleDamaged;
    }

    protected override void Update()
    {
        base.Update();
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh) return;

        // Salir del caparazon si llevamos tiempo sin recibir mas golpes
        if (isGuarding && Time.time >= shellUntil)
        {
            SetGuard(false);
            nextWanderPickTime = 0f;
        }

        if (!isGuarding) TickWander();
        else TickShelled();
    }

    private void TickWander()
    {
        if (Time.time >= nextWanderPickTime &&
            (!navMeshAgent.hasPath || navMeshAgent.remainingDistance < 0.2f))
        {
            Vector3 dest;
            if (TryPickRandomDestination(out dest))
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(dest);
            }
            nextWanderPickTime = Time.time + Random.Range(wanderIdleMin, wanderIdleMax);
        }
    }

    private void TickShelled()
    {
        // Dentro del caparazon: no se mueve. Queda quieta en su sitio.
        navMeshAgent.isStopped = true;
        if (navMeshAgent.hasPath) navMeshAgent.ResetPath();
    }

    private void HandleDamaged(int amount, Vector2 hitDirection)
    {
        // Refresca el timer y se mete (o se queda) en el caparazon
        shellUntil = Time.time + shellDuration;
        Vector2 fromAttacker = -hitDirection;
        if (fromAttacker.sqrMagnitude > 0.0001f) blockDir = fromAttacker.normalized;
        SetGuard(true);

        // Orientar el sprite hacia el atacante para que la pose del caparazon sea coherente
        if (Mathf.Abs(blockDir.x) > 0.01f)
        {
            transform.localScale = new Vector3(blockDir.x > 0 ? 1f : -1f, 1f, 1f);
        }
    }

    private void SetGuard(bool on)
    {
        if (isGuarding == on) return;
        isGuarding = on;
        if (animator != null) animator.SetBool("isGuarding", on);
    }

    public float GetDamageMultiplier(Vector2 incomingHitDirection)
    {
        if (!isGuarding) return 1f;
        Vector2 fromAttacker = -incomingHitDirection.normalized;
        float angle = Vector2.Angle(blockDir.normalized, fromAttacker);
        return angle <= blockAngle * 0.5f ? shellDamageMult : 1f;
    }

    private bool TryPickRandomDestination(out Vector3 dest)
    {
        Vector3 candidate = Random.insideUnitSphere * wanderRadius + transform.position;
        candidate.z = transform.position.z;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidate, out hit, wanderRadius, NavMesh.AllAreas))
        {
            dest = hit.position;
            return true;
        }
        dest = transform.position;
        return false;
    }
}
}
