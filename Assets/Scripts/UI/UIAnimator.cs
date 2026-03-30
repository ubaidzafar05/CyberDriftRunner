using UnityEngine;
using UnityEngine.UI;

public sealed class UIAnimator : MonoBehaviour
{
    public static UIAnimator Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PunchScale(Transform target, float punchAmount = 1.3f, float duration = 0.2f)
    {
        if (target == null)
        {
            return;
        }

        StartCoroutine(PunchScaleRoutine(target, punchAmount, duration));
    }

    public void SlideIn(RectTransform target, Vector2 fromOffset, float duration = 0.3f)
    {
        if (target == null)
        {
            return;
        }

        StartCoroutine(SlideInRoutine(target, fromOffset, duration));
    }

    public void FadeIn(CanvasGroup group, float duration = 0.3f)
    {
        if (group == null)
        {
            return;
        }

        StartCoroutine(FadeRoutine(group, 0f, 1f, duration));
    }

    public void FadeOut(CanvasGroup group, float duration = 0.3f)
    {
        if (group == null)
        {
            return;
        }

        StartCoroutine(FadeRoutine(group, 1f, 0f, duration));
    }

    public void FlashColor(Text text, Color flashColor, float duration = 0.3f)
    {
        if (text == null)
        {
            return;
        }

        StartCoroutine(FlashColorRoutine(text, flashColor, duration));
    }

    private System.Collections.IEnumerator PunchScaleRoutine(Transform target, float punchAmount, float duration)
    {
        Vector3 original = target.localScale;
        Vector3 punched = original * punchAmount;
        float elapsed = 0f;
        float halfDuration = duration * 0.5f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(original, punched, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(punched, original, t);
            yield return null;
        }

        target.localScale = original;
    }

    private System.Collections.IEnumerator SlideInRoutine(RectTransform target, Vector2 fromOffset, float duration)
    {
        Vector2 finalPos = target.anchoredPosition;
        Vector2 startPos = finalPos + fromOffset;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.anchoredPosition = Vector2.Lerp(startPos, finalPos, t);
            yield return null;
        }

        target.anchoredPosition = finalPos;
    }

    private System.Collections.IEnumerator FadeRoutine(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }

    private System.Collections.IEnumerator FlashColorRoutine(Text text, Color flashColor, float duration)
    {
        Color original = text.color;
        text.color = flashColor;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            text.color = Color.Lerp(flashColor, original, elapsed / duration);
            yield return null;
        }

        text.color = original;
    }
}
