using UnityEngine;

namespace Game.Audio
{
public class AmbientLoop2D : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [Range(0f,1f)] [SerializeField] private float volume = 0.5f;
    private AudioSource src;
    private void Awake() {
        src = gameObject.AddComponent<AudioSource>();
        src.clip = clip; src.loop = true; src.playOnAwake = false;
        src.spatialBlend = 0f; src.volume = volume;
        if (clip != null) src.Play();
    }
}
}
