using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum EnemyType { Soldier, Sniper, Heavy }

public class EnemyAI : Entity
{
    [Header("Enemy Type")]
    public EnemyType enemyType = EnemyType.Soldier;

    /// <summary>Move destination this round (set by ComputePlan).</summary>
    public Vector2Int PlannedMove   { get; private set; }
    /// <summary>Attack tile this round, or (-1,-1) if no attack planned.</summary>
    public Vector2Int PlannedAttack { get; private set; }
    public bool       WillAttack    { get; private set; }

    private const int SNIPER_MIN_DIST = 2;
    private const int SNIPER_MAX_DIST = 4;

    protected override void Awake()
    {
        faction = Faction.Enemy;
        base.Awake();
        ApplyArchetypeStats();
    }

    private void ApplyArchetypeStats()
    {
        // Damage values rebalanced (Iteration 3): early-game survivability improved.
        // Previous: Soldier 15, Sniper 8, Heavy 25 — worst-case wave 6 was 63 dmg/round (lethal in 2 rounds).
        // New:      Soldier  8, Sniper 5, Heavy 12 — worst-case wave 6 is  33 dmg/round (recoverable).
        switch (enemyType)
        {
            case EnemyType.Soldier:
                maxHP = 30; speed = 1; moveRange = 2; meleeDamage = 8;
                break;
            case EnemyType.Sniper:
                maxHP = 20; speed = 2; moveRange = 2; rangedDamage = 5;
                rangedRangeMin = 2; rangedRangeMax = 4;
                break;
            case EnemyType.Heavy:
                maxHP = 60; speed = 0; moveRange = 1; meleeDamage = 12;
                break;
        }
        CurrentHP = maxHP;
    }

    // ── Telegraph: planning ───────────────────────────────────────────────────

    /// <summary>
    /// Called by TelegraphSystem at the start of a round.
    /// Computes planned move destination + planned attack tile.
    /// </summary>
    public void ComputePlan()
    {
        var targetEntity = FindTarget();
        if (targetEntity == null)
        {
            PlannedMove = GridPos;
            WillAttack = false;
            return;
        }

        var targetPos = targetEntity.GridPos;

        // Plan move
        PlannedMove = enemyType == EnemyType.Sniper
            ? GetSniperMoveTarget(targetPos)
            : GetApproachTarget(targetPos);

        // Plan attack: would we be in range from the planned move position?
        int distAfterMove = enemyType == EnemyType.Sniper
            ? GridManager.Instance.ManhattanDistance(PlannedMove, targetPos)
            : GridManager.Instance.ChebyshevDistance(PlannedMove, targetPos);

        bool inRange = enemyType switch
        {
            EnemyType.Sniper => distAfterMove >= rangedRangeMin && distAfterMove <= rangedRangeMax,
            _                => distAfterMove <= 1
        };

        if (inRange)
        {
            PlannedAttack = targetPos;
            WillAttack    = true;
        }
        else
        {
            WillAttack = false;
        }
    }

    /// <summary>Find nearest player-aligned entity (Shoki or companion).</summary>
    private Entity FindTarget()
    {
        Entity best = null;
        int bestDist = int.MaxValue;
        foreach (var e in TurnManager.Instance.GetTurnOrder())
        {
            if (e == null || !e.IsAlive) continue;
            if (e.faction != Faction.Player) continue;
            int d = GridManager.Instance.ManhattanDistance(GridPos, e.GridPos);
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }

    // ── Turn Execution ────────────────────────────────────────────────────────

    public IEnumerator ExecuteTurn()
    {
        if (!IsAlive) yield break;

        yield return new WaitForSeconds(0.2f);

        // Move toward planned destination
        if (PlannedMove != GridPos)
        {
            var path = GridManager.Instance.FindPath(GridPos, PlannedMove);
            if (path != null)
            {
                int steps = Mathf.Min(moveRange, path.Count);
                for (int i = 0; i < steps; i++)
                {
                    if (!IsAlive) yield break;
                    MoveTo(path[i]);
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        if (!IsAlive) yield break;
        yield return new WaitForSeconds(0.15f);

        // Attack the telegraphed tile (committed — even if target moved away)
        if (WillAttack)
        {
            int dist = enemyType == EnemyType.Sniper
                ? GridManager.Instance.ManhattanDistance(GridPos, PlannedAttack)
                : GridManager.Instance.ChebyshevDistance(GridPos, PlannedAttack);

            bool stillInRange = enemyType switch
            {
                EnemyType.Sniper => dist >= rangedRangeMin && dist <= rangedRangeMax,
                _                => dist <= 1
            };

            if (stillInRange)
            {
                var tile = GridManager.Instance.GetTile(PlannedAttack);
                var occupant = tile?.occupant;
                if (occupant != null && occupant.faction == Faction.Player)
                {
                    if (enemyType == EnemyType.Sniper)
                        CombatSystem.Instance.EnemyRangedAttack(this, occupant);
                    else
                        CombatSystem.Instance.EnemyMeleeAttack(this, occupant);
                }
                // else: telegraphed tile is empty — attack misses (player dodged)
            }
        }
    }

    // ── Planning helpers ──────────────────────────────────────────────────────

    private Vector2Int GetApproachTarget(Vector2Int targetPos)
    {
        var adjacents = GridManager.Instance.GetCardinalNeighbours(targetPos)
            .Where(p => GridManager.Instance.IsWalkable(p) || p == GridPos)
            .OrderBy(p => GridManager.Instance.ManhattanDistance(GridPos, p))
            .ToList();
        if (adjacents.Count == 0) return GridPos;

        // Approach as far as moveRange allows
        var path = GridManager.Instance.FindPath(GridPos, adjacents[0]);
        if (path == null || path.Count == 0) return GridPos;
        int steps = Mathf.Min(moveRange, path.Count);
        return path[steps - 1];
    }

    private Vector2Int GetSniperMoveTarget(Vector2Int targetPos)
    {
        int dist = GridManager.Instance.ManhattanDistance(GridPos, targetPos);
        if (dist >= SNIPER_MIN_DIST && dist <= SNIPER_MAX_DIST) return GridPos;

        if (dist < SNIPER_MIN_DIST)
        {
            // Retreat — find walkable tile within move range farthest from target
            var candidates = new List<Vector2Int>();
            for (int dx = -moveRange; dx <= moveRange; dx++)
            for (int dy = -moveRange; dy <= moveRange; dy++)
            {
                var p = GridPos + new Vector2Int(dx, dy);
                if (GridManager.Instance.IsWalkable(p)) candidates.Add(p);
            }
            if (candidates.Count == 0) return GridPos;
            candidates.Sort((a, b) =>
                GridManager.Instance.ManhattanDistance(b, targetPos)
                    .CompareTo(GridManager.Instance.ManhattanDistance(a, targetPos)));
            return candidates[0];
        }

        return GetApproachTarget(targetPos);
    }
}
