using UnityEngine;
using UnityEngine.AI;

namespace Game.Audio
{

public class FootstepSfx : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip[] stepClips;
    [Range(0f,1f)] [SerializeField] private float volume = 0.6f;

    [Header("Cadencia")]
    [SerializeField] private float stepEvery = 1.2f;
    [SerializeField] private float minSpeed = 0.05f;
    [Range(0f,0.5f)] [SerializeField] private float jitter = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 22f;

    private NavMeshAgent agent;
    private Vector3 lastPos;
    private float distanceAccumulator;
    private float nextThreshold;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        lastPos = transform.position;
        nextThreshold = RollThreshold();
        EnsureSource();
    }

    private void EnsureSource()
    {
        if (sfxSource != null) return;
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 1f;
        sfxSource.rolloffMode = AudioRolloffMode.Linear;
        sfxSource.minDistance = minDistance;
        sfxSource.maxDistance = maxDistance;
        sfxSource.dopplerLevel = 0f;
    }

    private void Update()
    {
        Vector3 cur = transform.position;
        float delta = (cur - lastPos).magnitude;
        float speed = (agent != null && agent.enabled) ? agent.velocity.magnitude : (delta / Mathf.Max(0.0001f, Time.deltaTime));
        if (speed > minSpeed)
        {
            distanceAccumulator += delta;
            if (distanceAccumulator >= nextThreshold)
            {
                PlayStep();
                distanceAccumulator = 0f;
                nextThreshold = RollThreshold();
            }
        }
        else
        {
            distanceAccumulator = Mathf.Max(0f, distanceAccumulator - Time.deltaTime * 0.5f);
        }
        lastPos = cur;
    }

    private float RollThreshold() { float j = Random.Range(-jitter, jitter); return stepEvery * (1f + j); }

    private void PlayStep()
    {
        if (sfxSource == null || stepClips == null || stepClips.Length == 0) return;
        var clip = stepClips[Random.Range(0, stepClips.Length)];
        if (clip != null) sfxSource.PlayOneShot(clip, volume);
    }
}
}
