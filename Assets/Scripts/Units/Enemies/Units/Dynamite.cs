using UnityEngine;
using System.Collections.Generic;

public class Dynamite : MonoBehaviour
{
    [Header("Trayectoria (defaults; se pueden sobreescribir en Launch)")]
    [SerializeField] private float flightDuration = 0.8f;
    [SerializeField] private float arcHeight = 1.5f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Explosión")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float explosionRadius = 1.5f;
    [SerializeField] private int damageToPlayerUnit = 1;
    [SerializeField] private float damageToMother = 10f;
    [SerializeField] private LayerMask damageableLayers;

    [Header("Referencias")]
    [SerializeField] private Transform spriteChild; // hijo con el sprite + chispa

    private Vector3 startPos;
    private Vector3 targetPos;
    private float elapsed;
    private bool launched;

    public void Launch(Vector3 target, float duration = -1f, float height = -1f)
    {
        startPos = transform.position;
        targetPos = target;
        if (duration > 0f) flightDuration = duration;
        if (height > 0f) arcHeight = height;
        elapsed = 0f;
        launched = true;
    }

    void Update()
    {
        if (!launched) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightDuration);

        // Base sobre el plano del suelo
        transform.position = Vector3.Lerp(startPos, targetPos, t);

        // Altura falsa (parábola)
        float h = Mathf.Sin(t * Mathf.PI) * arcHeight;
        if (spriteChild != null)
        {
            spriteChild.localPosition = new Vector3(0f, h, 0f);
            spriteChild.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }

        if (t >= 1f) Explode();
    }

void Explode()
{
    launched = false;

    if (explosionPrefab != null)
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);

    // Filtro que ignora colliders marcados como Trigger
    ContactFilter2D filter = new ContactFilter2D();
    filter.SetLayerMask(damageableLayers);
    filter.useLayerMask = true;
    filter.useTriggers = false; // ← clave
    List<Collider2D> hits = new List<Collider2D>();
    Physics2D.OverlapCircle(transform.position, explosionRadius, filter, hits);

    foreach (var hit in hits)
    {
        if (hit == null) continue;

        Vector2 delta = (Vector2)(hit.transform.position - transform.position);
        Vector2 dir;
        if (delta.sqrMagnitude > 0.0001f)
            dir = delta.normalized;
        else
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        if (hit.CompareTag("Mother"))
            hit.GetComponent<MotherHealth>()?.TakeDamage(damageToMother);
        else if (hit.CompareTag("PlayerUnit"))
            hit.GetComponent<DamageReceiverPlayer>()?.ApplyDamage(damageToPlayerUnit, true, false, dir);
        else
            hit.GetComponent<DamageReceiver>()?.ApplyDamage(damageToPlayerUnit, true, false, dir);
    }

    Destroy(gameObject);
}

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}