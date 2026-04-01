using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("SFX Clips")]
    public AudioClip keyCorrectClip;
    public AudioClip keyWrongClip;
    public AudioClip combatWinClip;
    public AudioClip combatHitClip;
    public AudioClip playerHurtClip;
    public AudioClip footstepClip;
    public AudioClip floorChangeClip;

    [Header("Music Clips")]
    public AudioClip musicExplore;
    public AudioClip musicCombat;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        SongInputHandler.Instance.OnKeyCorrect += _ => PlaySFX(keyCorrectClip);
        SongInputHandler.Instance.OnFail       += () => PlaySFX(keyWrongClip);
        CombatManager.Instance.OnCombatEnd     += () => PlaySFX(combatWinClip);
        PlayerStats.Instance.OnStatsChanged    += OnStatsChanged;
        GameManager.Instance.OnStateChanged    += OnGameStateChanged;

        PlayMusic(musicExplore, true);
    }

    void OnStatsChanged()
    {
        // Footstep is played via GridMover, hurt sound triggered here on damage
    }

    void OnGameStateChanged(GameState state)
    {
        if (state == GameState.InCombat)  PlayMusic(musicCombat,  true);
        if (state == GameState.Exploring) PlayMusic(musicExplore, true);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip) sfxSource.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip clip, bool loop)
    {
        if (!clip || musicSource.clip == clip) return;
        musicSource.clip   = clip;
        musicSource.loop   = loop;
        musicSource.Play();
    }
}
