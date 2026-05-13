using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject optionsPanel;
    public GameObject creditsPanel;
    public TMP_Dropdown qualityDropdown;

    [Header("Difficulty")]
    public GameObject playButton;
    public GameObject optionsButton;
    public GameObject quitButton;
    public GameObject easyButton;
    public GameObject mediumButton;
    public GameObject hardButton;

    public enum Difficulty { Easy, Medium, Hard }
    public static Difficulty SelectedDifficulty { get; private set; } = Difficulty.Medium;

    private void Start() {
        if (qualityDropdown == null) return;
        int savedQuality = PlayerPrefs.GetInt("QualityLevel", -1);
        if (savedQuality == -1) savedQuality = QualitySettings.GetQualityLevel();
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
        qualityDropdown.value = savedQuality;
        qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    public void PlayGame() {
        if (playButton != null) playButton.SetActive(false);
        if (optionsButton != null) optionsButton.SetActive(false);
        if (quitButton != null) quitButton.SetActive(false);
        if (easyButton != null) easyButton.SetActive(true);
        if (mediumButton != null) mediumButton.SetActive(true);
        if (hardButton != null) hardButton.SetActive(true);
    }

    public void StartEasy()   { SelectedDifficulty = Difficulty.Easy;   SceneManager.LoadScene("Level1"); }
    public void StartMedium() { SelectedDifficulty = Difficulty.Medium; SceneManager.LoadScene("Level1"); }
    public void StartHard()   { SelectedDifficulty = Difficulty.Hard;   SceneManager.LoadScene("Level1"); }

    public void CloseDifficulty() {
        if (easyButton != null) easyButton.SetActive(false);
        if (mediumButton != null) mediumButton.SetActive(false);
        if (hardButton != null) hardButton.SetActive(false);
        if (playButton != null) playButton.SetActive(true);
        if (optionsButton != null) optionsButton.SetActive(true);
        if (quitButton != null) quitButton.SetActive(true);
    }

    private void Update() {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (optionsPanel != null && optionsPanel.activeSelf) { CloseOptions(); return; }
        if (creditsPanel != null && creditsPanel.activeSelf) { CloseCredits(); return; }
        if (easyButton != null && easyButton.activeSelf) { CloseDifficulty(); return; }
    }

    public void QuitGame() { Application.Quit(); }
    public void OpenOptions()  { mainMenu.SetActive(false); optionsPanel.SetActive(true); }
    public void CloseOptions() { optionsPanel.SetActive(false); mainMenu.SetActive(true); }
    public void OpenCredits()  { mainMenu.SetActive(false); creditsPanel.SetActive(true); }
    public void CloseCredits() { creditsPanel.SetActive(false); mainMenu.SetActive(true); }
    public void OnQualityChanged(int index) { QualitySettings.SetQualityLevel(index, true); PlayerPrefs.SetInt("QualityLevel", index); }
}
}
