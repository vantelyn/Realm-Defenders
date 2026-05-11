using UnityEngine;
using TMPro;
using Game.Buildings;

namespace Game.UI
{

/// <summary>
/// Pantalla de victoria. Se muestra cuando el Castle alcanza el nivel maximo
/// (Castle.OnRoundWon). Pausa el juego (timeScale=0) y muestra el tiempo de
/// la ronda. Las estadisticas (unidades eliminadas, perdidas, etc.) son
/// placeholders, se rellenan cuando exista el tracking correspondiente.
/// </summary>
public class VictoryScreen : MonoBehaviour
{
    [Tooltip("Panel hijo que contiene el visual de la victoria. Se activa al ganar.")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text timeLabel;
    [SerializeField] private TMP_Text killsLabel;
    [SerializeField] private TMP_Text alliesLostLabel;
    [SerializeField] private TMP_Text buildingsLostLabel;

    private void Awake()
    {
        // El GO root debe estar activo desde el inicio para que este Awake se llame
        // y la suscripcion al evento estatico de Castle persista. El panel hijo es
        // el que se enciende al ganar.
        if (panel != null) panel.SetActive(false);
        Castle.OnRoundWon += Show;
    }

    private void OnDestroy()
    {
        Castle.OnRoundWon -= Show;
    }

    private void Show(float roundDuration)
    {
        if (panel == null) return;

        if (timeLabel != null)
        {
            int mins = Mathf.FloorToInt(roundDuration / 60f);
            int secs = Mathf.FloorToInt(roundDuration % 60f);
            timeLabel.text = mins.ToString("00") + ":" + secs.ToString("00");
        }

        // Placeholders para estadisticas (pendientes de tracking)
        if (killsLabel != null) killsLabel.text = "-";
        if (alliesLostLabel != null) alliesLostLabel.text = "-";
        if (buildingsLostLabel != null) buildingsLostLabel.text = "-";

        panel.SetActive(true);
        transform.SetAsLastSibling();
        Time.timeScale = 0f;
    }
}
}
