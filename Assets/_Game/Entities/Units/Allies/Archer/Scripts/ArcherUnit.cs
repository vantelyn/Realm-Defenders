using UnityEngine;
using Game.Projectiles;

namespace Game.Units
{

public class ArcherUnit : PlayerUnit
{
    [Header("SFX")]
    [SerializeField] private AudioClip fireArrowClip;
    [Range(0f,1f)] [SerializeField] private float fireArrowVolume = 0.9f;
    [SerializeField] private AudioSource sfxSource;
    private void EnsureSfx() {
        if (sfxSource != null) return;
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false; sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode = AudioRolloffMode.Linear;
        sfxSource.minDistance = 4f; sfxSource.maxDistance = 22f; sfxSource.dopplerLevel = 0f;
    }
    [Header("Archery")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private float maxRange = 8f;

    private Vector2 pendingAimDir = Vector2.right;
    private float pendingDistance = 5f;

    public override bool HasSecondary => false;
    public float MaxRange => maxRange * RangeMultiplier;

    public override void PrimaryAttack(Vector2 worldAimDirection)
    {
        if (isAttacking) return;

        float rawDistance = worldAimDirection.magnitude;
        pendingAimDir = rawDistance > 0.01f
            ? worldAimDirection / rawDistance
            : (transform.localScale.x > 0 ? Vector2.right : Vector2.left);
        pendingDistance = Mathf.Min(rawDistance, MaxRange);
        pendingDistance = Mathf.Max(0.5f, pendingDistance);

        if ((pendingAimDir.x > 0 && transform.localScale.x < 0) ||
            (pendingAimDir.x < 0 && transform.localScale.x > 0))
        {
            Vector3 s = transform.localScale;
            s.x *= -1;
            transform.localScale = s;
        }

        animator.SetTrigger("doShoot");
    }

    // Animation Event en el clip de disparo
    public void SpawnArrow()
    {
        if (arrowPrefab == null) return;
        EnsureSfx();
        if (sfxSource != null && fireArrowClip != null) sfxSource.PlayOneShot(fireArrowClip, fireArrowVolume);
        Vector3 spawnPos = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position;
        Vector3 targetPos = (Vector3)(pendingAimDir * pendingDistance) + (Vector3)(Vector2)transform.position;
        targetPos.z = spawnPos.z;
        GameObject arrowGO = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        Arrow arrow = arrowGO.GetComponent<Arrow>();
        if (arrow != null) arrow.Launch(targetPos);
    }
}
}
