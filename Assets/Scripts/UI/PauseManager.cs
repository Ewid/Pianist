using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;

    private bool isPaused = false;

    void Start()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }

    void OnDestroy()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(TogglePause);
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
        }

        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    void PauseGame()
    {
        Time.timeScale = 0f;
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        if (pauseButton != null)
        {
            pauseButton.gameObject.SetActive(true);
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }
} 