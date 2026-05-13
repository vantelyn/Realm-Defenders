using System.Collections;
using UnityEngine;

namespace Game.Audio
{

public class PeriodicAmbientSfx : MonoBehaviour
{
    [SerializeField] private AudioClip[] clips;
    [Range(0f,1f)] [SerializeField] private float volume = 0.7f;
    [SerializeField] private float minInterval = 6f;
    [SerializeField] private float maxInterval = 15f;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 22f;

    private void Awake()
    {
        if (sfxSource == null) {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false; sfxSource.spatialBlend = 1f;
            sfxSource.rolloffMode = AudioRolloffMode.Linear;
            sfxSource.minDistance = minDistance; sfxSource.maxDistance = maxDistance;
            sfxSource.dopplerLevel = 0f;
        }
    }

    private void Start() { StartCoroutine(Loop()); }

    private IEnumerator Loop()
    {
        yield return new WaitForSeconds(Random.Range(0f, maxInterval));
        while (true) {
            float wait = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);
            if (sfxSource != null && clips != null && clips.Length > 0) {
                var clip = clips[Random.Range(0, clips.Length)];
                if (clip != null) sfxSource.PlayOneShot(clip, volume);
            }
        }
    }
}
}
