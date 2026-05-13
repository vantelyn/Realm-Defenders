using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Game.Managers
{

/// <summary>
/// Conecta los 3 botones del PauseMenu (Restart / GoToMenu / Exit) a sus acciones.
/// Restaura Time.timeScale antes de cambiar de escena para evitar quedar pausado
/// si la siguiente escena no instancia PauseManager.
/// </summary>
public class PauseMenuActions : MonoBehaviour
{
    [SerializeField] private Button restartButton;
    [SerializeField] private Button goToMenuButton;
    [SerializeField] private Button exitButton;

    [SerializeField] private string menuSceneName = "MenuPrincipal";

    private void Awake()
    {
        if (restartButton != null) restartButton.onClick.AddListener(OnRestart);
        if (goToMenuButton != null) goToMenuButton.onClick.AddListener(OnGoToMenu);
        if (exitButton != null) exitButton.onClick.AddListener(OnExit);
    }

    private void OnDestroy()
    {
        if (restartButton != null) restartButton.onClick.RemoveListener(OnRestart);
        if (goToMenuButton != null) goToMenuButton.onClick.RemoveListener(OnGoToMenu);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExit);
    }

    private void OnRestart()
    {
        Time.timeScale = 1f;
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }

    private void OnGoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    private void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

}
