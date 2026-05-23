using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerAP))]
[RequireComponent(typeof(CharacterHealth))]
public class PlayerNavMeshMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private MoveRangeGridVisualizer moveRangeVisualizer;

    [Header("Movement")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float navMeshSampleDistance = 1.5f;

    [Header("Click Blocking")]
    [SerializeField] private bool blockMovementOnEnemyOrLootClick = true;
    [SerializeField] private LayerMask extraMovementBlockerMask;

    [Header("AP Cost")]
    [SerializeField] private float unitsPerAP = 2f;
    public float UnitsPerAP => unitsPerAP;

    public event Action OnMoveStarted;
    public event Action OnMoveFinished;

    private NavMeshAgent agent;
    private PlayerAP playerAP;
    private CharacterHealth health;
    private CharacterStatusEffects statusEffects;

    private bool turnInputEnabled;
    private bool blockMovementThisFrame;
    private bool unlimitedMovementMode;

    private Coroutine movementWatchRoutine;

    public bool IsCurrentlyMoving => IsActuallyMoving();
    public bool IsUnlimitedMovementMode => unlimitedMovementMode;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        playerAP = GetComponent<PlayerAP>();
        health = GetComponent<CharacterHealth>();
        statusEffects = GetComponent<CharacterStatusEffects>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (combatController == null)
            combatController = GetComponent<PlayerCombatController>();

        if (moveRangeVisualizer == null)
            moveRangeVisualizer = FindFirstObjectByType<MoveRangeGridVisualizer>();

        turnInputEnabled = false;
        unlimitedMovementMode = false;
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            StopMovementImmediately(false);
            return;
        }

        if (!turnInputEnabled)
        {
            blockMovementThisFrame = false;
            return;
        }

        if (Mouse.current == null || mainCamera == null)
        {
            blockMovementThisFrame = false;
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
            {
                blockMovementThisFrame = false;
                return;
            }

            if (blockMovementThisFrame)
            {
                blockMovementThisFrame = false;
                return;
            }

            if (combatController != null && combatController.BlockMovementThisFrame)
            {
                blockMovementThisFrame = false;
                return;
            }

            if (combatController != null && combatController.HasTargetingSkillSelected)
            {
                blockMovementThisFrame = false;
                return;
            }

            TryMoveToMouse();
        }

        blockMovementThisFrame = false;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void TryMoveToMouse()
    {
        if (Mouse.current == null || mainCamera == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (!TryCalculateMovePreviewAtScreenPoint(
                mousePosition,
                out int apCost,
                out float pathLength,
                out Vector3 destination))
            return;

        if (!unlimitedMovementMode && !playerAP.HasEnoughAP(apCost))
        {
            GameLog.Warning(
                $"Nu ai destul AP pentru deplasare. " +
                $"Cost: {apCost}, AP curent: {playerAP.CurrentAP}/{playerAP.MaxAP}, " +
                $"Lungime traseu: {pathLength:F2}, UnitsPerAP: {GetEffectiveUnitsPerAP():F2}, " +
                $"Player: {gameObject.name}"
            );
            return;
        }

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            GameLog.Warning("PlayerNavMeshMover: agentul playerului nu este pe NavMesh.");
            return;
        }

        agent.isStopped = false;

        bool destinationSet = agent.SetDestination(destination);
        if (!destinationSet)
        {
            GameLog.Warning("PlayerNavMeshMover: destinatia nu a putut fi setata. AP-ul nu a fost consumat.");
            return;
        }

        if (!unlimitedMovementMode)
        {
            bool spent = playerAP.SpendAP(apCost);
            if (!spent)
            {
                agent.ResetPath();
                GameLog.Warning("PlayerNavMeshMover: AP-ul nu a putut fi consumat. Miscarea a fost anulata.");
                return;
            }
        }

        OnMoveStarted?.Invoke();

        if (CombatTelemetryTracker.Instance != null)
            CombatTelemetryTracker.Instance.RecordMovementAction();

        if (!unlimitedMovementMode && moveRangeVisualizer != null && moveRangeVisualizer.gameObject.activeInHierarchy)
            moveRangeVisualizer.BeginHideUntilMovementEnds();

        if (movementWatchRoutine != null)
            StopCoroutine(movementWatchRoutine);

        movementWatchRoutine = StartCoroutine(WatchMovementUntilFinished());

        if (unlimitedMovementMode)
            GameLog.Info($"Deplasare efectuata | Mod liber | Lungime traseu: {pathLength:F2}");
        else
            GameLog.Info($"Deplasare efectuata | Cost: {apCost} AP | Lungime traseu: {pathLength:F2}");
    }

    public bool TryCalculateMovePreviewAtScreenPoint(
        Vector2 screenPosition,
        out int apCost,
        out float pathLength,
        out Vector3 destination)
    {
        apCost = 0;
        pathLength = 0f;
        destination = Vector3.zero;

        if (mainCamera == null || agent == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (blockMovementOnEnemyOrLootClick && IsMovementClickBlockedBeforeGround(ray))
            return false;

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask, QueryTriggerInteraction.Ignore))
            return false;

        if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
            return false;

        NavMeshPath path = new NavMeshPath();
        bool foundPath = agent.CalculatePath(navHit.position, path);

        if (!foundPath)
            return false;

        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        pathLength = GetPathLength(path);

        if (pathLength < 0.05f)
            return false;

        apCost = unlimitedMovementMode
            ? 0
            : Mathf.CeilToInt(pathLength / GetEffectiveUnitsPerAP());

        destination = navHit.position;
        return true;
    }

    private bool IsMovementClickBlockedBeforeGround(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
            return false;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (hitCollider == null)
                continue;

            Transform hitTransform = hitCollider.transform;

            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            GameObject hitObject = hitCollider.gameObject;

            if (IsLayerInMask(hitObject.layer, groundMask))
                return false;

            if (IsLayerInMask(hitObject.layer, extraMovementBlockerMask))
                return true;

            if (IsEnemyOrLootObject(hitTransform))
                return true;
        }

        return false;
    }

    private bool IsEnemyOrLootObject(Transform hitTransform)
    {
        if (hitTransform == null)
            return false;

        CharacterHealth hitHealth = hitTransform.GetComponentInParent<CharacterHealth>();
        if (hitHealth != null && hitHealth != health)
            return true;

        CharacterStats hitStats = hitTransform.GetComponentInParent<CharacterStats>();
        CharacterStats ownStats = GetComponent<CharacterStats>();

        if (hitStats != null && hitStats != ownStats)
            return true;

        EnemyLootContainer lootContainer = hitTransform.GetComponentInParent<EnemyLootContainer>();
        if (lootContainer != null)
            return true;

        EnemyTurnController enemyTurnController = hitTransform.GetComponentInParent<EnemyTurnController>();
        if (enemyTurnController != null)
            return true;

        return false;
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private IEnumerator WatchMovementUntilFinished()
    {
        yield return null;

        while (IsActuallyMoving())
            yield return null;

        movementWatchRoutine = null;
        OnMoveFinished?.Invoke();
    }

    private bool IsActuallyMoving()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (agent.pathPending)
            return true;

        if (agent.isStopped)
            return false;

        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.02f)
            return true;

        if (agent.velocity.sqrMagnitude > 0.0001f)
            return true;

        return false;
    }

    private void StopMovementImmediately(bool notifyFinished)
    {
        if (movementWatchRoutine != null)
        {
            StopCoroutine(movementWatchRoutine);
            movementWatchRoutine = null;
        }

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (notifyFinished)
            OnMoveFinished?.Invoke();
    }

    private float GetPathLength(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2)
            return 0f;

        float total = 0f;

        for (int i = 1; i < path.corners.Length; i++)
            total += Vector3.Distance(path.corners[i - 1], path.corners[i]);

        return total;
    }

    public void SetTurnInputEnabled(bool enabled)
    {
        turnInputEnabled = enabled;

        if (!enabled)
            StopMovementImmediately(true);
    }

    public void SetUnlimitedMovementMode(bool enabled)
    {
        unlimitedMovementMode = enabled;

        if (moveRangeVisualizer != null)
            moveRangeVisualizer.gameObject.SetActive(!enabled);
    }

    public void SetMoveRangeVisualizerEnabled(bool enabled)
    {
        if (moveRangeVisualizer != null)
            moveRangeVisualizer.gameObject.SetActive(enabled);
    }

    public void BlockMovementForCurrentFrame()
    {
        blockMovementThisFrame = true;
        StopMovementImmediately(false);
    }

    public float GetMovementCostMultiplier()
    {
        return statusEffects != null ? statusEffects.MovementCostMultiplier : 1f;
    }

    public float GetEffectiveUnitsPerAP()
    {
        return unitsPerAP / Mathf.Max(1f, GetMovementCostMultiplier());
    }
}