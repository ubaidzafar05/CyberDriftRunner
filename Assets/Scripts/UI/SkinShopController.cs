using UnityEngine;
using UnityEngine.UI;

public sealed class SkinShopController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text creditsText;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Font font;

    private readonly System.Collections.Generic.List<Text> statusTexts = new System.Collections.Generic.List<Text>();

    public void Configure(GameObject targetPanel, Text targetCreditsText, RectTransform targetContentRoot, Font targetFont)
    {
        panel = targetPanel;
        creditsText = targetCreditsText;
        contentRoot = targetContentRoot;
        font = targetFont;
    }

    private void Start()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        BuildCatalog();
    }

    private void Update()
    {
        if (creditsText != null && ProgressionManager.Instance != null)
        {
            int currency = ProgressionManager.Instance.SoftCurrency;
            string next = $"Bank {currency}";
            if (creditsText.text != next)
            {
                creditsText.text = next;
            }
        }

        RefreshStatuses();
    }

    public void TogglePanel()
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    private void BuildCatalog()
    {
        if (contentRoot == null || ProgressionManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < ProgressionManager.Instance.Skins.Count; i++)
        {
            CreateRow(ProgressionManager.Instance.Skins[i], i);
        }
    }

    private void CreateRow(SkinDefinition skin, int index)
    {
        GameObject row = new GameObject($"{skin.Id}_Row", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(contentRoot, false);

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(560f, 90f);
        rowRect.anchoredPosition = new Vector2(0f, -55f - (index * 100f));

        Image background = row.GetComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.16f, 0.9f);

        Text nameText = CreateText(row.transform, skin.DisplayName, new Vector2(-165f, 0f), new Vector2(220f, 40f), 22, TextAnchor.MiddleLeft, skin.BaseColor);
        Text statusText = CreateText(row.transform, string.Empty, new Vector2(40f, 0f), new Vector2(180f, 40f), 20, TextAnchor.MiddleCenter, Color.white);
        Button actionButton = CreateButton(row.transform, new Vector2(210f, 0f), new Vector2(140f, 50f), skin.IsPremium ? "Buy" : "Use");

        actionButton.onClick.AddListener(() => HandleSkinAction(skin));
        statusTexts.Add(statusText);
        nameText.text = skin.DisplayName;
    }

    private void HandleSkinAction(SkinDefinition skin)
    {
        if (ProgressionManager.Instance.IsUnlocked(skin.Id))
        {
            ProgressionManager.Instance.SelectSkin(skin.Id);
            return;
        }

        if (!skin.IsPremium)
        {
            ProgressionManager.Instance.TryUnlockWithSoftCurrency(skin.Id);
            return;
        }

        if (MonetizationManager.Instance != null)
        {
            MonetizationManager.Instance.Purchase(skin.PremiumProductId, succeeded =>
            {
                if (succeeded)
                {
                    ProgressionManager.Instance.UnlockSkin(skin.Id);
                    ProgressionManager.Instance.SelectSkin(skin.Id);
                }
            });
        }
    }

    private void RefreshStatuses()
    {
        if (ProgressionManager.Instance == null)
        {
            return;
        }

        for (int i = 0; i < ProgressionManager.Instance.Skins.Count && i < statusTexts.Count; i++)
        {
            SkinDefinition skin = ProgressionManager.Instance.Skins[i];
            statusTexts[i].text = GetStatusText(skin);
        }
    }

    private string GetStatusText(SkinDefinition skin)
    {
        if (ProgressionManager.Instance.SelectedSkinId == skin.Id)
        {
            return "Selected";
        }

        if (ProgressionManager.Instance.IsUnlocked(skin.Id))
        {
            return "Unlocked";
        }

        return skin.IsPremium ? "Premium" : $"{skin.SoftCurrencyCost} credits";
    }

    private Text CreateText(Transform parent, string content, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textObject = new GameObject($"{content}_Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private Button CreateButton(Transform parent, Vector2 anchoredPosition, Vector2 size, string label)
    {
        GameObject buttonObject = new GameObject($"{label}_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.85f, 1f, 0.92f);

        CreateText(buttonObject.transform, label, Vector2.zero, size, 18, TextAnchor.MiddleCenter, Color.black);
        return buttonObject.GetComponent<Button>();
    }
}
