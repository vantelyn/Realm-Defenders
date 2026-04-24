using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSwitcher : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "Level2";
    [SerializeField] private KeyCode switchKey = KeyCode.N;

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    // Para conectarlo también a un botón de UI si quieres.
    public void GoToNextLevel()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}