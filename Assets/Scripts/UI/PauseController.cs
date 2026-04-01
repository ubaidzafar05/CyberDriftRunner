using UnityEngine;
using UnityEngine.UI;

public sealed class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    private bool _isPaused;

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
        if (Input.GetKeyDown(KeyCode.Escape) && GameManager.Instance != null)
        {
            TogglePause();
        }

        SyncPanelState();
    }

    public void TogglePause()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

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
        if (GameManager.Instance == null || !GameManager.Instance.TryPauseRun())
        {
            return;
        }

        _isPaused = true;
        SyncPanelState();
    }

    public void Resume()
    {
        if (GameManager.Instance == null || !GameManager.Instance.ResumeRun())
        {
            return;
        }

        _isPaused = false;
        SyncPanelState();
    }

    public void QuitToMenu()
    {
        _isPaused = false;
        SyncPanelState();
        GameManager.Instance?.ReturnToMenu();
    }

    public void Configure(GameObject panel, Button pause, Button resume, Button quit)
    {
        pausePanel = panel;
        pauseButton = pause;
        resumeButton = resume;
        quitButton = quit;
    }

    private void SyncPanelState()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        _isPaused = GameManager.Instance.State == GameState.Paused;
        if (pausePanel != null && pausePanel.activeSelf != _isPaused)
        {
            pausePanel.SetActive(_isPaused);
        }
    }
}
