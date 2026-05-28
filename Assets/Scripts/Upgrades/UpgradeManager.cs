using UnityEngine;

public enum UpgradeType { Melee, Ranged }

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public float MeleeDamageMultiplier  { get; private set; } = 1f;
    public float RangedDamageMultiplier { get; private set; } = 1f;

    private const float UPGRADE_AMOUNT = 0.25f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ApplyUpgrade(UpgradeType type)
    {
        if (type == UpgradeType.Melee)  MeleeDamageMultiplier  += UPGRADE_AMOUNT;
        if (type == UpgradeType.Ranged) RangedDamageMultiplier += UPGRADE_AMOUNT;
        Debug.Log($"Upgrade {type}: melee x{MeleeDamageMultiplier:F2}, ranged x{RangedDamageMultiplier:F2}");
        GameManager.Instance.OnUpgradeChosen();
    }

    public void Reset()
    {
        MeleeDamageMultiplier  = 1f;
        RangedDamageMultiplier = 1f;
    }
}
