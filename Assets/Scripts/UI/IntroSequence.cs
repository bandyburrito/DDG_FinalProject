using UnityEngine;

/// <summary>
/// Undertale-style typewriter intro. Reveals one character at a time, plays a
/// short click on every visible (non-whitespace) character, and waits on Space
/// between pages.
///
/// Controls:
///   Hold Space     → fast-forward the reveal (4× speed) while held.
///   Tap Space      → on a fully-revealed page, advance to the next page (or end).
///   Enter          → advance a completed page (explicit alternative to Space).
///   Escape         → skip the entire intro and jump straight to wave 1.
///
/// The click sound is procedurally generated on Awake so no .wav asset is needed.
/// Pitch is randomized ±10% per character to avoid the machine-gun monotone of a
/// fixed-pitch click loop — same trick Undertale uses.
/// </summary>
public class IntroSequence : MonoBehaviour
{
    public static IntroSequence Instance { get; private set; }

    // ── Intro narrative ──────────────────────────────────────────────────────
    // Short, punchy pages. Each page ends on a beat. Page count ≈ playtime in
    // pages × ~6 seconds (at 30 chars/sec) so the whole intro is well under a
    // minute at default speed.
    private readonly string[] _pages = new[]
    {
        "You don't remember the crash.\n\nOnly the cold light of the lab,\nthe straps at your wrists,\nand a voice in no tongue you knew\ncataloguing you as SPECIMEN-014.\n\nYou remembered your own name.\nShoki.",

        "Then the alarms began.\n\nSomething far worse than you\nhad broken loose below.\nThe straps slackened.\nThe door slid open.\n\nTen floors stand between you\nand the open sky.\n\nRun."
    };

    public int     PageIndex     { get; private set; }
    public int     CharsRevealed { get; private set; }
    public string  CurrentPage   => _pages[Mathf.Clamp(PageIndex, 0, _pages.Length - 1)];
    public bool    IsPageComplete => CharsRevealed >= CurrentPage.Length;
    public bool    IsFinished    => PageIndex >= _pages.Length;

    [Header("Pacing")]
    [Tooltip("Characters revealed per second at normal speed.")]
    public float charsPerSecond = 30f;
    [Tooltip("Multiplier applied while a fast-forward key is held.")]
    public float fastForwardMul = 4f;

    private float       _accum;
    private int         _lastSoundedChar = -1;

    // Audio: prefers a real keyboard-typing loop dropped at Resources/Audio/key_click.*
    // (e.g. the user-supplied mp3). Falls back to a per-character synthesized click if
    // the file is missing. The two modes work differently:
    //   • Loop mode  — long track loops continuously while text is revealing,
    //                  pauses when the page is complete (waiting for Space). One
    //                  AudioSource, Play/Pause, no per-char triggers.
    //   • Click mode — short procedural sample fired via PlayOneShot for every
    //                  visible non-whitespace character with pitch wobble.
    private AudioSource _src;
    private AudioClip   _clickClip;     // short sample (mp3 < 1s OR synth fallback)
    private AudioClip   _typingLoop;    // long sample (mp3 ≥ 1s) — continuous track
    private bool        _useLoopMode;   // chosen at Awake based on clip length

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        var userClip = Resources.Load<AudioClip>("Audio/key_click");

        _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake  = false;
        _src.spatialBlend = 0f;

