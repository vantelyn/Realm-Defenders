using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Game.Managers;
using Game.Units;

namespace Game.UI
{
public class DefeatScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text subtitleLabel;
    [SerializeField] private TMP_Text timeLabel;
    [SerializeField] private TMP_Text killsLabel;
    [SerializeField] private TMP_Text alliesLostLabel;
    [SerializeField] private TMP_Text buildingsLostLabel;
    [SerializeField] private string defaultMessage = "You have no units left and no way to recover.";
    [SerializeField] private string kingDiedMessage = "The future king is dead.";
    [Header("Return")]
    [SerializeField] private Button returnButton;
    [SerializeField] private string mainMenuSceneName = "MenuPrincipal";
    private bool visible;
    private float startTime;
    private void Awake() {
        if (panel != null) panel.SetActive(false);
        DefeatChecker.OnDefeat += ShowDefault;
        FutureKing.OnKingDied += ShowKingDied;
        if (returnButton != null) returnButton.onClick.AddListener(OnReturnClicked);
        startTime = Time.unscaledTime;
    }
    private void OnDestroy() {
        DefeatChecker.OnDefeat -= ShowDefault;
        FutureKing.OnKingDied -= ShowKingDied;
        if (returnButton != null) returnButton.onClick.RemoveListener(OnReturnClicked);
    }
    private void Update() { if (visible && Input.GetKeyDown(KeyCode.Escape)) InputArbiter.EscapeConsumed = true; }
    private void ShowDefault(float duration) { Show(duration, defaultMessage); }
    private void ShowKingDied() { Show(Time.unscaledTime - startTime, kingDiedMessage); }
    private void Show(float duration, string message) {
        if (panel == null || visible) return;
        if (subtitleLabel != null) subtitleLabel.text = message;
        if (timeLabel != null) {
            int mins = Mathf.FloorToInt(duration / 60f); int secs = Mathf.FloorToInt(duration % 60f);
            timeLabel.text = mins.ToString("00") + ":" + secs.ToString("00");
        }
        if (killsLabel != null) killsLabel.text = GameStats.EnemiesKilled.ToString();
        if (alliesLostLabel != null) alliesLostLabel.text = GameStats.AlliesLost.ToString();
        if (buildingsLostLabel != null) buildingsLostLabel.text = GameStats.BuildingsLost.ToString();
        panel.SetActive(true);
        transform.SetAsLastSibling();
        Time.timeScale = 0f;
        visible = true;
    }
    private void OnReturnClicked() { Time.timeScale = 1f; SceneManager.LoadScene(mainMenuSceneName); }
}
}
