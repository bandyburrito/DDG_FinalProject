using UnityEngine;

public class PlaceholderUI : MonoBehaviour
{
    private GUIStyle _titleStyle;
    private GUIStyle _btnStyle;
    private GUIStyle _centerLabel;
    private bool _stylesInit = false;

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
        var state = GameManager.Instance?.State ?? GameState.MainMenu;

        if (state == GameState.Combat || state == GameState.WaveTransition) DrawHUD();
        if (state == GameState.UpgradeScreen) DrawUpgradeScreen();
        if (state == GameState.CompanionScreen) DrawCompanionScreen();
        if (state == GameState.Win) DrawEndScreen("Escaped!", Color.green);
        if (state == GameState.GameOver) DrawEndScreen($"Captured on Wave {GameManager.Instance.CurrentWave}", Color.red);
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
