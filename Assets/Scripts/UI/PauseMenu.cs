using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool isGamePaused = false;

    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;
    public GameObject languagesMenuUI;

    [SerializeField]
    private FreeFlyCamera _freeFlyCamera;

    [SerializeField]
    private KeyCode _pause = KeyCode.Escape;

    private void Update()
    {
        if (isGamePaused)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetKeyDown(_pause))
        {
            if (isGamePaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);

        if (languagesMenuUI.activeSelf)
        {
            languagesMenuUI.SetActive(false);
        }

        if (settingsMenuUI.activeSelf)
        {
            settingsMenuUI.SetActive(false);
            settingsMenuUI.GetComponent<SettingsMenu>().UpdateUISettingsValues();
        }

        pauseMenuUI.GetComponent<Animator>().enabled = true;

        Time.timeScale = 1f;

        Cursor.visible = false;
        _freeFlyCamera.enabled = true;

        isGamePaused = false;
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;

        _freeFlyCamera.enabled = false;

        isGamePaused = true;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
