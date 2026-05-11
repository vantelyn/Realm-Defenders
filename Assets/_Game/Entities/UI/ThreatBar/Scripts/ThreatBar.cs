using UnityEngine;
using UnityEngine.UI;
using Game.Targeting;

using Game.Managers;
namespace Game.UI
{

/// <summary>
/// Ajusta el fillAmount de una Image seg�n la amenaza total del ThreatManager.
/// Reactivo: se actualiza cuando el registro cambia.
/// </summary>
public class ThreatBar : MonoBehaviour
{
    [Tooltip("Valor de amenaza al que la barra llega al 100%.")]
    [SerializeField] private float maxThreat = 100f;

    [SerializeField] private Image fillImage;

    private void OnEnable()
    {
        ThreatManager.OnRegistryChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        ThreatManager.OnRegistryChanged -= Refresh;
    }

    private void Refresh()
    {
        if (fillImage == null) return;
        float ratio = Mathf.Clamp01(ThreatManager.GetTotalThreat() / maxThreat);
        fillImage.fillAmount = ratio;
    }
}
}
