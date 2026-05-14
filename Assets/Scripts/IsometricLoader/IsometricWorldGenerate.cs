using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;


namespace finished1
{

    public class IsometricWorldGenerate : MonoBehaviour
    {
        public Tilemap bottomTilemap;

        public Tilemap topTilemap;

        public TileBase bottomTile;

        public TileBase walkableTile;

        public Transform player;
        public GameObject playerPrefab;
        public Transform playerParent;
        public bool instantiatePlayerPrefabIfMissing = true;
        public bool spawnPlayerOnStart = true;
        public bool requireWalkableSpawn = true;
        public int cornerSpawnMaxOffset = 8;
        public int cornerSpawnAttempts = 80;
        public float spawnZOffset = 0f;

        [Min(1)] public int width = 200;
        [Min(1)] public int height = 200;
        public Vector3Int origin = Vector3Int.zero;

        public bool useRandomSeed = true;
        public int seed = 12345;
        public int minimumDistanceBetweenClusters = 5;
        [Min(1)]
        public int clusterCenterPlacementAttempts = 30;
        public bool enforceMinimumDistanceBetweenClusters = true;

        public bool useClusters = true;
        public int clusterCount = 25;
        [Min(0)] public int clusterRadiusMin = 2;
        [Min(0)] public int clusterRadiusMax = 7;

        [Range(0f, 1f)] public float clusterFillChance = 0.65f;

        [Min(0)] public int additionalRandomWalkableCells = 150;

        [Range(0f, 1f)] public float walkableChancePerCell = 0.02f;

        [Min(0)] public int edgePadding = 0;

        [Min(1)] public int tilesPerFrame = 2000;

        public int bottomSortingOrder = 0;
        public int topSortingOrder = 10;

        public Camera sceneCameraOverride;

        public bool adjustCinemachineTargetOffset = true;
        public Component cameraPositionComposerOverride;
        [Range(0f, 1f)]
        public float cameraDeadZoneNormalized = 0.15f;
        public float maxTargetOffsetX = 1.0f;
        public float maxTargetOffsetY = 0.75f;
        public bool updateCameraOffsetEveryFrame = true;

        public bool bakeNavMeshOnGenerate = true;
        public Component navMeshSurface;
        
        public int navMeshBuildWaitFrames = 1;
        public UnityEvent onNavMeshBaked;

        // Walkability set in tilemap cell coordinates.
        private readonly HashSet<Vector3Int> _walkableCells = new HashSet<Vector3Int>();

        private Transform _runtimePlayer;
        private Vector3 _mapCenterWorld;
        private Vector3 _mapHalfExtentsWorld;
        private bool _mapBoundsValid;

        public Component _cameraPositionComposer;
        private Vector3 _initialTargetOffset;
        private bool _capturedInitialTargetOffset;

        public Component cinemachineCameraOverride;
        public Component _cinemachineCamera;
        private bool _cinemachineTargetInitialized;

        /// <summary>Returns true if the given tilemap cell is walkable.</summary>
        public bool IsWalkableCell(Vector3Int cell) => _walkableCells.Contains(cell);

        /// <summary>Returns true if the given world position is walkable (top tilemap cell coordinates).</summary>
        public bool IsWalkableWorld(Vector3 worldPosition) => IsWalkableCell(topTilemap.WorldToCell(worldPosition));

        private void Start()
        {
            Regenerate();
        }

        private void LateUpdate()
        {
            if (!adjustCinemachineTargetOffset || !updateCameraOffsetEveryFrame) return;
            if (!_mapBoundsValid) return;

            var target = ResolvePlayerTransform();
            if (target == null) return;
            ApplyCinemachineTargetOffset(target.position);
        }

        /// <summary>
        /// Regenerates the map. You can call this from other scripts to rebuild the world.
        /// </summary>
        public void Regenerate()
        {
            StopAllCoroutines();
            StartCoroutine(GenerateRoutine());
        }

