using UnityEngine;

public class PlaceholderUI : MonoBehaviour
{
    private GUIStyle _titleStyle;
    private GUIStyle _btnStyle;
    private GUIStyle _centerLabel;
    private GUIStyle _introBodyStyle;

    // Main menu specific — Figma mockup styling (left-aligned, large title, hover-fill buttons)
    private GUIStyle _menuTitleStyle;
    private GUIStyle _menuSubtitleStyle;
    private GUIStyle _menuButtonStyle;
    private GUIStyle _menuSmallButtonStyle;
    private GUIStyle _panelButtonStyle;
    private GUIStyle _loadingStyle;

    // Settings card styles
    private GUIStyle _menuSubtitleHeading;
    private GUIStyle _settingsLabel;
    private GUIStyle _settingsValueRight;

    // In-combat HUD styles (JetBrains Mono + palette)
    private GUIStyle _hudLabel;
    private GUIStyle _hudLabelRight;

    // Cached 1×1 solid-colour textures used for backgrounds / button hover fills.
    // Generated once and re-used — much cheaper than calling GUI.DrawTexture with
    // Texture2D.whiteTexture + a per-draw GUI.color change.
    private Texture2D _texHover;
    private Texture2D _texPanel;
    private Texture2D _texTransparent;

    // Shoki portrait for the main-menu hero image. Loaded lazily on first paint.
    private Texture2D _shokiPortrait;

    private bool _stylesInit = false;

    /// <summary>Settings panel is an overlay shown from either the main menu or the pause menu.</summary>
    private bool _showSettings = false;

    /// <summary>State at the previous frame's tick — used to detect that Esc this
    /// frame belongs to IntroSequence even if IntroSequence ran first and already
    /// advanced the state out of Intro before our Update.</summary>
    private GameState _prevState;

    void Update()
    {
        var gm = GameManager.Instance;
        var nowState = gm?.State ?? GameState.MainMenu;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Escape closes the settings overlay first.
            if (_showSettings) { _showSettings = false; return; }
            // Esc during (or just-exited) Intro belongs to IntroSequence as a skip,
            // not to the pause menu. The OR-check survives either Update order.
            if (nowState == GameState.Intro || _prevState == GameState.Intro)
            {
                _prevState = nowState;
                return;
            }
            gm?.TogglePause();
        }

