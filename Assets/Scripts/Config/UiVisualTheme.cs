using UnityEngine;

[CreateAssetMenu(menuName = "Cyber Drift Runner/Config/UI Visual Theme", fileName = "UiVisualTheme")]
public sealed class UiVisualTheme : ScriptableObject
{
    [SerializeField] private Color canvasBackground = new Color(0.01f, 0.015f, 0.04f, 1f);
    [SerializeField] private Color panelFill = new Color(0.015f, 0.024f, 0.05f, 0.86f);
    [SerializeField] private Color overlayPanelFill = new Color(0.012f, 0.018f, 0.04f, 0.92f);
    [SerializeField] private Color hudPanelFill = new Color(0.01f, 0.022f, 0.048f, 0.6f);
    [SerializeField] private Color panelAccent = new Color(0f, 0.96f, 1f, 0.82f);
    [SerializeField] private Color commerceAccent = new Color(1f, 0.78f, 0.2f, 0.92f);
    [SerializeField] private Color destructiveAccent = new Color(1f, 0.32f, 0.36f, 0.88f);
    [SerializeField] private Color titleText = Color.white;
    [SerializeField] private Color bodyText = new Color(0.74f, 0.88f, 1f);
    [SerializeField] private Color mutedText = new Color(0.58f, 0.76f, 0.94f);
    [SerializeField] private Color warningText = new Color(1f, 0.84f, 0.24f);
    [SerializeField] private Color successText = new Color(0.34f, 1f, 0.72f);

    public Color CanvasBackground => canvasBackground;
    public Color PanelFill => panelFill;
    public Color OverlayPanelFill => overlayPanelFill;
    public Color HudPanelFill => hudPanelFill;
    public Color PanelAccent => panelAccent;
    public Color CommerceAccent => commerceAccent;
    public Color DestructiveAccent => destructiveAccent;
    public Color TitleText => titleText;
    public Color BodyText => bodyText;
    public Color MutedText => mutedText;
    public Color WarningText => warningText;
    public Color SuccessText => successText;
}
