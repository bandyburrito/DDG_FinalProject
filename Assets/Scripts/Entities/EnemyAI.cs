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

        // Move toward planned destination — smooth lerp per tile for visual continuity.
        // CRITICAL: route the per-tile walk through TurnManager.StartCoroutine, not our own.
        // If we walk onto a Pit during this turn, MoveTo() → Die() → SetActive(false) will
        // STOP any coroutine hosted on our GameObject — including the one ExecuteTurn yields on.
        // Hosting it on TurnManager keeps it running past our death so ExecuteTurn resumes cleanly.
        if (PlannedMove != GridPos)
        {
            var path = GridManager.Instance.FindPath(GridPos, PlannedMove);
            if (path != null)
            {
                int steps = Mathf.Min(moveRange, path.Count);
                for (int i = 0; i < steps; i++)
                {
                    if (!IsAlive) yield break;
                    yield return TurnManager.Instance.StartCoroutine(WalkToTileSmooth(path[i], 0.14f));
                }
            }
        }

        if (!IsAlive) yield break;
        yield return new WaitForSeconds(0.15f);

        // Attack phase.
        // Primary: try the telegraphed tile if WillAttack was planned and range holds.
        // Fallback: if the enemy is a melee type and ended up adjacent to ANY player-faction
        //   entity (e.g. the path was blocked mid-walk so they stopped one tile short of
        //   the planned position) — attack that entity anyway. This prevents the "walks
        //   right next to you but does nothing" silent-miss when movement is obstructed.
        bool attacked = false;

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
                    attacked = true;
                }
            }
        }

        // Fallback for melee units: scan all 8 neighbours for a player-faction target.
        // Fires only if the primary attack didn't land — covers the case where the enemy
        // walked to a different tile than planned (path blocked) but is still adjacent.
        if (!attacked && enemyType != EnemyType.Sniper)
        {
            foreach (var nb in GridManager.Instance.GetAllNeighbours8(GridPos))
            {
                var t = GridManager.Instance.GetTile(nb);
                if (t?.occupant != null && t.occupant.faction == Faction.Player && t.occupant.IsAlive)
                {
                    CombatSystem.Instance.EnemyMeleeAttack(this, t.occupant);
                    break;   // one attack per turn
                }
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

        // Approach as far as moveRange allows. Try each slot in nearest-first order so a
        // teammate blocking the closest one doesn't freeze us — we route to the next best.
        foreach (var slot in adjacents)
        {
            var path = GridManager.Instance.FindPath(GridPos, slot);
            if (path == null || path.Count == 0) continue;
            int steps = Mathf.Min(moveRange, path.Count);
            return path[steps - 1];
        }
        return GridPos;
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
