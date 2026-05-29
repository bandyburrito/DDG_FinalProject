using UnityEngine;
using System.Collections;

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

        // Hit feedback — particle burst. Red for player damage (ouch),
        // bright yellow for enemy hits (positive "you landed it" feedback).
        Color burstTint = faction == Faction.Player
            ? new Color(0.95f, 0.25f, 0.25f, 1f)
            : new Color(1.00f, 0.92f, 0.30f, 1f);
        HitBurst.SpawnAt(transform.position + Vector3.up * 0.7f, burstTint, 9);

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

    /// <summary>
    /// Smoothly lerp the visual position to the next tile over a short duration, then
    /// commit the grid state via MoveTo (which updates occupancy, sort order, and fires
    /// traps on arrival). Facing flips at the start of the walk so the sprite "looks"
    /// in the direction it's about to head — animation hooks can attach to this method.
    /// </summary>
    public IEnumerator WalkToTileSmooth(Vector2Int targetTile, float duration = 0.18f)
    {
        Vector3 fromVisual = transform.position;
        Vector3 toVisual   = GridManager.Instance.GridToWorld(targetTile);

        FaceTarget(targetTile);   // flip sprite at the START so it leads the movement

        float t = 0f;
        while (t < duration && IsAlive)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(fromVisual, toVisual, Mathf.Clamp01(t / duration));
            yield return null;
        }
        if (!IsAlive) yield break;

        transform.position = toVisual;

        // Commit grid state — this is when traps fire, occupancy updates, and sort
        // order is recomputed. Player visually arrives AT the trap tile before the
        // trap effect fires, which reads correctly to the eye.
        MoveTo(targetTile);
    }
}
