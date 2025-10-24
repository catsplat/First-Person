using UnityEngine;
using UnityEngine.SceneManagement;

public class Navigation : MonoBehaviour
{

    // Go to main menu scene
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("main menu");
    }

    // Reload current scene
    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void LoadTutorial()
    {
        SceneManager.LoadScene("Tutorial"); // Replace with your tutorial scene name
    }

    public void LoadLevelSelector()
    {
        SceneManager.LoadScene("LevelSelector"); // Replace with your level selector scene name
    }

    public void LoadFreePlay()
    {
        SceneManager.LoadScene("Demo"); // Replace with your free play scene name
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}