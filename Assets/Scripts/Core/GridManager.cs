using UnityEngine;
using System.Collections.Generic;

public enum TileType { Empty, Obstacle, Spike, Pit, SlowZone, Void }

[System.Serializable]
public class TileData
{
    public TileType type;
    public Entity occupant;

    public bool IsWalkable => type != TileType.Obstacle && type != TileType.Pit && type != TileType.Void && occupant == null;
    public bool HasTrap    => type == TileType.Spike || type == TileType.Pit || type == TileType.SlowZone;
    /// <summary>True for tiles where the ground is missing — entities can't stand there and nothing renders.</summary>
    public bool IsVoid     => type == TileType.Void;
}

/// <summary>
/// 8x8 isometric grid. World positions are projected so that grid axes
/// map to a diamond layout. Sprites can be plain squares — we tilt them
/// via transform rotation+scale to look isometric.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid")]
    public int width  = 8;
    public int height = 8;

    [Header("Isometric Projection")]
    public float tileWidth  = 1.0f;   // half-diamond width (X spacing)
    public float tileHeight = 0.5f;   // half-diamond height (Y spacing)

    [Header("Tile Prefabs")]
    public GameObject groundTilePrefab;
    /// <summary>Sprites the floor can render with. Each Empty tile picks one at random
    /// when it spawns, so the ground reads as varied instead of a single repeated texture.
    /// Populated by PlaceholderSetup at boot; if empty, the prefab's own sprite is used.</summary>
    public Sprite[] groundSpriteVariants;
    public GameObject obstacleTilePrefab;
    public GameObject spikeTrapPrefab;
    public GameObject pitTrapPrefab;
    public GameObject slowZonePrefab;
    public GameObject moveHighlightPrefab;
    public GameObject attackHighlightPrefab;
    public GameObject telegraphAttackPrefab;
    public GameObject telegraphMovePrefab;

    private TileData[,] _grid;
    private GameObject[,] _tileObjects;
    private List<GameObject> _highlights = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void InitialiseGrid()
    {
        // Tear down the PREVIOUS grid using ITS OWN dimensions — not the new width/height.
        // The room size can change between waves; indexing the old array with the new
        // (possibly larger) bounds would throw IndexOutOfRangeException and abort the
        // whole room build (the "map failed to spawn on round 2" bug).
        if (_tileObjects != null)
        {
            int oldW = _tileObjects.GetLength(0);
            int oldH = _tileObjects.GetLength(1);
            for (int x = 0; x < oldW; x++)
                for (int y = 0; y < oldH; y++)
                    if (_tileObjects[x, y]) Destroy(_tileObjects[x, y]);
        }

        _grid        = new TileData[width, height];
        _tileObjects = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            _grid[x, y] = new TileData { type = TileType.Empty };
            SpawnTileVisual(new Vector2Int(x, y), TileType.Empty);
        }
    }

    // ── Tile Type Mutation ────────────────────────────────────────────────────

    public void SetTileType(Vector2Int pos, TileType type)
    {
        if (!IsInBounds(pos)) return;
        _grid[pos.x, pos.y].type = type;
        SpawnTileVisual(pos, type);
    }

    private void SpawnTileVisual(Vector2Int pos, TileType type)
    {
        if (_tileObjects[pos.x, pos.y] != null) Destroy(_tileObjects[pos.x, pos.y]);

        GameObject prefab = type switch
        {
            TileType.Obstacle => obstacleTilePrefab,
            TileType.Spike    => spikeTrapPrefab,
            TileType.Pit      => pitTrapPrefab,
            TileType.SlowZone => slowZonePrefab,
            TileType.Void     => null,             // no ground rendered for voids
            _                 => groundTilePrefab
        };
        if (prefab == null) return;

        var go = Instantiate(prefab, GridToWorld(pos), Quaternion.identity, transform);
        go.SetActive(true);
        go.name = $"Tile_{pos.x}_{pos.y}";
        // Ground tiles always rendered behind highlights and entities
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr) sr.sortingOrder = -100 + (height - pos.y);

        // Per-tile sprite variation for ground — randomly swap to one of the variant
        // sprites so the floor doesn't read as one uniform repeated texture.
        if (sr != null && type == TileType.Empty
            && groundSpriteVariants != null && groundSpriteVariants.Length > 0)
        {
            var v = groundSpriteVariants[Random.Range(0, groundSpriteVariants.Length)];
            if (v != null) sr.sprite = v;
        }

        _tileObjects[pos.x, pos.y] = go;
    }

    // ── Coordinate Conversion (Isometric) ─────────────────────────────────────

    public Vector3 GridToWorld(Vector2Int pos)
    {
        // Standard 2:1 isometric projection
        float x = (pos.x - pos.y) * tileWidth;
        float y = (pos.x + pos.y) * tileHeight;
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// Tiles are drawn so the cube's TOP FACE sits in the upper portion of the sprite,
    /// while entities pivot at their feet (sprite y = 0). Placing an entity at the raw
    /// tile coord puts its feet at the tile's *centre*, which reads as "sunk into the
    /// cube" rather than "standing on top". This lifts the entity so its feet visually
    /// land on the front edge of the diamond top face — the iso "ground" of that tile.
    /// </summary>
    public const float EntityYOffset = 0.42f;

    public Vector3 GridToWorldEntity(Vector2Int pos)
        => GridToWorld(pos) + Vector3.up * EntityYOffset;

    /// <summary>Small lift for the move/attack highlight overlays so they sit centred on
    /// the new tile art's top face (which is in the upper-middle of the canvas, not the
    /// exact pivot centre). Without this the highlight slid below the front edge of the
    /// diamond and looked like it was floating under the tile.</summary>
    public const float HighlightYOffset = 0.10f;

    public Vector2Int WorldToGrid(Vector3 world)
    {
        // Inverse of GridToWorld
        float gx = (world.x / tileWidth + world.y / tileHeight) * 0.5f;
        float gy = (world.y / tileHeight - world.x / tileWidth) * 0.5f;
        return new Vector2Int(Mathf.RoundToInt(gx), Mathf.RoundToInt(gy));
    }

    /// <summary>Entity sort order — above tiles and highlights, with Y-based depth.</summary>
    public int GetSortOrder(Vector2Int pos) => 100 + (height - pos.y) * 10;

    /// <summary>
    /// Re-frame an orthographic camera so the WHOLE current grid is visible, centred,
    /// with margin for entity/cube sprite overhang. Called after each wave's grid is
    /// (re)built so variable room sizes always fit on screen.
    ///
    /// Iso world bounds for a width×height grid (GridToWorld with the current tile sizes):
    ///   x ∈ [-(height-1)·tileWidth , (width-1)·tileWidth]
    ///   y ∈ [0 , (width-1 + height-1)·tileHeight]
    /// </summary>
    public void FitCamera(Camera cam, float marginX = 3.0f, float marginY = 2.5f)
    {
        if (cam == null || !cam.orthographic) return;

        float minX = -(height - 1) * tileWidth;
        float maxX =  (width  - 1) * tileWidth;
        float minY = 0f;
        float maxY = (width - 1 + height - 1) * tileHeight;

        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;

        float halfW = (maxX - minX) * 0.5f + marginX;
        float halfH = (maxY - minY) * 0.5f + marginY;

        float aspect = cam.aspect > 0.01f ? cam.aspect : 16f / 9f;
        // orthographicSize is HALF the vertical view height; width is size·aspect.
        cam.orthographicSize    = Mathf.Max(halfH, halfW / aspect);
        cam.transform.position  = new Vector3(centerX, centerY, cam.transform.position.z);
    }

    // ── Accessors ─────────────────────────────────────────────────────────────

    public TileData GetTile(Vector2Int pos) =>
        IsInBounds(pos) ? _grid[pos.x, pos.y] : null;

    public bool IsInBounds(Vector2Int pos) =>
        pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;

    public bool IsWalkable(Vector2Int pos) =>
        IsInBounds(pos) && _grid[pos.x, pos.y].IsWalkable;

    /// <summary>
    /// The single source of truth for "what blocks pathing" — used by BOTH
    /// GetReachableTiles (the player's move-range highlights) and FindPath (the
    /// click-to-walk pathfinder). Traps (Spike, Pit, SlowZone) are deliberately
    /// NOT blocked — the player must be able to step onto them so the trap
    /// system can fire (death by pit, slow effect, etc.). Only solid Obstacle
    /// tiles and missing-ground Void tiles block movement.
    ///
    /// AI movement uses TileData.IsWalkable instead, which DOES block pits so
    /// enemies don't suicide-walk into them.
    /// </summary>
    private static bool BlocksPath(TileData tile) =>
        tile == null || tile.type == TileType.Obstacle || tile.type == TileType.Void;

    public void SetOccupant(Vector2Int pos, Entity e)
    { if (IsInBounds(pos)) _grid[pos.x, pos.y].occupant = e; }

    public void ClearOccupant(Vector2Int pos)
    { if (IsInBounds(pos)) _grid[pos.x, pos.y].occupant = null; }

    // ── Highlights ────────────────────────────────────────────────────────────

    public void ClearHighlights()
    {
        foreach (var h in _highlights) if (h) Destroy(h);
        _highlights.Clear();
    }

    public void ShowMoveHighlights(List<Vector2Int> tiles)
    {
        ClearHighlights();
        foreach (var t in tiles) SpawnHighlight(moveHighlightPrefab, t);
    }

    public void ShowAttackHighlights(List<Vector2Int> tiles)
    {
        foreach (var t in tiles) SpawnHighlight(attackHighlightPrefab, t);
    }

    public GameObject SpawnTelegraphAttack(Vector2Int tile)  => SpawnHighlight(telegraphAttackPrefab, tile);
    public GameObject SpawnTelegraphMove(Vector2Int tile)    => SpawnHighlight(telegraphMovePrefab, tile);

    private GameObject SpawnHighlight(GameObject prefab, Vector2Int tile)
    {
        if (prefab == null) return null;
        // Lift the highlight by HighlightYOffset so the procedural diamond sprite sits
        // on the new tile art's top face. Without it, the highlight rendered at the tile
        // sprite's pivot centre, which landed BELOW the front edge of the diamond — the
        // "goofy" floating highlight under the cube the user reported.
        var spawnPos = GridToWorld(tile) + Vector3.up * HighlightYOffset + Vector3.back * 0.05f;
        var go = Instantiate(prefab, spawnPos, Quaternion.identity);
        go.SetActive(true);
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr) sr.sortingOrder = -50;  // Above tiles (-92..-99), below entities (110+)
        _highlights.Add(go);
        return go;
    }

    public void RemoveHighlight(GameObject go)
    {
        _highlights.Remove(go);
        if (go) Destroy(go);
    }

    // ── BFS Pathfinding ───────────────────────────────────────────────────────

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, bool ignoreOccupants = false)
    {
        if (start == goal) return new List<Vector2Int>();
        var visited = new Dictionary<Vector2Int, Vector2Int>();
        var queue   = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited[start] = start;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal) return Reconstruct(visited, start, goal);

            foreach (var nb in GetCardinalNeighbours(current))
            {
                if (visited.ContainsKey(nb)) continue;
                var tile = GetTile(nb);
                if (BlocksPath(tile)) continue;                // obstacles + voids only — traps remain walkable
                // Never route through OR land on a tile another character occupies.
                // (Previously the goal was exempted via `nb != goal`, which let two entities
                // plan onto the same tile and end up stacked — telegraphs are computed at
                // round start, so a planned destination can be taken before the turn runs.)
                if (!ignoreOccupants && tile.occupant != null) continue;
                visited[nb] = current;
                queue.Enqueue(nb);
            }
        }
        return null;
    }

    private List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> came, Vector2Int start, Vector2Int goal)
    {
        var path = new List<Vector2Int>();
        var cur = goal;
        while (cur != start) { path.Add(cur); cur = came[cur]; }
        path.Reverse();
        return path;
    }

    public List<Vector2Int> GetCardinalNeighbours(Vector2Int pos)
    {
        var list = new List<Vector2Int>(4);
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var d in dirs) { var n = pos + d; if (IsInBounds(n)) list.Add(n); }
        return list;
    }

    public List<Vector2Int> GetAllNeighbours8(Vector2Int pos)
    {
        var list = new List<Vector2Int>(8);
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            var n = pos + new Vector2Int(dx, dy);
            if (IsInBounds(n)) list.Add(n);
        }
        return list;
    }

    public int ManhattanDistance(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    public int ChebyshevDistance(Vector2Int a, Vector2Int b) =>
        Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

    /// <summary>
    /// Depth-limited BFS — returns every tile reachable from <paramref name="start"/> in
    /// ≤ <paramref name="maxSteps"/> cardinal moves. Used by the move-highlight system so
    /// the highlights only mark tiles the player can actually walk to. The previous
    /// implementation used Chebyshev distance, which includes diagonals — but pathfinding
    /// is 4-directional, so the player would stop short on any tile that requires a turn.
    /// </summary>
    public List<Vector2Int> GetReachableTiles(Vector2Int start, int maxSteps, bool ignoreOccupants = false)
    {
        var result = new List<Vector2Int>();
        var depth  = new Dictionary<Vector2Int, int> { [start] = 0 };
        var queue  = new Queue<Vector2Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int d = depth[current];
            if (d >= maxSteps) continue;

            foreach (var nb in GetCardinalNeighbours(current))
            {
                if (depth.ContainsKey(nb)) continue;
                var tile = GetTile(nb);
                if (BlocksPath(tile)) continue;                // obstacles + voids only — traps remain walkable
                if (!ignoreOccupants && tile.occupant != null) continue;

                depth[nb] = d + 1;
                queue.Enqueue(nb);
                result.Add(nb);
            }
        }
        return result;
    }
}
