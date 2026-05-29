using UnityEngine;

public class PlaceholderUI : MonoBehaviour
{
    private GUIStyle _titleStyle;
    private GUIStyle _btnStyle;
    private GUIStyle _centerLabel;
    private bool _stylesInit = false;

    /// <summary>Settings panel is an overlay shown from either the main menu or the pause menu.</summary>
    private bool _showSettings = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Escape closes the settings overlay first, otherwise toggles pause.
            if (_showSettings) { _showSettings = false; return; }
            GameManager.Instance?.TogglePause();
        }
    }

    void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;
        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _titleStyle.normal.textColor = Color.white;
        _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };

        _centerLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _centerLabel.normal.textColor = Color.white;
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
        else
        {
            if (state == GameState.Combat || state == GameState.WaveTransition) DrawHUD();
            if (state == GameState.UpgradeScreen) DrawUpgradeScreen();
            if (state == GameState.CompanionScreen) DrawCompanionScreen();
            if (state == GameState.Win) DrawEndScreen("Escaped!", Color.green);
            if (state == GameState.GameOver) DrawEndScreen($"Captured on Wave {gm.CurrentWave}", Color.red);

            if (gm != null && gm.IsPaused) DrawPauseMenu();
        }

        // Settings overlays everything (reachable from main menu and pause menu).
        if (_showSettings) DrawSettings();
    }

    // ── Menus ─────────────────────────────────────────────────────────────────

    void DrawMainMenu()
    {
        float sw = Screen.width, sh = Screen.height;
        GUI.color = new Color(0.10f, 0.06f, 0.16f, 1f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, sh * 0.18f, sw, 60), "Shoki's Adventure", _titleStyle);
        GUI.color = new Color(1, 1, 1, 0.6f);
        GUI.Label(new Rect(0, sh * 0.27f, sw, 24), "A tactical escape through 10 waves", _centerLabel);
        GUI.color = Color.white;

        float bw = 220, bh = 50, bx = (sw - bw) * 0.5f;
        float by = sh * 0.42f;

        if (GUI.Button(new Rect(bx, by, bw, bh), "Start Game", _btnStyle))
            GameManager.Instance.StartNewGame();

        if (GUI.Button(new Rect(bx, by + 64, bw, bh), "Settings", _btnStyle))
            _showSettings = true;

        if (GUI.Button(new Rect(bx, by + 128, bw, bh), "Quit", _btnStyle))
            GameManager.Instance.QuitGame();

        GUI.color = new Color(1, 1, 1, 0.4f);
        GUI.Label(new Rect(0, sh - 30, sw, 20),
            "Click = Move/Attack   |   Q = Toggle Mode   |   Space = End Turn   |   Esc = Pause", _centerLabel);
        GUI.color = Color.white;
    }

    void DrawPauseMenu()
    {
        float sw = Screen.width, sh = Screen.height;
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, sh * 0.18f, sw, 60), "Paused", _titleStyle);

        float bw = 220, bh = 48, bx = (sw - bw) * 0.5f;
        float by = sh * 0.36f;

        if (GUI.Button(new Rect(bx, by, bw, bh), "Resume", _btnStyle))
            GameManager.Instance.SetPaused(false);

        if (GUI.Button(new Rect(bx, by + 60, bw, bh), "Settings", _btnStyle))
            _showSettings = true;

        if (GUI.Button(new Rect(bx, by + 120, bw, bh), "Restart Run", _btnStyle))
            GameManager.Instance.RestartGame();

        if (GUI.Button(new Rect(bx, by + 180, bw, bh), "Main Menu", _btnStyle))
            GameManager.Instance.ReturnToMainMenu();

        if (GUI.Button(new Rect(bx, by + 240, bw, bh), "Quit", _btnStyle))
            GameManager.Instance.QuitGame();
    }

    void DrawSettings()
    {
        float sw = Screen.width, sh = Screen.height;
        GUI.color = new Color(0, 0, 0, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, sh * 0.18f, sw, 60), "Settings", _titleStyle);

        var gm = GameManager.Instance;
        float panelW = 360, px = (sw - panelW) * 0.5f;
        float y = sh * 0.36f;

        // Master volume
        float vol = gm != null ? gm.MasterVolume : 1f;
        GUI.Label(new Rect(px, y, panelW, 22), $"Master Volume: {Mathf.RoundToInt(vol * 100)}%", _btnStyle);
        float newVol = GUI.HorizontalSlider(new Rect(px + 20, y + 30, panelW - 40, 20), vol, 0f, 1f);
        if (gm != null && !Mathf.Approximately(newVol, vol)) gm.SetMasterVolume(newVol);

        // Fullscreen toggle
        bool fs = Screen.fullScreen;
        bool newFs = GUI.Toggle(new Rect(px + 20, y + 70, panelW - 40, 24), fs, " Fullscreen");
        if (gm != null && newFs != fs) gm.SetFullscreen(newFs);

        // Back
        float bw = 200, bh = 46;
        if (GUI.Button(new Rect((sw - bw) * 0.5f, y + 130, bw, bh), "Back", _btnStyle))
            _showSettings = false;
    }

    void DrawHUD()
    {
        var player = PlayerController.Instance;
        if (player == null) return;
        float sw = Screen.width;

        // HP (top left)
        GUI.Label(new Rect(10, 10, 200, 22), $"HP: {player.CurrentHP} / {player.maxHP}");
        float ratio = (float)player.CurrentHP / player.maxHP;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(10, 35, 200, 14), Texture2D.whiteTexture);
        GUI.color = ratio > 0.3f ? Color.red : new Color(0.6f, 0f, 0f);
        GUI.DrawTexture(new Rect(11, 36, 198 * ratio, 12), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Wave (top right)
        GUI.Label(new Rect(sw - 210, 10, 200, 22),
            $"Wave {GameManager.Instance.CurrentWave} / {GameManager.Instance.totalWaves}");

        // Companions (top right, below wave)
        var comps = GameManager.Instance.ActiveCompanions;
        if (comps.Count > 0)
        {
            GUI.Label(new Rect(sw - 210, 35, 200, 20), $"Companions ({comps.Count}/3):");
            for (int i = 0; i < comps.Count; i++)
            {
                var c = comps[i];
                if (c == null) continue;
                string status = c.IsAlive ? $"{c.companionType} {c.CurrentHP}/{c.maxHP}" : $"{c.companionType} (dead)";
                GUI.Label(new Rect(sw - 210, 55 + i * 18, 200, 18), status);
            }
        }

        // Attack mode (bottom right)
        string mode = player.currentMode == AttackMode.Melee ? "Melee [Q]" : "Ranged [Q]";
        GUI.Label(new Rect(sw - 210, Screen.height - 40, 200, 22), $"Attack: {mode}");

        // Multipliers (bottom left)
        GUI.Label(new Rect(10, Screen.height - 55, 200, 18),
            $"Melee x{UpgradeManager.Instance.MeleeDamageMultiplier:F2}");
        GUI.Label(new Rect(10, Screen.height - 35, 200, 18),
            $"Ranged x{UpgradeManager.Instance.RangedDamageMultiplier:F2}");

        // Controls reminder — centered at top
        GUI.color = new Color(1, 1, 1, 0.45f);
        GUI.Label(new Rect(0, 10, sw, 22),
            "Click=Move/Attack | Q=Toggle Mode | Space=End Turn", _centerLabel);
        GUI.color = Color.white;

        // Telegraph legend — centered at top, yellow
        GUI.color = new Color(1, 0.85f, 0.1f, 1f);
        GUI.Label(new Rect(0, 34, sw, 22),
            "Yellow tile = enemy will attack there", _centerLabel);
        GUI.color = Color.white;

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

            // Outer frame — yellow for current turn, dark grey for upcoming
            GUI.color = isCurrent
                ? new Color(1f, 0.85f, 0.1f, 1f)
                : new Color(0.20f, 0.20f, 0.24f, 0.85f);
            GUI.DrawTexture(new Rect(x - 3, Y - 3, BOX + 6, BOX + 6), Texture2D.whiteTexture);

            // Inner fill — slightly tinted by faction
            Color fill = e.faction == Faction.Player
                ? new Color(0.10f, 0.22f, 0.30f, 0.95f)   // bluish for player/companions
                : new Color(0.30f, 0.10f, 0.10f, 0.95f);  // reddish for enemies
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
        GUI.color = new Color(1, 1, 1, 0.55f);
        GUI.Label(new Rect(0, Y + BOX + 4, Screen.width, 16),
            "Turn order →", _centerLabel);
        GUI.color = Color.white;
    }

    void DrawUpgradeScreen()
    {
        float sw = Screen.width, sh = Screen.height;
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, sh * 0.15f, sw, 50), "Wave Cleared", _titleStyle);

        // Heal feedback
        int heal = GameManager.Instance.LastHealAmount;
        if (heal > 0)
        {
            GUI.color = new Color(0.4f, 1f, 0.4f);
            GUI.Label(new Rect(0, sh * 0.25f, sw, 30), $"Recovered {heal} HP from your wounds", _btnStyle);
            GUI.color = Color.white;
        }

        GUI.Label(new Rect(0, sh * 0.32f, sw, 30), "Choose Your Upgrade", _btnStyle);

        float mel = UpgradeManager.Instance.MeleeDamageMultiplier;
        float ran = UpgradeManager.Instance.RangedDamageMultiplier;

        if (GUI.Button(new Rect(sw * 0.2f, sh * 0.45f, sw * 0.25f, 90),
            $"+25% Melee Damage\nx{mel:F2} -> x{mel + 0.25f:F2}", _btnStyle))
        {
            UpgradeManager.Instance.ApplyUpgrade(UpgradeType.Melee);
        }

        if (GUI.Button(new Rect(sw * 0.55f, sh * 0.45f, sw * 0.25f, 90),
            $"+25% Ranged Damage\nx{ran:F2} -> x{ran + 0.25f:F2}", _btnStyle))
        {
            UpgradeManager.Instance.ApplyUpgrade(UpgradeType.Ranged);
        }
    }

    void DrawCompanionScreen()
    {
        float sw = Screen.width, sh = Screen.height;
        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, sh * 0.15f, sw, 50), "Choose a Companion", _titleStyle);

        float cardW = sw * 0.22f;
        float cardH = sh * 0.35f;
        float y = sh * 0.4f;

        if (GUI.Button(new Rect(sw * 0.08f, y, cardW, cardH),
            "DRONE\n\nHP 20  Speed +2\nRanged 2-4 tiles\nDamage 6\n\nGlass cannon", _btnStyle))
        {
            GameManager.Instance.OnCompanionChosen(CompanionType.Drone);
        }

        if (GUI.Button(new Rect(sw * 0.39f, y, cardW, cardH),
            "BRAWLER\n\nHP 40  Speed 0\nMelee\nDamage 12\n\nTank, soaks hits", _btnStyle))
        {
            GameManager.Instance.OnCompanionChosen(CompanionType.Brawler);
        }

        if (GUI.Button(new Rect(sw * 0.70f, y, cardW, cardH),
            "TRICKSTER\n\nHP 25  Speed +3\nFlexible 1-2 tiles\nDamage 5\n\nActs TWICE per turn", _btnStyle))
        {
            GameManager.Instance.OnCompanionChosen(CompanionType.Trickster);
        }
    }

    void DrawEndScreen(string message, Color tint)
    {
        float sw = Screen.width, sh = Screen.height;
        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = tint;
        GUI.Label(new Rect(0, sh * 0.3f, sw, 50), message, _titleStyle);
        GUI.color = Color.white;
        if (GUI.Button(new Rect(sw / 2 - 80, sh * 0.55f, 160, 50), "Restart", _btnStyle))
            GameManager.Instance.RestartGame();
    }
}
