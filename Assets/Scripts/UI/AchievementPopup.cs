using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public sealed class AchievementPopup : MonoBehaviour
{
    public static AchievementPopup Instance { get; private set; }

    private GameObject _popupRoot;
    private Text _titleText;
    private Text _descText;
    private CanvasGroup _canvasGroup;
    private RectTransform _panelRect;
    private readonly System.Collections.Generic.Queue<(string title, string desc)> _queue = new();
    private bool _showing;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (AchievementSystem.Instance != null)
        {
            AchievementSystem.Instance.OnAchievementUnlocked += HandleAchievementUnlocked;
        }
    }

    private void OnDisable()
    {
        if (AchievementSystem.Instance != null)
        {
            AchievementSystem.Instance.OnAchievementUnlocked -= HandleAchievementUnlocked;
        }
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private void HandleAchievementUnlocked(AchievementDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        Show(definition.Title, definition.Description);
    }

    public void Show(string title, string description)
    {
        _queue.Enqueue((title, description));
        if (!_showing)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        _showing = true;
        while (_queue.Count > 0)
        {
            var (title, desc) = _queue.Dequeue();
            yield return StartCoroutine(ShowPopup(title, desc));
            yield return new WaitForSecondsRealtime(0.3f);
        }
        _showing = false;
    }

    private IEnumerator ShowPopup(string title, string desc)
    {
        EnsureUI();
        _titleText.text = title;
        _descText.text = desc;

        AudioManager.Instance?.PlayPowerUp();
        HapticFeedback.Instance?.VibrateMedium();

        // Slide in from top
        float startY = 300f;
        float endY = 160f;
        float t = 0f;

        _canvasGroup.alpha = 0f;
        _popupRoot.SetActive(true);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 3.5f;
            float ease = 1f - Mathf.Pow(1f - t, 3f); // ease out cubic
            _panelRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, endY, ease));
            _canvasGroup.alpha = ease;
            yield return null;
        }

        // Hold
        yield return new WaitForSecondsRealtime(2.5f);

        // Fade out
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 3f;
            _canvasGroup.alpha = 1f - t;
            yield return null;
        }

        _popupRoot.SetActive(false);
    }

    private void EnsureUI()
    {
        if (_popupRoot != null) return;

        _popupRoot = new GameObject("AchievementPopup");
        Canvas canvas = _popupRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;
        CanvasScaler scaler = _popupRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        _canvasGroup = _popupRoot.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        // Panel
        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(_popupRoot.transform, false);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.15f, 0.92f);
        _panelRect = panel.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0.5f, 1f);
        _panelRect.anchorMax = new Vector2(0.5f, 1f);
        _panelRect.pivot = new Vector2(0.5f, 1f);
        _panelRect.sizeDelta = new Vector2(650f, 120f);
        _panelRect.anchoredPosition = new Vector2(0f, 160f);

        // Gold accent bar
        GameObject accent = new GameObject("Accent", typeof(RectTransform));
        accent.transform.SetParent(panel.transform, false);
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = new Color(1f, 0.8f, 0.2f);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(1f, 0f);
        accentRect.pivot = new Vector2(0.5f, 0f);
        accentRect.sizeDelta = new Vector2(0f, 4f);
        accentRect.anchoredPosition = Vector2.zero;

        // Trophy icon text
        CreatePopupText(panel.transform, font, "🏆", new Vector2(-260f, 0f),
            new Vector2(60f, 60f), 40, new Color(1f, 0.8f, 0.2f));

        // Title
        _titleText = CreatePopupText(panel.transform, font, "Achievement Unlocked!",
            new Vector2(20f, 18f), new Vector2(460f, 40f), 24, new Color(1f, 0.85f, 0.3f));
        _titleText.alignment = TextAnchor.MiddleLeft;

        // Description
        _descText = CreatePopupText(panel.transform, font, "",
            new Vector2(20f, -18f), new Vector2(460f, 36f), 18, new Color(0.8f, 0.85f, 0.9f));
        _descText.alignment = TextAnchor.MiddleLeft;

        _popupRoot.SetActive(false);
    }

    private Text CreatePopupText(Transform parent, Font font, string content,
        Vector2 pos, Vector2 size, int fontSize, Color color)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
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
