using UnityEngine;

namespace Game.Audio
{

/// <summary>Reproduce un grunt aleatorio. Llamar PlayGrunt() desde el script de la unidad
/// cuando se dispare el ataque (junto al SFX del arma).</summary>
public class AttackGruntSfx : MonoBehaviour
{
    [SerializeField] private AudioClip[] gruntClips;
    [Range(0f,1f)] [SerializeField] private float volume = 0.8f;
    [SerializeField] private AudioSource sfxSource;

    private void EnsureSrc()
    {
        if (sfxSource != null) return;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false; sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode = AudioRolloffMode.Linear;
        sfxSource.minDistance = 4f; sfxSource.maxDistance = 22f; sfxSource.dopplerLevel = 0f;
    }

    public void PlayGrunt()
    {
        EnsureSrc();
        if (gruntClips == null || gruntClips.Length == 0) return;
        var clip = gruntClips[Random.Range(0, gruntClips.Length)];
        if (clip != null) sfxSource.PlayOneShot(clip, volume);
    }
}
}
