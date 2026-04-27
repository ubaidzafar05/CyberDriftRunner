using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public sealed class SceneLoader : MonoBehaviour
{
    private const float LoadTimeoutSeconds = 12f;

    public static SceneLoader Instance { get; private set; }

    private GameObject _loadingScreen;
    private Image _progressBar;
    private Text _percentText;
    private Text _tipText;
    private CanvasGroup _canvasGroup;
    private bool _isLoading;

    private static readonly string[] LoadingTips = new[]
    {
        "Swipe up to jump over low barriers",
        "Hold the hack button to slow time",
        "Collect credits to unlock new skins",
        "Combo multiplier boosts your score",
        "Fever mode grants invincibility!",
        "Slide under high obstacles",
        "The magnet power-up pulls credits toward you",
        "Double score power-up stacks with combos",
        "Shield absorbs one hit",
        "Speed increases the further you run",
        "Daily rewards reset at midnight",
        "Complete challenges for bonus credits"
    };

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        _isLoading = false;
        if (_loadingScreen != null)
        {
            Destroy(_loadingScreen);
            _loadingScreen = null;
        }
    }

    public void LoadScene(string sceneName)
    {
        if (_isLoading) return;
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        _isLoading = true;
        CreateLoadingUI();
        float startedAt = Time.unscaledTime;

        // Fade in
        float fadeIn = 0f;
        while (fadeIn < 1f)
        {
            fadeIn += Time.unscaledDeltaTime * 4f;
            _canvasGroup.alpha = fadeIn;
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        // Load async
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"[SceneLoader] Failed to start async load for scene '{sceneName}'. Falling back to direct load.");
            DestroyLoadingUi();
            _isLoading = false;
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            if (Time.unscaledTime - startedAt >= LoadTimeoutSeconds)
            {
                Debug.LogError($"[SceneLoader] Timed out while loading scene '{sceneName}'. Forcing activation.");
                break;
            }

            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            UpdateProgress(progress);
            yield return null;
        }
        UpdateProgress(1f);

        // Brief hold so players can read the tip
        yield return new WaitForSecondsRealtime(0.4f);

        operation.allowSceneActivation = true;

        // Wait for scene activation
        while (!operation.isDone)
        {
            if (Time.unscaledTime - startedAt >= LoadTimeoutSeconds)
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' exceeded load timeout after activation request.");
                break;
            }

            yield return null;
        }

        // Fade out
        float fadeOut = 1f;
        while (fadeOut > 0f)
        {
            fadeOut -= Time.unscaledDeltaTime * 4f;
            _canvasGroup.alpha = fadeOut;
            yield return null;
        }

        DestroyLoadingUi();
        _isLoading = false;
    }

    private void UpdateProgress(float progress)
    {
        if (_progressBar != null)
        {
            _progressBar.fillAmount = progress;
        }
        if (_percentText != null)
        {
            _percentText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }
    }

    private void CreateLoadingUI()
    {
        _loadingScreen = new GameObject("LoadingScreen");
        Canvas canvas = _loadingScreen.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9998;
        CanvasScaler scaler = _loadingScreen.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        _loadingScreen.AddComponent<GraphicRaycaster>();
        _canvasGroup = _loadingScreen.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        // Background
        GameObject bg = new GameObject("BG", typeof(RectTransform));
        bg.transform.SetParent(_loadingScreen.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.08f, 1f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Title
        CreateLoadingText(font, "CYBER DRIFT RUNNER", new Vector2(0f, 120f),
            new Vector2(800f, 60f), 36, Color.cyan);

        // Progress bar background
        GameObject barBg = new GameObject("BarBG", typeof(RectTransform));
        barBg.transform.SetParent(_loadingScreen.transform, false);
        Image barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.15f, 0.15f, 0.25f);
        RectTransform barBgRect = barBg.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.5f, 0.5f);
        barBgRect.anchorMax = new Vector2(0.5f, 0.5f);
        barBgRect.anchoredPosition = new Vector2(0f, 0f);
        barBgRect.sizeDelta = new Vector2(600f, 20f);

        // Progress bar fill
        GameObject barFill = new GameObject("BarFill", typeof(RectTransform));
        barFill.transform.SetParent(barBg.transform, false);
        _progressBar = barFill.AddComponent<Image>();
        _progressBar.color = new Color(0f, 0.9f, 1f);
        _progressBar.type = Image.Type.Filled;
        _progressBar.fillMethod = Image.FillMethod.Horizontal;
        _progressBar.fillAmount = 0f;
        RectTransform fillRect = barFill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Percent text
        _percentText = CreateLoadingText(font, "0%", new Vector2(0f, -30f),
            new Vector2(200f, 40f), 24, Color.white);

        // Tip text
        string tip = LoadingTips[Random.Range(0, LoadingTips.Length)];
        _tipText = CreateLoadingText(font, $"TIP: {tip}", new Vector2(0f, -100f),
            new Vector2(700f, 60f), 20, new Color(0.7f, 0.8f, 0.9f));
    }

    private void DestroyLoadingUi()
    {
        if (_loadingScreen == null)
        {
            return;
        }

        Destroy(_loadingScreen);
        _loadingScreen = null;
    }

    private Text CreateLoadingText(Font font, string content, Vector2 pos,
        Vector2 size, int fontSize, Color color)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform));
        obj.transform.SetParent(_loadingScreen.transform, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return text;
    }
}
