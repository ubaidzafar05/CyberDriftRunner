using UnityEngine;
using UnityEngine.UI;

public sealed class RateAppPrompt : MonoBehaviour
{
    public static RateAppPrompt Instance { get; private set; }

    private const string SessionCountKey = "cdr.session.count";
    private const string HasRatedKey = "cdr.has.rated";
    private const string DismissedCountKey = "cdr.rate.dismissed";
    private const int FirstPromptSession = 5;
    private const int RepromptInterval = 15;
    private const int MaxDismissals = 3;

    private const string PlayStoreUrl = "https://play.google.com/store/apps/details?id=com.cyberdrift.runner";

    private GameObject _promptPanel;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RecordSession()
    {
        if (PlayerPrefs.GetInt(HasRatedKey, 0) == 1) return;

        int count = PlayerPrefs.GetInt(SessionCountKey, 0) + 1;
        PlayerPrefs.SetInt(SessionCountKey, count);
        PlayerPrefs.Save();
    }

    public bool ShouldPrompt()
    {
        if (PlayerPrefs.GetInt(HasRatedKey, 0) == 1) return false;

        int dismissed = PlayerPrefs.GetInt(DismissedCountKey, 0);
        if (dismissed >= MaxDismissals) return false;

        int sessions = PlayerPrefs.GetInt(SessionCountKey, 0);
        int threshold = FirstPromptSession + (dismissed * RepromptInterval);
        return sessions >= threshold;
    }

    public void TryShowPrompt()
    {
        if (!ShouldPrompt()) return;
        ShowRateDialog();
    }

    private void ShowRateDialog()
    {
        if (_promptPanel != null) return;

        _promptPanel = new GameObject("RatePrompt");
        Canvas canvas = _promptPanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9500;
        CanvasScaler scaler = _promptPanel.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        _promptPanel.AddComponent<GraphicRaycaster>();

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        // Overlay
        GameObject overlay = CreateFillImage(_promptPanel.transform, new Color(0f, 0f, 0f, 0.8f));

        // Panel
        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(_promptPanel.transform, false);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.06f, 0.14f, 0.95f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600f, 360f);

        CreateDialogText(panel.transform, font, "⭐ Enjoying the game? ⭐",
            new Vector2(0f, 130f), new Vector2(500f, 50f), 28, Color.white);
        CreateDialogText(panel.transform, font,
            "Your rating helps us improve\nand keeps the game free!",
            new Vector2(0f, 60f), new Vector2(500f, 80f), 20, new Color(0.8f, 0.85f, 0.9f));

        // Stars display
        CreateDialogText(panel.transform, font, "⭐⭐⭐⭐⭐",
            new Vector2(0f, 0f), new Vector2(300f, 50f), 36, new Color(1f, 0.8f, 0.2f));

        // Rate button
        GameObject rateBtn = CreateDialogButton(panel.transform, font, "Rate Now",
            new Vector2(0f, -70f), new Vector2(280f, 55f), new Color(0.1f, 0.7f, 0.3f));
        rateBtn.GetComponent<Button>().onClick.AddListener(OnRateClicked);

        // Later button
        GameObject laterBtn = CreateDialogButton(panel.transform, font, "Maybe Later",
            new Vector2(0f, -130f), new Vector2(220f, 45f), new Color(0.3f, 0.3f, 0.4f));
        laterBtn.GetComponent<Button>().onClick.AddListener(OnLaterClicked);
    }

    private void OnRateClicked()
    {
        PlayerPrefs.SetInt(HasRatedKey, 1);
        PlayerPrefs.Save();

#if UNITY_ANDROID
        // Try Google Play In-App Review first, fall back to store URL
        try
        {
            Application.OpenURL("market://details?id=" + Application.identifier);
        }
        catch
        {
            Application.OpenURL(PlayStoreUrl);
        }
#else
        Application.OpenURL(PlayStoreUrl);
#endif

        DismissPrompt();
    }

    private void OnLaterClicked()
    {
        int dismissed = PlayerPrefs.GetInt(DismissedCountKey, 0) + 1;
        PlayerPrefs.SetInt(DismissedCountKey, dismissed);
        PlayerPrefs.Save();
        DismissPrompt();
    }

    private void DismissPrompt()
    {
        if (_promptPanel != null) Destroy(_promptPanel);
        _promptPanel = null;
    }

    private GameObject CreateFillImage(Transform parent, Color color)
    {
        GameObject obj = new GameObject("Fill", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return obj;
    }

    private Text CreateDialogText(Transform parent, Font font, string content,
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

    private GameObject CreateDialogButton(Transform parent, Font font, string label,
        Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject obj = new GameObject("Button", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = bgColor;
        obj.AddComponent<Button>().targetGraphic = img;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        GameObject textObj = new GameObject("Label", typeof(RectTransform));
        textObj.transform.SetParent(obj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = font;
        text.fontSize = 22;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return obj;
    }
}
