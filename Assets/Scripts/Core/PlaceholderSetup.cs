using UnityEngine;
using System.Collections;

/// <summary>
/// One-click bootstrap. Attach to an empty "Bootstrap" GameObject in the scene.
/// Builds all managers, placeholder sprites, UI, and camera.
/// </summary>
public class PlaceholderSetup : MonoBehaviour
{
    private static Sprite _square;
    private static Sprite _diamond;

    /// <summary>
    /// When true, a run starts automatically the frame after the scene loads (used by
    /// RestartGame). When false, we boot into the MainMenu and wait for the Start button.
    /// </summary>
    public static bool StartGameOnLoad = false;

    void Awake()
    {
        _square  = MakeSquareSprite();
        _diamond = MakeDiamondSprite();

        // Managers
        var mgr = new GameObject("_Managers");
        mgr.AddComponent<GameManager>();
        mgr.AddComponent<TurnManager>();
        mgr.AddComponent<CombatSystem>();
        mgr.AddComponent<TrapSystem>();
        mgr.AddComponent<TelegraphSystem>();
        mgr.AddComponent<RoomGenerator>();
        mgr.AddComponent<EnemySpawner>();
        mgr.AddComponent<UpgradeManager>();
        mgr.AddComponent<UIManager>();
        mgr.AddComponent<AudioManager>();   // background-loop music tied to GameState
        mgr.AddComponent<IntroSequence>();  // Undertale-style typewriter narrative

        // Grid — load tiles from Resources/Tiles/ if present, fallback to procedural diamonds
        var gridGO = new GameObject("_Grid");
        var grid = gridGO.AddComponent<GridManager>();
        grid.groundTilePrefab      = MakeTilePrefab("Ground",   "ground",      Hex("3A3A3A"));
        grid.obstacleTilePrefab    = MakeTilePrefab("Obstacle", "obstacle",    Hex("6B4226"));
        grid.spikeTrapPrefab       = MakeTilePrefab("Spike",    "spike",       Hex("FF8C00"));
        grid.pitTrapPrefab         = MakeTilePrefab("Pit",      "pit",         Hex("0A0A0A"));
        grid.slowZonePrefab        = MakeTilePrefab("SlowZone", "slow",        Hex("8B00FF"));
        grid.moveHighlightPrefab   = MakeOverlayPrefab("MoveHL",   "ground", new Color(0.2f, 0.6f, 1f, 0.55f));
        grid.attackHighlightPrefab = MakeOverlayPrefab("AtkHL",    "ground", new Color(1f, 0.4f, 0.0f, 0.7f));  // bright orange — won't blend into red enemies
        grid.telegraphAttackPrefab = MakeOverlayPrefab("TelAtk",   "ground", new Color(1f, 0.85f, 0.1f, 0.6f));
        grid.telegraphMovePrefab   = MakeOverlayPrefab("TelMove",  "ground", new Color(0.8f, 0.8f, 0.8f, 0.25f));

        // Enemy prefabs — non-red colours so they don't blend into attack highlights
        var sp = EnemySpawner.Instance;
        sp.soldierPrefab = MakeEnemyPrefab("Soldier", "soldier", Hex("707080"), EnemyType.Soldier); // gunmetal grey
        sp.sniperPrefab  = MakeEnemyPrefab("Sniper",  "sniper",  Hex("4A8A3A"), EnemyType.Sniper);  // camo green
        sp.heavyPrefab   = MakeEnemyPrefab("Heavy",   "heavy",   Hex("2A4A6A"), EnemyType.Heavy);   // navy steel

        // Companion prefabs — distinct from enemies and Shoki
        sp.dronePrefab     = MakeCompanionPrefab("Drone",     "drone",     Hex("40C0E0"), CompanionType.Drone);     // cyan
        sp.brawlerPrefab   = MakeCompanionPrefab("Brawler",   "brawler",   Hex("E08020"), CompanionType.Brawler);   // amber (was green — conflicted with sniper)
        sp.tricksterPrefab = MakeCompanionPrefab("Trickster", "trickster", Hex("E040A0"), CompanionType.Trickster); // magenta

        // Player (Shoki)
        var player = new GameObject("Shoki");
        var psr = player.AddComponent<SpriteRenderer>();
        var shokiSprite = SpriteLoader.LoadEntity("shoki");
        if (shokiSprite != null) { psr.sprite = shokiSprite; }
        else { psr.sprite = _square; psr.color = Hex("00E5FF"); }
        psr.sortingOrder = 100;
        player.AddComponent<PlayerController>();

        // UI
        gameObject.AddComponent<PlaceholderUI>();

        // Camera — frame the 8x8 isometric grid
        var cam = Camera.main;
        if (cam == null) { var c = new GameObject("Main Camera"); cam = c.AddComponent<Camera>(); c.tag = "MainCamera"; }
        cam.orthographic = true;
        // Centre of isometric 8x8 grid is roughly at (0, 8*tileHeight) — calculated dynamically below
        cam.transform.position = new Vector3(0f, 4f, -10f);
        cam.orthographicSize = 6f;
        cam.backgroundColor = Palette.BgMain;   // shared base across menu/loading/intro/gameplay
    }

