using UnityEngine;

[CreateAssetMenu(menuName = "Cyber Drift Runner/Config/Audio Style Profile", fileName = "AudioStyleProfile")]
public sealed class AudioStyleProfile : ScriptableObject
{
    [Header("Music")]
    [SerializeField] private AudioClip menuLoop;
    [SerializeField] private AudioClip gameplayLoop;
    [SerializeField] private AudioClip bossLoop;

    [Header("Gameplay Sfx")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip slideClip;
    [SerializeField] private AudioClip shootClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip powerUpClip;
    [SerializeField] private AudioClip hackClip;
    [SerializeField] private AudioClip reviveClip;
    [SerializeField] private AudioClip bossDefeatClip;

    public AudioClip MenuLoop => menuLoop;
    public AudioClip GameplayLoop => gameplayLoop;
    public AudioClip BossLoop => bossLoop;
    public AudioClip JumpClip => jumpClip;
    public AudioClip SlideClip => slideClip;
    public AudioClip ShootClip => shootClip;
    public AudioClip HitClip => hitClip;
    public AudioClip PowerUpClip => powerUpClip;
    public AudioClip HackClip => hackClip;
    public AudioClip ReviveClip => reviveClip;
    public AudioClip BossDefeatClip => bossDefeatClip;
}
