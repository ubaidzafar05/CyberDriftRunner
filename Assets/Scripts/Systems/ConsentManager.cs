using UnityEngine;
using UnityEngine.UI;

public sealed class ConsentManager : MonoBehaviour
{
    public static ConsentManager Instance { get; private set; }

    private const string ConsentKey = "cdr.gdpr.consent";
    private const string PrivacyPolicyUrl = "https://cyberdriftrunner.github.io/privacy";

    public bool HasConsented => PlayerPrefs.GetInt(ConsentKey, 0) == 1;

    public System.Action OnConsentGranted;

    private GameObject _consentPanel;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!HasConsented)
        {
            ShowConsentDialog();
        }
    }

    public void ShowConsentDialog()
    {
        if (_consentPanel != null) return;

        _consentPanel = new GameObject("ConsentPanel");
        Canvas canvas = _consentPanel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        _consentPanel.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _consentPanel.AddComponent<GraphicRaycaster>();

        // Dark overlay
        GameObject overlay = new GameObject("Overlay", typeof(RectTransform));
        overlay.transform.SetParent(_consentPanel.transform, false);
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.85f);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        // Panel
        GameObject panel = new GameObject("DialogPanel", typeof(RectTransform));
        panel.transform.SetParent(_consentPanel.transform, false);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.15f, 0.95f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700f, 520f);
        panelRect.anchoredPosition = Vector2.zero;

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        // Title
        CreateConsentText(panel.transform, font, "Data & Privacy", new Vector2(0f, 200f),
            new Vector2(600f, 60f), 32, Color.white);

        // Body text
        CreateConsentText(panel.transform, font,
            "We collect anonymous gameplay data (scores, session length, device type) " +
            "to improve your experience. We also use ads to keep the game free.\n\n" +
            "By tapping Accept, you agree to our Privacy Policy and consent to " +
            "data collection and personalized ads.\n\n" +
            "You can change this anytime in Settings.",
            new Vector2(0f, 40f), new Vector2(600f, 240f), 20, new Color(0.8f, 0.85f, 0.9f));

        // Privacy Policy link button
        GameObject linkObj = CreateConsentButton(panel.transform, font, "View Privacy Policy",
            new Vector2(0f, -100f), new Vector2(300f, 50f), new Color(0.2f, 0.6f, 1f));
        linkObj.GetComponent<Button>().onClick.AddListener(OpenPrivacyPolicy);

        // Accept button
        GameObject acceptObj = CreateConsentButton(panel.transform, font, "Accept & Continue",
            new Vector2(0f, -170f), new Vector2(340f, 60f), new Color(0.1f, 0.7f, 0.3f));
        acceptObj.GetComponent<Button>().onClick.AddListener(AcceptConsent);

        // Decline (limited experience)
        GameObject declineObj = CreateConsentButton(panel.transform, font, "Continue without personalized ads",
            new Vector2(0f, -230f), new Vector2(400f, 40f), new Color(0.4f, 0.4f, 0.4f));
        declineObj.GetComponent<Button>().onClick.AddListener(DeclinePersonalized);
    }

    public void AcceptConsent()
    {
        PlayerPrefs.SetInt(ConsentKey, 1);
        PlayerPrefs.SetInt("cdr.ads.personalized", 1);
        PlayerPrefs.Save();
        if (_consentPanel != null) Destroy(_consentPanel);
        OnConsentGranted?.Invoke();
    }

    public void DeclinePersonalized()
    {
        PlayerPrefs.SetInt(ConsentKey, 1);
        PlayerPrefs.SetInt("cdr.ads.personalized", 0);
        PlayerPrefs.Save();
        if (_consentPanel != null) Destroy(_consentPanel);
        OnConsentGranted?.Invoke();
    }

    public void RevokeConsent()
    {
        PlayerPrefs.DeleteKey(ConsentKey);
        PlayerPrefs.DeleteKey("cdr.ads.personalized");
        PlayerPrefs.Save();
    }

    public void OpenPrivacyPolicy()
    {
        Application.OpenURL(PrivacyPolicyUrl);
    }

    public static bool IsPersonalizedAdsEnabled =>
        PlayerPrefs.GetInt("cdr.ads.personalized", 0) == 1;

    private Text CreateConsentText(Transform parent, Font font, string content,
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
        text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return text;
    }

    private GameObject CreateConsentButton(Transform parent, Font font, string label,
        Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject obj = new GameObject("Button", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = bgColor;
        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
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
