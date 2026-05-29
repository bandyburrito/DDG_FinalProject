using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public event System.Action<Entity> OnTurnStart;
    public event System.Action         OnRoundStart;
    public event System.Action         OnAllEnemiesDead;
    public event System.Action         OnPlayerLost;

    private List<Entity> _allEntities = new();
    private List<Entity> _turnOrder   = new();
    private int          _currentIndex = 0;

    public Entity CurrentEntity => _turnOrder.Count > 0 && _currentIndex >= 0 && _currentIndex < _turnOrder.Count ? _turnOrder[_currentIndex] : null;
    public bool IsPlayerTurn => CurrentEntity is PlayerController;

    /// <summary>Read-only access for the HUD turn-order display.</summary>
    public int CurrentIndex => _currentIndex;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterEntity(Entity e)
    {
        if (!_allEntities.Contains(e)) _allEntities.Add(e);
    }

    public void UnregisterEntity(Entity e)
    {
        _allEntities.Remove(e);

        int idx = _turnOrder.IndexOf(e);
        if (idx < 0) return;

        _turnOrder.RemoveAt(idx);

        // When something is removed from _turnOrder, every entity after it shifts down
        // by one. If the removed entity sat AT OR BEFORE the currently-processing slot,
        // decrement _currentIndex so AdvanceTurn's ++ lands on the entity that was
        // originally at idx+1 (now at idx). Otherwise we'd silently skip a turn — or,
        // worse, the index walks off the list and the turn loop wedges.
        //
        // _currentIndex can legitimately become -1 here (e.g. the entity at idx 0 dies
        // during its own turn). The next AdvanceTurn brings it back to 0.
        if (idx <= _currentIndex) _currentIndex--;
    }

    public void ClearAll()
    {
        _allEntities.Clear();
        _turnOrder.Clear();
        _currentIndex = 0;
    }

    public void StartCombat() { StartNewRound(); }

    private void StartNewRound()
    {
        RollInitiative();
        OnRoundStart?.Invoke();
        // Compute and display enemy telegraphs for this round
        TelegraphSystem.Instance?.ComputeAndDisplayAll(_turnOrder);
        _currentIndex = 0;
        StartCoroutine(ProcessCurrentTurn());
    }

    private void RollInitiative()
    {
        _turnOrder = _allEntities
            .Where(e => e != null && e.IsAlive)
            .Select(e => (entity: e, roll: Random.Range(1, 21) + e.speed))
            .OrderByDescending(x => x.roll)
            .ThenByDescending(x => x.entity is PlayerController ? 1 : 0)
            .Select(x => x.entity)
            .ToList();
    }

    private IEnumerator ProcessCurrentTurn()
    {
        if (_turnOrder.Count == 0) yield break;

        // Skip dead / null entities. Guard the lower bound — _currentIndex can be -1
        // briefly after UnregisterEntity removes the current slot.
        while (_currentIndex >= 0
               && _currentIndex < _turnOrder.Count
               && (_turnOrder[_currentIndex] == null || !_turnOrder[_currentIndex].IsAlive))
            _currentIndex++;

        if (_currentIndex < 0 || _currentIndex >= _turnOrder.Count)
        {
            TelegraphSystem.Instance?.ClearAll();
            yield return new WaitForSeconds(0.2f);
            if (CheckEndConditions()) yield break;
            StartNewRound();
            yield break;
        }

        var entity = _turnOrder[_currentIndex];
        OnTurnStart?.Invoke(entity);
        entity.OnTurnBegin();

        if (entity is EnemyAI enemy)
        {
            // Clear this enemy's telegraph as they execute
            TelegraphSystem.Instance?.ClearForEntity(enemy);
            yield return StartCoroutine(enemy.ExecuteTurn());
            if (CheckEndConditions()) yield break;
            AdvanceTurn();
        }
        else if (entity is CompanionAI companion)
        {
            yield return StartCoroutine(companion.ExecuteTurn());
            if (CheckEndConditions()) yield break;
            AdvanceTurn();
        }
        // Player turn advances via EndPlayerTurn()
    }

    public void EndPlayerTurn()
    {
        if (!IsPlayerTurn) return;
        AdvanceTurn();
    }

    private void AdvanceTurn()
    {
        _currentIndex++;
        if (CheckEndConditions()) return;
        StartCoroutine(ProcessCurrentTurn());
    }

    private bool CheckEndConditions()
    {
        var player = PlayerController.Instance;
        if (player == null || !player.IsAlive)
        {
            OnPlayerLost?.Invoke();
            return true;
        }

        bool allEnemiesDead = _allEntities.Where(e => e is EnemyAI).All(e => !e.IsAlive);
        if (allEnemiesDead)
        {
            TelegraphSystem.Instance?.ClearAll();
            OnAllEnemiesDead?.Invoke();
            return true;
        }
        return false;
    }

    public List<Entity> GetTurnOrder() => new List<Entity>(_turnOrder);
}
