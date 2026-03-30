using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Real-time FPS counter overlay. Toggled via SettingsManager.ShowFps.
/// </summary>
public sealed class FpsCounter : MonoBehaviour
{
    [SerializeField] private Text fpsText;

    private float _deltaTime;
    private float _updateInterval = 0.25f;
    private float _timer;

    public void Configure(Text text)
    {
        fpsText = text;
    }

    private void Update()
    {
        if (fpsText == null) return;

        bool show = SettingsManager.Instance != null && SettingsManager.Instance.ShowFps;
        fpsText.gameObject.SetActive(show);
        if (!show) return;

        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        _timer += Time.unscaledDeltaTime;

        if (_timer >= _updateInterval)
        {
            _timer = 0f;
            float fps = 1f / _deltaTime;
            Color color = fps >= 55f ? Color.green : fps >= 30f ? Color.yellow : Color.red;
            fpsText.color = color;
            fpsText.text = $"{fps:0} FPS";
        }
    }
}
