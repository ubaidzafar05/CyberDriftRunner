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
        public Text LevelText;
        public Text CostText;
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
        for (int i = 0; i < types.Length; i++)
        {
            CreateUpgradeRow(types[i], i);
        }
    }

    private void CreateUpgradeRow(UpgradeSystem.UpgradeType type, int index)
    {
        GameObject row = new GameObject($"Upgrade_{type}", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(contentRoot, false);

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(580f, 80f);
        rowRect.anchoredPosition = new Vector2(0f, -50f - (index * 90f));

        Image bg = row.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.14f, 0.92f);

        Text nameText = CreateText(row.transform, UpgradeSystem.Instance.GetName(type),
            new Vector2(-180f, 12f), new Vector2(210f, 30f), 18, TextAnchor.MiddleLeft, Color.white);

        Text descText = CreateText(row.transform, UpgradeSystem.Instance.GetDescription(type),
            new Vector2(-180f, -14f), new Vector2(210f, 24f), 13, TextAnchor.MiddleLeft, new Color(0.6f, 0.8f, 1f));

        Text levelText = CreateText(row.transform, "",
            new Vector2(60f, 0f), new Vector2(100f, 30f), 16, TextAnchor.MiddleCenter, Color.cyan);

        Text costText = CreateText(row.transform, "",
            new Vector2(160f, 14f), new Vector2(120f, 24f), 14, TextAnchor.MiddleCenter, Color.yellow);

        Button buyBtn = CreateButton(row.transform, new Vector2(220f, 0f), new Vector2(100f, 50f), "Upgrade");
        UpgradeSystem.UpgradeType capturedType = type;
        buyBtn.onClick.AddListener(() => OnPurchase(capturedType));

        _rows.Add(new UpgradeRow
        {
            Type = type,
            LevelText = levelText,
            CostText = costText,
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
        return text;
    }

    private Button CreateButton(Transform parent, Vector2 pos, Vector2 size, string label)
    {
        GameObject obj = new GameObject($"{label}_Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.15f, 0.85f, 1f, 0.92f);

        CreateText(obj.transform, label, Vector2.zero, size, 15, TextAnchor.MiddleCenter, Color.black);
        return obj.GetComponent<Button>();
    }
}