        private IEnumerator GenerateRoutine()
        {
            if (bottomTilemap == null || topTilemap == null || bottomTile == null || walkableTile == null)
            {
                Debug.LogError($"{nameof(IsometricWorldGenerate)}: Assign bottom/top Tilemaps and both Tile assets in the Inspector.");
                yield break;
            }

            _mapBoundsValid = false;

            // Ensure renderers exist / ordering is sane.
            var bottomRenderer = bottomTilemap.GetComponent<TilemapRenderer>();
            if (bottomRenderer != null) bottomRenderer.sortingOrder = bottomSortingOrder;

            var topRenderer = topTilemap.GetComponent<TilemapRenderer>();
            if (topRenderer != null) topRenderer.sortingOrder = topSortingOrder;

            int actualSeed = useRandomSeed ? Random.Range(int.MinValue, int.MaxValue) : seed;
            var rng = new System.Random(actualSeed);
            _walkableCells.Clear();

            bottomTilemap.ClearAllTiles();
            topTilemap.ClearAllTiles();

            // 1) Fill the whole bottom with green (not walkable).
            int writtenThisFrame = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = origin + new Vector3Int(x, y, 0);
                    bottomTilemap.SetTile(cell, bottomTile);
                    writtenThisFrame++;

                    if (writtenThisFrame >= tilesPerFrame)
                    {
                        writtenThisFrame = 0;
                        yield return null;
                    }
                }
            }

