using UnityEngine;
using System.Collections.Generic;

public class RoomGenerator : MonoBehaviour
{
    public static RoomGenerator Instance { get; private set; }

    [Header("Trap Counts (fixed, not density)")]
    [Range(0, 4)] public int spikeCount    = 1;
    [Range(0, 2)] public int pitCount      = 0;
    [Range(0, 3)] public int slowZoneCount = 1;

    [Header("Obstacles")]
    [Range(0, 6)] public int obstacleCount = 2;

    [Header("Void Tiles — missing ground / chipped edges")]
    [Range(0, 8)] public int cornerChipMax  = 5;   // max void tiles chipped from corners
    [Range(0, 4)] public int interiorHoleMax = 2;  // max void tiles in interior

    private List<Vector2Int> _spawnPool = new();
    private int _spawnIndex = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void GenerateRoom(int wave)
    {
        Random.InitState(System.DateTime.Now.Millisecond + wave * 1337);
        GridManager.Instance.InitialiseGrid();

        // Carve void tiles BEFORE placing anything else so traps/obstacles can't land in voids
        CarveVoids();

        PlaceRandom(TileType.Obstacle, obstacleCount, border: 1);
        PlaceRandom(TileType.Spike,    spikeCount,    border: 1);
        PlaceRandom(TileType.Pit,      pitCount,      border: 1);
        PlaceRandom(TileType.SlowZone, slowZoneCount, border: 1);

        BuildSpawnPool(border: 0);

        // Player at first spawn
        if (_spawnPool.Count == 0) return;
        var playerSpawn = _spawnPool[0];
        _spawnPool.RemoveAt(0);
        _spawnIndex = 0;

        var player = PlayerController.Instance;
        if (player != null) player.PlaceAt(playerSpawn);
    }

    /// <summary>
    /// Carve missing-ground tiles from the room so each wave has a different silhouette.
    /// Chips small clusters out of each corner, then drops 0–N interior holes.
    /// Voids block movement and pathfinding (treated like Obstacle for BFS, but render as nothing).
    /// </summary>
    private void CarveVoids()
    {
        int w = GridManager.Instance.width;
        int h = GridManager.Instance.height;

        // ── Chip corners ───────────────────────────────────────────────────────
        // Each corner gets a random small cluster bitten out of it.
        Vector2Int[] corners =
        {
            new(0, 0), new(w - 1, 0), new(0, h - 1), new(w - 1, h - 1)
        };

        int totalCornerChips = Random.Range(2, cornerChipMax + 1);
        int chipsPerCornerAvg = Mathf.Max(1, totalCornerChips / 4);

        foreach (var corner in corners)
        {
            int chips = Random.Range(0, chipsPerCornerAvg + 2);
            for (int i = 0; i < chips; i++)
            {
                // Pick a tile within a 2-tile reach of the corner, biased toward the corner itself.
                int dx = Random.Range(0, 3);
                int dy = Random.Range(0, 3);
                var p = new Vector2Int(
                    corner.x == 0 ? dx : corner.x - dx,
                    corner.y == 0 ? dy : corner.y - dy
                );
                TryCarve(p);
            }
        }

        // ── Interior holes ─────────────────────────────────────────────────────
        // A small number of single-tile voids away from the borders. Player can walk around them.
        int holes = Random.Range(0, interiorHoleMax + 1);
        for (int i = 0; i < holes; i++)
        {
            int x = Random.Range(2, w - 2);
            int y = Random.Range(2, h - 2);
            TryCarve(new Vector2Int(x, y));
        }
    }

    private void TryCarve(Vector2Int p)
    {
        if (!GridManager.Instance.IsInBounds(p)) return;
        var tile = GridManager.Instance.GetTile(p);
        if (tile != null && tile.type == TileType.Empty)
            GridManager.Instance.SetTileType(p, TileType.Void);
    }

    private void PlaceRandom(TileType type, int count, int border)
    {
        int w = GridManager.Instance.width;
        int h = GridManager.Instance.height;
        int placed = 0, attempts = 0;

        while (placed < count && attempts < count * 20)
        {
            attempts++;
            int x = Random.Range(border, w - border);
            int y = Random.Range(border, h - border);
            var pos = new Vector2Int(x, y);
            var tile = GridManager.Instance.GetTile(pos);
            if (tile != null && tile.type == TileType.Empty)
            {
                GridManager.Instance.SetTileType(pos, type);
                placed++;
            }
        }
    }

    private void BuildSpawnPool(int border)
    {
        _spawnPool.Clear();
        int w = GridManager.Instance.width;
        int h = GridManager.Instance.height;

        // All edge tiles
        var edges = new List<Vector2Int>();
        for (int x = border; x < w - border; x++)
        {
            edges.Add(new Vector2Int(x, border));
            edges.Add(new Vector2Int(x, h - 1 - border));
        }
        for (int y = border + 1; y < h - 1 - border; y++)
        {
            edges.Add(new Vector2Int(border, y));
            edges.Add(new Vector2Int(w - 1 - border, y));
        }

        edges.RemoveAll(p =>
        {
            var tile = GridManager.Instance.GetTile(p);
            return tile != null && tile.type != TileType.Empty;
        });

        // Shuffle
        for (int i = edges.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (edges[i], edges[j]) = (edges[j], edges[i]);
        }

        // Pick spawns with min 3 tile separation
        const int MIN_DIST = 3;
        foreach (var c in edges)
        {
            bool tooClose = false;
            foreach (var existing in _spawnPool)
                if (GridManager.Instance.ManhattanDistance(c, existing) < MIN_DIST)
                { tooClose = true; break; }
            if (tooClose) continue;
            GridManager.Instance.SetTileType(c, TileType.Empty);
            _spawnPool.Add(c);
            if (_spawnPool.Count >= 10) break;
        }
    }

    public Vector2Int GetEnemySpawnPoint()
    {
        if (_spawnPool.Count == 0) return new Vector2Int(0, 0);
        var pos = _spawnPool[_spawnIndex % _spawnPool.Count];
        _spawnIndex++;
        return pos;
    }

    /// <summary>Find an empty tile adjacent to the player for companion spawn.</summary>
    public Vector2Int GetCompanionSpawnNearPlayer()
    {
        var playerPos = PlayerController.Instance.GridPos;
        foreach (var nb in GridManager.Instance.GetAllNeighbours8(playerPos))
            if (GridManager.Instance.IsWalkable(nb)) return nb;
        return playerPos + Vector2Int.right; // fallback
    }
}
