using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LimitedTimeEventSystem : MonoBehaviour
{
    public static LimitedTimeEventSystem Instance { get; private set; }

    public float ScoreMultiplier { get; private set; } = 1f;
    public float CreditMultiplier { get; private set; } = 1f;
    public string ActiveEventLabel { get; private set; } = "Standard Ops";
    public bool HasActiveEvent => ScoreMultiplier > 1.001f || CreditMultiplier > 1.001f;

    public event Action OnEventChanged;

    private float _nextRefreshAt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshNow();
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefreshAt)
        {
            return;
        }

        RefreshNow();
    }

    public void RefreshNow()
    {
        DateTime local = DateTime.Now;
        float scoreMultiplier = 1f;
        float creditMultiplier = 1f;
        List<string> labels = new List<string>(2);

        if (local.DayOfWeek == DayOfWeek.Saturday || local.DayOfWeek == DayOfWeek.Sunday)
        {
            creditMultiplier *= 1.5f;
            labels.Add("Weekend Neon Rush");
        }

        if (local.Hour >= 18 && local.Hour < 19)
        {
            scoreMultiplier *= 1.1f;
            creditMultiplier *= 2f;
            labels.Add("Happy Hour");
        }
        else if (local.Hour >= 22 && local.Hour < 23)
        {
            scoreMultiplier *= 1.2f;
            labels.Add("Late Shift");
        }

        bool changed = !Mathf.Approximately(ScoreMultiplier, scoreMultiplier) ||
                       !Mathf.Approximately(CreditMultiplier, creditMultiplier) ||
                       ActiveEventLabel != (labels.Count > 0 ? string.Join(" + ", labels) : "Standard Ops");

        ScoreMultiplier = scoreMultiplier;
        CreditMultiplier = creditMultiplier;
        ActiveEventLabel = labels.Count > 0 ? string.Join(" + ", labels) : "Standard Ops";
        _nextRefreshAt = Time.unscaledTime + 60f;

        if (changed)
        {
            OnEventChanged?.Invoke();
        }
    }
}
