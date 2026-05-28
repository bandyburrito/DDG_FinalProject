using UnityEngine;

public enum Faction { Player, Enemy }

public abstract class Entity : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP          = 100;
    public int speed          = 1;
    public int moveRange      = 3;
    public int meleeDamage    = 15;
    public int rangedDamage   = 8;
    public int rangedRangeMin = 2;
    public int rangedRangeMax = 4;

    [Header("Faction")]
    public Faction faction = Faction.Enemy;

    public int CurrentHP { get; protected set; }
    public bool IsAlive => CurrentHP > 0;
    public Vector2Int GridPos { get; protected set; }
    public bool HasMoved { get; protected set; }
    public bool HasActed { get; protected set; }

    public event System.Action<Entity> OnDeath;
    public event System.Action<int, int> OnHPChanged;

    protected virtual void Awake()
    {
        CurrentHP = maxHP;
    }

    public virtual void OnTurnBegin()
    {
        HasMoved = false;
        HasActed = false;
    }

    public virtual void MoveTo(Vector2Int newPos)
    {
        var oldPos = GridPos;
        GridManager.Instance.ClearOccupant(GridPos);
        GridPos = newPos;
        GridManager.Instance.SetOccupant(newPos, this);
        transform.position = GridManager.Instance.GridToWorld(newPos);

        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.sortingOrder = GridManager.Instance.GetSortOrder(newPos);

        UpdateFacingFromMove(oldPos, newPos);

        HasMoved = true;
        TrapSystem.Instance.ResolveTrap(this, newPos);
    }

    /// <summary>
    /// Flip sprite horizontally based on movement direction.
    /// Default sprite faces right-down (south-east in iso).
    /// Moving toward grid-left (x decreasing) or grid-down (y decreasing) → face left.
    /// </summary>
    private void UpdateFacingFromMove(Vector2Int from, Vector2Int to)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        // In iso projection, grid +x is up-right and grid +y is up-left.
        // The visual horizontal axis is (gridX - gridY). If that decreases, we moved left.
        int oldDir = from.x - from.y;
        int newDir = to.x - to.y;
        if (newDir < oldDir)      sr.flipX = true;
        else if (newDir > oldDir) sr.flipX = false;
    }

    /// <summary>Face a target tile (for attacks). Same left/right flip logic.</summary>
    public void FaceTarget(Vector2Int target)
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;
        int myDir     = GridPos.x - GridPos.y;
        int targetDir = target.x - target.y;
        if (targetDir < myDir)      sr.flipX = true;
        else if (targetDir > myDir) sr.flipX = false;
    }

    public virtual void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP, maxHP);
        if (CurrentHP <= 0) Die();
    }

    protected virtual void Die()
    {
        GridManager.Instance.ClearOccupant(GridPos);
        TurnManager.Instance.UnregisterEntity(this);
        OnDeath?.Invoke(this);
        gameObject.SetActive(false);
    }

    public void PlaceAt(Vector2Int pos)
    {
        GridPos = pos;
        GridManager.Instance.SetOccupant(pos, this);
        transform.position = GridManager.Instance.GridToWorld(pos);

        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.sortingOrder = GridManager.Instance.GetSortOrder(pos);
    }
}
