using UnityEngine;

public class GameManager : MonoBehaviour
{
    private bool gameIsPaused = false;

    void Update()
    {
        OpenClosePauseMenu();
    }

    void OpenClosePauseMenu()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) 
        {
            if (gameIsPaused)
            {
                UIManager.Instance.ResumeGame();
                gameIsPaused = false;
            }
            else
            {
                UIManager.Instance.PauseGame();
                gameIsPaused = true;
            }
        
        }
    }
}
