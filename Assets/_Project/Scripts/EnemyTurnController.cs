using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerAP))]
[RequireComponent(typeof(CharacterBasicAttack))]
[RequireComponent(typeof(CharacterHealth))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(TurnActionLimiter))]
public class EnemyTurnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CharacterStats targetStats;
    [SerializeField] private EnemyAnimationController animationController;
    [SerializeField] private TurnActionLimiter actionLimiter;
    [SerializeField] private TurnAgentLock turnLock;

    [Header("Special Attacks")]
    [SerializeField] private AttackDefinition mediumAttack;
    [SerializeField] private AttackDefinition heavyAttack;

    [Header("Turn")]
    [SerializeField] private float thinkDelay = 0.1f;
    [SerializeField] private float afterMoveDelay = 0.15f;
    [SerializeField] private float afterAttackDelay = 1f;

    [Header("Movement AP")]
    [SerializeField] private float metersPerAP = 1.5f;
    [SerializeField] private float minMoveDistance = 0.2f;

    [Header("Melee Positioning")]
    [SerializeField] private float attackRangeBuffer = 0.2f;
    [SerializeField] private float destinationTolerance = 0.15f;
    [SerializeField] private float idleApproachGap = 0.35f;

    private const string MediumAttackKey = "ENEMY_MEDIUM_ATTACK";
    private const string HeavyAttackKey = "ENEMY_HEAVY_ATTACK";

    private PlayerAP ap;
    private CharacterBasicAttack basicAttack;
    private CharacterHealth health;
    private CharacterStats selfStats;

    private Coroutine turnRoutine;
    private bool isTakingTurn;

    public bool IsTakingTurn => isTakingTurn;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animationController == null)
            animationController = GetComponent<EnemyAnimationController>();

        if (actionLimiter == null)
            actionLimiter = GetComponent<TurnActionLimiter>();

        if (turnLock == null)
            turnLock = GetComponent<TurnAgentLock>();

        ap = GetComponent<PlayerAP>();
        basicAttack = GetComponent<CharacterBasicAttack>();
        health = GetComponent<CharacterHealth>();
        selfStats = GetComponent<CharacterStats>();
    }

    public void SetTarget(CharacterStats newTarget)
    {
        targetStats = newTarget;
    }

    public void StartTurn(Action onTurnFinished = null)
    {
        if (turnRoutine != null)
            StopCoroutine(turnRoutine);

        turnRoutine = StartCoroutine(BeginTurnSequence(onTurnFinished));
    }

    private IEnumerator BeginTurnSequence(Action onTurnFinished)
    {
        if (turnLock != null)
            yield return turnLock.UnlockForTurn();

        yield return TurnRoutine(onTurnFinished);
        turnRoutine = null;
    }

    private IEnumerator TurnRoutine(Action onTurnFinished)
    {
        isTakingTurn = true;

        if (health == null || health.IsDead || targetStats == null)
        {
            EndTurn(onTurnFinished);
            yield break;
        }

        CharacterHealth targetHealth = targetStats.GetComponent<CharacterHealth>();
        if (targetHealth == null || targetHealth.IsDead)
        {
            EndTurn(onTurnFinished);
            yield break;
        }

        ap.RestoreAllAP();
        actionLimiter?.ResetTurnUsage();

        GameLog.Info($"{name} isi incepe tura cu {ap.CurrentAP} AP.");

        yield return new WaitForSeconds(thinkDelay);

        if (TryUseBestAvailableAttack())
        {
            yield return new WaitForSeconds(afterAttackDelay);
            EndTurn(onTurnFinished);
            yield break;
        }

        bool moved = TryMoveTowardTargetWithinAP();
        if (!moved)
        {
            GameLog.Warning($"{name} nu a putut efectua miscarea spre tinta.");
            EndTurn(onTurnFinished);
            yield break;
        }

        yield return WaitUntilMovementStops();
        yield return new WaitForSeconds(afterMoveDelay);

        targetHealth = targetStats != null ? targetStats.GetComponent<CharacterHealth>() : null;
        if (targetHealth != null && !targetHealth.IsDead)
        {
            if (TryUseBestAvailableAttack())
                yield return new WaitForSeconds(afterAttackDelay);
        }

        EndTurn(onTurnFinished);
    }

    private bool TryUseBestAvailableAttack()
    {
        if (TryUseSpecialAttack(heavyAttack, HeavyAttackKey, EnemyAttackAnimationType.Heavy))
            return true;

        if (TryUseSpecialAttack(mediumAttack, MediumAttackKey, EnemyAttackAnimationType.Medium))
            return true;

        if (basicAttack != null &&
            targetStats != null &&
            (actionLimiter == null || actionLimiter.CanUseBasicAttack()) &&
            basicAttack.IsTargetInAttackRange(targetStats.transform) &&
            basicAttack.TryAttackTarget(targetStats))
        {
            return true;
        }

        return false;
    }

    private bool TryUseSpecialAttack(AttackDefinition attack, string actionKey, EnemyAttackAnimationType animationType)
    {
        if (attack == null || targetStats == null || selfStats == null)
            return false;

        if (actionLimiter != null && !actionLimiter.CanUseCustomAction(actionKey))
            return false;

        CharacterHealth targetHealth = targetStats.GetComponent<CharacterHealth>();
        if (targetHealth == null || targetHealth.IsDead)
            return false;

        if (!IsTargetInRange(attack.Range))
            return false;

        if (!ap.HasEnoughAP(attack.ApCost))
            return false;

        if (!ap.SpendAP(attack.ApCost))
            return false;

        actionLimiter?.MarkCustomActionUsed(actionKey);

        StopMovement();
        PlayAttackAnimation(animationType);

        DamageResult result = DamageCalculator.ResolveAttack(selfStats, targetStats, attack);

        if (result.Hit)
        {
            targetHealth.TakeDamage(result.FinalDamage);

            if (DamageNumberManager.Instance != null)
            {
                DamageNumberManager.Instance.ShowDamage(
                    result.FinalDamage,
                    targetStats.transform,
                    result.DamageType,
                    result.WasCritical
                );
            }
        }
        else
        {
            if (DamageNumberManager.Instance != null)
                DamageNumberManager.Instance.ShowMiss(targetStats.transform);
        }

        LogSpecialAttackResult(attack, targetStats, targetHealth, result);
        return true;
    }

    private void LogSpecialAttackResult(AttackDefinition attack, CharacterStats target, CharacterHealth targetHealth, DamageResult result)
    {
        string attackerName = gameObject.name;
        string targetName = target != null ? target.gameObject.name : "Target";

        string line = result.BuildLogLine(attackerName, attack.AttackName, targetName);

        if (targetHealth != null)
            line += $" | Target HP: {targetHealth.CurrentHP}/{targetHealth.MaxHP}";

        GameLog.Combat(line);
    }

    private void PlayAttackAnimation(EnemyAttackAnimationType type)
    {
        if (animationController == null)
            return;

        switch (type)
        {
            case EnemyAttackAnimationType.Medium:
                animationController.PlayMediumAttackAnimation(targetStats != null ? targetStats.transform : null);
                break;

            case EnemyAttackAnimationType.Heavy:
                animationController.PlayHeavyAttackAnimation(targetStats != null ? targetStats.transform : null);
                break;

            default:
                animationController.PlayBasicAttackAnimation(targetStats != null ? targetStats.transform : null);
                break;
        }
    }

    private bool TryMoveTowardTargetWithinAP()
    {
        if (agent == null || !agent.enabled || targetStats == null || basicAttack == null)
            return false;

        int currentAP = ap.CurrentAP;
        if (currentAP <= 0)
            return false;

        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(targetStats.transform.position, path))
            return false;

        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        if (path.corners == null || path.corners.Length < 2)
            return false;

        float totalPathLength = GetPathLength(path);
        if (totalPathLength <= 0.01f)
            return false;

        AttackMovePlan plan = BuildBestMovePlan(currentAP, totalPathLength);
        int moveAPBudget = Mathf.Max(0, currentAP - plan.ReservedAP);
        if (moveAPBudget <= 0)
            return false;

        float maxMoveDistance = moveAPBudget * metersPerAP;
        if (maxMoveDistance < minMoveDistance)
            return false;

        float desiredRemainingDistance = plan.CanAttackAfterMove
            ? GetDesiredRemainingPathDistanceToAttack(plan.AttackRange)
            : GetDesiredRemainingPathDistanceToIdleNearTarget();

        float desiredMoveDistance = totalPathLength - desiredRemainingDistance;
        if (desiredMoveDistance <= minMoveDistance)
            return false;

        float actualMoveDistance = Mathf.Min(maxMoveDistance, desiredMoveDistance);
        if (actualMoveDistance < minMoveDistance)
            return false;

        Vector3 limitedDestination = GetPointAlongPath(path.corners, actualMoveDistance, out float sampledMoveDistance);
        if (sampledMoveDistance < minMoveDistance)
            return false;

        if (!NavMesh.SamplePosition(limitedDestination, out NavMeshHit hit, 1.0f, agent.areaMask))
            return false;

        int apCostForMove = Mathf.CeilToInt(sampledMoveDistance / metersPerAP);
        apCostForMove = Mathf.Clamp(apCostForMove, 1, ap.CurrentAP);

        if (!ap.SpendAP(apCostForMove))
            return false;

        agent.stoppingDistance = Mathf.Max(0.08f, destinationTolerance);
        agent.isStopped = false;
        agent.SetDestination(hit.position);

        return true;
    }

    private AttackMovePlan BuildBestMovePlan(int currentAP, float totalPathLength)
    {
        if (CanReachAndUseAttackThisTurn(heavyAttack, HeavyAttackKey, currentAP, totalPathLength))
            return new AttackMovePlan(heavyAttack.ApCost, heavyAttack.Range, true);

        if (CanReachAndUseAttackThisTurn(mediumAttack, MediumAttackKey, currentAP, totalPathLength))
            return new AttackMovePlan(mediumAttack.ApCost, mediumAttack.Range, true);

        if (CanReachAndUseBasicThisTurn(currentAP, totalPathLength))
            return new AttackMovePlan(basicAttack.GetAttackAPCost(), basicAttack.GetAttackRange(), true);

        return new AttackMovePlan(0, basicAttack.GetAttackRange(), false);
    }

    private bool CanReachAndUseAttackThisTurn(AttackDefinition attack, string actionKey, int currentAP, float totalPathLength)
    {
        if (attack == null)
            return false;

        if (actionLimiter != null && !actionLimiter.CanUseCustomAction(actionKey))
            return false;

        if (currentAP < attack.ApCost)
            return false;

        int moveAPBudget = currentAP - attack.ApCost;
        if (moveAPBudget <= 0)
            return false;

        float maxMoveDistance = moveAPBudget * metersPerAP;
        float desiredRemainingDistance = GetDesiredRemainingPathDistanceToAttack(attack.Range);
        float requiredMoveDistance = totalPathLength - desiredRemainingDistance;

        return requiredMoveDistance > minMoveDistance && requiredMoveDistance <= maxMoveDistance;
    }

    private bool CanReachAndUseBasicThisTurn(int currentAP, float totalPathLength)
    {
        if (basicAttack == null)
            return false;

        if (actionLimiter != null && !actionLimiter.CanUseBasicAttack())
            return false;

        int basicCost = basicAttack.GetAttackAPCost();
        if (currentAP < basicCost)
            return false;

        int moveAPBudget = currentAP - basicCost;
        if (moveAPBudget <= 0)
            return false;

        float maxMoveDistance = moveAPBudget * metersPerAP;
        float desiredRemainingDistance = GetDesiredRemainingPathDistanceToAttack(basicAttack.GetAttackRange());
        float requiredMoveDistance = totalPathLength - desiredRemainingDistance;

        return requiredMoveDistance > minMoveDistance && requiredMoveDistance <= maxMoveDistance;
    }

    private float GetDesiredRemainingPathDistanceToAttack(float attackRange)
    {
        float attackerRadius = GetBodyRadius(transform);
        float targetRadius = GetBodyRadius(targetStats.transform);

        return attackerRadius + targetRadius + Mathf.Max(0f, attackRange - attackRangeBuffer);
    }

    private bool IsTargetInRange(float range)
    {
        if (basicAttack == null || targetStats == null)
            return false;

        return basicAttack.GetSurfaceDistanceToTarget(targetStats.transform) <= range;
    }

    private float GetBodyRadius(Transform t)
    {
        if (t == null)
            return 0.5f;

        if (t.TryGetComponent<NavMeshAgent>(out var navAgent))
            return Mathf.Max(0.1f, navAgent.radius);

        if (t.TryGetComponent<CapsuleCollider>(out var capsule))
        {
            float scale = Mathf.Max(t.lossyScale.x, t.lossyScale.z);
            return capsule.radius * scale;
        }

        if (t.TryGetComponent<SphereCollider>(out var sphere))
        {
            float scale = Mathf.Max(t.lossyScale.x, t.lossyScale.z);
            return sphere.radius * scale;
        }

        if (t.TryGetComponent<Collider>(out var col))
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);

        return 0.5f;
    }

    private Vector3 GetPointAlongPath(Vector3[] corners, float maxDistance, out float actualDistance)
    {
        actualDistance = 0f;

        if (corners == null || corners.Length == 0)
            return transform.position;

        Vector3 result = corners[0];

        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 from = corners[i - 1];
            Vector3 to = corners[i];
            float segmentLength = Vector3.Distance(from, to);

            if (actualDistance + segmentLength >= maxDistance)
            {
                float remaining = maxDistance - actualDistance;
                float t = segmentLength > 0.001f ? remaining / segmentLength : 0f;
                result = Vector3.Lerp(from, to, t);
                actualDistance = maxDistance;
                return result;
            }

            actualDistance += segmentLength;
            result = to;
        }

        return result;
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

    private IEnumerator WaitUntilMovementStops()
    {
        if (agent == null || !agent.enabled)
            yield break;

        while (agent.pathPending)
            yield return null;

        while (true)
        {
            bool reached =
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance + destinationTolerance;

            bool almostStill = agent.velocity.sqrMagnitude <= 0.01f;

            if (reached && almostStill)
                break;

            yield return null;
        }

        agent.ResetPath();
        agent.isStopped = true;
    }

    private void StopMovement()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void EndTurn(Action onTurnFinished)
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        turnLock?.LockNow();

        isTakingTurn = false;
        onTurnFinished?.Invoke();
    }

    private float GetDesiredRemainingPathDistanceToIdleNearTarget()
    {
        float attackerRadius = GetBodyRadius(transform);
        float targetRadius = GetBodyRadius(targetStats.transform);

        return attackerRadius + targetRadius + Mathf.Max(0.05f, idleApproachGap);
    }

    private readonly struct AttackMovePlan
    {
        public readonly int ReservedAP;
        public readonly float AttackRange;
        public readonly bool CanAttackAfterMove;

        public AttackMovePlan(int reservedAP, float attackRange, bool canAttackAfterMove)
        {
            ReservedAP = reservedAP;
            AttackRange = attackRange;
            CanAttackAfterMove = canAttackAfterMove;
        }
    }

    private enum EnemyAttackAnimationType
    {
        Basic,
        Medium,
        Heavy
    }
}