using UnityEngine;
using UnityEngine.AI;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualModel;

    [Header("Animation")]
    [SerializeField] private float runThreshold = 0.1f;
    [SerializeField] private float rotationSpeed = 12f;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (agent == null || animator == null)
            return;

        Vector3 horizontalVelocity = agent.velocity;
        horizontalVelocity.y = 0f;

        float speed = horizontalVelocity.magnitude;
        bool isRunning = speed > runThreshold;

        animator.SetBool(IsRunningHash, isRunning);

        if (isRunning && visualModel != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalVelocity.normalized);
            visualModel.rotation = Quaternion.Slerp(
                visualModel.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}