using System.Collections;
using UnityEngine;

namespace Game.Units
{

public class SnakeUnit : AggressiveWildlife
{
    [Header("SFX")]
    [SerializeField] private AudioClip rattleClip;
    [Range(0f,1f)] [SerializeField] private float rattleVolume = 0.8f;
    [SerializeField] private float rattleMinInterval = 6f;
    [SerializeField] private float rattleMaxInterval = 15f;
    [SerializeField] private AudioSource sfxSource;

    protected override void Start()
    {
        base.Start();
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false; sfxSource.spatialBlend = 1f;
            sfxSource.rolloffMode = AudioRolloffMode.Linear;
            sfxSource.minDistance = 3f; sfxSource.maxDistance = 15f;
        }
        StartCoroutine(RattleRoutine());
    }

    protected override void OnAttackBegin()
    {
        if (sfxSource != null && rattleClip != null) sfxSource.PlayOneShot(rattleClip, rattleVolume);
    }

    private IEnumerator RattleRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0f, rattleMaxInterval));
        for (;;)
        {
            float wait = Random.Range(rattleMinInterval, rattleMaxInterval);
            yield return new WaitForSeconds(wait);
            if (sfxSource != null && rattleClip != null) sfxSource.PlayOneShot(rattleClip, rattleVolume);
        }
    }
}
}
