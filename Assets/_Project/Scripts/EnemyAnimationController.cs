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
    [SerializeField] private float runThreshold = 0.1f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Action Locks")]
    [SerializeField] private float hurtLockDuration = 0.45f;
    [SerializeField] private float attackLockDuration = 0.7f;

    [Header("Death")]
    [SerializeField] private bool disableAgentOnDeath = true;
    [SerializeField] private bool disableColliderOnDeath = true;

    private CharacterHealth health;
    private int lastHp;
    private float actionLockTimer;
    private bool isDead;

    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
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
            animator.SetBool(IsRunningHash, false);
            return;
        }

        if (agent == null)
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

    public void PlayAttackAnimation(Transform target = null)
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
        if (animator == null || isDead)
            return;

        isDead = true;
        actionLockTimer = 0f;
        animator.SetBool(IsRunningHash, false);
        animator.ResetTrigger(HurtHash);
        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(DieHash);

        if (disableAgentOnDeath && agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.enabled = false;
        }

        if (disableColliderOnDeath && mainCollider != null)
            mainCollider.enabled = false;
    }

    private void RotateVisualToward(Vector3 direction, bool instant = false)
    {
        if (visualModel == null || direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        if (instant)
            visualModel.rotation = targetRotation;
        else
            visualModel.rotation = Quaternion.Slerp(
                visualModel.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }
}