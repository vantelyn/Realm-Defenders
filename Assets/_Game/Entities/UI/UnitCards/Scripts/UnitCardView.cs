using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Combat;
using Game.Units;
using Game.Managers;

namespace Game.UI
{

/// <summary>
/// Tarjeta UI de una unidad seleccionada. Muestra retrato + barra de vida + texto HP.
/// Click sobre el card selecciona solo esa unidad.
/// La jerarquia interna (Portrait/HpBar/HpText) la construye el Panel en runtime via Build().
/// </summary>
public class UnitCardView : MonoBehaviour
{
    private PlayerUnit unit;
    private DamageReceiverPlayer health;
    private SelectionManager selectionManager;

    private Image portraitImage;
    private Image hpFillImage;
    private TMP_Text hpText;
    private Button button;

    private static readonly Color HPHigh = new Color(0.30f, 0.85f, 0.30f);
    private static readonly Color HPMid  = new Color(0.95f, 0.85f, 0.20f);
    private static readonly Color HPLow  = new Color(0.85f, 0.25f, 0.25f);

    public void Init(Image portrait, Image hpFill, TMP_Text hp, Button btn)
    {
        portraitImage = portrait;
        hpFillImage = hpFill;
        hpText = hp;
        button = btn;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    public void Bind(PlayerUnit u, SelectionManager sm)
    {
        unit = u;
        selectionManager = sm;
        health = u != null ? u.Health : null;
        if (portraitImage != null)
        {
            portraitImage.sprite = u != null ? u.Portrait : null;
            portraitImage.enabled = portraitImage.sprite != null;
        }
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (unit == null || health == null)
        {
            if (hpFillImage != null) hpFillImage.fillAmount = 0f;
            if (hpText != null) hpText.text = "";
            return;
        }
        float ratio = health.HealthRatio;
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = ratio;
            hpFillImage.color = (ratio > 0.6f) ? HPHigh : (ratio > 0.3f ? HPMid : HPLow);
        }
        if (hpText != null)
        {
            hpText.text = health.CurrentHealth + "/" + health.MaxHealth;
        }
    }

    private void OnClick()
    {
        if (unit == null || selectionManager == null) return;
        selectionManager.SelectOnly(unit);
    }
}

}
