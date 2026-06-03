using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Pale steel "slash" tint for the melee sweep flash — neutral so it reads as the
    // attack arc itself, distinct from the yellow enemy-hit bursts that fire on top.
    private static readonly Color MeleeSweepTint = new Color(0.80f, 0.88f, 1.00f, 1f);

    public void PlayerMeleeAttack(Vector2Int origin)
    {
        int damage = Mathf.RoundToInt(
            PlayerController.Instance.meleeDamage * UpgradeManager.Instance.MeleeDamageMultiplier);

        foreach (var pos in GridManager.Instance.GetAllNeighbours8(origin))
        {
            // Flash every tile in the 8-tile ring so the player SEES that melee hits all
            // around them — even tiles with no enemy on them get a small sweep puff.
            Vector3 fx = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.25f;
            HitBurst.SpawnAt(fx, MeleeSweepTint, 4);

            var tile = GridManager.Instance.GetTile(pos);
            if (tile?.occupant is EnemyAI enemy) enemy.TakeDamage(damage);   // enemies also get their own hit-burst
        }
    }

    // Energy-cyan tint for the piercing-line beam VFX.
    private static readonly Color LineTint = new Color(0.35f, 0.85f, 1.00f, 1f);

    /// <summary>
    /// Default ranged attack — a cross-shaped splash: full damage on the clicked tile,
    /// half damage to its 4 cardinal neighbours. Once ranged has been upgraded 3 times
    /// (UpgradeManager.RangedLineUnlocked) the shot UPGRADES into a piercing LINE that
    /// fires in the aimed cardinal direction and hits every enemy in that row/column to
    /// the edge of the board — the capstone that lets ranged rival melee's 8-tile sweep.
    /// </summary>
    public void PlayerRangedAttack(Vector2Int origin, Vector2Int target)
    {
        AudioManager.PlayLaser();

        int damage = Mathf.RoundToInt(
            PlayerController.Instance.rangedDamage * UpgradeManager.Instance.RangedDamageMultiplier);

        if (UpgradeManager.Instance.RangedLineUnlocked)
        {
            FireRangedLine(origin, target, damage);
            return;
        }

        int splash = Mathf.Max(1, Mathf.CeilToInt(damage * 0.5f));

        // Primary target
        var tile = GridManager.Instance.GetTile(target);
        if (tile?.occupant is EnemyAI enemy) enemy.TakeDamage(damage);

        // Splash to cardinal neighbours of the impact tile
        foreach (var pos in GridManager.Instance.GetCardinalNeighbours(target))
        {
            var t = GridManager.Instance.GetTile(pos);
            if (t?.occupant is EnemyAI nb) nb.TakeDamage(splash);
        }
    }

    /// <summary>
    /// Piercing-line shot. Snaps the player→target aim to the dominant cardinal direction,
    /// then sweeps from the player across the board in that line: full damage to every
    /// enemy it passes through (it pierces), stopping only at a solid Obstacle or the edge.
    /// </summary>
    private void FireRangedLine(Vector2Int origin, Vector2Int target, int damage)
    {
        Vector2Int delta = target - origin;
        Vector2Int dir = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? new Vector2Int((int)Mathf.Sign(delta.x), 0)   // horizontal row
            : new Vector2Int(0, (int)Mathf.Sign(delta.y));  // vertical column
        if (dir == Vector2Int.zero) dir = Vector2Int.right; // safety (target == origin)

        var cur = origin + dir;
        int safety = 64;   // hard cap so a bad dir can never infinite-loop
        while (GridManager.Instance.IsInBounds(cur) && safety-- > 0)
        {
            var t = GridManager.Instance.GetTile(cur);
            // A solid obstacle stops the beam; void tiles are open air, the beam flies over.
            if (t != null && t.type == TileType.Obstacle) break;

            // Beam VFX on every tile the line crosses so the player sees its full reach.
            Vector3 fx = GridManager.Instance.GridToWorld(cur) + Vector3.up * 0.4f;
            HitBurst.SpawnAt(fx, LineTint, 4);

            if (t?.occupant is EnemyAI enemy) enemy.TakeDamage(damage);

            cur += dir;
        }
    }

    public void EnemyMeleeAttack(EnemyAI attacker, Entity target) =>
        target.TakeDamage(attacker.meleeDamage);

    public void EnemyRangedAttack(EnemyAI attacker, Entity target) =>
        target.TakeDamage(attacker.rangedDamage);
}
