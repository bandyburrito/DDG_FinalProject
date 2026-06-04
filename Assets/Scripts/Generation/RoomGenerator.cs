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

    [Header("Room Size Variety")]
    [Range(6, 10)] public int baseSize  = 8;   // smallest room dimension
    [Range(0, 5)]  public int maxGrowth = 3;   // extra tiles each axis may grow

    public void GenerateRoom(int wave)
    {
        Random.InitState(System.DateTime.Now.Millisecond + wave * 1337);

        // ── Vary the room size per wave ──────────────────────────────────────
        // Base square, then grow each axis 0..maxGrowth INDEPENDENTLY so rooms range
        // from a tight 8×8 to a sprawling 11×11 with non-square ("weird") footprints —
        // no longer locked to one fixed size every wave.
        GridManager.Instance.width  = baseSize + Random.Range(0, maxGrowth + 1);
        GridManager.Instance.height = baseSize + Random.Range(0, maxGrowth + 1);

        GridManager.Instance.InitialiseGrid();

        // Re-frame the camera to fit whatever size we just rolled.
        GridManager.Instance.FitCamera(Camera.main);

        // Carve void tiles BEFORE placing anything else so traps/obstacles can't land in voids
        CarveVoids();

        // Bigger rooms get proportionally more clutter so the extra space isn't empty.
        int area      = GridManager.Instance.width * GridManager.Instance.height;
        int areaBonus = Mathf.Clamp((area - baseSize * baseSize) / 20, 0, 4);

        PlaceRandom(TileType.Obstacle, obstacleCount + areaBonus,     border: 1);
        PlaceRandom(TileType.Spike,    spikeCount + areaBonus / 2,    border: 1);
        PlaceRandom(TileType.Pit,      pitCount,                      border: 1);
        PlaceRandom(TileType.SlowZone, slowZoneCount + areaBonus / 2, border: 1);

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
        // Scales with room area so bigger maps aren't a flat empty plain.
        int holes = Random.Range(0, interiorHoleMax + 1 + (w + h) / 8);
        for (int i = 0; i < holes; i++)
        {
            int x = Random.Range(2, w - 2);
            int y = Random.Range(2, h - 2);
            TryCarve(new Vector2Int(x, y));
        }

        // ── Edge erosion ───────────────────────────────────────────────────────
        // Bite random single tiles out of the four edges for a jagged, irregular outline
        // (not just clean corners). More bites on bigger rooms = weirder shapes.
        int edgeBites = Random.Range(2, 4 + (w + h) / 4);
        for (int i = 0; i < edgeBites; i++)
        {
            // Pick a random point on a random edge.
            Vector2Int p = Random.Range(0, 4) switch
            {
                0 => new Vector2Int(Random.Range(0, w), 0),        // bottom
                1 => new Vector2Int(Random.Range(0, w), h - 1),    // top
                2 => new Vector2Int(0, Random.Range(0, h)),        // left
                _ => new Vector2Int(w - 1, Random.Range(0, h)),    // right
            };
            TryCarve(p);
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

        // Find the largest connected playable area — voids + obstacles can split the room
        // into pockets, and the player + enemies must all spawn in the SAME pocket so they
        // can reach each other. Without this, you can get trapped behind a void wall.
        var mainArea = ComputeLargestConnectedComponent();

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
            // Reject any tile that's not Empty, not in the main playable area, or that
            // has no walkable cardinal neighbour (an entity spawned there couldn't move
            // on turn 1 — the "trapped at spawn" symptom).
            if (tile == null || tile.type != TileType.Empty) return true;
            if (!mainArea.Contains(p)) return true;
            return !HasWalkableNeighbour(p);
        });

        // If the edges came up empty (extremely irregular room shape), fall back to ANY
        // tile in the main area with a walkable neighbour — better than no spawns at all.
        if (edges.Count == 0)
        {
            foreach (var p in mainArea)
            {
                var tile = GridManager.Instance.GetTile(p);
                if (tile != null && tile.type == TileType.Empty && HasWalkableNeighbour(p))
                    edges.Add(p);
            }
        }

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

    /// <summary>True if at least one cardinal neighbour of <paramref name="p"/> is NOT
    /// an Obstacle, Void, or out-of-bounds — i.e. the entity at p has somewhere to walk.</summary>
    private static bool HasWalkableNeighbour(Vector2Int p)
    {
        foreach (var nb in GridManager.Instance.GetCardinalNeighbours(p))
        {
            var t = GridManager.Instance.GetTile(nb);
            if (t == null) continue;
            if (t.type == TileType.Obstacle || t.type == TileType.Void) continue;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Flood-fill across non-blocking tiles and return the largest pocket — the only
    /// area we'll spawn into. Treats Obstacle and Void as walls; everything else (Empty +
    /// traps) is walkable. Used to guarantee the player and enemies can actually reach
    /// each other even after void-erosion carves the room.
    /// </summary>
    private static HashSet<Vector2Int> ComputeLargestConnectedComponent()
    {
        int w = GridManager.Instance.width;
        int h = GridManager.Instance.height;
        var visited = new HashSet<Vector2Int>();
        HashSet<Vector2Int> largest = new();

        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            var start = new Vector2Int(x, y);
            if (visited.Contains(start)) continue;
            var startTile = GridManager.Instance.GetTile(start);
            if (startTile == null) continue;
            if (startTile.type == TileType.Obstacle || startTile.type == TileType.Void) continue;

            var component = new HashSet<Vector2Int> { start };
            var queue     = new Queue<Vector2Int>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                foreach (var nb in GridManager.Instance.GetCardinalNeighbours(cur))
                {
                    if (visited.Contains(nb)) continue;
                    var t = GridManager.Instance.GetTile(nb);
                    if (t == null) continue;
                    if (t.type == TileType.Obstacle || t.type == TileType.Void) continue;
                    visited.Add(nb);
                    component.Add(nb);
                    queue.Enqueue(nb);
                }
            }
            if (component.Count > largest.Count) largest = component;
        }
        return largest;
    }

    public Vector2Int GetEnemySpawnPoint()
    {
        if (_spawnPool.Count == 0) return new Vector2Int(0, 0);
        var pos = _spawnPool[_spawnIndex % _spawnPool.Count];
        _spawnIndex++;
        return pos;
    }

    /// <summary>Find an empty tile adjacent to the player for companion spawn. Prefers
    /// tiles in the main playable area with a walkable neighbour, so a newly-spawned
    /// companion can't end up stranded behind a void or unable to move on its first turn.</summary>
    public Vector2Int GetCompanionSpawnNearPlayer()
    {
        var playerPos = PlayerController.Instance.GridPos;
        var mainArea  = ComputeLargestConnectedComponent();

        // First pass: in-area neighbour with somewhere to walk.
        foreach (var nb in GridManager.Instance.GetAllNeighbours8(playerPos))
            if (GridManager.Instance.IsWalkable(nb) && mainArea.Contains(nb) && HasWalkableNeighbour(nb))
                return nb;

        // Second pass: any walkable neighbour (better than no companion at all).
        foreach (var nb in GridManager.Instance.GetAllNeighbours8(playerPos))
            if (GridManager.Instance.IsWalkable(nb)) return nb;

        return playerPos + Vector2Int.right; // last-resort fallback
    }
}
