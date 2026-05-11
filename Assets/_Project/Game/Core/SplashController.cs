using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{

public class SplashController : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "MenuPrincipal";
    [SerializeField] private float autoAdvanceDelay = 3f;
    [SerializeField] private KeyCode skipKey = KeyCode.Escape;

    private float timer;

    private void Update()
    {
        if (Input.GetKeyDown(skipKey))
        {
            LoadNext();
            return;
        }

        timer += Time.deltaTime;
        if (timer >= autoAdvanceDelay) LoadNext();
    }

    private void LoadNext()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
}
