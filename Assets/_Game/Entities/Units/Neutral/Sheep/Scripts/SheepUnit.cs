using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Units
{

public class SheepUnit : NPC
{
    [Header("Sheep Behavior")]
    [SerializeField] private float wanderRadius = 5f;
    [SerializeField] private float eatDurationMin = 3f;
    [SerializeField] private float eatDurationMax = 6f;
    [SerializeField] private float arriveDistance = 0.15f;

    protected override void Start()
    {
        base.Start();
        StopCurrentRoutine();
        currentMovementRoutine = StartCoroutine(WanderAndEatRoutine());
    }

    private IEnumerator WanderAndEatRoutine()
    {
        // Peque�a espera para que el agente est� listo tras el Start del padre
        yield return null;

        while (true)
        {
            if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            Vector3 dest;
            if (!TryPickRandomDestination(out dest))
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(dest);

            // Esperar al pathfinding (proteger por si knockback desactiva agent)
            while (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh && navMeshAgent.pathPending) yield return null;

            // Caminar hasta llegar
            while (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh && navMeshAgent.remainingDistance > arriveDistance)
            {
                if (!navMeshAgent.hasPath) break; // path se perdio, salir
                yield return null;
            }

            // Parar suavemente (solo si el agent esta activo)
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
            }

            // Forzar animaci�n a Idle antes del trigger
            if (animator != null)
            {
                animator.SetBool("isRunning", false);
                animator.SetTrigger("eatGrass");
            }

            float eatTime = Random.Range(eatDurationMin, eatDurationMax);
            yield return new WaitForSeconds(eatTime);
        }
    }

    private bool TryPickRandomDestination(out Vector3 dest)
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius + transform.position;
        randomDirection.z = transform.position.z;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            dest = hit.position;
            return true;
        }
        dest = transform.position;
        return false;
    }
}
}
