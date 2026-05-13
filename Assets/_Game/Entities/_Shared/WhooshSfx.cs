using UnityEngine;

namespace Game.Audio
{
public class WhooshSfx : MonoBehaviour
{
    [SerializeField] private AudioClip[] whooshClips;
    [Range(0f,1f)] [SerializeField] private float volume = 0.8f;
    [SerializeField] private AudioSource sfxSource;
    private void EnsureSrc() { if (sfxSource != null) return; sfxSource = gameObject.AddComponent<AudioSource>(); sfxSource.playOnAwake = false; sfxSource.spatialBlend = 1f; sfxSource.rolloffMode = AudioRolloffMode.Linear; sfxSource.minDistance = 4f; sfxSource.maxDistance = 22f; sfxSource.dopplerLevel = 0f; }
    public void PlayWhoosh() { EnsureSrc(); if (whooshClips == null || whooshClips.Length == 0) return; var c = whooshClips[Random.Range(0, whooshClips.Length)]; if (c != null) sfxSource.PlayOneShot(c, volume); }
}
}
