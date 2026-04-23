using TMPro;
using UnityEngine;

/// <summary>
/// Muestra la amenaza total del ThreatRegistry en un TextMeshProUGUI.
/// Reactivo: se actualiza automáticamente cuando el registro cambia (registro,
/// muerte, cambio de threatLevel).
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ThreatDisplay : MonoBehaviour
{
    [SerializeField] private string format = "F1";

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
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
        if (label != null)
        {
            label.text = ThreatRegistry.GetTotalThreat().ToString(format);
        }
    }
}