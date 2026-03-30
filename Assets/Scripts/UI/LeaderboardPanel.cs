using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class LeaderboardPanel : MonoBehaviour
{
    private GameObject _panelRoot;
    private Transform _contentParent;
    private Font _font;
    private bool _isVisible;

    private readonly List<GameObject> _rows = new();

    public void TogglePanel()
    {
        if (_panelRoot == null)
        {
            BuildPanel();
        }

        _isVisible = !_isVisible;
        _panelRoot.SetActive(_isVisible);

        if (_isVisible)
        {
            RefreshEntries();
        }
    }

    public void Configure(GameObject panel, Transform content, Font font)
    {
        _panelRoot = panel;
        _contentParent = content;
        _font = font;
    }

    private void RefreshEntries()
    {
        foreach (var row in _rows)
        {
            if (row != null) Destroy(row);
        }
        _rows.Clear();

        if (LeaderboardSystem.Instance == null) return;

        var entries = LeaderboardSystem.Instance.GetTopEntries(20);
        int playerBest = ProgressionManager.Instance != null ? ProgressionManager.Instance.HighScore : 0;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            bool isPlayer = entry.Score == playerBest && entry.PlayerName == "You";
            CreateRow(i + 1, entry.PlayerName, entry.Score, entry.Distance, isPlayer);
        }

        // If player not in top 20, show their rank at bottom
        if (playerBest > 0 && LeaderboardSystem.Instance != null)
        {
            int rank = LeaderboardSystem.Instance.GetRank(playerBest);
            if (rank > 20)
            {
                CreateSeparatorRow();
                CreateRow(rank, "You", playerBest, 0f, true);
            }
        }
    }

    private void CreateRow(int rank, string name, int score, float distance, bool highlight)
    {
        if (_contentParent == null || _font == null) return;

        GameObject row = new GameObject($"Rank{rank}", typeof(RectTransform));
        row.transform.SetParent(_contentParent, false);

        Image bg = row.AddComponent<Image>();
        bg.color = highlight ? new Color(0f, 0.5f, 0.7f, 0.3f) : new Color(0.1f, 0.1f, 0.18f, 0.5f);

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        float yPos = -(_rows.Count * 48f);
        rowRect.anchoredPosition = new Vector2(0f, yPos);
        rowRect.sizeDelta = new Vector2(0f, 44f);

        Color textColor = highlight ? Color.cyan : Color.white;
        string medalPrefix = rank <= 3 ? new[] { "🥇", "🥈", "🥉" }[rank - 1] + " " : "";

        CreateCellText(row.transform, $"{medalPrefix}#{rank}", new Vector2(-220f, 0f),
            new Vector2(100f, 40f), 20, textColor);
        CreateCellText(row.transform, name, new Vector2(-80f, 0f),
            new Vector2(160f, 40f), 20, textColor);
        CreateCellText(row.transform, $"{score:N0}", new Vector2(80f, 0f),
            new Vector2(140f, 40f), 20, new Color(0f, 1f, 0.8f));
        if (distance > 0f)
        {
            CreateCellText(row.transform, $"{distance:0}m", new Vector2(200f, 0f),
                new Vector2(100f, 40f), 18, new Color(0.7f, 0.8f, 0.9f));
        }

        _rows.Add(row);
    }

    private void CreateSeparatorRow()
    {
        GameObject sep = new GameObject("Separator", typeof(RectTransform));
        sep.transform.SetParent(_contentParent, false);
        RectTransform rect = sep.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -(_rows.Count * 48f));
        rect.sizeDelta = new Vector2(0f, 24f);

        Text dots = sep.AddComponent<Text>();
        dots.text = "· · ·";
        dots.font = _font;
        dots.fontSize = 20;
        dots.color = new Color(0.5f, 0.5f, 0.6f);
        dots.alignment = TextAnchor.MiddleCenter;

        _rows.Add(sep);
    }

    private void CreateCellText(Transform parent, string content, Vector2 pos,
        Vector2 size, int fontSize, Color color)
    {
        GameObject obj = new GameObject("Cell", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = _font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }

    private void BuildPanel()
    {
        _font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        _panelRoot = new GameObject("LeaderboardPanel");
        _panelRoot.transform.SetParent(transform, false);

        // Will be built properly by SceneBootstrapper
        _panelRoot.SetActive(false);
    }
}
