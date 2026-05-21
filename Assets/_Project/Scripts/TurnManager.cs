using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PlayerTurnController playerTurn;
    [SerializeField] private List<EnemyTurnController> enemyTurns = new();
    [SerializeField] private Button endTurnButton;

    [Header("Flow")]
    [SerializeField] private bool autoStartOnStart = true;
    [SerializeField] private float nextTurnDelay = 0.2f;

    private readonly List<TurnActor> roundOrder = new();
    private readonly List<CharacterHealth> observedHealths = new();

    private int currentTurnIndex = -1;
    private int roundNumber = 0;

    private bool combatActive;
    private bool playerTurnActive;
    private bool combatStartedOnce;
    private Coroutine advanceRoutine;

    public bool IsCombatActive => combatActive;
    public bool IsPlayerTurnActive => combatActive && playerTurnActive;
    public int CurrentRound => roundNumber;

    public event Action OnTurnStateChanged;

    [Serializable]
    public struct TurnActorPortraitData
    {
        public bool IsValid;
        public bool IsPlayer;
        public Sprite Portrait;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (playerTurn == null)
            playerTurn = FindFirstObjectByType<PlayerTurnController>();

        RefreshEnemyList();

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(EndPlayerTurn);

        UpdateEndTurnButton(false);
    }

    private void Start()
    {
        if (autoStartOnStart)
            StartCombatOnce();
    }

    private void OnDestroy()
    {
        UnsubscribeFromDeathEvents();

        if (Instance == this)
            Instance = null;

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(EndPlayerTurn);
    }

    public void ResetCombatState()
    {
        if (advanceRoutine != null)
        {
            StopCoroutine(advanceRoutine);
            advanceRoutine = null;
        }

        combatActive = false;
        playerTurnActive = false;
        combatStartedOnce = false;

        currentTurnIndex = -1;
        roundNumber = 0;
        roundOrder.Clear();

        if (playerTurn != null)
            playerTurn.EndTurn();

        RefreshEnemyList();
        UpdateEndTurnButton(false);
        NotifyTurnStateChanged();
    }

    public void SetEnemyTurns(IEnumerable<EnemyTurnController> newEnemyTurns)
    {
        enemyTurns = newEnemyTurns != null
            ? newEnemyTurns
                .Where(e => e != null && !IsEnemyDead(e))
                .Distinct()
                .ToList()
            : new List<EnemyTurnController>();

        SubscribeToDeathEvents();
        NotifyTurnStateChanged();
    }

    public void StartCombat()
    {
        StartCombatOnce();
    }

    public void StartCombatOnce()
    {
        if (combatStartedOnce || combatActive)
        {
            Debug.Log("TurnManager: StartCombatOnce ignorat, combatul este deja pornit.");
            return;
        }

        RefreshEnemyList();
        SubscribeToDeathEvents();

        combatStartedOnce = true;
        combatActive = true;
        playerTurnActive = false;
        roundNumber = 0;
        currentTurnIndex = -1;

        BuildRoundOrder(forcePlayerFirst: true);

        if (roundOrder.Count == 0)
        {
            combatActive = false;
            combatStartedOnce = false;
            UpdateEndTurnButton(false);
            NotifyTurnStateChanged();
            return;
        }

        AdvanceToNextActor();
    }

    public void RefreshEnemyList()
    {
        enemyTurns.RemoveAll(e => e == null || IsEnemyDead(e));

        if (enemyTurns.Count == 0)
        {
            enemyTurns = FindObjectsByType<EnemyTurnController>(FindObjectsSortMode.None)
                .Where(e => e != null && !IsEnemyDead(e))
                .Distinct()
                .ToList();
        }

        SubscribeToDeathEvents();
    }

    public void EndPlayerTurn()
    {
        if (!IsPlayerTurnActive || playerTurn == null)
            return;

        playerTurn.EndTurn();
        playerTurnActive = false;
        UpdateEndTurnButton(false);
        QueueAdvance();
    }

    public TurnActorPortraitData GetCurrentActorPortraitData()
    {
        TurnActor actor = GetCurrentAliveActor();
        return BuildPortraitData(actor);
    }

    public TurnActorPortraitData GetNextActorPortraitData()
    {
        TurnActor actor = PeekNextAliveActor();
        return BuildPortraitData(actor);
    }

    public List<TurnActorPortraitData> GetVisibleTurnOrderPortraits()
    {
        List<TurnActorPortraitData> result = new List<TurnActorPortraitData>();

        if (!combatActive || roundOrder.Count == 0)
            return result;

        HashSet<CharacterHealth> addedActors = new HashSet<CharacterHealth>();

        int startIndex = Mathf.Clamp(currentTurnIndex, 0, Mathf.Max(0, roundOrder.Count - 1));

        for (int i = startIndex; i < roundOrder.Count; i++)
        {
            TurnActor actor = roundOrder[i];

            if (IsActorAlive(actor) && addedActors.Add(actor.Health))
                result.Add(BuildPortraitData(actor));
        }

        for (int i = 0; i < startIndex; i++)
        {
            TurnActor actor = roundOrder[i];

            if (IsActorAlive(actor) && addedActors.Add(actor.Health))
                result.Add(BuildPortraitData(actor));
        }

        return result;
    }

    private void QueueAdvance()
    {
        if (!combatActive)
            return;

        if (advanceRoutine != null)
            StopCoroutine(advanceRoutine);

        advanceRoutine = StartCoroutine(AdvanceAfterDelay());
    }

    private IEnumerator AdvanceAfterDelay()
    {
        yield return new WaitForSeconds(nextTurnDelay);

        advanceRoutine = null;
        AdvanceToNextActor();
    }

    private void AdvanceToNextActor()
    {
        if (!combatActive)
            return;

        if (CheckCombatEnded())
            return;

        currentTurnIndex++;

        while (true)
        {
            if (currentTurnIndex >= roundOrder.Count)
            {
                BuildRoundOrder(forcePlayerFirst: false);

                if (CheckCombatEnded())
                    return;

                if (roundOrder.Count == 0)
                    return;

                currentTurnIndex = 0;
            }

            TurnActor actor = roundOrder[currentTurnIndex];

            if (IsActorAlive(actor))
            {
                StartActorTurn(actor);
                return;
            }

            currentTurnIndex++;
        }
    }

    private void StartActorTurn(TurnActor actor)
    {
        if (actor == null)
        {
            QueueAdvance();
            return;
        }

        if (actor.IsPlayer)
        {
            playerTurnActive = true;
            UpdateEndTurnButton(true);

            bool playerCanAct = playerTurn != null && playerTurn.BeginTurn();

            if (!playerCanAct)
            {
                playerTurnActive = false;
                UpdateEndTurnButton(false);
                NotifyTurnStateChanged();

                if (CheckCombatEnded())
                    return;

                QueueAdvance();
                return;
            }

            NotifyTurnStateChanged();
            return;
        }

        playerTurnActive = false;
        UpdateEndTurnButton(false);

        if (playerTurn != null)
            playerTurn.EndTurn();

        NotifyTurnStateChanged();

        if (actor.Enemy == null)
        {
            QueueAdvance();
            return;
        }

        actor.Enemy.StartTurn(OnEnemyTurnFinished);
    }

    private void OnEnemyTurnFinished()
    {
        QueueAdvance();
    }

    private void BuildRoundOrder(bool forcePlayerFirst)
    {
        roundNumber++;
        roundOrder.Clear();

        List<TurnActor> aliveEnemies = enemyTurns
            .Where(e => e != null && !IsEnemyDead(e))
            .Select(CreateEnemyActor)
            .Where(a => a != null && a.IsAlive)
            .OrderByDescending(a => a.Initiative)
            .ToList();

        TurnActor playerActor = CreatePlayerActor();

        if (forcePlayerFirst)
        {
            if (playerActor != null && playerActor.IsAlive)
                roundOrder.Add(playerActor);

            roundOrder.AddRange(aliveEnemies);
            return;
        }

        List<TurnActor> allActors = new List<TurnActor>();

        if (playerActor != null && playerActor.IsAlive)
            allActors.Add(playerActor);

        allActors.AddRange(aliveEnemies);

        roundOrder.AddRange(
            allActors
                .OrderByDescending(a => a.Initiative)
                .ThenByDescending(a => a.IsPlayer ? 1 : 0)
        );
    }

    private List<TurnActor> BuildPreviewRoundOrder()
    {
        List<TurnActor> result = new List<TurnActor>();

        List<TurnActor> aliveEnemies = enemyTurns
            .Where(e => e != null && !IsEnemyDead(e))
            .Select(CreateEnemyActor)
            .Where(a => a != null && a.IsAlive)
            .OrderByDescending(a => a.Initiative)
            .ToList();

        TurnActor playerActor = CreatePlayerActor();

        if (playerActor != null && playerActor.IsAlive)
            result.Add(playerActor);

        result.AddRange(aliveEnemies);

        return result
            .OrderByDescending(a => a.Initiative)
            .ThenByDescending(a => a.IsPlayer ? 1 : 0)
            .ToList();
    }

    private TurnActor CreatePlayerActor()
    {
        if (playerTurn == null || playerTurn.Health == null || playerTurn.Stats == null)
            return null;

        return new TurnActor
        {
            Player = playerTurn,
            Health = playerTurn.Health,
            Stats = playerTurn.Stats
        };
    }

    private TurnActor CreateEnemyActor(EnemyTurnController enemy)
    {
        if (enemy == null)
            return null;

        CharacterHealth health = enemy.GetComponent<CharacterHealth>();
        CharacterStats stats = enemy.GetComponent<CharacterStats>();

        if (health == null || stats == null)
            return null;

        return new TurnActor
        {
            Enemy = enemy,
            Health = health,
            Stats = stats
        };
    }

    private bool IsActorAlive(TurnActor actor)
    {
        return actor != null && actor.Health != null && !actor.Health.IsDead;
    }

    private TurnActor GetCurrentAliveActor()
    {
        if (!combatActive || currentTurnIndex < 0 || currentTurnIndex >= roundOrder.Count)
            return null;

        TurnActor actor = roundOrder[currentTurnIndex];
        return IsActorAlive(actor) ? actor : null;
    }

    private TurnActor PeekNextAliveActor()
    {
        if (!combatActive || roundOrder.Count == 0)
            return null;

        int nextIndex = currentTurnIndex + 1;

        for (int i = nextIndex; i < roundOrder.Count; i++)
        {
            if (IsActorAlive(roundOrder[i]))
                return roundOrder[i];
        }

        List<TurnActor> nextRound = BuildPreviewRoundOrder();

        for (int i = 0; i < nextRound.Count; i++)
        {
            if (IsActorAlive(nextRound[i]))
                return nextRound[i];
        }

        return null;
    }

    private TurnActorPortraitData BuildPortraitData(TurnActor actor)
    {
        if (actor == null)
            return default;

        GameObject actorObject = actor.IsPlayer
            ? actor.Player.gameObject
            : actor.Enemy.gameObject;

        TurnOrderPortrait portraitSource = actorObject.GetComponent<TurnOrderPortrait>();

        return new TurnActorPortraitData
        {
            IsValid = true,
            IsPlayer = actor.IsPlayer,
            Portrait = portraitSource != null ? portraitSource.Portrait : null
        };
    }

    private bool CheckCombatEnded()
    {
        bool playerDead = playerTurn == null || playerTurn.Health == null || playerTurn.Health.IsDead;

        bool anyEnemyAlive = enemyTurns.Any(e =>
        {
            if (e == null)
                return false;

            CharacterHealth h = e.GetComponent<CharacterHealth>();
            return h != null && !h.IsDead;
        });

        if (playerDead)
        {
            StopAdvanceRoutine();

            combatActive = false;
            playerTurnActive = false;
            UpdateEndTurnButton(false);

            if (playerTurn != null)
                playerTurn.EndTurn();

            NotifyTurnStateChanged();
            return true;
        }

        if (!anyEnemyAlive)
        {
            StopAdvanceRoutine();

            combatActive = false;
            playerTurnActive = false;
            UpdateEndTurnButton(false);

            if (playerTurn != null)
                playerTurn.SetExplorationControl(true);

            NotifyTurnStateChanged();
            return true;
        }

        return false;
    }

    private void HandleActorDied(CharacterHealth deadHealth)
    {
        if (deadHealth == null)
            return;

        bool isPlayerDeath =
            playerTurn != null &&
            playerTurn.Health != null &&
            playerTurn.Health == deadHealth;

        if (!isPlayerDeath)
        {
            enemyTurns.RemoveAll(e =>
            {
                if (e == null)
                    return true;

                CharacterHealth enemyHealth = e.GetComponent<CharacterHealth>();
                return enemyHealth == null || enemyHealth == deadHealth || enemyHealth.IsDead;
            });
        }

        if (!combatActive)
        {
            NotifyTurnStateChanged();
            return;
        }

        bool ended = CheckCombatEnded();

        if (!ended)
            NotifyTurnStateChanged();
    }

    private void SubscribeToDeathEvents()
    {
        UnsubscribeFromDeathEvents();

        if (playerTurn == null)
            playerTurn = FindFirstObjectByType<PlayerTurnController>();

        if (playerTurn != null && playerTurn.Health != null)
            SubscribeHealth(playerTurn.Health);

        for (int i = 0; i < enemyTurns.Count; i++)
        {
            EnemyTurnController enemy = enemyTurns[i];

            if (enemy == null)
                continue;

            CharacterHealth health = enemy.GetComponent<CharacterHealth>();

            if (health != null)
                SubscribeHealth(health);
        }
    }

    private void SubscribeHealth(CharacterHealth health)
    {
        if (health == null || observedHealths.Contains(health))
            return;

        health.OnDied += HandleActorDied;
        observedHealths.Add(health);
    }

    private void UnsubscribeFromDeathEvents()
    {
        for (int i = 0; i < observedHealths.Count; i++)
        {
            if (observedHealths[i] != null)
                observedHealths[i].OnDied -= HandleActorDied;
        }

        observedHealths.Clear();
    }

    private bool IsEnemyDead(EnemyTurnController enemy)
    {
        if (enemy == null)
            return true;

        CharacterHealth health = enemy.GetComponent<CharacterHealth>();
        return health == null || health.IsDead;
    }

    private void StopAdvanceRoutine()
    {
        if (advanceRoutine == null)
            return;

        StopCoroutine(advanceRoutine);
        advanceRoutine = null;
    }

    private void UpdateEndTurnButton(bool playerCanEndTurn)
    {
        if (endTurnButton == null)
            return;

        endTurnButton.gameObject.SetActive(combatActive);
        endTurnButton.interactable = playerCanEndTurn;
    }

    private void NotifyTurnStateChanged()
    {
        OnTurnStateChanged?.Invoke();
    }

    private sealed class TurnActor
    {
        public PlayerTurnController Player;
        public EnemyTurnController Enemy;
        public CharacterHealth Health;
        public CharacterStats Stats;

        public bool IsPlayer => Player != null;
        public bool IsAlive => Health != null && !Health.IsDead;
        public int Initiative => Stats != null ? Stats.Initiative : 0;
    }
}