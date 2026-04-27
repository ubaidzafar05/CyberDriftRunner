using UnityEngine;
using UnityEngine.UI;

public sealed class SkinShopController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text creditsText;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Font font;

    private readonly System.Collections.Generic.List<RowBinding> rows = new System.Collections.Generic.List<RowBinding>();

    private enum RowKind
    {
        Skin,
        ShopItem
    }

    private sealed class RowBinding
    {
        public RowKind Kind;
        public SkinDefinition Skin;
        public ShopItemDefinition ShopItem;
        public Image Background;
        public Text StatusText;
        public Text PriceText;
        public Text RarityText;
        public Button ActionButton;
        public Text ActionLabel;
    }

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
        if (creditsText != null)
        {
            int soft = EconomySystem.Instance != null ? EconomySystem.Instance.Credits : (ProgressionManager.Instance != null ? ProgressionManager.Instance.SoftCurrency : 0);
            int premium = EconomySystem.Instance != null ? EconomySystem.Instance.PremiumCurrency : 0;
            string next = $"Credits {soft} | Premium {premium}";
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
        if (contentRoot == null)
        {
            return;
        }

        float nextY = -40f;
        if (ProgressionManager.Instance != null)
        {
            CreateHeader("Skins", ref nextY);
            for (int i = 0; i < ProgressionManager.Instance.Skins.Count; i++)
            {
                CreateSkinRow(ProgressionManager.Instance.Skins[i], ref nextY);
            }
        }

        if (ShopSystem.Instance != null)
        {
            CreateHeader("Trails", ref nextY);
            AddCategoryRows(ShopItemType.Trail, ref nextY);
            CreateHeader("Weapons", ref nextY);
            AddCategoryRows(ShopItemType.WeaponSkin, ref nextY);
            CreateHeader("Packs", ref nextY);
            AddCategoryRows(ShopItemType.CreditPack, ref nextY);
        }
    }

    private void AddCategoryRows(ShopItemType type, ref float nextY)
    {
        for (int i = 0; i < ShopSystem.Instance.Items.Count; i++)
        {
            ShopItemDefinition item = ShopSystem.Instance.Items[i];
            if (item.ItemType != type)
            {
                continue;
            }

            CreateShopItemRow(item, ref nextY);
        }
    }

    private void CreateHeader(string label, ref float nextY)
    {
        GameObject headerRoot = new GameObject($"{label}_Header", typeof(RectTransform), typeof(Image));
        headerRoot.transform.SetParent(contentRoot, false);
        RectTransform headerRootRect = headerRoot.GetComponent<RectTransform>();
        headerRootRect.anchorMin = new Vector2(0.5f, 1f);
        headerRootRect.anchorMax = new Vector2(0.5f, 1f);
        headerRootRect.sizeDelta = new Vector2(560f, 44f);
        headerRootRect.anchoredPosition = new Vector2(0f, nextY);
        Image headerBg = headerRoot.GetComponent<Image>();
        headerBg.color = new Color(0.03f, 0.06f, 0.1f, 0.72f);

        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(headerRoot.transform, false);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0.5f);
        accentRect.anchorMax = new Vector2(0f, 0.5f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.anchoredPosition = new Vector2(16f, 0f);
        accentRect.sizeDelta = new Vector2(124f, 6f);
        accent.GetComponent<Image>().color = ResolveCategoryColor(label);

        GameObject header = new GameObject($"{label}_HeaderText", typeof(RectTransform), typeof(Text));
        header.transform.SetParent(contentRoot, false);
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(560f, 40f);
        rect.anchoredPosition = new Vector2(0f, nextY);

        Text text = header.GetComponent<Text>();
        text.font = font;
        text.text = label;
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = ResolveCategoryColor(label);
        nextY -= 58f;
    }

    private void CreateSkinRow(SkinDefinition skin, ref float nextY)
    {
        GameObject row = new GameObject($"{skin.Id}_Row", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(contentRoot, false);

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(560f, 96f);
        rowRect.anchoredPosition = new Vector2(0f, nextY);
        nextY -= 106f;

        Image background = row.GetComponent<Image>();
        background.color = ResolveRowBackground(false, skin.IsPremium);
        Outline outline = row.AddComponent<Outline>();
        outline.effectColor = ResolveRarityColor(skin.IsPremium, skin.SoftCurrencyCost);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        CreatePreviewSwatch(row.transform, new Vector2(-232f, 0f), skin.BaseColor, skin.EmissionColor);
        Text nameText = CreateText(row.transform, skin.DisplayName, new Vector2(-118f, 14f), new Vector2(220f, 32f), 22, TextAnchor.MiddleLeft, skin.BaseColor);
        Text rarityText = CreateText(row.transform, skin.IsPremium ? "PREMIUM" : ResolveSoftRarityLabel(skin.SoftCurrencyCost), new Vector2(-118f, -16f), new Vector2(180f, 22f), 14, TextAnchor.MiddleLeft, ResolveRarityColor(skin.IsPremium, skin.SoftCurrencyCost));
        Text statusText = CreateText(row.transform, string.Empty, new Vector2(44f, -14f), new Vector2(168f, 26f), 16, TextAnchor.MiddleCenter, Color.white);
        Text priceText = CreateText(row.transform, skin.IsPremium ? "15 premium" : $"{skin.SoftCurrencyCost} credits", new Vector2(52f, 16f), new Vector2(168f, 24f), 16, TextAnchor.MiddleCenter, Color.yellow);
        Button actionButton = CreateButton(row.transform, new Vector2(210f, 0f), new Vector2(140f, 50f), skin.IsPremium ? "Buy" : "Use");
        Text actionLabel = actionButton.GetComponentInChildren<Text>();

        actionButton.onClick.AddListener(() => HandleSkinAction(skin));
        rows.Add(new RowBinding
        {
            Kind = RowKind.Skin,
            Skin = skin,
            Background = background,
            StatusText = statusText,
            PriceText = priceText,
            RarityText = rarityText,
            ActionButton = actionButton,
            ActionLabel = actionLabel
        });
        nameText.text = skin.DisplayName;
    }

    private void CreateShopItemRow(ShopItemDefinition item, ref float nextY)
    {
        GameObject row = new GameObject($"{item.Id}_Row", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(contentRoot, false);

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 1f);
        rowRect.anchorMax = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(560f, 96f);
        rowRect.anchoredPosition = new Vector2(0f, nextY);
        nextY -= 106f;

        Image background = row.GetComponent<Image>();
        background.color = ResolveRowBackground(item.CurrencyType == ShopCurrencyType.PremiumCurrency || item.CurrencyType == ShopCurrencyType.RealMoney, item.CurrencyType != ShopCurrencyType.SoftCurrency);
        Outline outline = row.AddComponent<Outline>();
        outline.effectColor = ResolveItemAccent(item);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        CreatePreviewSwatch(row.transform, new Vector2(-232f, 0f), ResolveItemAccent(item), ResolveItemAccent(item) * 1.15f);
        CreateText(row.transform, item.DisplayName, new Vector2(-118f, 14f), new Vector2(220f, 28f), 20, TextAnchor.MiddleLeft, Color.white);
        CreateText(row.transform, item.Description, new Vector2(-118f, -12f), new Vector2(220f, 22f), 13, TextAnchor.MiddleLeft, new Color(0.65f, 0.8f, 1f));
        Text rarityText = CreateText(row.transform, ResolveItemRarity(item), new Vector2(52f, -14f), new Vector2(168f, 22f), 14, TextAnchor.MiddleCenter, ResolveItemAccent(item));
        Text statusText = CreateText(row.transform, string.Empty, new Vector2(52f, 12f), new Vector2(168f, 22f), 16, TextAnchor.MiddleCenter, Color.white);
        Text priceText = CreateText(row.transform, GetPriceText(item), new Vector2(52f, 34f), new Vector2(168f, 18f), 15, TextAnchor.MiddleCenter, Color.yellow);
        Button actionButton = CreateButton(row.transform, new Vector2(210f, 0f), new Vector2(140f, 50f), "Buy");
        Text actionLabel = actionButton.GetComponentInChildren<Text>();

        actionButton.onClick.AddListener(() => HandleShopItemAction(item));
        rows.Add(new RowBinding
        {
            Kind = RowKind.ShopItem,
            ShopItem = item,
            Background = background,
            StatusText = statusText,
            PriceText = priceText,
            RarityText = rarityText,
            ActionButton = actionButton,
            ActionLabel = actionLabel
        });
    }

    private void HandleSkinAction(SkinDefinition skin)
    {
        if (ProgressionManager.Instance == null) return;

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
                if (succeeded && ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.UnlockSkin(skin.Id);
                    ProgressionManager.Instance.SelectSkin(skin.Id);
                }
            });
        }
    }

    private void RefreshStatuses()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            RowBinding row = rows[i];
            if (row.Kind == RowKind.Skin)
            {
                RefreshSkinRow(row);
            }
            else
            {
                RefreshShopItemRow(row);
            }
        }
    }

    private void RefreshSkinRow(RowBinding row)
    {
        if (ProgressionManager.Instance == null)
        {
            return;
        }

        row.StatusText.text = GetStatusText(row.Skin);
        bool owned = ProgressionManager.Instance.IsUnlocked(row.Skin.Id);
        bool selected = ProgressionManager.Instance.SelectedSkinId == row.Skin.Id;
        row.PriceText.text = row.Skin.IsPremium ? "15 premium" : $"{row.Skin.SoftCurrencyCost} credits";
        row.StatusText.color = selected ? new Color(0.22f, 1f, 0.74f) : owned ? new Color(0.72f, 0.9f, 1f) : ResolveRarityColor(row.Skin.IsPremium, row.Skin.SoftCurrencyCost);
        row.PriceText.color = !owned ? ResolveRarityColor(row.Skin.IsPremium, row.Skin.SoftCurrencyCost) : new Color(0.84f, 0.86f, 0.95f);
        row.Background.color = selected
            ? new Color(0.04f, 0.16f, 0.18f, 0.97f)
            : ResolveRowBackground(false, row.Skin.IsPremium);
        row.ActionButton.interactable = !selected;
        row.ActionLabel.text = owned ? (selected ? "Equipped" : "Equip") : (row.Skin.IsPremium ? "Buy" : "Unlock");
    }

    private void RefreshShopItemRow(RowBinding row)
    {
        if (ShopSystem.Instance == null)
        {
            return;
        }

        bool owned = ShopSystem.Instance.IsOwned(row.ShopItem.Id);
        bool selected = IsSelected(row.ShopItem);
        row.StatusText.text = selected ? "Selected" : owned ? "Owned" : "Available";
        row.PriceText.text = GetPriceText(row.ShopItem);
        row.StatusText.color = selected ? new Color(0.22f, 1f, 0.74f) : owned ? new Color(0.72f, 0.9f, 1f) : ResolveItemAccent(row.ShopItem);
        row.PriceText.color = owned ? new Color(0.84f, 0.86f, 0.95f) : ResolveItemAccent(row.ShopItem);
        row.RarityText.color = ResolveItemAccent(row.ShopItem);
        row.Background.color = selected
            ? new Color(0.04f, 0.16f, 0.18f, 0.97f)
            : ResolveRowBackground(row.ShopItem.CurrencyType != ShopCurrencyType.SoftCurrency, row.ShopItem.CurrencyType != ShopCurrencyType.SoftCurrency);
        row.ActionButton.interactable = !(owned && selected);
        row.ActionLabel.text = owned ? (selected ? "Selected" : "Equip") : "Buy";
    }

    private bool IsSelected(ShopItemDefinition item)
    {
        if (ShopSystem.Instance == null)
        {
            return false;
        }

        switch (item.ItemType)
        {
            case ShopItemType.Trail:
                return ShopSystem.Instance.SelectedTrailId == item.RewardId;
            case ShopItemType.WeaponSkin:
                return ShopSystem.Instance.SelectedWeaponSkinId == item.RewardId;
            default:
                return false;
        }
    }

    private string GetStatusText(SkinDefinition skin)
    {
        if (ProgressionManager.Instance == null) return "";

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

    private string GetPriceText(ShopItemDefinition item)
    {
        switch (item.CurrencyType)
        {
            case ShopCurrencyType.SoftCurrency:
                return $"{item.Price} credits";
            case ShopCurrencyType.PremiumCurrency:
                return $"{item.Price} premium";
            default:
                return item.Price <= 1 ? "$0.99" : $"${item.Price:0.00}";
        }
    }

    private static Color ResolveCategoryColor(string label)
    {
        switch (label)
        {
            case "Skins":
                return new Color(1f, 0.42f, 0.8f);
            case "Trails":
                return new Color(0.22f, 0.95f, 1f);
            case "Weapons":
                return new Color(1f, 0.76f, 0.22f);
            default:
                return new Color(0.62f, 0.86f, 1f);
        }
    }

    private static Color ResolveRarityColor(bool premium, int price)
    {
        if (premium)
        {
            return new Color(1f, 0.42f, 0.8f);
        }

        if (price >= 220)
        {
            return new Color(1f, 0.8f, 0.22f);
        }

        return new Color(0.22f, 0.95f, 1f);
    }

    private static string ResolveSoftRarityLabel(int price)
    {
        if (price >= 220) return "EPIC";
        if (price >= 120) return "RARE";
        return "STANDARD";
    }

    private static Color ResolveItemAccent(ShopItemDefinition item)
    {
        switch (item.ItemType)
        {
            case ShopItemType.Trail:
                return new Color(0.22f, 0.95f, 1f);
            case ShopItemType.WeaponSkin:
                return new Color(1f, 0.78f, 0.24f);
            case ShopItemType.CreditPack:
                return new Color(0.34f, 1f, 0.72f);
            default:
                return new Color(1f, 0.42f, 0.8f);
        }
    }

    private static string ResolveItemRarity(ShopItemDefinition item)
    {
        if (item.CurrencyType == ShopCurrencyType.RealMoney)
        {
            return "BUNDLE";
        }

        if (item.CurrencyType == ShopCurrencyType.PremiumCurrency)
        {
            return "LEGENDARY";
        }

        return item.Price >= 240 ? "EPIC" : "RARE";
    }

    private static Color ResolveRowBackground(bool premium, bool special)
    {
        if (premium)
        {
            return new Color(0.12f, 0.06f, 0.16f, 0.95f);
        }

        if (special)
        {
            return new Color(0.08f, 0.1f, 0.16f, 0.95f);
        }

        return new Color(0.05f, 0.08f, 0.14f, 0.95f);
    }

    private void CreatePreviewSwatch(Transform parent, Vector2 anchoredPosition, Color baseColor, Color glowColor)
    {
        GameObject swatch = new GameObject("PreviewSwatch", typeof(RectTransform), typeof(Image), typeof(Outline));
        swatch.transform.SetParent(parent, false);
        RectTransform rect = swatch.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(72f, 72f);
        swatch.GetComponent<Image>().color = baseColor;
        Outline outline = swatch.GetComponent<Outline>();
        outline.effectColor = glowColor;
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject inner = new GameObject("InnerGlow", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(swatch.transform, false);
        RectTransform innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = new Vector2(0.5f, 0.5f);
        innerRect.anchorMax = new Vector2(0.5f, 0.5f);
        innerRect.sizeDelta = new Vector2(34f, 34f);
        inner.GetComponent<Image>().color = glowColor * 0.8f;
    }

    private void HandleShopItemAction(ShopItemDefinition item)
    {
        if (ShopSystem.Instance == null)
        {
            return;
        }

        if (ShopSystem.Instance.IsOwned(item.Id))
        {
            ShopSystem.Instance.TrySelect(item.Id);
            return;
        }

        ShopSystem.Instance.TryPurchase(item.Id);
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
        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    private Button CreateButton(Transform parent, Vector2 anchoredPosition, Vector2 size, string label)
    {
        GameObject buttonObject = new GameObject($"{label}_Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline), typeof(Shadow));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.03f, 0.07f, 0.12f, 0.96f);
        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.16f, 0.95f, 1f, 0.84f);
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = buttonObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = new Vector2(3f, -3f);

        GameObject accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(buttonObject.transform, false);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.offsetMin = new Vector2(10f, -8f);
        accentRect.offsetMax = new Vector2(-10f, -2f);
        accent.GetComponent<Image>().color = new Color(0.16f, 0.95f, 1f, 0.88f);

        CreateText(buttonObject.transform, label, Vector2.zero, size, 18, TextAnchor.MiddleCenter, Color.white);
        return buttonObject.GetComponent<Button>();
    }
}