        _prevState = nowState;
    }

    void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        // Make JetBrains Mono the default font for every default GUI.Label/Button call,
        // including the inline HUD labels that don't pass an explicit style. We set the
        // skin-level default AND each common sub-style explicitly because Unity's GUI
        // skin inheritance can be flaky — sub-styles sometimes hold their own font ref.
        var regular = FontLoader.Regular;
        var bold    = FontLoader.Bold;
        if (regular != null)
        {
            GUI.skin.font          = regular;
            GUI.skin.label.font    = regular;
            GUI.skin.button.font   = regular;
            GUI.skin.toggle.font   = regular;
            GUI.skin.textField.font = regular;
            GUI.skin.box.font      = regular;
        }

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            font      = bold,
            fontSize  = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _titleStyle.normal.textColor = Palette.Text;

        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            font      = regular,
            fontSize  = 16
        };

        _centerLabel = new GUIStyle(GUI.skin.label)
        {
            font      = bold,
            fontSize  = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _centerLabel.normal.textColor = Palette.Text;

        // HUD labels — left/right aligned, JetBrains Mono, palette text.
        _hudLabel = new GUIStyle(GUI.skin.label)
        {
            font      = regular,
            fontSize  = 15,
            alignment = TextAnchor.MiddleLeft
        };
        _hudLabel.normal.textColor = Palette.Text;

        _hudLabelRight = new GUIStyle(_hudLabel) { alignment = TextAnchor.MiddleRight };

        // Intro narrative body — bigger, soft-white, top-anchored center, wraps lines.
        _introBodyStyle = new GUIStyle(GUI.skin.label)
        {
            font      = regular,
            fontSize  = 22,
            alignment = TextAnchor.UpperCenter,
            wordWrap  = true,
            richText  = false
        };
        _introBodyStyle.normal.textColor = Palette.Text;

        // ── 1×1 background textures for the hover-fill button trick ──────────
        _texHover       = MakeSolidTex(Palette.BgHover);
        _texPanel       = MakeSolidTex(Palette.BgPanel);
        _texTransparent = MakeSolidTex(new Color(0, 0, 0, 0));

        // ── Main menu styles (Figma mockup: left-aligned, monospace, hover fill) ──
        _menuTitleStyle = new GUIStyle(GUI.skin.label)
        {
            font      = bold,
            fontSize  = 56,
            alignment = TextAnchor.UpperLeft,
            fontStyle = FontStyle.Bold,
            padding   = new RectOffset(0, 0, 0, 0)
        };
        _menuTitleStyle.normal.textColor = Palette.Text;

        _menuSubtitleStyle = new GUIStyle(GUI.skin.label)
        {
            font      = regular,
            fontSize  = 18,
            alignment = TextAnchor.UpperLeft
        };
        _menuSubtitleStyle.normal.textColor = Palette.TextMute;

        // Hover-fill button — uses GUI.Button (gets free hover state tracking via
        // control IDs) with custom background textures. Normal is fully transparent,
        // hover swaps in the BgHover fill. No border so it reads as a clean block.
        _menuButtonStyle = new GUIStyle(GUI.skin.button)
        {
            font      = regular,
            fontSize  = 30,
            alignment = TextAnchor.MiddleLeft,
            padding   = new RectOffset(20, 20, 6, 6),
            border    = new RectOffset(0, 0, 0, 0),
            margin    = new RectOffset(0, 0, 0, 0),
            fixedHeight = 0
        };
        _menuButtonStyle.normal.background = _texTransparent;
        _menuButtonStyle.normal.textColor  = Palette.Text;
        _menuButtonStyle.hover.background  = _texHover;
        _menuButtonStyle.hover.textColor   = Palette.Text;
        _menuButtonStyle.active.background = _texHover;
        _menuButtonStyle.active.textColor  = Palette.AccentYellow;
        _menuButtonStyle.focused.background = _texTransparent;
        _menuButtonStyle.focused.textColor = Palette.Text;

        // Smaller variant used for the Settings link in the bottom corner of the menu.
        _menuSmallButtonStyle = new GUIStyle(_menuButtonStyle)
        {
            fontSize  = 16,
            padding   = new RectOffset(14, 14, 4, 4)
        };
        _menuSmallButtonStyle.normal.textColor = Palette.TextMute;

        // Centered hover-fill button for overlay panels (pause / upgrade / companion /
        // end). Unlike the menu buttons these have a visible BgPanel base so they read
        // as buttons sitting on the dimmed overlay, and brighten to BgHover on hover.
        _panelButtonStyle = new GUIStyle(GUI.skin.button)
        {
            font      = regular,
            fontSize  = 18,
            alignment = TextAnchor.MiddleCenter,
            padding   = new RectOffset(12, 12, 8, 8),
            border    = new RectOffset(0, 0, 0, 0),
            margin    = new RectOffset(0, 0, 0, 0),
            wordWrap  = true
        };
        _panelButtonStyle.normal.background  = _texPanel;
        _panelButtonStyle.normal.textColor   = Palette.Text;
        _panelButtonStyle.hover.background    = _texHover;
        _panelButtonStyle.hover.textColor     = Palette.Text;
        _panelButtonStyle.active.background   = _texHover;
        _panelButtonStyle.active.textColor    = Palette.AccentYellow;
        _panelButtonStyle.focused.background  = _texPanel;
        _panelButtonStyle.focused.textColor   = Palette.Text;

        _loadingStyle = new GUIStyle(GUI.skin.label)
        {
            font      = bold,
            fontSize  = 28,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        _loadingStyle.normal.textColor = Palette.Text;

        // ── Settings card styles ─────────────────────────────────────────────
        _menuSubtitleHeading = new GUIStyle(GUI.skin.label)
        {
            font      = bold,
            fontSize  = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        _menuSubtitleHeading.normal.textColor = Palette.Text;

        _settingsLabel = new GUIStyle(GUI.skin.label)
        {
            font      = regular,
            fontSize  = 17,
            alignment = TextAnchor.MiddleLeft
        };
        _settingsLabel.normal.textColor = Palette.Text;

        _settingsValueRight = new GUIStyle(GUI.skin.label)
        {
            font      = regular,
            fontSize  = 17,
            alignment = TextAnchor.MiddleRight
        };
        _settingsValueRight.normal.textColor = Palette.TextMute;
    }

    private static Texture2D MakeSolidTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    private Texture2D GetShokiPortrait()
    {
        if (_shokiPortrait == null)
        {
            var sprite = SpriteLoader.LoadEntity("shoki");
            if (sprite != null) _shokiPortrait = sprite.texture;
        }
        return _shokiPortrait;
    }

    void OnGUI()
    {
        InitStyles();
        var gm    = GameManager.Instance;
        var state = gm?.State ?? GameState.MainMenu;

        if (state == GameState.MainMenu)
        {
            DrawMainMenu();
        }
        else if (state == GameState.Loading)
        {
            DrawLoading();
        }
        else if (state == GameState.Intro)
        {
            DrawIntro();
        }
        else
        {
            if (state == GameState.Combat || state == GameState.WaveTransition) DrawHUD();
            if (state == GameState.UpgradeScreen) DrawUpgradeScreen();
            if (state == GameState.CompanionScreen) DrawCompanionScreen();
            if (state == GameState.Win) DrawEndScreen("Escaped!", Palette.Success);
            if (state == GameState.GameOver) DrawEndScreen($"Captured on Wave {gm.CurrentWave}", Palette.Danger);

            if (gm != null && gm.IsPaused) DrawPauseMenu();
        }

        // Settings overlays everything (reachable from main menu and pause menu).
        if (_showSettings) DrawSettings();
    }

    // ── Menus ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Main menu — Figma-mockup layout: title and stacked Play/Quit buttons on the
    /// LEFT, Shoki portrait on the RIGHT, palette-grounded background. Buttons use
    /// GUIStyle.hover.background for the highlight box (free hover tracking via IMGUI
    /// control IDs). A small Settings button sits at the bottom-left so the option
    /// stays reachable from the menu without competing with Play/Quit visually.
    /// </summary>
    void DrawMainMenu()
    {
        float sw = Screen.width, sh = Screen.height;

        // ── Background ───────────────────────────────────────────────────────
        GUI.color = Palette.BgMain;
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ── Shoki portrait — drawn first so the left text overlays cleanly ───
        var portrait = GetShokiPortrait();
        if (portrait != null)
        {
            float pH = sh * 0.7f;
            float pW = pH;                                  // square source PNG
            float pX = sw - pW - sw * 0.05f;                // right edge inset
            float pY = (sh - pH) * 0.5f;
            // ScaleMode.ScaleToFit + point filtering keeps the pixel art crisp at any size
            GUI.DrawTexture(new Rect(pX, pY, pW, pH), portrait, ScaleMode.ScaleToFit, true);
        }

        // ── Left column: title + subtitle + buttons ─────────────────────────
        float padX  = sw * 0.06f;
        float colW  = sw * 0.55f;
        float titleY = sh * 0.16f;

        GUI.Label(new Rect(padX, titleY, colW, 80), "Shoki's Adventure", _menuTitleStyle);
        GUI.Label(new Rect(padX, titleY + 78, colW, 28),
            "a tactical escape through 10 floors", _menuSubtitleStyle);

        // Stacked buttons — generous height + left-aligned label so the hover
        // fill reads as a "row" rather than a centered pill (matches the mockup).
        float btnW = sw * 0.28f;
        float btnH = 56;
        float btnGap = 18;   // slightly more breathing room between Play / Quit
        float btnY  = titleY + 150;

        if (GUI.Button(new Rect(padX, btnY, btnW, btnH), "Play", _menuButtonStyle))
            GameManager.Instance.StartNewGame();

        if (GUI.Button(new Rect(padX, btnY + (btnH + btnGap), btnW, btnH), "Quit", _menuButtonStyle))
            GameManager.Instance.QuitGame();

        // Settings — small, muted, in the bottom-left (where the placeholder box
        // sat in your Figma mockup). Doesn't compete with Play/Quit for attention
        // but stays one click away from the menu.
        float setW = 200, setH = 38;
        if (GUI.Button(new Rect(padX, sh - setH - sh * 0.06f, setW, setH),
                        "Settings", _menuSmallButtonStyle))
        {
            _showSettings = true;
        }
    }

    /// <summary>
    /// Brief transition screen between the player clicking Play and the intro crawl
    /// starting. Lives for ~1.4 s (driven by GameManager.LoadingThenIntro). Just a
    /// centered "Loading…" with a three-dot pulse so it doesn't look frozen.
    /// </summary>
    void DrawLoading()
    {
        float sw = Screen.width, sh = Screen.height;

        GUI.color = Palette.BgMain;
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Animated dots so it never looks frozen — three positions over ~0.9 s.
        int dots = Mathf.FloorToInt(Time.unscaledTime * 3f) % 4;   // 0..3
        string msg = "Loading" + new string('.', dots);

        GUI.Label(new Rect(0, sh * 0.45f, sw, 60), msg, _loadingStyle);

        GUI.contentColor = Palette.TextMute;
        GUI.Label(new Rect(0, sh * 0.55f, sw, 24),
            "preparing the facility…", _centerLabel);
        GUI.contentColor = Color.white;
    }

    /// <summary>
    /// Undertale-style typewriter intro. IntroSequence (separate component) drives the
    /// character index — this method just renders the currently-revealed substring with
    /// a footer hint that reflects whether the page is still typing or fully shown.
    /// </summary>
    void DrawIntro()
    {
        float sw = Screen.width, sh = Screen.height;

        // Pure black backdrop — Undertale-style, maximum focus on the typed text.
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var seq = IntroSequence.Instance;
        if (seq == null) return;

        // Body — currently-revealed substring of the active page, centered.
        string visible = seq.CurrentPage.Substring(0, Mathf.Min(seq.CharsRevealed, seq.CurrentPage.Length));
        GUI.Label(new Rect(sw * 0.1f, sh * 0.30f, sw * 0.8f, sh * 0.45f), visible, _introBodyStyle);

        // Prompt appears ONLY once the page has finished typing — keeps the screen
        // clean text-on-black while revealing, then a gentle pulsing cue to advance.
        if (seq.IsPageComplete)
        {
            float pulse = 0.35f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f);
            GUI.contentColor = new Color(Palette.TextMute.r, Palette.TextMute.g, Palette.TextMute.b, pulse);
            GUI.Label(new Rect(0, sh - 56, sw, 22), "[ space ]", _centerLabel);
            GUI.contentColor = Color.white;
        }
    }

    /// <summary>Dimmed palette backdrop shared by the overlay screens (pause / upgrade /
    /// companion / end) so they all read as the same world rather than flat black.</summary>
    void DrawDimOverlay(float alpha)
    {
        GUI.color = new Color(Palette.BgDeep.r, Palette.BgDeep.g, Palette.BgDeep.b, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawPauseMenu()
    {
        float sw = Screen.width, sh = Screen.height;
        DrawDimOverlay(0.85f);

        GUI.Label(new Rect(0, sh * 0.18f, sw, 60), "Paused", _titleStyle);

        float bw = 220, bh = 48, bx = (sw - bw) * 0.5f;
        float by = sh * 0.36f;

        if (GUI.Button(new Rect(bx, by, bw, bh), "Resume", _panelButtonStyle))
            GameManager.Instance.SetPaused(false);

        if (GUI.Button(new Rect(bx, by + 60, bw, bh), "Settings", _panelButtonStyle))
            _showSettings = true;

        if (GUI.Button(new Rect(bx, by + 120, bw, bh), "Restart Run", _panelButtonStyle))
            GameManager.Instance.RestartGame();

        if (GUI.Button(new Rect(bx, by + 180, bw, bh), "Main Menu", _panelButtonStyle))
            GameManager.Instance.ReturnToMainMenu();

        if (GUI.Button(new Rect(bx, by + 240, bw, bh), "Quit", _panelButtonStyle))
            GameManager.Instance.QuitGame();
    }

    /// <summary>
    /// Settings overlay — restyled to match the menu/intro theme: dimmed palette
    /// backdrop, a framed BgPanel card, JetBrains Mono labels in palette colours, and
    /// the same hover-fill buttons as the main menu. Reachable from both the main menu
    /// and the pause menu.
    /// </summary>
    void DrawSettings()
    {
        float sw = Screen.width, sh = Screen.height;

        // Dimmed palette backdrop (not flat black) so it reads as the same world.
        GUI.color = new Color(Palette.BgDeep.r, Palette.BgDeep.g, Palette.BgDeep.b, 0.94f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ── Centered card ────────────────────────────────────────────────────
        float panelW = 460, panelH = 360;
        float px = (sw - panelW) * 0.5f;
        float py = (sh - panelH) * 0.5f;

        // Card fill + a 2px accent top border for a touch of polish.
        GUI.color = Palette.BgPanel;
        GUI.DrawTexture(new Rect(px, py, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Palette.AccentCyan;
        GUI.DrawTexture(new Rect(px, py, panelW, 3), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var gm = GameManager.Instance;
        float innerX = px + 40;
        float innerW = panelW - 80;
        float y = py + 28;

        // Title (left-aligned inside the card) — styles carry their own palette colours.
        GUI.Label(new Rect(innerX, y, innerW, 40), "Settings", _menuSubtitleHeading);
        y += 64;

        // ── Master volume ────────────────────────────────────────────────────
        float vol = gm != null ? gm.MasterVolume : 1f;
        GUI.Label(new Rect(innerX, y, innerW, 24), "Master Volume", _settingsLabel);
        GUI.Label(new Rect(innerX, y, innerW, 24), $"{Mathf.RoundToInt(vol * 100)}%",
                  _settingsValueRight);
        float newVol = GUI.HorizontalSlider(new Rect(innerX, y + 30, innerW, 20), vol, 0f, 1f);
        if (gm != null && !Mathf.Approximately(newVol, vol)) gm.SetMasterVolume(newVol);
        y += 74;

        // ── Fullscreen toggle ────────────────────────────────────────────────
        bool fs = Screen.fullScreen;
        GUI.Label(new Rect(innerX, y, innerW - 60, 24), "Fullscreen", _settingsLabel);
        // Render the toggle as a right-aligned [ ON ] / [ OFF ] hover button instead of
        // the default checkbox — fits the monospace theme better.
        string toggleLabel = fs ? "[ ON ]" : "[ OFF ]";
        if (GUI.Button(new Rect(innerX + innerW - 90, y - 6, 90, 34), toggleLabel, _menuSmallButtonStyle))
        {
            if (gm != null) gm.SetFullscreen(!fs);
        }
        y += 64;

        // ── Back ─────────────────────────────────────────────────────────────
        if (GUI.Button(new Rect(innerX, py + panelH - 64, 160, 44), "Back", _menuButtonStyle))
            _showSettings = false;
    }

    void DrawHUD()
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        float sw = Screen.width;

        // HP (top left)
        GUI.Label(new Rect(10, 10, 200, 22), $"HP: {player.CurrentHP} / {player.maxHP}", _hudLabel);
        float ratio = (float)player.CurrentHP / player.maxHP;
        // Track in deep-panel colour; fill shifts Success → AccentYellow → Danger by ratio.
        GUI.color = Palette.BgDeep;
        GUI.DrawTexture(new Rect(10, 35, 200, 14), Texture2D.whiteTexture);
        GUI.color = ratio > 0.6f ? Palette.Success
                  : ratio > 0.3f ? Palette.AccentYellow
                                 : Palette.Danger;
        GUI.DrawTexture(new Rect(11, 36, 198 * ratio, 12), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Wave (top right)
        GUI.Label(new Rect(sw - 210, 10, 200, 22),
            $"Wave {GameManager.Instance.CurrentWave} / {GameManager.Instance.totalWaves}", _hudLabelRight);

        // Companions (top right, below wave)
        var comps = GameManager.Instance.ActiveCompanions;
        if (comps.Count > 0)
        {
            GUI.Label(new Rect(sw - 210, 35, 200, 20), $"Companions ({comps.Count}/3):", _hudLabelRight);
            for (int i = 0; i < comps.Count; i++)
            {
                var c = comps[i];
                if (c == null) continue;
                string status = c.IsAlive ? $"{c.companionType} {c.CurrentHP}/{c.maxHP}" : $"{c.companionType} (dead)";
                GUI.Label(new Rect(sw - 210, 55 + i * 18, 200, 18), status, _hudLabelRight);
            }
        }

        // Attack mode (bottom right)
        string mode = player.currentMode == AttackMode.Melee ? "Melee [Q]" : "Ranged [Q]";
        GUI.Label(new Rect(sw - 210, Screen.height - 40, 200, 22), $"Attack: {mode}", _hudLabelRight);

        // Multipliers (bottom left)
        GUI.Label(new Rect(10, Screen.height - 55, 200, 18),
            $"Melee x{UpgradeManager.Instance.MeleeDamageMultiplier:F2}", _hudLabel);
        GUI.Label(new Rect(10, Screen.height - 35, 200, 18),
            $"Ranged x{UpgradeManager.Instance.RangedDamageMultiplier:F2}", _hudLabel);

        // Controls reminder — centered at top, muted palette text
        GUI.contentColor = Palette.TextMute;
        GUI.Label(new Rect(0, 10, sw, 22),
            "Click=Move/Attack | Q=Toggle Mode | Space=Skip Phase (move → action)", _centerLabel);
        GUI.contentColor = Color.white;

        // Telegraph legend — centered at top, accent yellow
        GUI.contentColor = Palette.AccentYellow;
        GUI.Label(new Rect(0, 34, sw, 22),
            "Yellow tile = enemy will attack there", _centerLabel);
        GUI.contentColor = Color.white;

        // Turn-order strip (BG3-style portrait row)
        DrawTurnOrder();
    }

    /// <summary>
    /// Draws a horizontal row of portraits showing the upcoming turn order, starting
    /// from the currently-acting entity. Inspired by Baldur's Gate 3's combat tracker.
    /// Uses each entity's SpriteRenderer.sprite as the portrait — no extra asset wiring needed.
    /// </summary>
    void DrawTurnOrder()
    {
        if (TurnManager.Instance == null) return;
        var order = TurnManager.Instance.GetTurnOrder();
        if (order == null || order.Count == 0) return;

        int curIdx = Mathf.Max(0, TurnManager.Instance.CurrentIndex);
        const float BOX  = 52f;
        const float GAP  = 6f;
        const float Y    = 64f;
        int maxDisplay   = Mathf.Min(8, order.Count - curIdx);
        if (maxDisplay <= 0) return;

        float totalW = maxDisplay * BOX + (maxDisplay - 1) * GAP;
        float startX = (Screen.width - totalW) * 0.5f;

        for (int offset = 0; offset < maxDisplay; offset++)
        {
            int i = curIdx + offset;
            if (i >= order.Count) break;
            var e = order[i];
            if (e == null) continue;

            float x = startX + offset * (BOX + GAP);
            bool isCurrent = (offset == 0);
            bool isDead    = !e.IsAlive;

            // Outer frame — accent yellow for current turn, panel grey for upcoming
            GUI.color = isCurrent ? Palette.AccentYellow : Palette.BgPanel;
            GUI.DrawTexture(new Rect(x - 3, Y - 3, BOX + 6, BOX + 6), Texture2D.whiteTexture);

            // Inner fill — tinted by faction: cyan-ish for allies, danger-red for enemies
            Color fill = e.faction == Faction.Player
                ? new Color(0.10f, 0.22f, 0.30f, 0.95f)   // cool ally tint
                : new Color(0.30f, 0.10f, 0.12f, 0.95f);  // hostile tint
            GUI.color = fill;
            GUI.DrawTexture(new Rect(x, Y, BOX, BOX), Texture2D.whiteTexture);

            // Portrait — render the entity's sprite into the box
            var sr = e.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null && sr.sprite.texture != null)
            {
                GUI.color = isDead ? new Color(0.4f, 0.4f, 0.4f, 0.5f) : Color.white;
                var sprite = sr.sprite;
                var tex    = sprite.texture;
                var srect  = sprite.rect;
                Rect uv = new Rect(
                    srect.x / tex.width,
                    srect.y / tex.height,
                    srect.width  / tex.width,
                    srect.height / tex.height
                );
                const float INSET = 5f;
                GUI.DrawTextureWithTexCoords(
                    new Rect(x + INSET, Y + INSET, BOX - INSET * 2, BOX - INSET * 2),
                    tex, uv);
            }

            // Dead overlay
            if (isDead)
            {
                GUI.color = new Color(0.8f, 0.1f, 0.1f, 0.45f);
                GUI.DrawTexture(new Rect(x, Y, BOX, BOX), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
        }

        // Small label below the strip
        GUI.contentColor = Palette.TextMute;
        GUI.Label(new Rect(0, Y + BOX + 4, Screen.width, 16),
            "Turn order →", _centerLabel);
        GUI.contentColor = Color.white;
    }

    void DrawUpgradeScreen()
    {
        float sw = Screen.width, sh = Screen.height;
        DrawDimOverlay(0.82f);

        GUI.Label(new Rect(0, sh * 0.15f, sw, 50), "Wave Cleared", _titleStyle);

        // Heal feedback
        int heal = GameManager.Instance.LastHealAmount;
        if (heal > 0)
        {
            GUI.contentColor = Palette.Success;
            GUI.Label(new Rect(0, sh * 0.25f, sw, 30), $"Recovered {heal} HP from your wounds", _centerLabel);
            GUI.contentColor = Color.white;
        }

        GUI.Label(new Rect(0, sh * 0.32f, sw, 30), "Choose Your Upgrade", _centerLabel);

        float mel = UpgradeManager.Instance.MeleeDamageMultiplier;
        float ran = UpgradeManager.Instance.RangedDamageMultiplier;

        if (GUI.Button(new Rect(sw * 0.2f, sh * 0.45f, sw * 0.25f, 90),
            $"+25% Melee Damage\nx{mel:F2} -> x{mel + 0.25f:F2}", _panelButtonStyle))
        {
            UpgradeManager.Instance.ApplyUpgrade(UpgradeType.Melee);
        }

        if (GUI.Button(new Rect(sw * 0.55f, sh * 0.45f, sw * 0.25f, 90),
            $"+25% Ranged Damage\nx{ran:F2} -> x{ran + 0.25f:F2}", _panelButtonStyle))
        {
            UpgradeManager.Instance.ApplyUpgrade(UpgradeType.Ranged);
        }
    }

    void DrawCompanionScreen()
    {
        float sw = Screen.width, sh = Screen.height;
        DrawDimOverlay(0.88f);

        GUI.Label(new Rect(0, sh * 0.15f, sw, 50), "Choose a Companion", _titleStyle);

        float cardW = sw * 0.22f;
        float cardH = sh * 0.35f;
        float y = sh * 0.4f;

        if (GUI.Button(new Rect(sw * 0.08f, y, cardW, cardH),
            "DRONE\n\nHP 20  Speed +2\nRanged 2-4 tiles\nDamage 6\n\nGlass cannon", _panelButtonStyle))
        {
            GameManager.Instance.OnCompanionChosen(CompanionType.Drone);
        }

        if (GUI.Button(new Rect(sw * 0.39f, y, cardW, cardH),
            "BRAWLER\n\nHP 40  Speed 0\nMelee\nDamage 12\n\nTank, soaks hits", _panelButtonStyle))
        {
            GameManager.Instance.OnCompanionChosen(CompanionType.Brawler);
        }

        if (GUI.Button(new Rect(sw * 0.70f, y, cardW, cardH),
            "TRICKSTER\n\nHP 25  Speed +3\nFlexible 1-2 tiles\nDamage 5\n\nActs TWICE per turn", _panelButtonStyle))
        {
            GameManager.Instance.OnCompanionChosen(CompanionType.Trickster);
        }
    }

    void DrawEndScreen(string message, Color tint)
    {
        float sw = Screen.width, sh = Screen.height;
        DrawDimOverlay(0.9f);
        GUI.contentColor = tint;
        GUI.Label(new Rect(0, sh * 0.3f, sw, 50), message, _titleStyle);
        GUI.contentColor = Color.white;
        if (GUI.Button(new Rect(sw / 2 - 90, sh * 0.55f, 180, 50), "Restart", _panelButtonStyle))
            GameManager.Instance.RestartGame();
    }
}
