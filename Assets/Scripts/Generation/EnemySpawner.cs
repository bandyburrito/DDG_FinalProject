using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Enemy Prefabs")]
    public GameObject soldierPrefab;
    public GameObject sniperPrefab;
    public GameObject heavyPrefab;

    [Header("Companion Prefabs")]
    public GameObject dronePrefab;
    public GameObject brawlerPrefab;
    public GameObject tricksterPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public EnemyAI SpawnEnemy(EnemyType type, Vector2Int gridPos)
    {
        var prefab = type switch
        {
            EnemyType.Soldier => soldierPrefab,
            EnemyType.Sniper  => sniperPrefab,
            EnemyType.Heavy   => heavyPrefab,
            _                 => soldierPrefab
        };
        if (prefab == null) { Debug.LogWarning($"No prefab for {type}"); return null; }

        var go = Instantiate(prefab, GridManager.Instance.GridToWorld(gridPos), Quaternion.identity);
        go.SetActive(true);
        go.name = $"Enemy_{type}_{gridPos.x}_{gridPos.y}";

        var enemy = go.GetComponent<EnemyAI>();
        enemy.PlaceAt(gridPos);
        TurnManager.Instance.RegisterEntity(enemy);
        return enemy;
    }

    public CompanionAI SpawnCompanion(CompanionType type, Vector2Int gridPos)
    {
        var prefab = type switch
        {
            CompanionType.Drone     => dronePrefab,
            CompanionType.Brawler   => brawlerPrefab,
            CompanionType.Trickster => tricksterPrefab,
            _                       => dronePrefab
        };
        if (prefab == null) { Debug.LogWarning($"No prefab for companion {type}"); return null; }

        var go = Instantiate(prefab, GridManager.Instance.GridToWorld(gridPos), Quaternion.identity);
        go.SetActive(true);
        go.name = $"Companion_{type}";

        var companion = go.GetComponent<CompanionAI>();
        companion.PlaceAt(gridPos);
        // Persist across waves — keep alive between rooms unless killed
        DontDestroyOnLoad(go);
        return companion;
    }
}
