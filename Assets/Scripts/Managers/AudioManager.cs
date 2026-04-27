using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioStyleProfile styleProfile;
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private bool sfxEnabled = true;
    [SerializeField] private float musicVolume = 0.25f;
    [SerializeField] private float sfxVolume = 0.5f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip gameplayLoop;
    private AudioClip menuLoop;
    private AudioClip bossLoop;
    private AudioClip jumpClip;
    private AudioClip slideClip;
    private AudioClip shootClip;
    private AudioClip hitClip;
    private AudioClip powerUpClip;
    private AudioClip hackClip;
    private AudioClip reviveClip;
    private AudioClip bossDefeatClip;
    private bool _loggedAuthoringFallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        musicSource = CreateSource("MusicSource", true, musicVolume);
        sfxSource = CreateSource("SfxSource", false, sfxVolume);
        CreateClips();
        LogAuthoringFallbacks();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnBossEncounterChanged += HandleBossEncounterChanged;
        }

        ApplySettings();
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnBossEncounterChanged -= HandleBossEncounterChanged;
            }
        }
    }

    public void PlayJump() => PlaySfx(jumpClip);
    public void PlaySlide() => PlaySfx(slideClip);
    public void PlayShoot() => PlaySfx(shootClip);
    public void PlayHit() => PlaySfx(hitClip);
    public void PlayPowerUp() => PlaySfx(powerUpClip);
    public void PlayHackPulse() => PlaySfx(hackClip);
    public void PlayRevive() => PlaySfx(reviveClip);
    public void PlayBossDefeated() => PlaySfx(bossDefeatClip);

    public void SetAudioEnabled(bool enabled)
    {
        musicEnabled = enabled;
        sfxEnabled = enabled;
        if (!musicEnabled)
        {
            musicSource.Stop();
            return;
        }

        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null) musicSource.volume = musicVolume;
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        if (!musicEnabled && musicSource != null)
        {
            musicSource.Stop();
        }
        else if (musicEnabled)
        {
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
    }

    public void SetSfxEnabled(bool enabled)
    {
        sfxEnabled = enabled;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAudioListener();
        ApplySettings();
        if (!musicEnabled)
        {
            musicSource.Stop();
            return;
        }

        AudioClip nextClip = ResolveMusicClip(scene.name);
        if (musicSource.clip == nextClip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = nextClip;
        musicSource.Play();
    }

    private AudioClip ResolveMusicClip(string sceneName)
    {
        if (sceneName != SceneNames.GameScene)
        {
            return menuLoop;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsBossEncounterActive && bossLoop != null)
        {
            return bossLoop;
        }

        return gameplayLoop;
    }

    private void HandleBossEncounterChanged(bool active, BossController boss)
    {
        if (SceneManager.GetActiveScene().name != SceneNames.GameScene || !musicEnabled)
        {
            return;
        }

        AudioClip nextClip = active ? (bossLoop != null ? bossLoop : gameplayLoop) : gameplayLoop;
        if (musicSource.clip == nextClip)
        {
            return;
        }

        musicSource.clip = nextClip;
        musicSource.Play();
    }

    private static void EnsureAudioListener()
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        if (cameras == null || cameras.Length == 0)
        {
            return;
        }

        AudioListener chosenListener = null;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null)
            {
                continue;
            }

            AudioListener listener = camera.GetComponent<AudioListener>();
            if (listener == null)
            {
                listener = camera.gameObject.AddComponent<AudioListener>();
            }

            bool shouldEnable = chosenListener == null && (camera.CompareTag("MainCamera") || camera == Camera.main || i == 0);
            listener.enabled = shouldEnable;
            if (shouldEnable)
            {
                chosenListener = listener;
            }
        }
    }


    private void ApplySettings()
    {
        if (SettingsManager.Instance == null)
        {
            return;
        }

        musicEnabled = SettingsManager.Instance.AudioEnabled;
        sfxEnabled = SettingsManager.Instance.AudioEnabled;
        musicVolume = SettingsManager.Instance.MusicVolume;
        sfxVolume = SettingsManager.Instance.SfxVolume;
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (!sfxEnabled || clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private AudioSource CreateSource(string sourceName, bool loop, float volume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = volume;
        return source;
    }

    private void CreateClips()
    {
        menuLoop = ResolveClip(styleProfile != null ? styleProfile.MenuLoop : null, () => ProceduralAudioFactory.CreateMusicLoop("MenuLoop", 84f, 1.15f));
        gameplayLoop = ResolveClip(styleProfile != null ? styleProfile.GameplayLoop : null, () => ProceduralAudioFactory.CreateMusicLoop("GameplayLoop", 118f, 1.35f));
        bossLoop = ResolveClip(styleProfile != null ? styleProfile.BossLoop : null, () => ProceduralAudioFactory.CreateMusicLoop("BossLoop", 140f, 1.2f));
        jumpClip = ResolveClip(styleProfile != null ? styleProfile.JumpClip : null, () => ProceduralAudioFactory.CreateTone("Jump", 660f, 0.12f, 0.3f));
        slideClip = ResolveClip(styleProfile != null ? styleProfile.SlideClip : null, () => ProceduralAudioFactory.CreateTone("Slide", 220f, 0.08f, 0.18f));
        shootClip = ResolveClip(styleProfile != null ? styleProfile.ShootClip : null, () => ProceduralAudioFactory.CreateTone("Shoot", 820f, 0.07f, 0.25f));
        hitClip = ResolveClip(styleProfile != null ? styleProfile.HitClip : null, () => ProceduralAudioFactory.CreateNoiseBurst("Hit", 0.18f, 0.2f));
        powerUpClip = ResolveClip(styleProfile != null ? styleProfile.PowerUpClip : null, () => ProceduralAudioFactory.CreateTone("PowerUp", 540f, 0.2f, 0.32f));
        hackClip = ResolveClip(styleProfile != null ? styleProfile.HackClip : null, () => ProceduralAudioFactory.CreateTone("Hack", 420f, 0.09f, 0.18f));
        reviveClip = ResolveClip(styleProfile != null ? styleProfile.ReviveClip : null, () => ProceduralAudioFactory.CreateTone("Revive", 720f, 0.24f, 0.28f));
        bossDefeatClip = ResolveClip(styleProfile != null ? styleProfile.BossDefeatClip : null, () => ProceduralAudioFactory.CreateNoiseBurst("BossDefeat", 0.24f, 0.32f));
    }

    private static AudioClip ResolveClip(AudioClip authoredClip, System.Func<AudioClip> fallbackFactory)
    {
        if (authoredClip != null)
        {
            return authoredClip;
        }

        return fallbackFactory();
    }

    private void LogAuthoringFallbacks()
    {
        if (_loggedAuthoringFallback || styleProfile == null)
        {
            if (!_loggedAuthoringFallback && styleProfile == null)
            {
                Debug.LogWarning("[AudioManager] AudioStyleProfile is missing. Procedural fallback audio is active.");
                _loggedAuthoringFallback = true;
            }

            return;
        }

        int missingCount = 0;
        missingCount += styleProfile.MenuLoop == null ? 1 : 0;
        missingCount += styleProfile.GameplayLoop == null ? 1 : 0;
        missingCount += styleProfile.BossLoop == null ? 1 : 0;
        missingCount += styleProfile.JumpClip == null ? 1 : 0;
        missingCount += styleProfile.SlideClip == null ? 1 : 0;
        missingCount += styleProfile.ShootClip == null ? 1 : 0;
        missingCount += styleProfile.HitClip == null ? 1 : 0;
        missingCount += styleProfile.PowerUpClip == null ? 1 : 0;
        missingCount += styleProfile.HackClip == null ? 1 : 0;
        missingCount += styleProfile.ReviveClip == null ? 1 : 0;
        missingCount += styleProfile.BossDefeatClip == null ? 1 : 0;
        if (missingCount > 0)
        {
            Debug.LogWarning($"[AudioManager] AudioStyleProfile is incomplete. Procedural fallback audio is active for {missingCount} clip slots.");
            _loggedAuthoringFallback = true;
        }
    }
}
