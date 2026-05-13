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

    [Header("SFX")]
    [SerializeField] private AudioClip[] baaClips;
    [Range(0f,1f)] [SerializeField] private float baaVolume = 0.8f;
    [SerializeField] private float baaMinInterval = 8f;
    [SerializeField] private float baaMaxInterval = 20f;
    [SerializeField] private AudioSource sfxSource;

    protected override void Start()
    {
        base.Start();
        EnsureSfxSource();
        StopCurrentRoutine();
        currentMovementRoutine = StartCoroutine(WanderAndEatRoutine());
        StartCoroutine(BaaRoutine());
    }

    private void EnsureSfxSource()
    {
        if (sfxSource != null) return;
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode = AudioRolloffMode.Linear;
        sfxSource.minDistance = 3f;
        sfxSource.maxDistance = 15f;
    }

    private IEnumerator BaaRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, baaMaxInterval));
        for (;;)
        {
            float wait = Random.Range(baaMinInterval, baaMaxInterval);
            yield return new WaitForSeconds(wait);
            if (sfxSource != null && baaClips != null && baaClips.Length > 0)
            {
                var clip = baaClips[Random.Range(0, baaClips.Length)];
                if (clip != null) sfxSource.PlayOneShot(clip, baaVolume);
            }
        }
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
