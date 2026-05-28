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

    public void ResolveTrap(Entity entity, Vector2Int pos)
    {
        var tile = GridManager.Instance.GetTile(pos);
        if (tile == null || !tile.HasTrap) return;

        switch (tile.type)
        {
            case TileType.Spike:
                entity.TakeDamage(spikeDamage);
                GridManager.Instance.SetTileType(pos, TileType.Empty);
                Debug.Log($"{entity.name} hit a spike trap.");
                break;

            case TileType.Pit:
                entity.TakeDamage(9999);
                Debug.Log($"{entity.name} fell into a pit!");
                break;

            case TileType.SlowZone:
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
