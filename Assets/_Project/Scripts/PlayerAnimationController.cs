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
    private static readonly int BowShotHash = Animator.StringToHash("BowShot");
    private static readonly int SpellCastHash = Animator.StringToHash("SpellCast");

    private float attackLockTimer;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
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
            RotateVisualToward(horizontalVelocity.normalized);
    }

    public void PlayAttackAnimation(Transform target)
    {
        PlaySkillAnimation(SkillAnimationType.MeleeAttack, target);
    }

    public void PlaySkillAnimation(SkillAnimationType animationType, Transform target)
    {
        if (target != null)
            RotateTowardWorldPoint(target.position);

        PlaySkillAnimation(animationType);
    }

    public void PlaySkillAnimationAtPoint(SkillAnimationType animationType, Vector3 worldPoint)
    {
        RotateTowardWorldPoint(worldPoint);
        PlaySkillAnimation(animationType);
    }

    public void PlaySkillAnimation(SkillAnimationType animationType)
    {
        if (animator == null)
            return;

        if (animationType == SkillAnimationType.None)
            return;

        attackLockTimer = attackLockDuration;

        animator.SetBool(IsRunningHash, false);

        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(BowShotHash);
        animator.ResetTrigger(SpellCastHash);

        switch (animationType)
        {
            case SkillAnimationType.MeleeAttack:
                animator.SetTrigger(AttackHash);
                break;

            case SkillAnimationType.BowShot:
                animator.SetTrigger(BowShotHash);
                break;

            case SkillAnimationType.SpellCast:
                animator.SetTrigger(SpellCastHash);
                break;
        }
    }

    private void RotateTowardWorldPoint(Vector3 worldPoint)
    {
        Vector3 direction = worldPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            RotateVisualToward(direction.normalized, true);
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