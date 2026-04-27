using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GhostFrame
{
    public float Time;
    public Vector3 Position;
}

[Serializable]
public sealed class GhostRunData
{
    public int Score;
    public float Distance;
    public float SurvivalTime;
    public GhostFrame[] Frames;
}

public sealed class GhostRunManager : MonoBehaviour
{
    private const string GhostSaveKey = "cdr.ghost.best";

    public static GhostRunManager Instance { get; private set; }

    [SerializeField] private float sampleInterval = 0.08f;
    [SerializeField] private Color ghostColor = new Color(0.18f, 0.9f, 1f, 0.35f);

    private readonly List<GhostFrame> _frames = new List<GhostFrame>(4096);
    private PlayerController _player;
    private GhostRunData _bestRun;
    private GhostPlayback _playback;
    private float _nextSampleAt;
    private bool _isRecording;

    public bool HasBestRun => _bestRun != null && _bestRun.Frames != null && _bestRun.Frames.Length > 1;
    public float BestGhostDistance => _bestRun != null ? _bestRun.Distance : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void Update()
    {
        if (!_isRecording || _player == null || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        float runTime = GameManager.Instance.SurvivalTime;
        if (runTime < _nextSampleAt)
        {
            return;
        }

        _nextSampleAt = runTime + sampleInterval;
        _frames.Add(new GhostFrame
        {
            Time = runTime,
            Position = _player.transform.position
        });
    }

    public void PrepareForNewRun()
    {
        _frames.Clear();
        _player = null;
        _isRecording = false;
        _nextSampleAt = 0f;
        DestroyPlayback();
    }

    public void BindPlayer(PlayerController player)
    {
        _player = player;
        _frames.Clear();
        _nextSampleAt = 0f;
        _isRecording = player != null;

        if (player != null && HasBestRun)
        {
            SpawnPlayback(player.transform.position);
        }
    }

    public void CompleteRun(RunSummary summary)
    {
        if (!_isRecording || _frames.Count < 10)
        {
            return;
        }

        _frames.Add(new GhostFrame
        {
            Time = summary.SurvivalTime,
            Position = _player != null ? _player.transform.position : _frames[_frames.Count - 1].Position
        });

        bool shouldReplace = _bestRun == null ||
                             summary.Distance > _bestRun.Distance + 0.5f ||
                             (Mathf.Abs(summary.Distance - _bestRun.Distance) <= 0.5f && summary.Score > _bestRun.Score);

        if (!shouldReplace)
        {
            _isRecording = false;
            return;
        }

        _bestRun = new GhostRunData
        {
            Score = summary.Score,
            Distance = summary.Distance,
            SurvivalTime = summary.SurvivalTime,
            Frames = _frames.ToArray()
        };

        Save();
        _isRecording = false;
    }

    public string ExportBestRunJson()
    {
        return _bestRun != null ? JsonUtility.ToJson(_bestRun) : string.Empty;
    }

    public void ImportBestRunJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        GhostRunData imported = JsonUtility.FromJson<GhostRunData>(json);
        if (imported == null || imported.Frames == null || imported.Frames.Length < 2)
        {
            return;
        }

        _bestRun = imported;
        Save();
    }

    private void SpawnPlayback(Vector3 playerPosition)
    {
        DestroyPlayback();

        GameObject ghostObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ghostObject.name = "BestRunGhost";
        ghostObject.transform.position = new Vector3(playerPosition.x, playerPosition.y + 0.05f, playerPosition.z);

        Collider ghostCollider = ghostObject.GetComponent<Collider>();
        if (ghostCollider != null)
        {
            Destroy(ghostCollider);
        }

        Renderer ghostRenderer = ghostObject.GetComponent<Renderer>();
        Material ghostMaterial = new Material(Shader.Find("Standard"));
        ghostMaterial.color = ghostColor;
        if (ghostMaterial.HasProperty("_Mode"))
        {
            ghostMaterial.SetFloat("_Mode", 3f);
        }
        ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        ghostMaterial.SetInt("_ZWrite", 0);
        ghostMaterial.DisableKeyword("_ALPHATEST_ON");
        ghostMaterial.EnableKeyword("_ALPHABLEND_ON");
        ghostMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        ghostMaterial.renderQueue = 3000;
        ghostRenderer.sharedMaterial = ghostMaterial;

        TrailRenderer trail = ghostObject.AddComponent<TrailRenderer>();
        trail.time = 0.35f;
        trail.startWidth = 0.35f;
        trail.endWidth = 0f;
        trail.material = ghostMaterial;
        trail.startColor = ghostColor;
        trail.endColor = new Color(ghostColor.r, ghostColor.g, ghostColor.b, 0f);

        _playback = ghostObject.AddComponent<GhostPlayback>();
        _playback.Configure(_bestRun);
    }

    private void DestroyPlayback()
    {
        if (_playback != null)
        {
            Destroy(_playback.gameObject);
            _playback = null;
        }
    }

    private void Save()
    {
        if (_bestRun == null)
        {
            return;
        }

        SecurePrefs.SetString(GhostSaveKey, JsonUtility.ToJson(_bestRun));
        SecurePrefs.Save();
    }

    private void Load()
    {
        string json = SecurePrefs.GetString(GhostSaveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        GhostRunData loaded = JsonUtility.FromJson<GhostRunData>(json);
        if (loaded != null && loaded.Frames != null && loaded.Frames.Length > 1)
        {
            _bestRun = loaded;
        }
    }
}

public sealed class GhostPlayback : MonoBehaviour
{
    private GhostRunData _run;
    private int _frameIndex;

    public void Configure(GhostRunData run)
    {
        _run = run;
        _frameIndex = 0;
        if (_run != null && _run.Frames != null && _run.Frames.Length > 0)
        {
            transform.position = _run.Frames[0].Position;
        }
    }

    private void Update()
    {
        if (_run == null || _run.Frames == null || _run.Frames.Length < 2 || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        float runTime = GameManager.Instance.SurvivalTime;
        while (_frameIndex < _run.Frames.Length - 2 && _run.Frames[_frameIndex + 1].Time < runTime)
        {
            _frameIndex++;
        }

        GhostFrame current = _run.Frames[_frameIndex];
        GhostFrame next = _run.Frames[Mathf.Min(_frameIndex + 1, _run.Frames.Length - 1)];
        float segmentDuration = Mathf.Max(0.0001f, next.Time - current.Time);
        float t = Mathf.Clamp01((runTime - current.Time) / segmentDuration);
        transform.position = Vector3.Lerp(current.Position, next.Position, t);
    }
}
