using System.Collections;
using UnityEngine;
using Game.Combat;

/// <summary>
/// Sacudida procedural lateral del transform cuando el DamageReceiver del mismo
/// GameObject dispara OnDamaged. Se usa en arboles que NO tienen frames de hit
/// pintados (Tree1-4), donde la respuesta visual al golpe se hace por codigo en
/// lugar de por un clip de Animator (como sí hace Tree0Hit.anim del Tree0).
///
/// Combinado con HitFlashEffect en el mismo GO da:
///   tint pulse + lateral wobble = feedback claro del impacto sin frames extra.
/// </summary>
[RequireComponent(typeof(DamageReceiver))]
public class TreeHitWobble : MonoBehaviour
{
    [Header("Wobble")]
    [Tooltip("Amplitud maxima del desplazamiento horizontal en unidades de mundo.")]
    [SerializeField] private float amplitude = 0.08f;
    [Tooltip("Duracion total del wobble en segundos.")]
    [SerializeField] private float duration = 0.18f;
    [Tooltip("Numero de oscilaciones completas durante 'duration'. Mas alto = mas vibracion.")]
    [SerializeField] private float frequency = 18f;

    [Header("SFX")]
    [SerializeField] private AudioClip hitClip;
    [Range(0f,1f)] [SerializeField] private float hitVolume = 1f;
    [SerializeField] private AudioSource sfxSource;

    private DamageReceiver receiver;
    private Vector3 baseLocalPos;
    private Coroutine wobbleCo;

    private void Awake()
    {
        receiver = GetComponent<DamageReceiver>();
        baseLocalPos = transform.localPosition;
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 1f;
            sfxSource.rolloffMode = AudioRolloffMode.Linear;
            sfxSource.minDistance = 3f;
            sfxSource.maxDistance = 18f;
        }
    }

    private void OnEnable()
    {
        if (receiver != null) receiver.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (receiver != null) receiver.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(int amount, Vector2 hitDirection)
    {
        if (hitClip != null) AudioSource.PlayClipAtPoint(hitClip, transform.position, hitVolume);
        if (wobbleCo != null) StopCoroutine(wobbleCo);
        // Recapturamos la base por si algo movio el transform entre golpes.
        baseLocalPos = transform.localPosition;
        // Direccion inicial del wobble: lado opuesto al golpe (el arbol "huye" del hacha).
        float sign = hitDirection.x >= 0f ? 1f : -1f;
        wobbleCo = StartCoroutine(WobbleRoutine(sign));
    }

    private IEnumerator WobbleRoutine(float sign)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;
            // Decay lineal de amplitud + oscilacion sinusoidal
            float offset = sign * amplitude * (1f - normalized) * Mathf.Sin(normalized * frequency);
            transform.localPosition = baseLocalPos + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        transform.localPosition = baseLocalPos;
        wobbleCo = null;
    }
}
