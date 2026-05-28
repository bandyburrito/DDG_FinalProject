using UnityEngine;

public class CombatSystem : MonoBehaviour
{
    public static CombatSystem Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void PlayerMeleeAttack(Vector2Int origin)
    {
        int damage = Mathf.RoundToInt(
            PlayerController.Instance.meleeDamage * UpgradeManager.Instance.MeleeDamageMultiplier);

        foreach (var pos in GridManager.Instance.GetAllNeighbours8(origin))
        {
            var tile = GridManager.Instance.GetTile(pos);
            if (tile?.occupant is EnemyAI enemy) enemy.TakeDamage(damage);
        }
    }

    public void PlayerRangedAttack(Vector2Int origin, Vector2Int target)
    {
        int damage = Mathf.RoundToInt(
            PlayerController.Instance.rangedDamage * UpgradeManager.Instance.RangedDamageMultiplier);

        var tile = GridManager.Instance.GetTile(target);
        if (tile?.occupant is EnemyAI enemy) enemy.TakeDamage(damage);
    }

    public void EnemyMeleeAttack(EnemyAI attacker, Entity target) =>
        target.TakeDamage(attacker.meleeDamage);

    public void EnemyRangedAttack(EnemyAI attacker, Entity target) =>
        target.TakeDamage(attacker.rangedDamage);
}
