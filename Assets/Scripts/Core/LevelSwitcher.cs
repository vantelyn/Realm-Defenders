using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{

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

    // Para conectarlo tambi�n a un bot�n de UI si quieres.
    public void GoToNextLevel()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
}
