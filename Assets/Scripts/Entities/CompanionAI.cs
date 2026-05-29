using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum CompanionType { Drone, Brawler, Trickster }

/// <summary>
/// AI-controlled ally that fights alongside Shoki.
/// Up to 3 alive at once. Permadeath — dies once, gone for the run.
/// </summary>
public class CompanionAI : Entity
{
    [Header("Companion Type")]
    public CompanionType companionType = CompanionType.Drone;

    protected override void Awake()
    {
        faction = Faction.Player;
        base.Awake();
        ApplyArchetypeStats();
    }

    private void ApplyArchetypeStats()
    {
        switch (companionType)
        {
            case CompanionType.Drone:
                maxHP = 20; speed = 2; moveRange = 2;
                rangedDamage = 6; rangedRangeMin = 2; rangedRangeMax = 4;
                break;
            case CompanionType.Brawler:
                maxHP = 40; speed = 0; moveRange = 1; meleeDamage = 12;
                break;
            case CompanionType.Trickster:
                maxHP = 25; speed = 3; moveRange = 2;
                meleeDamage = 5; rangedDamage = 5; rangedRangeMin = 1; rangedRangeMax = 2;
                break;
        }
        CurrentHP = maxHP;
    }

    public IEnumerator ExecuteTurn()
    {
        if (!IsAlive) yield break;

        var target = FindNearestEnemy();
        if (target == null) yield break;

        yield return new WaitForSeconds(0.2f);

        // Move
        var movePos = ComputeMoveTarget(target.GridPos);
        if (movePos != GridPos)
        {
            var path = GridManager.Instance.FindPath(GridPos, movePos);
            if (path != null)
            {
                int steps = Mathf.Min(moveRange, path.Count);
                for (int i = 0; i < steps; i++)
                {
                    if (!IsAlive) yield break;
                    // Host the per-tile walk on TurnManager — survives our death (Pit, friendly fire, etc.)
                    yield return TurnManager.Instance.StartCoroutine(WalkToTileSmooth(path[i], 0.14f));
                }
            }
        }

        if (!IsAlive) yield break;
        yield return new WaitForSeconds(0.15f);

        // Attack
        AttackPhase(target);

        // Trickster: second attack (acts twice per turn)
        if (companionType == CompanionType.Trickster)
        {
            yield return new WaitForSeconds(0.2f);
            var t2 = FindNearestEnemy();
            if (t2 != null) AttackPhase(t2);
        }
    }

    private EnemyAI FindNearestEnemy()
    {
        EnemyAI best = null;
        int bestDist = int.MaxValue;
        foreach (var e in TurnManager.Instance.GetTurnOrder())
        {
            if (e is EnemyAI enemy && enemy.IsAlive)
            {
                int d = GridManager.Instance.ManhattanDistance(GridPos, enemy.GridPos);
                if (d < bestDist) { bestDist = d; best = enemy; }
            }
        }
        return best;
    }

    private Vector2Int ComputeMoveTarget(Vector2Int targetPos)
    {
        if (companionType == CompanionType.Drone)
        {
            // Stay 2-4 tiles away
            int dist = GridManager.Instance.ManhattanDistance(GridPos, targetPos);
            if (dist >= 2 && dist <= 4) return GridPos;
            if (dist < 2)
            {
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
        }

        // Brawler / Trickster: approach
        var adjacents = GridManager.Instance.GetCardinalNeighbours(targetPos)
            .Where(p => GridManager.Instance.IsWalkable(p) || p == GridPos)
            .OrderBy(p => GridManager.Instance.ManhattanDistance(GridPos, p))
            .ToList();
        if (adjacents.Count == 0) return GridPos;
        // Try each approach slot nearest-first so a blocked slot doesn't stall us.
        foreach (var slot in adjacents)
        {
            var path = GridManager.Instance.FindPath(GridPos, slot);
            if (path == null || path.Count == 0) continue;
            int steps = Mathf.Min(moveRange, path.Count);
            return path[steps - 1];
        }
        return GridPos;
    }

    private void AttackPhase(EnemyAI target)
    {
        int dist = GridManager.Instance.ManhattanDistance(GridPos, target.GridPos);

        switch (companionType)
        {
            case CompanionType.Drone:
                if (dist >= rangedRangeMin && dist <= rangedRangeMax)
                    target.TakeDamage(rangedDamage);
                break;
            case CompanionType.Brawler:
                if (dist <= 1) target.TakeDamage(meleeDamage);
                break;
            case CompanionType.Trickster:
                if (dist <= rangedRangeMax)
                    target.TakeDamage(dist <= 1 ? meleeDamage : rangedDamage);
                break;
        }
    }
}
