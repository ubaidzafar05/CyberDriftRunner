using UnityEngine;
using UnityEngine.UI;

public sealed class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    private bool _isPaused;
    private float _savedTimeScale;

    private void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Resume);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitToMenu);
        }
    }

    private void OnDestroy()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(TogglePause);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(Resume);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitToMenu);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (_isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        if (_isPaused || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        _isPaused = true;
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void Resume()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void QuitToMenu()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        GameManager.Instance?.ReturnToMenu();
    }

    public void Configure(GameObject panel, Button pause, Button resume, Button quit)
    {
        pausePanel = panel;
        pauseButton = pause;
        resumeButton = resume;
        quitButton = quit;
    }
}
