using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// At the start of each round, asks every enemy to compute its plan
/// and displays yellow pulsing tiles on their intended attack targets
/// + dim outlines on their intended move destinations.
/// Telegraphs persist through the round; cleared per-enemy as they execute.
/// </summary>
public class TelegraphSystem : MonoBehaviour
{
    public static TelegraphSystem Instance { get; private set; }

    private Dictionary<EnemyAI, List<GameObject>> _telegraphs = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ComputeAndDisplayAll(List<Entity> turnOrder)
    {
        ClearAll();

        foreach (var e in turnOrder)
        {
            if (!(e is EnemyAI enemy) || !enemy.IsAlive) continue;
            enemy.ComputePlan();

            var markers = new List<GameObject>();
            // Show planned move destination as a dim outline
            if (enemy.PlannedMove != enemy.GridPos)
            {
                var moveMarker = GridManager.Instance.SpawnTelegraphMove(enemy.PlannedMove);
                if (moveMarker) markers.Add(moveMarker);
            }
            // Show planned attack as pulsing yellow
            if (enemy.WillAttack)
            {
                var atkMarker = GridManager.Instance.SpawnTelegraphAttack(enemy.PlannedAttack);
                if (atkMarker)
                {
                    markers.Add(atkMarker);
                    // Attach pulse behaviour
                    atkMarker.AddComponent<PulseAlpha>();
                }
            }
            _telegraphs[enemy] = markers;
        }
    }

    public void ClearForEntity(EnemyAI enemy)
    {
        if (!_telegraphs.TryGetValue(enemy, out var markers)) return;
        foreach (var m in markers) if (m) GridManager.Instance.RemoveHighlight(m);
        _telegraphs.Remove(enemy);
    }

    public void ClearAll()
    {
        foreach (var kvp in _telegraphs)
            foreach (var m in kvp.Value) if (m) GridManager.Instance.RemoveHighlight(m);
        _telegraphs.Clear();
    }
}

/// <summary>Makes a SpriteRenderer's alpha pulse for the telegraph effect.</summary>
public class PulseAlpha : MonoBehaviour
{
    public float minAlpha = 0.3f;
    public float maxAlpha = 0.8f;
    public float speed    = 3f;

    private SpriteRenderer _sr;

    void Awake() { _sr = GetComponent<SpriteRenderer>(); }

    void Update()
    {
        if (_sr == null) return;
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        var c = _sr.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        _sr.color = c;
    }
}
