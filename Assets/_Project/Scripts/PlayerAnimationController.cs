using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterHealth))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualModel;

    [Header("Movement Animation")]
    [SerializeField] private float runThreshold = 0.1f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Action Locks")]
    [SerializeField] private float attackLockDuration = 0.6f;
    [SerializeField] private float hurtLockDuration = 0.45f;

    [Header("Death")]
    [SerializeField] private bool stopAgentOnDeath = true;

    private CharacterHealth health;
    private int lastHp;
    private float actionLockTimer;
    private bool isDead;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int BowShotHash = Animator.StringToHash("BowShot");
    private static readonly int SpellCastHash = Animator.StringToHash("SpellCast");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        health = GetComponent<CharacterHealth>();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDied += HandleDied;
            lastHp = health.CurrentHP;
        }
    }

    private void Start()
    {
        if (health != null)
            lastHp = health.CurrentHP;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDied -= HandleDied;
        }
    }

    private void Update()
    {
        if (agent == null || animator == null || isDead)
            return;

        if (actionLockTimer > 0f)
        {
            actionLockTimer -= Time.deltaTime;
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
        if (isDead)
            return;

        if (target != null)
            RotateTowardWorldPoint(target.position);

        PlaySkillAnimation(animationType);
    }

    public void PlaySkillAnimationAtPoint(SkillAnimationType animationType, Vector3 worldPoint)
    {
        if (isDead)
            return;

        RotateTowardWorldPoint(worldPoint);
        PlaySkillAnimation(animationType);
    }

    public void PlaySkillAnimation(SkillAnimationType animationType)
    {
        if (animator == null || isDead)
            return;

        if (animationType == SkillAnimationType.None)
            return;

        actionLockTimer = attackLockDuration;

        animator.SetBool(IsRunningHash, false);

        ResetActionTriggers();

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

    private void HandleHealthChanged(int currentHp, int maxHp)
    {
        if (animator == null || isDead)
        {
            lastHp = currentHp;
            return;
        }

        bool tookDamage = currentHp < lastHp;
        bool stillAlive = currentHp > 0;

        if (tookDamage && stillAlive)
        {
            actionLockTimer = hurtLockDuration;

            animator.SetBool(IsRunningHash, false);
            ResetActionTriggers();

            animator.ResetTrigger(HurtHash);
            animator.SetTrigger(HurtHash);
        }

        lastHp = currentHp;
    }

    private void HandleDied(CharacterHealth deadHealth)
    {
        if (animator == null || isDead)
            return;

        isDead = true;
        actionLockTimer = 0f;

        animator.SetBool(IsRunningHash, false);
        ResetActionTriggers();
        animator.ResetTrigger(HurtHash);
        animator.SetTrigger(DieHash);

        if (stopAgentOnDeath && agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void ResetActionTriggers()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(BowShotHash);
        animator.ResetTrigger(SpellCastHash);
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