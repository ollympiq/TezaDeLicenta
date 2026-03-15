using UnityEngine;
using UnityEngine.AI;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualModel;

    [Header("Movement Animation")]
    [SerializeField] private float runThreshold = 0.1f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Attack Animation")]
    [SerializeField] private float attackLockDuration = 0.6f;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private float attackLockTimer;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (agent == null || animator == null)
            return;

        if (attackLockTimer > 0f)
        {
            attackLockTimer -= Time.deltaTime;
            animator.SetBool(IsRunningHash, false);
            return;
        }

        Vector3 horizontalVelocity = agent.velocity;
        horizontalVelocity.y = 0f;

        float speed = horizontalVelocity.magnitude;
        bool isRunning = speed > runThreshold;

        animator.SetBool(IsRunningHash, isRunning);

        if (isRunning)
        {
            RotateVisualToward(horizontalVelocity.normalized);
        }
    }

    public void PlayAttackAnimation(Transform target)
    {
        if (animator == null)
            return;

        if (target != null)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                RotateVisualToward(dir.normalized, true);
        }

        attackLockTimer = attackLockDuration;
        animator.SetBool(IsRunningHash, false);
        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
    }

    private void RotateVisualToward(Vector3 direction, bool instant = false)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Transform targetTransform = visualModel != null ? visualModel : transform;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        if (instant)
        {
            targetTransform.rotation = targetRotation;
        }
        else
        {
            targetTransform.rotation = Quaternion.Slerp(
                targetTransform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}