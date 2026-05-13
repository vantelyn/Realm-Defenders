using UnityEngine;

namespace Game.Units
{

public class LancerUnit : PlayerUnit
{
    [Header("SFX")]
    [SerializeField] private AudioClip[] swordHitClips;
    [Range(0f,1f)] [SerializeField] private float swordHitVolume = 0.9f;
    [SerializeField] private AudioSource sfxSource;
    private void EnsureSfx() {
        if (sfxSource != null) return;
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false; sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode = AudioRolloffMode.Linear;
        sfxSource.minDistance = 4f; sfxSource.maxDistance = 22f; sfxSource.dopplerLevel = 0f;
    }
    private void PlaySwordHit() {
        EnsureSfx();
        if (swordHitClips == null || swordHitClips.Length == 0) return;
        var clip = swordHitClips[Random.Range(0, swordHitClips.Length)];
        if (clip != null) sfxSource.PlayOneShot(clip, swordHitVolume);
    }
    private struct AttackAnim
    {
        public int index;
        public Vector2 idealDir;
        public bool flipIfLeft;
    }

    private static readonly AttackAnim[] AttackAnims = new AttackAnim[]
    {
        new AttackAnim { index = 0, idealDir = new Vector2( 1f,  0f), flipIfLeft = false },
        new AttackAnim { index = 1, idealDir = new Vector2(-1f,  0f), flipIfLeft = false },
        new AttackAnim { index = 2, idealDir = new Vector2( 0f,  1f), flipIfLeft = false },
        new AttackAnim { index = 3, idealDir = new Vector2( 0f, -1f), flipIfLeft = false },
        new AttackAnim { index = 4, idealDir = new Vector2( 1f,  1f).normalized, flipIfLeft = true },
        new AttackAnim { index = 5, idealDir = new Vector2( 1f, -1f).normalized, flipIfLeft = true },
    };

    public override bool HasSecondary => false;

    public override void PrimaryAttack(Vector2 worldAimDirection)
    {
        if (isAttacking) return;

        attackDir = LastMovementDir.normalized;
        AttackAnim chosen = PickBestAnim(attackDir);

        if (chosen.flipIfLeft)
        {
            bool shouldFaceLeft = attackDir.x < 0f;
            bool isFacingLeft = transform.localScale.x < 0f;
            if (shouldFaceLeft != isFacingLeft)
            {
                Vector3 s = transform.localScale;
                s.x *= -1;
                transform.localScale = s;
            }
        }

        animator.SetInteger("attackDirection", chosen.index);
        animator.SetTrigger("doAttack");
        var grunt = GetComponent<Game.Audio.AttackGruntSfx>(); if (grunt != null) grunt.PlayGrunt();
        PlaySwordHit();
    }

    private AttackAnim PickBestAnim(Vector2 dir)
    {
        AttackAnim best = AttackAnims[0];
        float bestDot = -2f;

        for (int i = 0; i < AttackAnims.Length; i++)
        {
            AttackAnim a = AttackAnims[i];
            float dot = Vector2.Dot(dir, a.idealDir);
            if (a.flipIfLeft)
            {
                Vector2 mirrored = new Vector2(-a.idealDir.x, a.idealDir.y);
                dot = Mathf.Max(dot, Vector2.Dot(dir, mirrored));
            }
            if (dot > bestDot)
            {
                bestDot = dot;
                best = a;
            }
        }
        return best;
    }
}
}
