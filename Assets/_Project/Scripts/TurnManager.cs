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

    private int currentTurnIndex = -1;
    private int roundNumber = 0;

    private bool combatActive;
    private bool playerTurnActive;
    private Coroutine advanceRoutine;

    public bool IsCombatActive => combatActive;
    public bool IsPlayerTurnActive => combatActive && playerTurnActive;
    public int CurrentRound => roundNumber;

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
            StartCombat();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (endTurnButton != null)
            endTurnButton.onClick.RemoveListener(EndPlayerTurn);
    }

    public void StartCombat()
    {
        if (combatActive)
            return;

        RefreshEnemyList();

        combatActive = true;
        playerTurnActive = false;
        roundNumber = 0;
        currentTurnIndex = -1;

        BuildRoundOrder(forcePlayerFirst: true);
        AdvanceToNextActor();
    }

    public void RefreshEnemyList()
    {
        enemyTurns.RemoveAll(e => e == null);

        if (enemyTurns.Count == 0)
        {
            enemyTurns = FindObjectsByType<EnemyTurnController>(FindObjectsSortMode.None)
                .ToList();
        }
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

    private void QueueAdvance()
    {
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
            playerTurn.BeginTurn();
            return;
        }

        playerTurnActive = false;
        UpdateEndTurnButton(false);

        if (playerTurn != null)
            playerTurn.EndTurn();

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
            .Where(e => e != null)
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

    private bool CheckCombatEnded()
    {
        bool playerDead = playerTurn == null || playerTurn.Health == null || playerTurn.Health.IsDead;
        bool anyEnemyAlive = enemyTurns.Any(e =>
        {
            if (e == null) return false;
            CharacterHealth h = e.GetComponent<CharacterHealth>();
            return h != null && !h.IsDead;
        });

        if (playerDead)
        {
            combatActive = false;
            playerTurnActive = false;
            UpdateEndTurnButton(false);

            if (playerTurn != null)
                playerTurn.EndTurn();

            return true;
        }

        if (!anyEnemyAlive)
        {
            combatActive = false;
            playerTurnActive = false;
            UpdateEndTurnButton(false);

            if (playerTurn != null)
                playerTurn.SetExplorationControl(true);

            return true;
        }

        return false;
    }

    private void UpdateEndTurnButton(bool playerCanEndTurn)
    {
        if (endTurnButton == null)
            return;

        endTurnButton.gameObject.SetActive(combatActive);
        endTurnButton.interactable = playerCanEndTurn;
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