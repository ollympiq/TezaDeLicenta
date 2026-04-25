using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NavMeshObstacle))]
[RequireComponent(typeof(CharacterHealth))]
public class EnemyNavModeSwitcher : MonoBehaviour
{
    [SerializeField] private CharacterHealth health;
    [SerializeField] private bool useObstacleOnlyWhenDead = true;
    [SerializeField] private bool enableCarvingWhenDead = true;

    private NavMeshAgent agent;
    private NavMeshObstacle obstacle;
    private bool deadModeApplied;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();

        if (health == null)
            health = GetComponent<CharacterHealth>();

        if (obstacle != null)
            obstacle.enabled = false;
    }

    private void Update()
    {
        if (health == null || !health.IsDead || deadModeApplied)
            return;

        ApplyDeadMode();
    }

    private void ApplyDeadMode()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (obstacle != null)
        {
            if (useObstacleOnlyWhenDead)
            {
                obstacle.carving = enableCarvingWhenDead;
                obstacle.enabled = true;
            }
            else
            {
                obstacle.enabled = false;
            }
        }

        deadModeApplied = true;
    }
}