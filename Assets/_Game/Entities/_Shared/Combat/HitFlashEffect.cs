using System.Collections;
using UnityEngine;

namespace Game.Combat
{

/// <summary>
/// Efecto visual de "hit flash": al llamar Flash() tinta el SpriteRenderer
/// objetivo del color configurado durante una duracion corta, luego restaura
/// el color original. Si llega un nuevo Flash() mientras hay uno activo, lo
/// reinicia (extiende el efecto).
/// </summary>
public class HitFlashEffect : MonoBehaviour
{
    [SerializeField] private Color flashColor = new Color(1f, 0.4f, 0.4f, 1f);
    [Tooltip("Duracion total del tint (segundos)")]
    [SerializeField] private float duration = 0.18f;
    [Tooltip("SpriteRenderer al que se le aplica el tint. Si no se asigna, se busca en GetComponentInChildren.")]
    [SerializeField] private SpriteRenderer targetRenderer;

    private Color originalColor;
    private Coroutine activeFlash;
    private bool originalCached;

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<SpriteRenderer>();
        CacheOriginal();
    }

    private void CacheOriginal()
    {
        if (targetRenderer == null) return;
        originalColor = targetRenderer.color;
        originalCached = true;
    }

    public void Flash()
    {
        if (targetRenderer == null) return;
        if (!originalCached) CacheOriginal();
        if (activeFlash != null) StopCoroutine(activeFlash);
        activeFlash = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        targetRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        if (targetRenderer != null) targetRenderer.color = originalColor;
        activeFlash = null;
    }

    void OnDisable()
    {
        // Asegurar que no quedamos atrapados con el tint si nos desactivan a mitad
        if (targetRenderer != null && originalCached) targetRenderer.color = originalColor;
        activeFlash = null;
    }
}
}
