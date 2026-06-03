using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AttackMode { Melee, Ranged }

public class PlayerController : Entity
{
    public static PlayerController Instance { get; private set; }

    [Header("Attack Mode")]
    public AttackMode currentMode = AttackMode.Melee;

    private List<Vector2Int> _validMoveTiles   = new();
    private List<Vector2Int> _validAttackTiles = new();
    private bool _waitingForInput = false;
    private bool _isWalking       = false;   // blocks input while a path-walk is in progress

    protected override void Awake()
    {
        base.Awake();
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        faction       = Faction.Player;
        maxHP         = 100;
        speed         = 3;
        moveRange     = 2;   // 2-tile movement on 8x8 grid
        meleeDamage   = 15;
        // Bumped 8 → 12 (Iteration: ranged rebalance). Melee's 8-tile sweep dramatically
        // out-scales single-target ranged once UpgradeManager multipliers kick in; a higher
        // base damage plus the cross-splash in CombatSystem keeps ranged competitive without
        // letting it dominate the close-range game.
        rangedDamage  = 12;
        rangedRangeMin = 2;
        rangedRangeMax = 4;
        CurrentHP     = maxHP;
    }

    void Start()
    {
        TurnManager.Instance.OnTurnStart += OnTurnStarted;
    }

    void Update()
    {
        // Swallow gameplay input while paused — the IMGUI pause menu still receives clicks,
        // so without this a click on a pause button would also register as a move/attack.
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        // Block all input while a walk animation is playing
        if (!_waitingForInput || _isWalking) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentMode = currentMode == AttackMode.Melee ? AttackMode.Ranged : AttackMode.Melee;
            RefreshHighlights();
        }

        // Space now resolves the CURRENT phase rather than only ending the turn:
        //   1st press  → skip the movement phase, jump straight to attack options
        //   2nd press  → skip the action phase, end the turn
        // Two clean presses = "skip my turn entirely." Clicking a tile still performs
        // the action normally — Space is just the explicit out so players aren't
        // forced into a move or an attack they don't want to make.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!HasMoved)
            {
                HasMoved = true;
                _validMoveTiles.Clear();
                ShowAttackOptions();
            }
            else if (!HasActed)
            {
                EndTurn();
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
            HandleLeftClick();
    }

    private void OnTurnStarted(Entity e)
    {
        if (e != this) return;
        _waitingForInput = true;
        ShowMoveRange();
    }

    private void HandleLeftClick()
    {
        var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;
        var gridPos = GridManager.Instance.WorldToGrid(worldPos);

        if (!HasMoved && _validMoveTiles.Contains(gridPos))
        {
            StartCoroutine(WalkPath(gridPos));   // animated walk, no longer a teleport
            return;
        }

        if (HasMoved && !HasActed && _validAttackTiles.Contains(gridPos))
            PerformAttack(gridPos);
    }

    /// <summary>
    /// Walk the BFS-computed path tile by tile, smoothly lerping between tiles.
    /// Blocks input via _isWalking until arrival, then opens attack options.
    /// A trap that kills Shoki mid-path stops the coroutine (Die deactivates the GO).
    /// </summary>
    private IEnumerator WalkPath(Vector2Int destination)
    {
        _isWalking = true;
        GridManager.Instance.ClearHighlights();
        _validMoveTiles.Clear();

        var path = GridManager.Instance.FindPath(GridPos, destination);
        if (path != null && path.Count > 0)
        {
            int steps = Mathf.Min(moveRange, path.Count);
            for (int i = 0; i < steps; i++)
            {
                if (!IsAlive) break;
                // Host on TurnManager — survives a Pit kill on our last step so this loop unblocks
                yield return TurnManager.Instance.StartCoroutine(WalkToTileSmooth(path[i], 0.16f));
            }
        }

        _isWalking = false;
        if (IsAlive) ShowAttackOptions();   // Trap on a path tile may have killed us
    }

    private void ShowMoveRange()
    {
        // Use real BFS reachability — not Chebyshev box — so highlighted tiles match
        // what the walker can actually reach in moveRange cardinal steps.
        _validMoveTiles = GridManager.Instance.GetReachableTiles(GridPos, moveRange);
        GridManager.Instance.ShowMoveHighlights(_validMoveTiles);
    }

    public override void MoveTo(Vector2Int newPos)
    {
        base.MoveTo(newPos);
        GridManager.Instance.ClearHighlights();
        _validMoveTiles.Clear();
    }

    private void ShowAttackOptions()
    {
        GridManager.Instance.ClearHighlights();
        _validAttackTiles = currentMode == AttackMode.Melee ? GetMeleeTargets() : GetRangedTargets();
        GridManager.Instance.ShowAttackHighlights(_validAttackTiles);
    }

    private void RefreshHighlights()
    {
        if (!HasMoved) ShowMoveRange();
        else if (!HasActed) ShowAttackOptions();
    }

    private List<Vector2Int> GetMeleeTargets() =>
        GridManager.Instance.GetAllNeighbours8(GridPos);

    private List<Vector2Int> GetRangedTargets()
    {
        var t = new List<Vector2Int>();
        for (int dx = -rangedRangeMax; dx <= rangedRangeMax; dx++)
        for (int dy = -rangedRangeMax; dy <= rangedRangeMax; dy++)
        {
            var pos = GridPos + new Vector2Int(dx, dy);
            int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
            if (dist >= rangedRangeMin && dist <= rangedRangeMax && GridManager.Instance.IsInBounds(pos))
                t.Add(pos);
        }
        return t;
    }

    private void PerformAttack(Vector2Int clickedPos)
    {
        GridManager.Instance.ClearHighlights();
        HasActed = true;

        if (currentMode == AttackMode.Melee)
            CombatSystem.Instance.PlayerMeleeAttack(GridPos);
        else
            CombatSystem.Instance.PlayerRangedAttack(GridPos, clickedPos);

        EndTurn();
    }

    private void EndTurn()
    {
        _waitingForInput = false;
        GridManager.Instance.ClearHighlights();
        _validMoveTiles.Clear();
        _validAttackTiles.Clear();
        TurnManager.Instance.EndPlayerTurn();
    }

    /// <summary>Damage taken since the start of the current wave.</summary>
    public int DamageTakenThisWave { get; private set; }

    public void ResetWaveDamage() => DamageTakenThisWave = 0;

    /// <summary>Heal a fixed amount (clamped to maxHP). Used post-wave.</summary>
    public void Heal(int amount)
    {
        if (!IsAlive) return;
        int before = CurrentHP;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        if (CurrentHP > before) Debug.Log($"Shoki healed for {CurrentHP - before} HP.");
    }

    public override void TakeDamage(int amount)
    {
        int hpBefore = CurrentHP;
        base.TakeDamage(amount);
        int actualLoss = hpBefore - CurrentHP;
        DamageTakenThisWave += actualLoss;
    }

    protected override void Die()
    {
        base.Die();
        GameManager.Instance.PlayerDied();
    }
}
