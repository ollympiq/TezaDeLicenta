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

        // Lasam un frame pentru refresh-ul carve-ului in NavMesh
        yield return null;

        UnlockImmediate();
    }

    private void UnlockImmediate()
    {
        if (agent != null && !agent.enabled)
            agent.enabled = true;

        if (agent != null)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, agent.areaMask))
                agent.Warp(hit.position);
            else
                agent.Warp(transform.position);

            agent.isStopped = true;
            agent.ResetPath();
        }

        IsLocked = false;
    }
}