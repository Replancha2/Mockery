using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("SFX Clips")]
    public AudioClip[] sequenceStepClips;
    public AudioClip sequenceSuccessTuneClip;
    public AudioClip sequenceFailTuneClip;
    public AudioClip combatWinClip;
    public AudioClip combatHitClip;
    public AudioClip playerHurtClip;
    public AudioClip[] footstepClips;
    public AudioClip floorChangeClip;
    public AudioClip pageFlipClip;
    public AudioClip coinGainClip;
    public AudioClip purchaseClip;
    public AudioClip buffSelectionOpenClip;

    [Header("SFX Tuning")]
    [SerializeField, Range(0f, 0.25f)] private float randomPitchJitter = 0.06f;

    [Header("Music Clips")]
    public AudioClip musicExplore;
    public AudioClip musicCombat;
    public AudioClip musicBuffSelection;

    [Header("Per-Floor Explore Music")]
    [Tooltip("Index 0 = Floor 1, Index 1 = Floor 2, etc. Falls back to musicExplore when missing.")]
    public AudioClip[] floorExploreMusic;

    private SongInputHandler _boundSongInput;
    private CombatManager _boundCombatManager;
    private GameManager _boundGameManager;
    private FloorManager _boundFloorManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.AbsorbFrom(this);
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        TryBindRuntimeEvents();
        PlayExploreMusicForCurrentFloor();
    }

    void Update()
    {
        TryBindRuntimeEvents();
    }

    void OnDestroy()
    {
        UnbindRuntimeEvents();
    }

    void OnSequenceSuccess()
    {
        PlaySequenceSuccessTune();
    }

    void OnSequenceFail() => PlaySequenceFailTune();

    void OnCombatEnd() => PlayCombatWin();

    void OnGoldAdded(int amount)
    {
        if (amount > 0) PlayCoinGain();
    }

    void OnGoldSpent(int amount)
    {
        if (amount > 0) PlayPurchase();
    }

    void OnFloorChanged(int _)
    {
        PlaySFX(floorChangeClip);

        if (GameManager.Instance != null)
        {
            GameState state = GameManager.Instance.CurrentState;
            if (state == GameState.Exploring || state == GameState.Shopping)
                PlayExploreMusicForCurrentFloor();
        }
    }

    void OnGameStateChanged(GameState state)
    {
        if (state == GameState.BuffSelection)
        {
            // Buff selection is a dedicated modal moment: stop current ambience first,
            // then play an entry stinger and optional custom music.
            StopMusic();
            PlayBuffSelectionOpen();
            if (musicBuffSelection != null)
                PlayMusic(musicBuffSelection, true);
            return;
        }

        if (state == GameState.InCombat)
        {
            PlayMusic(musicCombat != null ? musicCombat : musicExplore, true);
            return;
        }

        if (state == GameState.Exploring || state == GameState.Shopping)
            PlayExploreMusicForCurrentFloor();
    }

    void PlayExploreMusicForCurrentFloor()
    {
        AudioClip clip = GetExploreMusicForCurrentFloor();
        PlayMusic(clip, true);
    }

    public void RefreshMusicForCurrentState()
    {
        if (GameManager.Instance == null)
        {
            PlayExploreMusicForCurrentFloor();
            return;
        }

        OnGameStateChanged(GameManager.Instance.CurrentState);
    }

    AudioClip GetExploreMusicForCurrentFloor()
    {
        int floor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;
        int index = Mathf.Max(0, floor - 1);

        if (floorExploreMusic != null && index < floorExploreMusic.Length && floorExploreMusic[index] != null)
            return floorExploreMusic[index];

        if (musicExplore != null) return musicExplore;
        if (musicCombat != null) return musicCombat;
        return musicBuffSelection;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;

        if (randomPitchJitter > 0f)
            sfxSource.pitch = 1f + Random.Range(-randomPitchJitter, randomPitchJitter);
        else
            sfxSource.pitch = 1f;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        sfxSource.pitch = 1f;
    }

    public void PlayRandomSFX(AudioClip[] clips, float volumeScale = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        int index = Random.Range(0, clips.Length);
        PlaySFX(clips[index], volumeScale);
    }

    public void PlayMusic(AudioClip clip, bool loop)
    {
        if (!clip || musicSource == null || musicSource.clip == clip) return;
        musicSource.clip   = clip;
        musicSource.loop   = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        if (!musicSource.isPlaying) return;
        musicSource.Stop();
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 worldPos, float volumeScale = 1f, float spatialBlend = 1f)
    {
        if (clip == null) return;

        GameObject go = new GameObject($"TempSFX_{clip.name}");
        go.transform.position = worldPos;

        AudioSource src = go.AddComponent<AudioSource>();
        src.spatialBlend = Mathf.Clamp01(spatialBlend);
        src.playOnAwake = false;
        src.volume = Mathf.Clamp01(volumeScale);
        src.clip = clip;
        src.Play();

        Destroy(go, clip.length + 0.1f);
    }

    public void PlayRandomFootstep() => PlayRandomSFX(footstepClips);

    public void PlayCoinGain() => PlaySFX(coinGainClip);

    public void PlayPurchase() => PlaySFX(purchaseClip);

    public void PlayBuffSelectionOpen() => PlaySFX(buffSelectionOpenClip);

    public void PlaySequenceSuccessTune() => PlaySFX(sequenceSuccessTuneClip);

    public void PlaySequenceFailTune() => PlaySFX(sequenceFailTuneClip);

    public void PlayCombatWin() => PlaySFX(combatWinClip);

    public void PlayRandomSequenceStep()
    {
        if (sequenceStepClips != null && sequenceStepClips.Length > 0)
        {
            PlayRandomSFX(sequenceStepClips);
            return;
        }

        // Fallback so sequence input still has audible feedback if list wasn't assigned yet.
        if (combatHitClip != null) PlaySFX(combatHitClip, 0.6f);
    }

    public void PlayEnemyAmbient(EnemyInstance enemy)
    {
        if (enemy == null || enemy.data == null) return;
        AudioClip[] clips = enemy.data.ambientClips;
        if (clips == null || clips.Length == 0) return;
        int index = Random.Range(0, clips.Length);
        PlaySFXAtPosition(clips[index], enemy.transform.position, 1f, 1f);
    }

    public void PlayEnemyAttack(EnemyInstance enemy)
    {
        if (enemy == null || enemy.data == null) return;
        if (enemy.data.attackClip != null)
            PlaySFXAtPosition(enemy.data.attackClip, enemy.transform.position, 1f, 1f);
        else
            PlaySFX(combatHitClip);
    }

    public void PlayEnemyDeath(EnemyInstance enemy)
    {
        if (enemy == null || enemy.data == null) return;
        if (enemy.data.deathClip != null)
            PlaySFXAtPosition(enemy.data.deathClip, enemy.transform.position, 1f, 1f);
    }

    public void PlayDragonElementShift(EnemyInstance enemy)
    {
        if (enemy == null || enemy.data == null) return;
        if (enemy.data.elementShiftClip != null)
            PlaySFXAtPosition(enemy.data.elementShiftClip, enemy.transform.position, 1f, 1f);
    }

    public void PlayPlayerHurt()
    {
        PlaySFX(playerHurtClip);
    }

    public void PlayBeggarStab(AudioClip customClip)
    {
        // Prefer NPC-specific clip from Beggar data; fallback keeps existing behavior stable.
        PlaySFX(customClip != null ? customClip : combatHitClip);
    }

    public void PlayPageFlip()
    {
        PlaySFX(pageFlipClip);
    }

    void TryBindRuntimeEvents()
    {
        if (_boundSongInput == null)
        {
            _boundSongInput = SongInputHandler.Instance != null ? SongInputHandler.Instance : FindFirstObjectByType<SongInputHandler>();
            if (_boundSongInput != null)
            {
                _boundSongInput.OnSuccess += OnSequenceSuccess;
                _boundSongInput.OnFail += OnSequenceFail;
            }
        }

        if (_boundCombatManager == null)
        {
            _boundCombatManager = CombatManager.Instance != null ? CombatManager.Instance : FindFirstObjectByType<CombatManager>();
            if (_boundCombatManager != null)
                _boundCombatManager.OnCombatEnd += OnCombatEnd;
        }

        if (_boundGameManager == null)
        {
            _boundGameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
            if (_boundGameManager != null)
            {
                _boundGameManager.OnStateChanged += OnGameStateChanged;
                OnGameStateChanged(_boundGameManager.CurrentState);
            }
        }

        if (_boundFloorManager == null)
        {
            _boundFloorManager = FloorManager.Instance != null ? FloorManager.Instance : FindFirstObjectByType<FloorManager>();
            if (_boundFloorManager != null)
            {
                _boundFloorManager.OnGoldAdded += OnGoldAdded;
                _boundFloorManager.OnGoldSpent += OnGoldSpent;
                _boundFloorManager.OnFloorChanged += OnFloorChanged;
            }
        }
    }

    void UnbindRuntimeEvents()
    {
        if (_boundSongInput != null)
        {
            _boundSongInput.OnSuccess -= OnSequenceSuccess;
            _boundSongInput.OnFail -= OnSequenceFail;
            _boundSongInput = null;
        }

        if (_boundCombatManager != null)
        {
            _boundCombatManager.OnCombatEnd -= OnCombatEnd;
            _boundCombatManager = null;
        }

        if (_boundGameManager != null)
        {
            _boundGameManager.OnStateChanged -= OnGameStateChanged;
            _boundGameManager = null;
        }

        if (_boundFloorManager != null)
        {
            _boundFloorManager.OnGoldAdded -= OnGoldAdded;
            _boundFloorManager.OnGoldSpent -= OnGoldSpent;
            _boundFloorManager.OnFloorChanged -= OnFloorChanged;
            _boundFloorManager = null;
        }
    }

    void AbsorbFrom(AudioManager other)
    {
        if (other == null) return;

        if (sfxSource == null && other.sfxSource != null) sfxSource = other.sfxSource;
        if (musicSource == null && other.musicSource != null) musicSource = other.musicSource;

        if ((sequenceStepClips == null || sequenceStepClips.Length == 0) && other.sequenceStepClips != null && other.sequenceStepClips.Length > 0) sequenceStepClips = other.sequenceStepClips;
        if (sequenceSuccessTuneClip == null && other.sequenceSuccessTuneClip != null) sequenceSuccessTuneClip = other.sequenceSuccessTuneClip;
        if (sequenceFailTuneClip == null && other.sequenceFailTuneClip != null) sequenceFailTuneClip = other.sequenceFailTuneClip;
        if (combatWinClip == null && other.combatWinClip != null) combatWinClip = other.combatWinClip;
        if (combatHitClip == null && other.combatHitClip != null) combatHitClip = other.combatHitClip;
        if (playerHurtClip == null && other.playerHurtClip != null) playerHurtClip = other.playerHurtClip;
        if ((footstepClips == null || footstepClips.Length == 0) && other.footstepClips != null && other.footstepClips.Length > 0) footstepClips = other.footstepClips;
        if (floorChangeClip == null && other.floorChangeClip != null) floorChangeClip = other.floorChangeClip;
        if (pageFlipClip == null && other.pageFlipClip != null) pageFlipClip = other.pageFlipClip;
        if (coinGainClip == null && other.coinGainClip != null) coinGainClip = other.coinGainClip;
        if (purchaseClip == null && other.purchaseClip != null) purchaseClip = other.purchaseClip;
        if (buffSelectionOpenClip == null && other.buffSelectionOpenClip != null) buffSelectionOpenClip = other.buffSelectionOpenClip;

        if (musicExplore == null && other.musicExplore != null) musicExplore = other.musicExplore;
        if (musicCombat == null && other.musicCombat != null) musicCombat = other.musicCombat;
        if (musicBuffSelection == null && other.musicBuffSelection != null) musicBuffSelection = other.musicBuffSelection;
        if ((floorExploreMusic == null || floorExploreMusic.Length == 0) && other.floorExploreMusic != null && other.floorExploreMusic.Length > 0) floorExploreMusic = other.floorExploreMusic;
    }
}
