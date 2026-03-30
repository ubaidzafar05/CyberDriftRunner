using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    private const int PoolSize = 12;
    private const float FloatDuration = 0.9f;
    private const float FloatHeight = 120f;

    private Canvas _canvas;
    private Font _font;
    private readonly List<FloatingEntry> _active = new();
    private readonly Queue<(Text text, RectTransform rect, CanvasGroup group)> _pool = new();

    private struct FloatingEntry
    {
        public Text Text;
        public RectTransform Rect;
        public CanvasGroup Group;
        public Vector2 StartPos;
        public float Timer;
    }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private void LateUpdate()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            FloatingEntry entry = _active[i];
            entry.Timer += Time.deltaTime;

            float t = Mathf.Clamp01(entry.Timer / FloatDuration);
            float ease = 1f - (1f - t) * (1f - t); // ease out quad

            entry.Rect.anchoredPosition = entry.StartPos + Vector2.up * (FloatHeight * ease);

            // Scale punch then shrink
            float scale = t < 0.15f ? Mathf.Lerp(0.5f, 1.3f, t / 0.15f) :
                          t < 0.3f ? Mathf.Lerp(1.3f, 1f, (t - 0.15f) / 0.15f) : 1f;
            entry.Rect.localScale = Vector3.one * scale;

            // Fade out in last 30%
            entry.Group.alpha = t > 0.7f ? 1f - ((t - 0.7f) / 0.3f) : 1f;

            _active[i] = entry;

            if (entry.Timer >= FloatDuration)
            {
                Release(entry);
                _active.RemoveAt(i);
            }
        }
    }

    public void SpawnAt(Vector3 worldPos, string text, Color color)
    {
        EnsureCanvas();

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 screenPos = cam.WorldToScreenPoint(worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(), screenPos, _canvas.worldCamera, out Vector2 localPos);

        // Random horizontal offset to avoid stacking
        localPos.x += Random.Range(-30f, 30f);

        var entry = Get();
        entry.text.text = text;
        entry.text.color = color;
        entry.text.fontSize = text.Length > 4 ? 28 : 36;
        entry.rect.anchoredPosition = localPos;
        entry.rect.gameObject.SetActive(true);
        entry.group.alpha = 1f;

        _active.Add(new FloatingEntry
        {
            Text = entry.text,
            Rect = entry.rect,
            Group = entry.group,
            StartPos = localPos,
            Timer = 0f
        });
    }

    public void SpawnScore(Vector3 worldPos, int amount)
    {
        SpawnAt(worldPos, $"+{amount}", new Color(0f, 1f, 0.8f));
    }

    public void SpawnCredits(Vector3 worldPos, int amount)
    {
        SpawnAt(worldPos, $"+{amount}¢", new Color(1f, 0.85f, 0.2f));
    }

    public void SpawnCombo(Vector3 worldPos, int multiplier)
    {
        SpawnAt(worldPos, $"x{multiplier}!", new Color(1f, 0.4f, 0.8f));
    }

    private void EnsureCanvas()
    {
        if (_canvas != null) return;

        GameObject canvasObj = new GameObject("FloatingTextCanvas");
        canvasObj.transform.SetParent(transform, false);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 8000;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        _font = Font.CreateDynamicFontFromOSFont("Arial", 14);

        for (int i = 0; i < PoolSize; i++)
        {
            _pool.Enqueue(CreatePooledText());
        }
    }

    private (Text text, RectTransform rect, CanvasGroup group) CreatePooledText()
    {
        GameObject obj = new GameObject("FloatText", typeof(RectTransform));
        obj.transform.SetParent(_canvas.transform, false);
        Text text = obj.AddComponent<Text>();
        text.font = _font;
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        // Outline for readability
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        CanvasGroup group = obj.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 50f);
        obj.SetActive(false);

        return (text, rect, group);
    }

    private (Text text, RectTransform rect, CanvasGroup group) Get()
    {
        if (_pool.Count > 0) return _pool.Dequeue();
        return CreatePooledText();
    }

    private void Release(FloatingEntry entry)
    {
        entry.Rect.gameObject.SetActive(false);
        _pool.Enqueue((entry.Text, entry.Rect, entry.Group));
    }
}
