using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public sealed class UIFlowController : MonoBehaviour
{
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject revivePanel;
    [SerializeField] private Canvas gameOverCanvas;
    [SerializeField] private UiVisualTheme visualTheme;

    private bool _loggedMissingBindings;

    private void Awake()
    {
        ApplyCanvasTheme();
        ValidateBindings();
        SyncState(force: true);
    }

    private void LateUpdate()
    {
        SyncState(force: false);
    }

    private void ApplyCanvasTheme()
    {
        if (visualTheme == null)
        {
            return;
        }

        ApplyThemeToCanvas(menuCanvas);
        ApplyThemeToCanvas(hudCanvas);
        ApplyThemeToCanvas(gameOverCanvas);
    }

    private void ApplyThemeToCanvas(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        Camera worldCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        if (worldCamera != null)
        {
            worldCamera.backgroundColor = visualTheme.CanvasBackground;
        }

        ApplyTextTheme(canvas);
        ApplyImageTheme(canvas);
        ApplyButtonTheme(canvas);
    }

    private void ApplyTextTheme(Canvas canvas)
    {
        Text[] texts = canvas.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            Text text = texts[i];
            if (text == null)
            {
                continue;
            }

            text.color = ResolveTextColor(text.name);
        }
    }

    private void ApplyImageTheme(Canvas canvas)
    {
        Image[] images = canvas.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.GetComponent<Button>() != null)
            {
                continue;
            }

            image.color = ResolvePanelColor(image.name);
        }
    }

    private void ApplyButtonTheme(Canvas canvas)
    {
        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            Image buttonImage = button.targetGraphic as Image;
            if (buttonImage != null)
            {
                Color baseColor = ResolveButtonColor(button.name);
                buttonImage.color = baseColor;
                button.colors = BuildButtonColors(baseColor);
            }
        }
    }

    private ColorBlock BuildButtonColors(Color baseColor)
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.14f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = Color.Lerp(baseColor, Color.gray, 0.5f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        return colors;
    }

    private Color ResolveTextColor(string elementName)
    {
        string normalized = NormalizeName(elementName);
        if (ContainsAny(normalized, "title", "header", "logo", "grade"))
        {
            return visualTheme.TitleText;
        }

        if (ContainsAny(normalized, "warning", "reward", "boss", "fever", "mission"))
        {
            return visualTheme.WarningText;
        }

        if (ContainsAny(normalized, "success", "revive", "ready"))
        {
            return visualTheme.SuccessText;
        }

        if (ContainsAny(normalized, "hint", "tip", "muted", "subtitle", "detail"))
        {
            return visualTheme.MutedText;
        }

        return visualTheme.BodyText;
    }

    private Color ResolvePanelColor(string elementName)
    {
        string normalized = NormalizeName(elementName);
        if (ContainsAny(normalized, "overlay", "dialog", "modal", "revive", "pause", "gameover"))
        {
            return visualTheme.OverlayPanelFill;
        }

        if (ContainsAny(normalized, "hud", "banner", "meter"))
        {
            return visualTheme.HudPanelFill;
        }

        if (ContainsAny(normalized, "accent", "frame", "border", "highlight"))
        {
            return visualTheme.PanelAccent;
        }

        return visualTheme.PanelFill;
    }

    private Color ResolveButtonColor(string elementName)
    {
        string normalized = NormalizeName(elementName);
        if (ContainsAny(normalized, "quit", "decline", "skip", "dismiss", "close", "back"))
        {
            return visualTheme.DestructiveAccent;
        }

        if (ContainsAny(normalized, "claim", "buy", "upgrade", "shop"))
        {
            return visualTheme.CommerceAccent;
        }

        return visualTheme.PanelAccent;
    }

    private static string NormalizeName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.ToLowerInvariant();
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        for (int i = 0; i < needles.Length; i++)
        {
            if (value.Contains(needles[i]))
            {
                return true;
            }
        }

        return false;
    }

    private void ValidateBindings()
    {
        if (_loggedMissingBindings)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        string error = GetBindingError(sceneName);
        if (error != null)
        {
            Debug.LogError($"UIFlowController configuration error in scene '{sceneName}': {error}", this);
            _loggedMissingBindings = true;
        }
    }

    private string GetBindingError(string sceneName)
    {
        if (visualTheme == null)
        {
            return "UiVisualTheme is missing.";
        }

        if (sceneName == SceneNames.MainMenu && menuCanvas == null)
        {
            return "Menu canvas is missing.";
        }

        if (sceneName == SceneNames.GameScene)
        {
            if (hudCanvas == null)
            {
                return "HUD canvas is missing.";
            }

            if (pausePanel == null)
            {
                return "Pause panel is missing.";
            }

            if (revivePanel == null)
            {
                return "Revive panel is missing.";
            }
        }

        if (sceneName == SceneNames.GameOver && gameOverCanvas == null)
        {
            return "Game over canvas is missing.";
        }

        return null;
    }

    private void SyncState(bool force)
    {
        GameState state = GameManager.Instance != null ? GameManager.Instance.State : GameState.Menu;
        string sceneName = SceneManager.GetActiveScene().name;

        if (menuCanvas != null)
        {
            menuCanvas.enabled = sceneName == SceneNames.MainMenu;
        }

        if (hudCanvas != null)
        {
            bool showHud = sceneName == SceneNames.GameScene && state != GameState.GameOver;
            if (force || hudCanvas.enabled != showHud)
            {
                hudCanvas.enabled = showHud;
            }
        }

        if (pausePanel != null)
        {
            bool showPause = sceneName == SceneNames.GameScene && state == GameState.Paused;
            if (force || pausePanel.activeSelf != showPause)
            {
                pausePanel.SetActive(showPause);
            }
        }

        if (revivePanel != null)
        {
            bool showRevive = sceneName == SceneNames.GameScene && state == GameState.RevivePrompt;
            if (force || revivePanel.activeSelf != showRevive)
            {
                revivePanel.SetActive(showRevive);
            }
        }

        if (gameOverCanvas != null)
        {
            gameOverCanvas.enabled = sceneName == SceneNames.GameOver;
        }
    }
}
