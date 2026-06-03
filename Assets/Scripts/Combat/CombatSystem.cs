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

    /// <summary>
    /// Cross-shaped splash: full damage on the clicked tile, half damage to its 4 cardinal
    /// neighbours. Melee already covers an 8-tile sweep at close range; the splash gives
    /// ranged its own scaling pattern (5 tiles total, but reduced collateral) so the two
    /// modes stay tactically distinct in the late game. Half-damage is rounded up so a
    /// 1-damage shot still chips the splash tiles instead of silently doing nothing.
    /// </summary>
    public void PlayerRangedAttack(Vector2Int origin, Vector2Int target)
    {
        int damage = Mathf.RoundToInt(
            PlayerController.Instance.rangedDamage * UpgradeManager.Instance.RangedDamageMultiplier);
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

    public void EnemyMeleeAttack(EnemyAI attacker, Entity target) =>
        target.TakeDamage(attacker.meleeDamage);

    public void EnemyRangedAttack(EnemyAI attacker, Entity target) =>
        target.TakeDamage(attacker.rangedDamage);
}
