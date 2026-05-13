using UnityEngine;
using Game.Combat;

namespace Game.Audio
{

[RequireComponent(typeof(DamageReceiver))]
public class HitSfx : MonoBehaviour
{
    [SerializeField] private AudioClip[] hitClips;
    [Range(0f,1f)] [SerializeField] private float volume = 0.9f;
    [SerializeField] private float cooldown = 0.1f;

    private DamageReceiver receiver;
    private float lastTime = -999f;

    private void Awake() { receiver = GetComponent<DamageReceiver>(); }
    private void OnEnable() { if (receiver != null) receiver.OnDamaged += HandleDamaged; }
    private void OnDisable() { if (receiver != null) receiver.OnDamaged -= HandleDamaged; }

    private void HandleDamaged(int amount, Vector2 dir)
    {
        if (Time.time - lastTime < cooldown) return;
        if (hitClips == null || hitClips.Length == 0) return;
        var clip = hitClips[Random.Range(0, hitClips.Length)];
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        lastTime = Time.time;
    }
}
}
