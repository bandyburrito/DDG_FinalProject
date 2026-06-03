using UnityEngine;

/// <summary>
/// State-driven background-music loop. Watches <see cref="GameManager.State"/> each
/// frame and swaps to the matching track when the state's "music bucket" changes.
///
/// Mapping:
///   MainMenu                → opening.wav
///   Combat                  → round.wav
///   Win / GameOver          → gamefinished.wav
///   WaveTransition,
///   UpgradeScreen,
///   CompanionScreen         → silence (the round track stops the moment combat
///                             ends, as requested — the breather between waves
///                             is intentionally quiet)
///
/// Switching always Stop()s the previous track and Play()s the new one from the top,
/// so each phase always opens on its loop's intro rather than mid-bar. Whenever a new
/// track starts it FADES IN from silence to the target volume over <see cref="FadeDuration"/>
/// seconds, so the round music swells up instead of slamming in at full volume. The pause
/// menu uses Pause/UnPause so resuming continues from the same position instead of
/// restarting the bar (less jarring than a hard cut).
///
/// Master volume comes from GameManager.MasterVolume via AudioListener.volume — we
/// don't multiply it onto the source ourselves to avoid double-application. The fade
/// rides the SOURCE volume (0 → 1), independent of the master listener volume.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    /// <summary>Seconds for a freshly-started track to swell from silence to full.</summary>
    public const float FadeDuration  = 2.2f;
    /// <summary>The "amount needed" — the source volume a fade resolves to.</summary>
    private const float TargetVolume = 1f;

    private AudioSource _source;
    private AudioClip _openingClip;
    private AudioClip _roundClip;
    private AudioClip _endClip;
    private AudioClip _currentClip;

    // Fade-in state. _fadeT counts up from 0 to FadeDuration after a track change.
    private float _fadeT = FadeDuration;   // start "complete" so nothing fades pre-play

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Loaded from Assets/Resources/Audio/*.wav at boot. Missing files log a warning
        // but don't crash — the source just stays silent for that bucket.
        _openingClip = Resources.Load<AudioClip>("Audio/opening");
        _roundClip   = Resources.Load<AudioClip>("Audio/round");
        _endClip     = Resources.Load<AudioClip>("Audio/gamefinished");

        if (_openingClip == null) Debug.LogWarning("[AudioManager] Missing Resources/Audio/opening.wav");
        if (_roundClip   == null) Debug.LogWarning("[AudioManager] Missing Resources/Audio/round.wav");
        if (_endClip     == null) Debug.LogWarning("[AudioManager] Missing Resources/Audio/gamefinished.wav");

        _source = gameObject.AddComponent<AudioSource>();
        _source.loop         = true;
        _source.playOnAwake  = false;
        _source.spatialBlend = 0f;     // 2D — full stereo, no falloff
        _source.volume       = TargetVolume;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var desired = ClipForState(gm.State);

        // 1) State-driven track switch — clean stop + restart, then fade in from silence.
        if (desired != _currentClip)
        {
            _currentClip = desired;
            _source.Stop();
            _source.clip = desired;
            if (desired != null)
            {
                _source.volume = 0f;   // begin silent; Update ramps it up over FadeDuration
                _fadeT         = 0f;
                _source.Play();
            }
        }

        // 2) Pause overlay (top-level pause menu). Pause/UnPause preserves playback
        // position so resuming doesn't restart the loop from zero.
        if (_currentClip == null) return;
        if (gm.IsPaused)
        {
            if (_source.isPlaying) _source.Pause();
            return;   // don't advance the fade while paused
        }
        if (!_source.isPlaying) _source.UnPause();

        // 3) Fade-in ramp. Uses unscaledDeltaTime so it's independent of any timeScale
        // changes; resolves to TargetVolume and then holds.
        if (_fadeT < FadeDuration)
        {
            _fadeT += Time.unscaledDeltaTime;
            _source.volume = Mathf.Lerp(0f, TargetVolume, Mathf.Clamp01(_fadeT / FadeDuration));
        }
    }

    private AudioClip ClipForState(GameState s) => s switch
    {
        // Opening track carries through the intro crawl so the typewriter clicks
        // sit on top of an atmospheric bed rather than dead silence.
        GameState.MainMenu or GameState.Loading or GameState.Intro => _openingClip,
        GameState.Combat                      => _roundClip,
        GameState.Win or GameState.GameOver   => _endClip,
        _                                     => null,
    };
}
