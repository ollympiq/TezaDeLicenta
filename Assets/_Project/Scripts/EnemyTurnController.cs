using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerAP))]
[RequireComponent(typeof(CharacterBasicAttack))]
[RequireComponent(typeof(CharacterHealth))]
public class EnemyTurnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CharacterStats targetStats;

    [Header("Turn")]
    [SerializeField] private float thinkDelay = 0.2f;
    [SerializeField] private float afterMoveDelay = 0.15f;
    [SerializeField] private float afterAttackDelay = 0.6f;

    [Header("Movement AP")]
    [SerializeField] private float metersPerAP = 1.5f;
    [SerializeField] private float minMoveDistance = 0.2f;
    [SerializeField] private bool reserveAPForAttack = true;

    private PlayerAP ap;
    private CharacterBasicAttack basicAttack;
    private CharacterHealth health;

    private Coroutine turnRoutine;
    private bool isTakingTurn;

    public bool IsTakingTurn => isTakingTurn;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        ap = GetComponent<PlayerAP>();
        basicAttack = GetComponent<CharacterBasicAttack>();
        health = GetComponent<CharacterHealth>();
    }

    public void SetTarget(CharacterStats newTarget)
    {
        targetStats = newTarget;
    }

    public void StartTurn(Action onTurnFinished = null)
    {
        if (turnRoutine != null)
            StopCoroutine(turnRoutine);

        turnRoutine = StartCoroutine(TurnRoutine(onTurnFinished));
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
        yield return new WaitForSeconds(thinkDelay);

        while (ap.CurrentAP > 0)
        {
            if (targetStats == null)
                break;

            targetHealth = targetStats.GetComponent<CharacterHealth>();
            if (targetHealth == null || targetHealth.IsDead)
                break;

            // 1. Atac daca poate
            if (basicAttack.TryAttackTarget(targetStats))
            {
                yield return new WaitForSeconds(afterAttackDelay);
                continue;
            }

            // 2. Daca nu poate ataca, incearca sa se miste limitat de AP
            bool moved = TryMoveTowardTargetWithinAP();
            if (!moved)
                break;

            yield return WaitUntilMovementStops();
            yield return new WaitForSeconds(afterMoveDelay);
        }

        EndTurn(onTurnFinished);
    }

    private bool TryMoveTowardTargetWithinAP()
    {
        if (agent == null || !agent.enabled || targetStats == null)
            return false;

        int currentAP = ap.CurrentAP;
        if (currentAP <= 0)
            return false;

        int attackCost = basicAttack.GetAttackAPCost();
        int moveAPBudget = currentAP;

        if (reserveAPForAttack && currentAP > attackCost)
            moveAPBudget = currentAP - attackCost;

        if (moveAPBudget <= 0)
            return false;

        float maxMoveDistance = moveAPBudget * metersPerAP;

        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(targetStats.transform.position, path))
            return false;

        if (path.corners == null || path.corners.Length < 2)
            return false;

        Vector3 limitedDestination = GetPointAlongPath(path.corners, maxMoveDistance, out float actualMoveDistance);

        if (actualMoveDistance < minMoveDistance)
            return false;

        int apCostForMove = Mathf.CeilToInt(actualMoveDistance / metersPerAP);
        apCostForMove = Mathf.Clamp(apCostForMove, 1, ap.CurrentAP);

        if (!ap.SpendAP(apCostForMove))
            return false;

        agent.isStopped = false;
        agent.SetDestination(limitedDestination);
        return true;
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

    private IEnumerator WaitUntilMovementStops()
    {
        if (agent == null || !agent.enabled)
            yield break;

        while (agent.pathPending)
            yield return null;

        while (agent.remainingDistance > agent.stoppingDistance + 0.05f || agent.velocity.sqrMagnitude > 0.01f)
            yield return null;

        agent.ResetPath();
        agent.isStopped = true;
    }

    private void EndTurn(Action onTurnFinished)
    {
        isTakingTurn = false;

        if (agent != null && agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        onTurnFinished?.Invoke();
    }
}