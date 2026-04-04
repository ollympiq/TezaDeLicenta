using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NavMeshObstacle))]
[RequireComponent(typeof(CharacterHealth))]
public class EnemyNavModeSwitcher : MonoBehaviour
{
    [SerializeField] private EnemyTurnController turnController;
    [SerializeField] private CharacterHealth health;

    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;
    private bool usingAgentMode;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();

        if (turnController == null)
            turnController = GetComponent<EnemyTurnController>();

        if (health == null)
            health = GetComponent<CharacterHealth>();

        ApplyInitialMode();
    }

    private void OnEnable()
    {
        ApplyInitialMode();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            DisableAllNavigation();
            return;
        }

        bool shouldUseAgent = turnController != null && turnController.IsTakingTurn;

        if (shouldUseAgent == usingAgentMode)
            return;

        if (shouldUseAgent)
            SetAgentModeInstant();
        else
            SetObstacleModeInstant();
    }

    private void ApplyInitialMode()
    {
        if (obstacle != null)
            obstacle.carving = false;

        bool shouldUseAgent = turnController != null && turnController.IsTakingTurn;

        if (shouldUseAgent)
            SetAgentModeInstant();
        else
            SetObstacleModeInstant();
    }

    private void SetAgentModeInstant()
    {
        if (obstacle != null)
            obstacle.enabled = false;

        if (agent != null)
        {
            if (!agent.enabled)
                agent.enabled = true;

            agent.Warp(transform.position);
            agent.isStopped = false;
            agent.ResetPath();
        }

        usingAgentMode = true;
    }

    private void SetObstacleModeInstant()
    {
        if (agent != null)
        {
            if (agent.enabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.enabled = false;
            }
        }

        if (obstacle != null)
        {
            obstacle.carving = false;
            obstacle.enabled = true;
        }

        usingAgentMode = false;
    }

    private void DisableAllNavigation()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (obstacle != null)
            obstacle.enabled = false;

        usingAgentMode = false;
    }
}