using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterHealth))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualModel;
    [SerializeField] private Collider mainCollider;
    [SerializeField] private PlayerNavMeshMover moveController;
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private CharacterSkillCaster skillCaster;

    [Header("Movement Animation")]
    [SerializeField] private float runThreshold = 0.1f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Action Locks")]
    [SerializeField] private float attackLockDuration = 0.6f;
    [SerializeField] private float hurtLockDuration = 0.45f;

    [Header("Death")]
    [SerializeField] private bool disableAgentOnDeath = true;
    [SerializeField] private bool disableColliderOnDeath = false;
    [SerializeField] private bool disableInputScriptsOnDeath = true;

    private CharacterHealth health;
    private int lastHp;
    private float actionLockTimer;
    private bool isDead;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DieHash = Animator.StringToHash("Die");

    public bool IsDead => isDead;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (moveController == null)
            moveController = GetComponent<PlayerNavMeshMover>();

        if (combatController == null)
            combatController = GetComponent<PlayerCombatController>();

        if (skillCaster == null)
            skillCaster = GetComponent<CharacterSkillCaster>();

        if (mainCollider == null)
            mainCollider = GetComponent<Collider>();

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
        if (animator == null)
            return;

        if (isDead)
        {
            animator.SetBool(IsRunningHash, false);
            return;
        }

        if (actionLockTimer > 0f)
        {
            actionLockTimer -= Time.deltaTime;
            animator.SetBool(IsRunningHash, false);
            return;
        }

        if (agent == null || !agent.enabled)
        {
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
        if (animator == null || isDead)
            return;

        if (target != null)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                RotateVisualToward(dir.normalized, true);
        }

        actionLockTimer = attackLockDuration;
        animator.SetBool(IsRunningHash, false);
        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
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
            animator.ResetTrigger(HurtHash);
            animator.SetTrigger(HurtHash);
        }

        lastHp = currentHp;
    }

    private void HandleDied(CharacterHealth deadHealth)
    {
        if (isDead)
            return;

        isDead = true;
        actionLockTimer = 0f;

        if (animator != null)
        {
            animator.SetBool(IsRunningHash, false);
            animator.ResetTrigger(HurtHash);
            animator.ResetTrigger(AttackHash);
            animator.SetTrigger(DieHash);
        }

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();

            if (disableAgentOnDeath)
                agent.enabled = false;
        }

        if (disableColliderOnDeath && mainCollider != null)
            mainCollider.enabled = false;

        if (disableInputScriptsOnDeath)
        {
            if (moveController != null)
                moveController.enabled = false;

            if (combatController != null)
                combatController.enabled = false;

            if (skillCaster != null)
                skillCaster.enabled = false;
        }
    }

    private void RotateVisualToward(Vector3 direction, bool instant = false)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Transform targetTransform = visualModel != null ? visualModel : transform;
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

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