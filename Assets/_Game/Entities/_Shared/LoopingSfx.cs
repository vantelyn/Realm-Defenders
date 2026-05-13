using UnityEngine;

namespace Game.Audio
{
public class LoopingSfx : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [Range(0f,1f)] [SerializeField] private float volume = 0.6f;
    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 15f;
    private AudioSource src;
    private void Awake() {
        src = gameObject.AddComponent<AudioSource>();
        src.clip = clip; src.loop = true; src.playOnAwake = false;
        src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance; src.maxDistance = maxDistance;
        src.volume = volume; src.dopplerLevel = 0f;
        if (clip != null) src.Play();
    }
}
}
