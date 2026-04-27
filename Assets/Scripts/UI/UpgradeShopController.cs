using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI controller for the permanent upgrade shop.
/// Displays all upgrade types with levels, costs, and purchase buttons.
/// Accessible from the main menu.
/// </summary>
public sealed class UpgradeShopController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Text currencyText;
    [SerializeField] private Font font;

    private readonly System.Collections.Generic.List<UpgradeRow> _rows = new System.Collections.Generic.List<UpgradeRow>();

    private struct UpgradeRow
    {
        public UpgradeSystem.UpgradeType Type;
        public Image Background;
        public Text LevelText;
        public Text CostText;
        public Text StatText;
        public Button BuyButton;
    }

    public void Configure(GameObject targetPanel, RectTransform content, Text currency, Font uiFont)
    {
        panel = targetPanel;
        contentRoot = content;
        currencyText = currency;
        font = uiFont;
    }

    private void Start()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        BuildUpgradeList();
    }

    private void Update()
    {
        if (!panel.activeSelf)
        {
            return;
        }

        RefreshAll();
    }

    public void TogglePanel()
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    private void BuildUpgradeList()
    {
        if (contentRoot == null || UpgradeSystem.Instance == null)
        {
            return;
        }

        var types = (UpgradeSystem.UpgradeType[])System.Enum.GetValues(typeof(UpgradeSystem.UpgradeType));
        int index = 0;
        CreateHeader("Mobility", ref index, new Color(0.22f, 0.95f, 1f));
        CreateUpgradeRow(UpgradeSystem.UpgradeType.BaseSpeed, index++);
        CreateUpgradeRow(UpgradeSystem.UpgradeType.JumpHeight, index++);
        CreateHeader("Combat + Hack", ref index, new Color(1f, 0.78f, 0.24f));
        CreateUpgradeRow(UpgradeSystem.UpgradeType.HackRange, index++);
        CreateUpgradeRow(UpgradeSystem.UpgradeType.TargetRange, index++);
        CreateUpgradeRow(UpgradeSystem.UpgradeType.SlowMotionDuration, index++);
        CreateHeader("Support", ref index, new Color(0.34f, 1f, 0.72f));
        CreateUpgradeRow(UpgradeSystem.UpgradeType.ShieldDuration, index++);
        CreateUpgradeRow(UpgradeSystem.UpgradeType.CreditMagnet, index++);
    }

    private void CreateHeader(string label, ref int index, Color color)
    {
        GameObject header = new GameObject($"{label}_Header", typeof(RectTransform), typeof(Text));
        header.transform.SetParent(contentRoot, false);
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(580f, 36f);
        rect.anchoredPosition = new Vector2(0f, -36f - (index * 90f));

        Text text = header.GetComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        index++;
    }

    private void CreateUpgradeRow(UpgradeSystem.UpgradeType type, int index)
    {
        GameObject row = new GameObject($"Upgrade_{type}", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(contentRoot, false);

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(580f, 92f);
        rowRect.anchoredPosition = new Vector2(0f, -24f - (index * 90f));

        Image bg = row.GetComponent<Image>();
        bg.color = ResolveRowColor(type);
        Outline outline = row.AddComponent<Outline>();
        outline.effectColor = ResolveAccentColor(type);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(row.transform, false);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.offsetMin = new Vector2(14f, -10f);
        accentRect.offsetMax = new Vector2(-14f, -4f);
        accent.GetComponent<Image>().color = ResolveAccentColor(type);

        Text nameText = CreateText(row.transform, UpgradeSystem.Instance.GetName(type),
            new Vector2(-180f, 16f), new Vector2(210f, 30f), 18, TextAnchor.MiddleLeft, Color.white);

        Text descText = CreateText(row.transform, UpgradeSystem.Instance.GetDescription(type),
            new Vector2(-180f, -12f), new Vector2(210f, 24f), 13, TextAnchor.MiddleLeft, new Color(0.6f, 0.8f, 1f));

        Text levelText = CreateText(row.transform, "",
            new Vector2(46f, -10f), new Vector2(100f, 30f), 16, TextAnchor.MiddleCenter, Color.cyan);

        Text costText = CreateText(row.transform, "",
            new Vector2(52f, 16f), new Vector2(120f, 24f), 14, TextAnchor.MiddleCenter, Color.yellow);

        Text statText = CreateText(row.transform, "",
            new Vector2(170f, 16f), new Vector2(146f, 22f), 14, TextAnchor.MiddleCenter, ResolveAccentColor(type));

        Button buyBtn = CreateButton(row.transform, new Vector2(220f, 0f), new Vector2(100f, 50f), "Upgrade");
        UpgradeSystem.UpgradeType capturedType = type;
        buyBtn.onClick.AddListener(() => OnPurchase(capturedType));

        _rows.Add(new UpgradeRow
        {
            Type = type,
            Background = bg,
            LevelText = levelText,
            CostText = costText,
            StatText = statText,
            BuyButton = buyBtn
        });
    }

    private void OnPurchase(UpgradeSystem.UpgradeType type)
    {
        UpgradeSystem.Instance?.TryPurchase(type);
    }

    private void RefreshAll()
    {
        if (UpgradeSystem.Instance == null)
        {
            return;
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            int level = UpgradeSystem.Instance.GetLevel(row.Type);
            bool maxed = UpgradeSystem.Instance.IsMaxed(row.Type);

            row.LevelText.text = maxed ? $"Lv {level} MAX" : $"Lv {level}/5";
            row.LevelText.color = maxed ? Color.green : Color.cyan;
            row.StatText.text = GetUpgradeValueLabel(row.Type, level);
            row.StatText.color = ResolveAccentColor(row.Type);
            row.Background.color = maxed ? new Color(0.06f, 0.16f, 0.1f, 0.96f) : ResolveRowColor(row.Type);

            if (maxed)
            {
                row.CostText.text = "MAXED";
                row.CostText.color = Color.green;
                row.BuyButton.interactable = false;
            }
            else
            {
                int cost = UpgradeSystem.Instance.GetCost(row.Type);
                bool canAfford = ProgressionManager.Instance != null && ProgressionManager.Instance.SoftCurrency >= cost;
                row.CostText.text = $"{cost} credits";
                row.CostText.color = canAfford ? Color.yellow : Color.red;
                row.BuyButton.interactable = canAfford;
            }
        }

        if (currencyText != null && ProgressionManager.Instance != null)
        {
            string next = $"Credits: {ProgressionManager.Instance.SoftCurrency}";
            if (currencyText.text != next)
            {
                currencyText.text = next;
            }
        }
    }

    private Text CreateText(Transform parent, string content, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, Color color)
    {
        GameObject obj = new GameObject($"{content}_Text", typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = color;
        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    private Button CreateButton(Transform parent, Vector2 pos, Vector2 size, string label)
    {
        GameObject obj = new GameObject($"{label}_Btn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline), typeof(Shadow));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.03f, 0.07f, 0.12f, 0.96f);
        Outline outline = obj.GetComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.95f, 1f, 0.82f);
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = obj.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(3f, -3f);

        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(obj.transform, false);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.offsetMin = new Vector2(10f, -8f);
        accentRect.offsetMax = new Vector2(-10f, -2f);
        accent.GetComponent<Image>().color = new Color(0.18f, 0.95f, 1f, 0.88f);

        CreateText(obj.transform, label, Vector2.zero, size, 15, TextAnchor.MiddleCenter, Color.white);
        return obj.GetComponent<Button>();
    }

    private static Color ResolveAccentColor(UpgradeSystem.UpgradeType type)
    {
        switch (type)
        {
            case UpgradeSystem.UpgradeType.BaseSpeed:
            case UpgradeSystem.UpgradeType.JumpHeight:
                return new Color(0.22f, 0.95f, 1f);
            case UpgradeSystem.UpgradeType.HackRange:
            case UpgradeSystem.UpgradeType.TargetRange:
            case UpgradeSystem.UpgradeType.SlowMotionDuration:
                return new Color(1f, 0.78f, 0.24f);
            default:
                return new Color(0.34f, 1f, 0.72f);
        }
    }

    private static Color ResolveRowColor(UpgradeSystem.UpgradeType type)
    {
        switch (type)
        {
            case UpgradeSystem.UpgradeType.BaseSpeed:
            case UpgradeSystem.UpgradeType.JumpHeight:
                return new Color(0.04f, 0.08f, 0.14f, 0.94f);
            case UpgradeSystem.UpgradeType.HackRange:
            case UpgradeSystem.UpgradeType.TargetRange:
            case UpgradeSystem.UpgradeType.SlowMotionDuration:
                return new Color(0.12f, 0.09f, 0.04f, 0.94f);
            default:
                return new Color(0.04f, 0.12f, 0.09f, 0.94f);
        }
    }

    private string GetUpgradeValueLabel(UpgradeSystem.UpgradeType type, int level)
    {
        switch (type)
        {
            case UpgradeSystem.UpgradeType.BaseSpeed:
                return $"+{Mathf.RoundToInt(level * 5f)}% speed";
            case UpgradeSystem.UpgradeType.JumpHeight:
                return $"+{Mathf.RoundToInt(level * 8f)}% jump";
            case UpgradeSystem.UpgradeType.HackRange:
                return $"+{Mathf.RoundToInt(level * 12f)}% hack reach";
            case UpgradeSystem.UpgradeType.ShieldDuration:
                return $"+{level:0}s shield";
            case UpgradeSystem.UpgradeType.TargetRange:
                return $"+{Mathf.RoundToInt(level * 10f)}% target lock";
            case UpgradeSystem.UpgradeType.CreditMagnet:
                return $"+{(level * 0.5f):0.0}m magnet";
            case UpgradeSystem.UpgradeType.SlowMotionDuration:
                return $"+{(level * 0.5f):0.0}s slowmo";
            default:
                return string.Empty;
        }
    }
}
