using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterHealth))]
public class EnemyAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualModel;
    [SerializeField] private Collider mainCollider;

    [Header("Movement")]
    [SerializeField] private float runEnterThreshold = 0.08f;
    [SerializeField] private float runExitThreshold = 0.03f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float stopGraceTime = 0.12f;

    [Header("Action Locks")]
    [SerializeField] private float hurtLockDuration = 0.45f;
    [SerializeField] private float basicAttackLockDuration = 0.7f;
    [SerializeField] private float mediumAttackLockDuration = 0.85f;
    [SerializeField] private float heavyAttackLockDuration = 1.0f;

    [Header("Death")]
    [SerializeField] private bool disableAgentOnDeath = true;
    [SerializeField] private bool disableColliderOnDeath = true;

    private CharacterHealth health;
    private int lastHp;
    private float actionLockTimer;
    private bool isDead;

    private float runningHoldTimer;
    private bool runningState;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int MediumAttackHash = Animator.StringToHash("MediumAttack");
    private static readonly int HeavyAttackHash = Animator.StringToHash("HeavyAttack");
    private static readonly int DieHash = Animator.StringToHash("Die");

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        health = GetComponent<CharacterHealth>();

        if (mainCollider == null)
            mainCollider = GetComponent<Collider>();
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
        if (animator == null || isDead)
            return;

        if (actionLockTimer > 0f)
        {
            actionLockTimer -= Time.deltaTime;
            SetRunning(false);
            return;
        }

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            SetRunning(false);
            return;
        }

        Vector3 desired = agent.desiredVelocity;
        desired.y = 0f;

        bool hasMovementIntent =
            agent.pathPending ||
            (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.05f);

        float speed = desired.magnitude;

        if (hasMovementIntent && speed > runExitThreshold)
            runningHoldTimer = stopGraceTime;
        else
            runningHoldTimer = Mathf.Max(0f, runningHoldTimer - Time.deltaTime);

        if (!runningState)
        {
            if (hasMovementIntent && (speed >= runEnterThreshold || runningHoldTimer > 0f))
                runningState = true;
        }
        else
        {
            if (!hasMovementIntent && runningHoldTimer <= 0f)
                runningState = false;
            else if (speed <= runExitThreshold && runningHoldTimer <= 0f)
                runningState = false;
        }

        animator.SetBool(IsRunningHash, runningState);

        if (runningState && desired.sqrMagnitude > 0.0001f)
            RotateVisualToward(desired.normalized);
    }

    public void PlayAttackAnimation(Transform target = null)
    {
        PlayBasicAttackAnimation(target);
    }

    public void PlayBasicAttackAnimation(Transform target = null)
    {
        if (animator == null || isDead)
            return;

        FaceTarget(target);

        actionLockTimer = basicAttackLockDuration;
        SetRunning(false);

        ResetAttackTriggers();
        animator.SetTrigger(AttackHash);
    }

    public void PlayMediumAttackAnimation(Transform target = null)
    {
        if (animator == null || isDead)
            return;

        FaceTarget(target);

        actionLockTimer = mediumAttackLockDuration;
        SetRunning(false);

        ResetAttackTriggers();
        animator.SetTrigger(MediumAttackHash);
    }

    public void PlayHeavyAttackAnimation(Transform target = null)
    {
        if (animator == null || isDead)
            return;

        FaceTarget(target);

        actionLockTimer = heavyAttackLockDuration;
        SetRunning(false);

        ResetAttackTriggers();
        animator.SetTrigger(HeavyAttackHash);
    }

    private void FaceTarget(Transform target)
    {
        if (target == null)
            return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
            RotateVisualToward(dir.normalized, true);
    }

    private void ResetAttackTriggers()
    {
        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(MediumAttackHash);
        animator.ResetTrigger(HeavyAttackHash);
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
            SetRunning(false);

            ResetAttackTriggers();
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
        SetRunning(false);

        animator.ResetTrigger(HurtHash);
        ResetAttackTriggers();
        animator.SetTrigger(DieHash);

        if (disableAgentOnDeath && agent != null)
        {
            if (agent.enabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.enabled = false;
            }
        }

        if (disableColliderOnDeath && mainCollider != null)
            mainCollider.enabled = false;
    }

    private void SetRunning(bool value)
    {
        runningState = value;
        runningHoldTimer = value ? stopGraceTime : 0f;

        if (animator != null)
            animator.SetBool(IsRunningHash, value);
    }

    private void RotateVisualToward(Vector3 direction, bool instant = false)
    {
        if (visualModel == null || direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        if (instant)
        {
            visualModel.rotation = targetRotation;
        }
        else
        {
            visualModel.rotation = Quaternion.Slerp(
                visualModel.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}