        if (userClip != null && userClip.length >= 1.0f)
        {
            // Real keyboard-typing track — loop continuously while typing.
            _typingLoop = userClip;
            _src.clip   = _typingLoop;
            _src.loop   = true;
            _src.volume = 0.55f;
            _useLoopMode = true;
        }
        else
        {
            // Short sample (either user-supplied click < 1s, or none → synth).
            _clickClip   = userClip != null ? userClip : SynthesizeClickClip();
            _useLoopMode = false;
        }
    }

    /// <summary>Reset to page 0 and start the typewriter. Called by GameManager when
    /// entering GameState.Intro.</summary>
    public void Begin()
    {
        PageIndex        = 0;
        CharsRevealed    = 0;
        _accum           = 0f;
        _lastSoundedChar = -1;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        bool active = gm != null && gm.State == GameState.Intro && !IsFinished;

        // ALWAYS silence the loop source when we're not actively typing — covers
        // state-leaves, Esc-skips, and IntroSequence ticks racing past FinishIntro.
        // Otherwise a mid-typing skip would leave the typing track playing forever.
        if (_useLoopMode && !active && _src != null && _src.isPlaying) _src.Pause();

        if (!active) return;

        // ── Escape: hard-skip the whole intro ────────────────────────────────
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PageIndex = _pages.Length;
            gm.FinishIntro();
            return;
        }

        // ── Reveal next characters ───────────────────────────────────────────
        // HOLD Space → fast-forward the reveal. (A tap of Space on an already-complete
        // page advances — handled below — so holding speeds up, releasing+tapping turns
        // the page.) Shift/Z kept as an alias for accessibility but Space is the headline.
        bool fastForward = Input.GetKey(KeyCode.Space)
                        || Input.GetKey(KeyCode.LeftShift)
                        || Input.GetKey(KeyCode.Z);
        float rate       = charsPerSecond * (fastForward ? fastForwardMul : 1f);

        if (!IsPageComplete)
        {
            // Use unscaledDeltaTime so a paused timeScale doesn't freeze the typewriter
            // (Intro isn't pauseable, but defending against it costs nothing).
            _accum += Time.unscaledDeltaTime * rate;
            int step = Mathf.FloorToInt(_accum);
            if (step > 0)
            {
                _accum -= step;
                int newReveal = Mathf.Min(CurrentPage.Length, CharsRevealed + step);
                // In click mode: play one short sample per visible character.
                // In loop mode: the continuous track handles itself — toggled at the
                // end of Update based on whether we're still revealing.
                if (!_useLoopMode)
                {
                    for (int i = CharsRevealed; i < newReveal; i++)
                    {
                        if (i > _lastSoundedChar)
                        {
                            char c = CurrentPage[i];
                            if (!char.IsWhiteSpace(c)) PlayClick();
                            _lastSoundedChar = i;
                        }
                    }
                }
                CharsRevealed = newReveal;
            }
        }

        // Loop mode: continuous typing-track Play/Pause based on whether we're still
        // typing this page. Page-complete OR finished → pause; otherwise → play.
        if (_useLoopMode)
        {
            bool shouldPlay = !IsPageComplete && !IsFinished;
            if (shouldPlay && !_src.isPlaying) _src.Play();
            else if (!shouldPlay && _src.isPlaying) _src.Pause();
        }

        // ── Advance to next page ─────────────────────────────────────────────
        // Only fires on a fresh keydown AND when the page is fully revealed. While the
        // page is still typing, a held Space just fast-forwards (above) — it does NOT
        // snap-complete, so the player reads the text rather than skipping it by accident.
        // Enter also advances (explicit, doesn't double as fast-forward).
        bool advancePressed = Input.GetKeyDown(KeyCode.Return)
                           || (Input.GetKeyDown(KeyCode.Space) && IsPageComplete);
        if (advancePressed && IsPageComplete)
        {
            PageIndex++;
            CharsRevealed    = 0;
            _accum           = 0f;
            _lastSoundedChar = -1;
            if (IsFinished) gm.FinishIntro();
        }
    }

    private void PlayClick()
    {
        if (_src == null || _clickClip == null) return;
        _src.pitch = Random.Range(0.9f, 1.1f);
        _src.PlayOneShot(_clickClip, 0.5f);
    }

    /// <summary>
    /// Generates a 30 ms typewriter-key click: a fast white-noise burst layered
    /// with a brief 1.6 kHz sine, both shaped by a steep exponential envelope.
    /// Sounds like a soft mechanical key without needing a .wav asset.
    /// </summary>
    private static AudioClip SynthesizeClickClip()
    {
        const int sampleRate = 44100;
        int len = Mathf.RoundToInt(sampleRate * 0.03f);   // 30 ms
        var samples = new float[len];
        var rng = new System.Random();
        for (int i = 0; i < len; i++)
        {
            float t      = i / (float)sampleRate;
            float env    = Mathf.Exp(-t * 90f);
            float noise  = (float)(rng.NextDouble() * 2 - 1) * 0.7f;
            float tone   = Mathf.Sin(2f * Mathf.PI * 1600f * t) * 0.3f;
            samples[i]   = (noise + tone) * env * 0.5f;
        }
        var clip = AudioClip.Create("key_click", len, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
