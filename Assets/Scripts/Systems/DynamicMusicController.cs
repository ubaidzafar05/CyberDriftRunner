using UnityEngine;

public sealed class DynamicMusicController : MonoBehaviour
{
    [Header("Music Layers")]
    [SerializeField] private AudioSource baseLayer;
    [SerializeField] private AudioSource actionLayer;
    [SerializeField] private AudioSource dangerLayer;
    [SerializeField] private AudioSource bossLayer;

    [Header("Thresholds")]
    [SerializeField] private float actionSpeedThreshold = 12f;
    [SerializeField] private float dangerSpeedThreshold = 18f;
    [SerializeField] private float bossDistanceThreshold = 5000f;

    [Header("Crossfade")]
    [SerializeField] private float crossfadeSpeed = 2f;

    private float _actionTarget;
    private float _dangerTarget;
    private float _bossTarget;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            FadeAllToZero();
            return;
        }

        float speed = GameManager.Instance.CurrentForwardSpeed;
        float distance = GameManager.Instance.Distance;

        _actionTarget = speed >= actionSpeedThreshold ? 1f : 0f;
        _dangerTarget = speed >= dangerSpeedThreshold ? 1f : 0f;
        _bossTarget = distance >= bossDistanceThreshold ? 1f : 0f;

        FadeLayer(baseLayer, 1f);
        FadeLayer(actionLayer, _actionTarget);
        FadeLayer(dangerLayer, _dangerTarget);
        FadeLayer(bossLayer, _bossTarget);
    }

    private void FadeLayer(AudioSource source, float targetVolume)
    {
        if (source == null)
        {
            return;
        }

        source.volume = Mathf.MoveTowards(source.volume, targetVolume, crossfadeSpeed * Time.unscaledDeltaTime);

        if (!source.isPlaying && targetVolume > 0f)
        {
            source.Play();
        }
    }

    private void FadeAllToZero()
    {
        FadeLayer(baseLayer, 0f);
        FadeLayer(actionLayer, 0f);
        FadeLayer(dangerLayer, 0f);
        FadeLayer(bossLayer, 0f);
    }

    public void SetupProceduralLayers()
    {
        if (baseLayer == null)
        {
            baseLayer = CreateLayer("BaseLayer", 220, 0.5f, 0.4f);
        }

        if (actionLayer == null)
        {
            actionLayer = CreateLayer("ActionLayer", 330, 0.3f, 0f);
        }

        if (dangerLayer == null)
        {
            dangerLayer = CreateLayer("DangerLayer", 440, 0.2f, 0f);
        }

        if (bossLayer == null)
        {
            bossLayer = CreateLayer("BossLayer", 165, 0.6f, 0f);
        }
    }

    private AudioSource CreateLayer(string layerName, float frequency, float loopDuration, float startVolume)
    {
        GameObject layerObject = new GameObject(layerName);
        layerObject.transform.SetParent(transform, false);
        AudioSource source = layerObject.AddComponent<AudioSource>();

        AudioClip clip = ProceduralAudioFactory.CreateTone(layerName, frequency, loopDuration, 0.4f);
        source.clip = clip;
        source.loop = true;
        source.volume = startVolume;
        source.playOnAwake = false;
        source.spatialBlend = 0f;

        if (startVolume > 0f)
        {
            source.Play();
        }

        return source;
    }
}
