using UnityEngine;
using Game.Combat;

namespace Game.Units
{

public class WarriorUnit : PlayerUnit, IDamageBlocker
{
    [Header("SFX")]
    [SerializeField] private AudioClip[] swordHitClips;
    [Range(0f,1f)] [SerializeField] private float swordHitVolume = 0.9f;
    [SerializeField] private AudioSource sfxSource;

    private void EnsureSfx()
    {
        if (sfxSource != null) return;
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false; sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode = AudioRolloffMode.Linear;
        sfxSource.minDistance = 4f; sfxSource.maxDistance = 22f; sfxSource.dopplerLevel = 0f;
    }

    private void PlaySwordHit()
    {
        EnsureSfx();
        if (swordHitClips == null || swordHitClips.Length == 0) return;
        var clip = swordHitClips[Random.Range(0, swordHitClips.Length)];
        if (clip != null) sfxSource.PlayOneShot(clip, swordHitVolume);
    }
    private const int BlockIndex = 2;

    [Header("Block")]
    [SerializeField] private float blockAngle = 180f;

    private bool isBlocking;
    private Vector2 blockDir = Vector2.right;

    public bool IsBlocking => isBlocking;
    public override bool IsBusy => base.IsBusy || isBlocking;
    public override bool HasSecondary => true;

    public override void PrimaryAttack(Vector2 worldAimDirection)
    {
        if (isAttacking) return;
        if (isBlocking) EndBlock(); // si estaba bloqueando y ataca, cortar bloqueo
        attackDir = LastMovementDir.normalized;

        int dirIndex = GetDirectionIndex(attackDir);
        int attackIndex = Random.Range(0, 2);

        animator.SetInteger("attackDirection", dirIndex);
        animator.SetInteger("attackIndex", attackIndex);
        animator.SetTrigger("doAttack");
        PlaySwordHit();
        var grunt = GetComponent<Game.Audio.AttackGruntSfx>(); if (grunt != null) grunt.PlayGrunt();
    }

    public override void SecondaryAction(Vector2 worldAimDirection, PlayerUnit hoveredUnit)
    {
        BeginBlock();
    }

    public override void EndSecondaryAction()
    {
        EndBlock();
    }

    private void BeginBlock()
    {
        if (isAttacking) return;
        if (isBlocking) return;

        blockDir = LastMovementDir.sqrMagnitude > 0.01f
            ? LastMovementDir.normalized
            : (transform.localScale.x > 0 ? Vector2.right : Vector2.left);

        int dirIndex = GetDirectionIndex(blockDir);
        animator.SetInteger("attackDirection", dirIndex);
        animator.SetInteger("attackIndex", BlockIndex);
        animator.SetBool("isBlocking", true);
        animator.SetTrigger("doAttack");
        PlaySwordHit();
        var grunt = GetComponent<Game.Audio.AttackGruntSfx>(); if (grunt != null) grunt.PlayGrunt();

        isBlocking = true;
        canMove = false;
        movementInput = Vector2.zero;
        if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
    }

    private void EndBlock()
    {
        if (!isBlocking) return;
        isBlocking = false;
        animator.SetBool("isBlocking", false);
        canMove = true;
    }

    protected override void OnBecameUnselected()
    {
        // Si pierde la seleccion mientras bloquea, terminar bloqueo limpiamente
        if (isBlocking) EndBlock();
        base.OnBecameUnselected();
    }

    public float GetDamageMultiplier(Vector2 incomingHitDirection)
    {
        if (!isBlocking) return 1f;
        Vector2 fromAttacker = -incomingHitDirection.normalized;
        float angle = Vector2.Angle(blockDir.normalized, fromAttacker);
        // Bloqueo frontal total. Lateral/trasero: sin reduccion.
        return angle <= blockAngle * 0.5f ? 0f : 1f;
    }

    // Animation Events legacy del clip de bloqueo. Mantener vacios como defensa
    // por si Unity llama un evento residual desde algun re-import.
    public void StartBlock() { }
    public void EndBlockEvent() { }
}
}
