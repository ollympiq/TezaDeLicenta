using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TurnAgentLock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private NavMeshObstacle turnObstacleProxy;

    [Header("Settings")]
    [SerializeField] private bool startLocked = true;
    [SerializeField] private bool snapToNavMeshWhenLocked = true;
    [SerializeField] private float snapSampleDistance = 2f;

    public bool IsLocked { get; private set; }

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (startLocked)
            LockNow();
        else
            UnlockImmediate();
    }

    public void LockNow()
    {
        SnapLockedTransformToNavMesh();

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (turnObstacleProxy != null)
            turnObstacleProxy.enabled = true;

        IsLocked = true;
    }

    public IEnumerator UnlockForTurn()
    {
        if (turnObstacleProxy != null)
            turnObstacleProxy.enabled = false;

        yield return null;
        UnlockImmediate();
    }

    public void SnapLockedTransformToNavMesh()
    {
        if (!snapToNavMeshWhenLocked || agent == null)
            return;

        int areaMask = agent.areaMask != 0 ? agent.areaMask : NavMesh.AllAreas;

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, snapSampleDistance, areaMask))
            return;

        Vector3 snapped = hit.position;
        snapped.y += agent.baseOffset;
        transform.position = snapped;
    }

    private void UnlockImmediate()
    {
        if (agent == null)
        {
            IsLocked = false;
            return;
        }

        if (!agent.enabled)
            agent.enabled = true;

        int areaMask = agent.areaMask != 0 ? agent.areaMask : NavMesh.AllAreas;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, snapSampleDistance, areaMask))
        {
            agent.Warp(hit.position);
        }
        else
        {
            Vector3 fallback = transform.position - Vector3.up * agent.baseOffset;
            agent.Warp(fallback);
        }

        agent.isStopped = true;
        agent.ResetPath();

        IsLocked = false;
    }
}