using UnityEngine;
using UnityEngine.UI;
using Game.Targeting;

namespace Game.UI
{

/// <summary>
/// Ajusta el fillAmount de una Image seg�n la amenaza total del ThreatRegistry.
/// Reactivo: se actualiza cuando el registro cambia.
/// </summary>
[RequireComponent(typeof(Image))]
public class ThreatBar : MonoBehaviour
{
    [Tooltip("Valor de amenaza al que la barra llega al 100%.")]
    [SerializeField] private float maxThreat = 100f;

    private Image fillImage;

    private void Awake()
    {
        fillImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        ThreatRegistry.OnRegistryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        ThreatRegistry.OnRegistryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (fillImage == null) return;
        float ratio = Mathf.Clamp01(ThreatRegistry.GetTotalThreat() / maxThreat);
        fillImage.fillAmount = ratio;
    }
}
}
