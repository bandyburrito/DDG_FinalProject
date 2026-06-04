using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Pale-steel melee flash + cyan ranged-impact flash — neutral tints distinct from
    // the yellow enemy-hit bursts that fire on top when an enemy actually takes damage.
    private static readonly Color MeleeSweepTint = new Color(0.80f, 0.88f, 1.00f, 1f);
    private static readonly Color BlastTint      = new Color(0.35f, 0.85f, 1.00f, 1f);

    /// <summary>
    /// Melee. Single-target until melee has been upgraded 3 times
    /// (UpgradeManager.MeleeCircleUnlocked); after that it becomes the full 8-tile
    /// circle, striking every adjacent tile at once.
    /// </summary>
    public void PlayerMeleeAttack(Vector2Int origin, Vector2Int target)
    {
        int damage = Mathf.RoundToInt(
            PlayerController.Instance.meleeDamage * UpgradeManager.Instance.MeleeDamageMultiplier);

        if (UpgradeManager.Instance.MeleeCircleUnlocked)
        {
            // Full circle — flash + damage every adjacent tile around the player.
            foreach (var pos in GridManager.Instance.GetAllNeighbours8(origin))
            {
                HitBurst.SpawnAt(GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.25f, MeleeSweepTint, 4);
                var t = GridManager.Instance.GetTile(pos);
                if (t?.occupant is EnemyAI e) e.TakeDamage(damage);
            }
        }
        else
        {
            // Single target — only the clicked adjacent tile.
            HitBurst.SpawnAt(GridManager.Instance.GridToWorld(target) + Vector3.up * 0.25f, MeleeSweepTint, 4);
            var tile = GridManager.Instance.GetTile(target);
            if (tile?.occupant is EnemyAI enemy) enemy.TakeDamage(damage);
        }
    }

    /// <summary>
    /// Ranged. Single-target until ranged has been upgraded 3 times
    /// (UpgradeManager.RangedAoeUnlocked); after that the shot detonates in a 3×3 blast
    /// centred on the tile it lands on, hitting everything in that square.
    /// </summary>
    public void PlayerRangedAttack(Vector2Int origin, Vector2Int target)
    {
        int damage = Mathf.RoundToInt(
            PlayerController.Instance.rangedDamage * UpgradeManager.Instance.RangedDamageMultiplier);

        if (UpgradeManager.Instance.RangedAoeUnlocked)
        {
            // 3×3 blast — the landing tile plus its 8 neighbours.
            HitBurst.SpawnAt(GridManager.Instance.GridToWorld(target) + Vector3.up * 0.4f, BlastTint, 8);
            DamageTileEnemy(target, damage);
            foreach (var pos in GridManager.Instance.GetAllNeighbours8(target))
            {
                HitBurst.SpawnAt(GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.4f, BlastTint, 4);
                DamageTileEnemy(pos, damage);
            }
        }
        else
        {
            // Single target — only the tile the shot lands on.
            HitBurst.SpawnAt(GridManager.Instance.GridToWorld(target) + Vector3.up * 0.4f, BlastTint, 5);
            DamageTileEnemy(target, damage);
        }
    }

    private void DamageTileEnemy(Vector2Int pos, int damage)
    {
        var tile = GridManager.Instance.GetTile(pos);
        if (tile?.occupant is EnemyAI enemy) enemy.TakeDamage(damage);
    }

    public void EnemyMeleeAttack(EnemyAI attacker, Entity target) =>
        target.TakeDamage(attacker.meleeDamage);

    public void EnemyRangedAttack(EnemyAI attacker, Entity target) =>
        target.TakeDamage(attacker.rangedDamage);
}
