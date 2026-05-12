using UnityEngine;
using UnityEngine.UI;
using Game.Managers;

namespace Game.UI
{

/// <summary>
/// Pinta el fillAmount de una Image segun la amenaza efectiva del ThreatManager.
/// Cambia el color del relleno al cruzar el umbral bajo: verde por debajo,
/// rojo (color por defecto del sprite/tinte) por encima.
/// </summary>
public class ThreatBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [Tooltip("Color del relleno cuando la amenaza esta por debajo del umbral bajo.")]
    [SerializeField] private Color lowColor = new Color(0.30f, 0.85f, 0.30f, 1f);
    [Tooltip("Color del relleno cuando la amenaza alcanza el umbral bajo o lo supera. Blanco preserva el rojo natural del sprite.")]
    [SerializeField] private Color highColor = Color.white;

    private void Update()
    {
        if (fillImage == null) return;
        float effective = ThreatManager.GetTotalThreat();
        float cap = ThreatManager.CurrentCap;
        fillImage.fillAmount = cap > 0f ? Mathf.Clamp01(effective / cap) : 0f;
        fillImage.color = effective < ThreatManager.LowThreshold ? lowColor : highColor;
    }
}
}