    IEnumerator Start()
    {
        yield return null; // wait one frame so all Start() methods fire first
        // Boot into the MainMenu by default. RestartGame sets StartGameOnLoad so a reload
        // jumps straight back into a fresh run instead of the menu.
        if (StartGameOnLoad)
        {
            StartGameOnLoad = false;
            GameManager.Instance.StartNewGame();
        }
    }

    // ── Sprite helpers ─────────────────────────────────────────────────────

    static Sprite MakeSquareSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
    }

    /// <summary>
    /// Diamond-shaped sprite (64x32) — proper 2:1 isometric aspect.
    /// Fills the texture corner-to-corner so tiles tessellate without gaps.
    /// </summary>
    static Sprite MakeDiamondSprite()
    {
        const int W = 64;  // 2:1 aspect for isometric
        const int H = 32;
        var tex = new Texture2D(W, H);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color[W * H];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        // Diamond fills the whole texture: corners at (W/2,0), (W,H/2), (W/2,H), (0,H/2)
        // Pixel is inside if |x-W/2|/(W/2) + |y-H/2|/(H/2) <= 1
        float hw = W * 0.5f, hh = H * 0.5f;
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float dx = Mathf.Abs(x + 0.5f - hw) / hw;
            float dy = Mathf.Abs(y + 0.5f - hh) / hh;
            if (dx + dy <= 1f)
                pixels[y * W + x] = Color.white;
        }
        // Replace inner N/N references for outline pass
        int Nx = W, Ny = H;
        var pixels2 = pixels;
        for (int y = 0; y < Ny; y++)
        for (int x = 0; x < Nx; x++)
        {
            if (pixels2[y * Nx + x] != Color.white) continue;
            bool edge = false;
            int[][] offs = { new[]{-1,0}, new[]{1,0}, new[]{0,-1}, new[]{0,1} };
            foreach (var o in offs)
            {
                int ax = x + o[0], ay = y + o[1];
                if (ax < 0 || ax >= Nx || ay < 0 || ay >= Ny || pixels2[ay * Nx + ax] == Color.clear)
                { edge = true; break; }
            }
            if (edge) pixels[y * Nx + x] = new Color(0, 0, 0, 0.7f);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        // PPU = 32 makes the 64x32 texture be exactly 2 units wide and 1 unit tall — matches isometric step
        return Sprite.Create(tex, new Rect(0, 0, W, H), Vector2.one * 0.5f, 32f);
    }

    static GameObject MakeTilePrefab(string name, string resourceName, Color fallbackCol)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        var loaded = SpriteLoader.LoadTile(resourceName);
        if (loaded != null)
        {
            sr.sprite = loaded;
            // Real sprite — show its full colour, no tint
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = _diamond;
            sr.color = fallbackCol;
        }
        go.SetActive(false);
        return go;
    }

    /// <summary>
    /// Overlay/highlight tile — always uses the flat procedural diamond so the highlight
    /// covers ONLY the top face of an isometric tile, not the cube sides. Tinting the full
    /// cube sprite would make adjacent neighbour faces look attackable too, which is a
    /// false-positive read for the player.
    /// </summary>
    static GameObject MakeOverlayPrefab(string name, string resourceName, Color tint)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _diamond;   // flat top-face shape only — ignore resourceName
        sr.color  = tint;
        go.SetActive(false);
        return go;
    }

    static GameObject MakeEnemyPrefab(string name, string resourceName, Color fallbackCol, EnemyType type)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        var loaded = SpriteLoader.LoadEntity(resourceName);
        if (loaded != null)
        {
            sr.sprite = loaded;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = _square;
            sr.color = fallbackCol;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        }
        var ai = go.AddComponent<EnemyAI>();
        ai.enemyType = type;
        go.AddComponent<HpBar>();   // small floating HP bar above the enemy
        go.SetActive(false);
        return go;
    }

    static GameObject MakeCompanionPrefab(string name, string resourceName, Color fallbackCol, CompanionType type)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        var loaded = SpriteLoader.LoadEntity(resourceName);
        if (loaded != null)
        {
            sr.sprite = loaded;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = _square;
            sr.color = fallbackCol;
            go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
        }
        var ai = go.AddComponent<CompanionAI>();
        ai.companionType = type;
        go.AddComponent<HpBar>();        // companions get the same floating HP bar
        go.SetActive(false);
        return go;
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
}
