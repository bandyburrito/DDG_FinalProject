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

    /// <summary>Ranged upgrades needed to unlock the piercing-line shot.</summary>
    public const int RANGED_LINE_THRESHOLD = 3;

    /// <summary>True once ranged has been upgraded enough to fire a piercing line.</summary>
    public bool RangedLineUnlocked => RangedUpgradeCount >= RANGED_LINE_THRESHOLD;

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

        if (type == UpgradeType.Ranged && RangedUpgradeCount == RANGED_LINE_THRESHOLD)
            Debug.Log("Ranged upgrade 3/3 — piercing LINE shot unlocked!");

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
