using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private bool sfxEnabled = true;
    [SerializeField] private float musicVolume = 0.25f;
    [SerializeField] private float sfxVolume = 0.5f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip gameplayLoop;
    private AudioClip menuLoop;
    private AudioClip jumpClip;
    private AudioClip slideClip;
    private AudioClip shootClip;
    private AudioClip hitClip;
    private AudioClip powerUpClip;
    private AudioClip hackClip;
    private AudioClip reviveClip;

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
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    public void PlayJump() => PlaySfx(jumpClip);
    public void PlaySlide() => PlaySfx(slideClip);
    public void PlayShoot() => PlaySfx(shootClip);
    public void PlayHit() => PlaySfx(hitClip);
    public void PlayPowerUp() => PlaySfx(powerUpClip);
    public void PlayHackPulse() => PlaySfx(hackClip);
    public void PlayRevive() => PlaySfx(reviveClip);

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

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!musicEnabled)
        {
            musicSource.Stop();
            return;
        }

        AudioClip nextClip = scene.name == SceneNames.GameScene ? gameplayLoop : menuLoop;
        if (musicSource.clip == nextClip && musicSource.isPlaying)
        {
            return;
        }

        musicSource.clip = nextClip;
        musicSource.Play();
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
        menuLoop = ProceduralAudioFactory.CreateMusicLoop("MenuLoop", 84f, 1.15f);
        gameplayLoop = ProceduralAudioFactory.CreateMusicLoop("GameplayLoop", 118f, 1.35f);
        jumpClip = ProceduralAudioFactory.CreateTone("Jump", 660f, 0.12f, 0.3f);
        slideClip = ProceduralAudioFactory.CreateTone("Slide", 220f, 0.08f, 0.18f);
        shootClip = ProceduralAudioFactory.CreateTone("Shoot", 820f, 0.07f, 0.25f);
        hitClip = ProceduralAudioFactory.CreateNoiseBurst("Hit", 0.18f, 0.2f);
        powerUpClip = ProceduralAudioFactory.CreateTone("PowerUp", 540f, 0.2f, 0.32f);
        hackClip = ProceduralAudioFactory.CreateTone("Hack", 420f, 0.09f, 0.18f);
        reviveClip = ProceduralAudioFactory.CreateTone("Revive", 720f, 0.24f, 0.28f);
    }
}
