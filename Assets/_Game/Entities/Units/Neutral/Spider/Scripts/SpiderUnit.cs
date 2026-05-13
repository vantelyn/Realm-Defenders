using UnityEngine;

namespace Game.Units
{
public class SpiderUnit : AggressiveWildlife
{
    [Header("SFX")]
    [SerializeField] private AudioClip[] attackClips;
    [Range(0f,1f)] [SerializeField] private float attackVolume = 0.9f;
    [Tooltip("Retraso en segundos entre el inicio de la animacion de ataque y la reproduccion del SFX (frames de wind-up).")]
    [SerializeField] private float attackSfxDelay = 0.35f;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (sfxSource == null) {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false; sfxSource.spatialBlend = 1f;
            sfxSource.rolloffMode = AudioRolloffMode.Linear;
            sfxSource.minDistance = 4f; sfxSource.maxDistance = 18f; sfxSource.dopplerLevel = 0f;
        }
    }

    protected override void OnAttackBegin()
    {
        if (attackClips == null || attackClips.Length == 0 || sfxSource == null) return;
        if (attackSfxDelay <= 0f) PlayRandomAttackClip();
        else Invoke(nameof(PlayRandomAttackClip), attackSfxDelay);
    }

    private void PlayRandomAttackClip()
    {
        if (attackClips == null || attackClips.Length == 0 || sfxSource == null) return;
        var c = attackClips[Random.Range(0, attackClips.Length)];
        if (c != null) sfxSource.PlayOneShot(c, attackVolume);
    }
}
}