            // 2) Decide which cells become walkable (purple overlay).
            if (useClusters)
            {
                int minX = origin.x + edgePadding;
                int minY = origin.y + edgePadding;
                int maxX = origin.x + width - 1 - edgePadding;
                int maxY = origin.y + height - 1 - edgePadding;

                if (minX > maxX || minY > maxY)
                {
                    // Edge padding is too large; fall back to no padding.
                    minX = origin.x;
                    minY = origin.y;
                    maxX = origin.x + width - 1;
                    maxY = origin.y + height - 1;
                }

                // Keep track of chosen cluster centers so we can enforce spacing.
                var placedCenters = new List<Vector2Int>(clusterCount);
                var placedRadii = new List<int>(clusterCount);

                for (int i = 0; i < clusterCount; i++)
                {
                    int centerX = 0;
                    int centerY = 0;
                    int radius = 0;
                    bool placed = false;

                    for (int attempt = 0; attempt < clusterCenterPlacementAttempts; attempt++)
                    {
                        int candidateX = rng.Next(minX, maxX + 1);
                        int candidateY = rng.Next(minY, maxY + 1);
                        int candidateRadius = rng.Next(clusterRadiusMin, clusterRadiusMax + 1);

                        if (!enforceMinimumDistanceBetweenClusters || minimumDistanceBetweenClusters <= 0 || placedCenters.Count == 0)
                        {
                            centerX = candidateX;
                            centerY = candidateY;
                            radius = candidateRadius;
                            placed = true;
                            break;
                        }

                        bool passesSeparation = true;
                        for (int c = 0; c < placedCenters.Count; c++)
                        {
                            var otherCenter = placedCenters[c];
                            int otherRadius = placedRadii[c];

                            int dx = candidateX - otherCenter.x;
                            int dy = candidateY - otherCenter.y;
                            int dist2 = dx * dx + dy * dy;

                            // Minimum allowed center-to-center distance so that cluster borders stay apart.
                            int minAllowed = otherRadius + candidateRadius + minimumDistanceBetweenClusters;
                            if (dist2 < minAllowed * minAllowed)
                            {
                                passesSeparation = false;
                                break;
                            }
                        }

                        if (passesSeparation)
                        {
                            centerX = candidateX;
                            centerY = candidateY;
                            radius = candidateRadius;
                            placed = true;
                            break;
                        }
                    }

                    if (!placed)
                    {
                        // Couldn't find a valid center within attempts; place anyway to avoid infinite loops.
                        centerX = rng.Next(minX, maxX + 1);
                        centerY = rng.Next(minY, maxY + 1);
                        radius = rng.Next(clusterRadiusMin, clusterRadiusMax + 1);
                    }

                    placedCenters.Add(new Vector2Int(centerX, centerY));
                    placedRadii.Add(radius);

                    int r2 = radius * radius;
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int cellX = centerX + dx;
                            int cellY = centerY + dy;
                            if (cellX < origin.x || cellX >= origin.x + width) continue;
                            if (cellY < origin.y || cellY >= origin.y + height) continue;

                            if ((dx * dx + dy * dy) <= r2)
                            {
                                if (rng.NextDouble() <= clusterFillChance)
                                {
                                    _walkableCells.Add(new Vector3Int(cellX, cellY, 0));
                                }
                            }
                        }
                    }
                }

                // Additional scattered walkable cells (isolated islands).
                for (int i = 0; i < additionalRandomWalkableCells; i++)
                {
                    int x = rng.Next(0, width);
                    int y = rng.Next(0, height);
                    _walkableCells.Add(origin + new Vector3Int(x, y, 0));
                }
            }
            else
            {
                // Simple random scatter.
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (rng.NextDouble() <= walkableChancePerCell)
                        {
                            _walkableCells.Add(origin + new Vector3Int(x, y, 0));
                        }
                    }
                }
            }

            // 3) Stamp walkable overlay tiles on the top tilemap.
            writtenThisFrame = 0;
            foreach (var cell in _walkableCells)
            {
                topTilemap.SetTile(cell, walkableTile);
                writtenThisFrame++;

                if (writtenThisFrame >= tilesPerFrame)
                {
                    writtenThisFrame = 0;
                    yield return null;
                }
            }

            CacheMapWorldBounds();

            if (spawnPlayerOnStart)
            {
                var targetPlayer = ResolvePlayerTransform();
                if (targetPlayer != null)
                {
                    player = targetPlayer; // keep inspector/reference consistent for other scripts
                    SpawnPlayerNearRandomCorner(rng, targetPlayer);
                    TryInitializeCinemachineTargetAndOffset();
                    ApplyCinemachineTargetOffset(targetPlayer.position);
                }
            }

            if (bakeNavMeshOnGenerate)
            {
                yield return BakeNavMeshRoutine();
            }
        }

        private void CacheMapWorldBounds()
        {
            var minCell = new Vector3Int(origin.x, origin.y, 0);
            var maxCell = new Vector3Int(origin.x + width - 1, origin.y + height - 1, 0);

            var minWorld = topTilemap.GetCellCenterWorld(minCell);
            var maxWorld = topTilemap.GetCellCenterWorld(maxCell);

            _mapCenterWorld = (minWorld + maxWorld) * 0.5f;
            _mapHalfExtentsWorld = (maxWorld - minWorld) * 0.5f;
            _mapHalfExtentsWorld = new Vector3(
                Mathf.Max(0.0001f, Mathf.Abs(_mapHalfExtentsWorld.x)),
                Mathf.Max(0.0001f, Mathf.Abs(_mapHalfExtentsWorld.y)),
                Mathf.Max(0.0001f, Mathf.Abs(_mapHalfExtentsWorld.z))
            );
            _mapBoundsValid = true;
        }

        private Transform ResolvePlayerTransform()
        {
            if (player != null) return player;

            if (playerPrefab == null || !instantiatePlayerPrefabIfMissing)
                return null;

            if (_runtimePlayer == null)
            {
                var go = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity, playerParent);
                _runtimePlayer = go.transform;
            }

            // In case the runtime instance got destroyed externally.
            if (_runtimePlayer == null) return null;
            return _runtimePlayer;
        }

        private void TryInitializeCinemachineTargetAndOffset()
        {
            if (!adjustCinemachineTargetOffset) return;

            if (_cameraPositionComposer == null)
            {
                _cameraPositionComposer = cameraPositionComposerOverride != null
                    ? cameraPositionComposerOverride
                    : FindFirstPositionComposerInScene();

                if (_cameraPositionComposer != null && !_capturedInitialTargetOffset)
                {
                    if (TryGetTargetOffset(_cameraPositionComposer, out var initial))
                    {
                        _initialTargetOffset = initial;
                        _capturedInitialTargetOffset = true;
                    }
                }
            }

            if (adjustCinemachineTargetOffset && !_cinemachineTargetInitialized)
            {
                _cinemachineCamera = cinemachineCameraOverride != null
                    ? cinemachineCameraOverride
                    : FindFirstCinemachineCameraInScene();
                _cinemachineTargetInitialized = true;
            }

            if (_cinemachineCamera != null && player != null)
            {
                // This is intentionally reflection-based so we don't need to depend on
                // the exact nested target property type at compile-time.
                SetCinemachineTrackingTargetViaReflection(player);
            }
        }

        private Component FindFirstPositionComposerInScene()
        {
            // Reflection-based find so this script compiles even if Cinemachine APIs change.
            var all = Resources.FindObjectsOfTypeAll<Component>();
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null) continue;
                var t = c.GetType();
                if (t == null) continue;
                if (t.Name == "CinemachinePositionComposer")
                    return c;
            }
            return null;
        }

        private Component FindFirstCinemachineCameraInScene()
        {
            var all = Resources.FindObjectsOfTypeAll<Component>();
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null) continue;
                var t = c.GetType();
                if (t == null) continue;
                if (t.Name == "CinemachineCamera")
                    return c;
            }
            return null;
        }

        private bool SetCinemachineTrackingTargetViaReflection(Transform newTarget)
        {
            if (_cinemachineCamera == null || newTarget == null) return false;

            var targetProp = _cinemachineCamera.GetType().GetProperty("Target");
            if (targetProp == null) return false;

            var targetObj = targetProp.GetValue(_cinemachineCamera);
            if (targetObj == null) return false;

            var trackingProp = targetObj.GetType().GetProperty("TrackingTarget");
            if (trackingProp == null || !trackingProp.CanWrite) return false;

            trackingProp.SetValue(targetObj, newTarget);
            targetProp.SetValue(_cinemachineCamera, targetObj);
            return true;
        }

        private void ApplyCinemachineTargetOffset(Vector3 playerWorldPos)
        {
            if (_cameraPositionComposer == null) return;

            float dx = playerWorldPos.x - _mapCenterWorld.x;
            float dy = playerWorldPos.y - _mapCenterWorld.y;

            float nx = dx / _mapHalfExtentsWorld.x; // -1..+1-ish
            float ny = dy / _mapHalfExtentsWorld.y;

            nx = Mathf.Clamp(nx, -1f, 1f);
            ny = Mathf.Clamp(ny, -1f, 1f);

            float absNx = Mathf.Abs(nx);
            float absNy = Mathf.Abs(ny);

            float mappedNx = MapDeadZone01(absNx, cameraDeadZoneNormalized);
            float mappedNy = MapDeadZone01(absNy, cameraDeadZoneNormalized);

            // If player is on -X, we shift the camera target toward +X (other side).
            float offsetX = -Mathf.Sign(nx) * mappedNx * maxTargetOffsetX;

            // If player is too high (+Y), shift camera target down (-Y).
            float offsetY = -Mathf.Sign(ny) * mappedNy * maxTargetOffsetY;

            var finalOffset = _initialTargetOffset + new Vector3(offsetX, offsetY, 0f);
            TrySetTargetOffset(_cameraPositionComposer, finalOffset);
        }

        private static bool TryGetTargetOffset(Component composer, out Vector3 value)
        {
            value = default;
            if (composer == null) return false;

            var prop = composer.GetType().GetProperty("TargetOffset");
            if (prop == null || !prop.CanRead) return false;
            if (prop.PropertyType != typeof(Vector3)) return false;

            value = (Vector3)prop.GetValue(composer, null);
            return true;
        }

        private static bool TrySetTargetOffset(Component composer, Vector3 value)
        {
            if (composer == null) return false;
            var prop = composer.GetType().GetProperty("TargetOffset");
            if (prop == null || !prop.CanWrite) return false;
            if (prop.PropertyType != typeof(Vector3)) return false;

            prop.SetValue(composer, value, null);
            return true;
        }

        private static float MapDeadZone01(float valueAbs, float deadZoneNormalized)
        {
            if (deadZoneNormalized <= 0f) return Mathf.Clamp01(valueAbs);
            if (valueAbs <= deadZoneNormalized) return 0f;
            float denom = 1f - deadZoneNormalized;
            if (denom <= 0f) return 0f;

            return Mathf.Clamp01((valueAbs - deadZoneNormalized) / denom);
        }

        private IEnumerator BakeNavMeshRoutine()
        {
            if (navMeshSurface == null)
            {
                navMeshSurface = FindFirstNavMeshSurfaceInScene();
                if (navMeshSurface == null)
                {
                    Debug.LogWarning($"{nameof(IsometricWorldGenerate)}: bakeNavMeshOnGenerate is enabled but no NavMeshSurface was assigned/found.");
                    yield break;
                }
            }

            // Give tilemap colliders a moment to update before baking.
            yield return new WaitForEndOfFrame();
            for (int i = 0; i < navMeshBuildWaitFrames; i++)
            {
                yield return null;
            }

            if (!TryBuildNavMesh(navMeshSurface))
            {
                Debug.LogWarning($"{nameof(IsometricWorldGenerate)}: Found NavMeshSurface but couldn't call BuildNavMesh().");
                yield break;
            }

            onNavMeshBaked?.Invoke();
        }

        private Component FindFirstNavMeshSurfaceInScene()
        {
            var all = Resources.FindObjectsOfTypeAll<Component>();
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                if (c == null) continue;
                var t = c.GetType();
                if (t == null) continue;
                if (t.Name == "NavMeshSurface") return c;
            }
            return null;
        }

        private static bool TryBuildNavMesh(Component surface)
        {
            if (surface == null) return false;

            var method = surface.GetType().GetMethod("BuildNavMesh", System.Type.EmptyTypes);
            if (method == null) return false;

            method.Invoke(surface, null);
            return true;
        }

        private void SpawnPlayerNearRandomCorner(System.Random rng, Transform targetPlayer)
        {
            if (_walkableCells.Count == 0 || targetPlayer == null)
                return;

            int minCellX = origin.x;
            int minCellY = origin.y;
            int maxCellX = origin.x + width - 1;
            int maxCellY = origin.y + height - 1;

            // Corners in cell coordinates.
            var corners = new Vector2Int[]
            {
                new Vector2Int(minCellX, minCellY),     // bottom-left
                new Vector2Int(minCellX, maxCellY),     // top-left
                new Vector2Int(maxCellX, minCellY),     // bottom-right
                new Vector2Int(maxCellX, maxCellY),     // top-right
            };

            Vector2Int chosenCorner = corners[rng.Next(0, corners.Length)];

            // Try to find a nearby walkable cell first.
            for (int attempt = 0; attempt < cornerSpawnAttempts; attempt++)
            {
                int dx = rng.Next(-cornerSpawnMaxOffset, cornerSpawnMaxOffset + 1);
                int dy = rng.Next(-cornerSpawnMaxOffset, cornerSpawnMaxOffset + 1);

                int x = Mathf.Clamp(chosenCorner.x + dx, minCellX, maxCellX);
                int y = Mathf.Clamp(chosenCorner.y + dy, minCellY, maxCellY);
                var cell = new Vector3Int(x, y, 0);

                if (!requireWalkableSpawn || _walkableCells.Contains(cell))
                {
                    var worldPos = topTilemap.GetCellCenterWorld(cell);
                    var baseZ = targetPlayer.position.z;
                    targetPlayer.position = new Vector3(worldPos.x, worldPos.y, baseZ + spawnZOffset);
                    return;
                }
            }

            // Fallback: spawn anywhere walkable.
            if (requireWalkableSpawn)
            {
                int index = rng.Next(0, _walkableCells.Count);
                int i = 0;
                foreach (var cell in _walkableCells)
                {
                    if (i == index)
                    {
                        var worldPos = topTilemap.GetCellCenterWorld(cell);
                        var baseZ = targetPlayer.position.z;
                        targetPlayer.position = new Vector3(worldPos.x, worldPos.y, baseZ + spawnZOffset);
                        return;
                    }
                    i++;
                }
            }
        }
    }
}

