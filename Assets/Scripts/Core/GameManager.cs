using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public enum GameState { MainMenu, Loading, Intro, Combat, UpgradeScreen, CompanionScreen, WaveTransition, Win, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Wave Settings")]
    public int totalWaves = 10;

    public int CurrentWave { get; private set; } = 0;
    public GameState State { get; private set; } = GameState.MainMenu;

    /// <summary>True while the run is paused (Time.timeScale frozen). Pause is an overlay
    /// on top of gameplay states — it does not change <see cref="State"/>.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>Master volume 0..1, mirrored to AudioListener.volume and persisted.</summary>
    public float MasterVolume { get; private set; } = 1f;

    /// <summary>Permadeath companions for this run. Persist across rooms.</summary>
    public List<CompanionAI> ActiveCompanions { get; } = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ApplySavedSettings();
    }

    // ── Pause ───────────────────────────────────────────────────────────────────

    /// <summary>Gameplay states where pausing is allowed (not menus / end screens).</summary>
    private bool CanPause =>
        State == GameState.Combat || State == GameState.WaveTransition ||
        State == GameState.UpgradeScreen || State == GameState.CompanionScreen;

    public void TogglePause()
    {
        if (!IsPaused && !CanPause) return;
        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        // Freezing timeScale halts WaitForSeconds in the turn coroutines and animations,
        // so the turn-based flow resumes exactly where it left off when unpaused.
        Time.timeScale = paused ? 0f : 1f;
    }

    // ── Settings (persisted via PlayerPrefs) ─────────────────────────────────────

    private void ApplySavedSettings()
    {
        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));
        AudioListener.volume = MasterVolume;
        Screen.fullScreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
    }

    public void SetMasterVolume(float v)
    {
        MasterVolume = Mathf.Clamp01(v);
        AudioListener.volume = MasterVolume;
        PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
    }

    // ── App / navigation ─────────────────────────────────────────────────────────

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        PlaceholderSetup.StartGameOnLoad = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void StartNewGame()
    {
        SetPaused(false);
        TurnManager.Instance.OnAllEnemiesDead -= OnWaveCleared;
        TurnManager.Instance.OnAllEnemiesDead += OnWaveCleared;

        CurrentWave = 0;
        UpgradeManager.Instance.Reset();
        ActiveCompanions.Clear();

        // Brief Loading screen → Intro → Wave 1. The loading state gives the menu
        // music a beat to land and prevents the jarring "click → wall of text".
        StartCoroutine(LoadingThenIntro());
    }

    private System.Collections.IEnumerator LoadingThenIntro()
    {
        State = GameState.Loading;
        // ~1.4s feels like an intentional transition without dragging.
        yield return new WaitForSecondsRealtime(1.4f);
        State = GameState.Intro;
        IntroSequence.Instance?.Begin();
    }

    /// <summary>Called by IntroSequence once the typewriter narrative is done
    /// (or skipped via Esc). Loads wave 1 and hands off to combat.</summary>
    public void FinishIntro()
    {
        if (State != GameState.Intro) return;
        LoadNextWave();
    }

    private void LoadNextWave()
    {
        CurrentWave++;
        State = GameState.WaveTransition;

        TurnManager.Instance.ClearAll();

        RoomGenerator.Instance.GenerateRoom(CurrentWave);

        // Register player + reset wave damage tracker
        var player = PlayerController.Instance;
        if (player != null)
        {
            TurnManager.Instance.RegisterEntity(player);
            player.ResetWaveDamage();
        }

        // Reposition surviving companions next to player and re-register them
        ActiveCompanions.RemoveAll(c => c == null || !c.IsAlive);
        foreach (var c in ActiveCompanions)
        {
            var pos = RoomGenerator.Instance.GetCompanionSpawnNearPlayer();
            c.gameObject.SetActive(true);
            c.PlaceAt(pos);
            TurnManager.Instance.RegisterEntity(c);
        }

        // Spawn enemies for this wave
        var enemies = WaveConfig.GetEnemiesForWave(CurrentWave);
        foreach (var type in enemies)
        {
            var pos = RoomGenerator.Instance.GetEnemySpawnPoint();
            EnemySpawner.Instance.SpawnEnemy(type, pos);
        }

        State = GameState.Combat;
        TurnManager.Instance.StartCombat();
    }

    /// <summary>Public so the UI can show the heal amount on the upgrade screen.</summary>
    public int LastHealAmount { get; private set; }

    private void OnWaveCleared()
    {
        // Heal Shoki for 75% of damage taken this wave (was 50% — Iteration 3 rebalance).
        // This combined with the enemy damage reduction makes early waves survivable
        // while still keeping HP attrition meaningful across a full 10-wave run.
        var player = PlayerController.Instance;
        if (player != null)
        {
            LastHealAmount = (player.DamageTakenThisWave * 3) / 4;
            if (LastHealAmount > 0) player.Heal(LastHealAmount);
        }

        if (CurrentWave >= totalWaves)
        {
            State = GameState.Win;
            return;
        }
        State = GameState.UpgradeScreen;
    }

    /// <summary>Called by PlaceholderUI when player picks the damage upgrade.</summary>
    public void OnUpgradeChosen()
    {
        // After every 3rd wave (3, 6, 9), show companion choice screen as well
        if (CurrentWave % 3 == 0 && CurrentWave < totalWaves && ActiveCompanions.Count < 3)
        {
            State = GameState.CompanionScreen;
        }
        else
        {
            LoadNextWave();
        }
    }

    /// <summary>Called by PlaceholderUI when player picks a companion.</summary>
    public void OnCompanionChosen(CompanionType type)
    {
        var pos = RoomGenerator.Instance.GetCompanionSpawnNearPlayer();
        var companion = EnemySpawner.Instance.SpawnCompanion(type, pos);
        if (companion != null) ActiveCompanions.Add(companion);
        LoadNextWave();
    }

    public void PlayerDied()
    {
        State = GameState.GameOver;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        // Reload the scene for a clean slate, then auto-start a fresh run once it's loaded.
        PlaceholderSetup.StartGameOnLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

public static class WaveConfig
{
    public static List<EnemyType> GetEnemiesForWave(int wave)
    {
        var list = new List<EnemyType>();
        // Rebalanced for 8x8 grid — fewer enemies per wave
        (int soldiers, int snipers, int heavies) comp = wave switch
        {
            1  => (2, 0, 0),
            2  => (2, 0, 0),
            3  => (3, 0, 0),
            4  => (2, 1, 0),
            5  => (2, 1, 0),
            6  => (2, 1, 1),
            7  => (2, 2, 0),
            8  => (1, 2, 1),
            9  => (2, 1, 1),
            10 => (2, 1, 2),
            _  => (2, 0, 0)
        };

        for (int i = 0; i < comp.soldiers; i++) list.Add(EnemyType.Soldier);
        for (int i = 0; i < comp.snipers;  i++) list.Add(EnemyType.Sniper);
        for (int i = 0; i < comp.heavies;  i++) list.Add(EnemyType.Heavy);
        return list;
    }
}
