using UnityEngine;
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
        rangedDamage  = 8;
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
        if (!_waitingForInput) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentMode = currentMode == AttackMode.Melee ? AttackMode.Ranged : AttackMode.Melee;
            RefreshHighlights();
        }

        if (Input.GetKeyDown(KeyCode.Space) && HasMoved)
            EndTurn();

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
            MoveTo(gridPos);
            if (IsAlive) ShowAttackOptions(); // Trap may have killed us
            return;
        }

        if (HasMoved && !HasActed && _validAttackTiles.Contains(gridPos))
            PerformAttack(gridPos);
    }

    private void ShowMoveRange()
    {
        _validMoveTiles.Clear();
        for (int dx = -moveRange; dx <= moveRange; dx++)
        for (int dy = -moveRange; dy <= moveRange; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            var pos = GridPos + new Vector2Int(dx, dy);
            if (GridManager.Instance.IsWalkable(pos)) _validMoveTiles.Add(pos);
        }
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
