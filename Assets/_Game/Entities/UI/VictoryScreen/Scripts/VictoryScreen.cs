using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Game.Buildings;
using Game.Managers;

namespace Game.UI
{

/// <summary>
/// Pantalla de victoria. Activa el panel cuando el Castle alcanza max level,
/// pausa el juego, rellena stats reales desde GameStats, consume Escape mientras
/// visible para que no se pueda cancelar via PauseManager, y ofrece un boton
/// para pasar al siguiente nivel.
/// </summary>
public class VictoryScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text timeLabel;
    [SerializeField] private TMP_Text killsLabel;
    [SerializeField] private TMP_Text alliesLostLabel;
    [SerializeField] private TMP_Text buildingsLostLabel;
    [SerializeField] private TMP_Text woodCollectedLabel;
    [SerializeField] private TMP_Text meatCollectedLabel;
    [SerializeField] private TMP_Text moneyCollectedLabel;

    [Header("Next Level")]
    [Tooltip("Boton que carga la siguiente escena.")]
    [SerializeField] private Button nextLevelButton;
    [Tooltip("Si esta vacio se carga la siguiente escena en build settings (current build index + 1).")]
    [SerializeField] private string nextLevelSceneName = "";

    private bool visible;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        Castle.OnRoundWon += Show;
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevelClicked);
    }

    private void OnDestroy()
    {
        Castle.OnRoundWon -= Show;
        if (nextLevelButton != null) nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
    }

    private void Update()
    {
        // No cancelable: consumir Escape antes de que PauseManager lo agarre.
        if (visible && Input.GetKeyDown(KeyCode.Escape)) InputArbiter.EscapeConsumed = true;
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
        if (killsLabel != null) killsLabel.text = GameStats.EnemiesKilled.ToString();
        if (alliesLostLabel != null) alliesLostLabel.text = GameStats.AlliesLost.ToString();
        if (buildingsLostLabel != null) buildingsLostLabel.text = GameStats.BuildingsLost.ToString();
        if (woodCollectedLabel != null) woodCollectedLabel.text = GameStats.WoodCollected.ToString();
        if (meatCollectedLabel != null) meatCollectedLabel.text = GameStats.MeatCollected.ToString();
        if (moneyCollectedLabel != null) moneyCollectedLabel.text = GameStats.MoneyCollected.ToString();

        panel.SetActive(true);
        transform.SetAsLastSibling();
        Time.timeScale = 0f;
        visible = true;
    }

    private void OnNextLevelClicked()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextLevelSceneName))
        {
            SceneManager.LoadScene(nextLevelSceneName);
            return;
        }
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings) SceneManager.LoadScene(next);
        else Debug.LogWarning("[VictoryScreen] No hay siguiente nivel en build settings.");
    }
}
}
