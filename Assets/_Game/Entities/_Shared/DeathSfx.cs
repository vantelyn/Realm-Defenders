using UnityEngine;
using Game.Combat;

namespace Game.Audio
{
public class DeathSfx : MonoBehaviour
{
    [SerializeField] private AudioClip[] deathClips;
    [Range(0f,1f)] [SerializeField] private float volume = 0.9f;
    [SerializeField] private float minDistance = 4f;
    [SerializeField] private float maxDistance = 22f;
    private DamageReceiver receiver;
    private DamageReceiverPlayer receiverPlayer;
    private bool fired;

    private void Awake() {
        receiver = GetComponent<DamageReceiver>();
        receiverPlayer = GetComponent<DamageReceiverPlayer>();
    }
    private void OnEnable() {
        if (receiver != null) receiver.OnDying += HandleDying;
        if (receiverPlayer != null) receiverPlayer.OnDying += HandleDying;
    }
    private void OnDisable() {
        if (receiver != null) receiver.OnDying -= HandleDying;
        if (receiverPlayer != null) receiverPlayer.OnDying -= HandleDying;
    }
    private void HandleDying() {
        if (fired) return; fired = true;
        if (deathClips == null || deathClips.Length == 0) return;
        var clip = deathClips[Random.Range(0, deathClips.Length)];
        if (clip == null) return;
        var go = new GameObject("DeathSfx_OneShot");
        go.transform.position = transform.position;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip; src.volume = volume; src.spatialBlend = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance; src.maxDistance = maxDistance;
        src.dopplerLevel = 0f; src.playOnAwake = false; src.Play();
        Object.Destroy(go, clip.length + 0.1f);
    }
}
}
