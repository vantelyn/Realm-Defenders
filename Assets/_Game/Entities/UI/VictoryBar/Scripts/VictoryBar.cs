using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Buildings;

namespace Game.UI
{

/// <summary>
/// Barra de progreso que se muestra entre el nivel 2 y 3 del Castle. Llena un
/// Image (fillAmount) segun Castle.EvolutionProgress y opcionalmente muestra un
/// label con el tiempo restante. El GO debe estar activo desde el inicio; la
/// visibilidad se gestiona via CanvasGroup (alpha 0/1) para que el Update siga
/// corriendo y la barra aparezca automaticamente al empezar la evolucion.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class VictoryBar : MonoBehaviour
{
    [Tooltip("Image con fillAmount que representa el progreso (0..1).")]
    [SerializeField] private Image fillImage;
    [Tooltip("Label opcional para mostrar el tiempo restante (formato mm:ss).")]
    [SerializeField] private TMP_Text timeLabel;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void Update()
    {
        Castle castle = Castle.Instance;
        if (castle == null || castle.Data == null)
        {
            SetVisible(false);
            return;
        }

        bool show = castle.IsEvolving;
        SetVisible(show);
        if (!show) return;

        float progress = castle.EvolutionProgress;
        if (fillImage != null) fillImage.fillAmount = progress;

        if (timeLabel != null)
        {
            float remaining = Mathf.Max(0f, (1f - progress) * castle.Data.evolutionDuration);
            int mins = Mathf.FloorToInt(remaining / 60f);
            int secs = Mathf.FloorToInt(remaining % 60f);
            timeLabel.text = mins.ToString("00") + ":" + secs.ToString("00");
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
}
