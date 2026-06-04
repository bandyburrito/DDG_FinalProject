using UnityEngine;

public enum UpgradeType { Melee, Ranged }

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public float MeleeDamageMultiplier  { get; private set; } = 1f;
    public float RangedDamageMultiplier { get; private set; } = 1f;

    /// <summary>How many times each attack mode has been upgraded this run.</summary>
    public int MeleeUpgradeCount  { get; private set; }
    public int RangedUpgradeCount { get; private set; }

    /// <summary>Upgrades needed to unlock each attack's area-of-effect form.</summary>
    public const int MELEE_CIRCLE_THRESHOLD = 3;   // single-target → full 8-tile circle
    public const int RANGED_AOE_THRESHOLD   = 3;   // single-target → 3×3 blast

    /// <summary>True once melee upgrades reach the full-circle threshold.</summary>
    public bool MeleeCircleUnlocked => MeleeUpgradeCount  >= MELEE_CIRCLE_THRESHOLD;
    /// <summary>True once ranged upgrades reach the 3×3 blast threshold.</summary>
    public bool RangedAoeUnlocked   => RangedUpgradeCount >= RANGED_AOE_THRESHOLD;

    private const float UPGRADE_AMOUNT = 0.25f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ApplyUpgrade(UpgradeType type)
    {
        AudioManager.PlayPowerUp();

        if (type == UpgradeType.Melee)  { MeleeDamageMultiplier  += UPGRADE_AMOUNT; MeleeUpgradeCount++;  }
        if (type == UpgradeType.Ranged) { RangedDamageMultiplier += UPGRADE_AMOUNT; RangedUpgradeCount++; }

        if (type == UpgradeType.Melee  && MeleeUpgradeCount  == MELEE_CIRCLE_THRESHOLD)
            Debug.Log("Melee 3/3 — full CIRCLE strike unlocked!");
        if (type == UpgradeType.Ranged && RangedUpgradeCount == RANGED_AOE_THRESHOLD)
            Debug.Log("Ranged 3/3 — 3×3 BLAST unlocked!");

        Debug.Log($"Upgrade {type}: melee x{MeleeDamageMultiplier:F2}, ranged x{RangedDamageMultiplier:F2}");
        GameManager.Instance.OnUpgradeChosen();
    }

    public void Reset()
    {
        MeleeDamageMultiplier  = 1f;
        RangedDamageMultiplier = 1f;
        MeleeUpgradeCount      = 0;
        RangedUpgradeCount     = 0;
    }
}
