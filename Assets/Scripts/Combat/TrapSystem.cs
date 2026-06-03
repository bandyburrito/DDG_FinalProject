using UnityEngine;

public class TrapSystem : MonoBehaviour
{
    public static TrapSystem Instance { get; private set; }

    [Header("Trap Damage")]
    public int spikeDamage = 10;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Trap feedback colours — chosen to read as the EFFECT, not just the tile:
    //   Spike → orange sparks (matches the spike sprite, says "ouch / hazard")
    //   Pit   → deep red "blood" spray (death cue, regardless of faction)
    //   Slow  → muddy brown spray (says "you're stuck / slowed")
    private static readonly Color SpikeTint = new Color(1.00f, 0.55f, 0.10f, 1f);
    private static readonly Color PitTint   = new Color(0.72f, 0.10f, 0.12f, 1f);
    private static readonly Color SlowTint  = new Color(0.45f, 0.32f, 0.16f, 1f);

    public void ResolveTrap(Entity entity, Vector2Int pos)
    {
        var tile = GridManager.Instance.GetTile(pos);
        if (tile == null || !tile.HasTrap) return;

        // Spawn the cue at the trap's grid position (not entity.transform) so it still
        // appears even when a Pit deactivates the entity on the same frame.
        Vector3 fx = GridManager.Instance.GridToWorld(pos) + Vector3.up * 0.45f;

        switch (tile.type)
        {
            case TileType.Spike:
                HitBurst.SpawnAt(fx, SpikeTint, 10);
                entity.TakeDamage(spikeDamage);
                GridManager.Instance.SetTileType(pos, TileType.Empty);
                Debug.Log($"{entity.name} hit a spike trap.");
                break;

            case TileType.Pit:
                // Bigger, redder spray — this is a kill. Fire BEFORE TakeDamage since
                // the lethal hit deactivates the entity's GameObject immediately.
                HitBurst.SpawnAt(fx, PitTint, 16);
                entity.TakeDamage(9999);
                Debug.Log($"{entity.name} fell into a pit!");
                break;

            case TileType.SlowZone:
                HitBurst.SpawnAt(fx, SlowTint, 9);
                if (entity.GetComponent<SlowEffect>() == null)
                    entity.gameObject.AddComponent<SlowEffect>();
                Debug.Log($"{entity.name} stepped into a slow zone.");
                break;
        }
    }
}

public class SlowEffect : MonoBehaviour
{
    private Entity _entity;
    private int _originalMoveRange;
    private bool _applied;

    void Start()
    {
        _entity = GetComponent<Entity>();
        _originalMoveRange = _entity.moveRange;
        _entity.moveRange = Mathf.Max(1, _entity.moveRange - 1);
    }

    void Update()
    {
        if (_applied && !_entity.HasMoved)
        {
            _entity.moveRange = _originalMoveRange;
            Destroy(this);
        }
        if (_entity.HasMoved) _applied = true;
    }
}
