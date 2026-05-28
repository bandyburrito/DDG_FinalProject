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

    public Entity CurrentEntity => _turnOrder.Count > 0 && _currentIndex < _turnOrder.Count ? _turnOrder[_currentIndex] : null;
    public bool IsPlayerTurn => CurrentEntity is PlayerController;

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
        _turnOrder.Remove(e);
        if (_currentIndex >= _turnOrder.Count) _currentIndex = 0;
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

        // Skip dead entities
        while (_currentIndex < _turnOrder.Count && !_turnOrder[_currentIndex].IsAlive)
            _currentIndex++;

        if (_currentIndex >= _turnOrder.Count)
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
