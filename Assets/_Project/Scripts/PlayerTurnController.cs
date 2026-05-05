using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerAP))]
[RequireComponent(typeof(CharacterHealth))]
[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(PlayerNavMeshMover))]
[RequireComponent(typeof(PlayerCombatController))]
[RequireComponent(typeof(TurnActionLimiter))]
public class PlayerTurnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAP ap;
    [SerializeField] private CharacterHealth health;
    [SerializeField] private CharacterStats stats;
    [SerializeField] private PlayerNavMeshMover mover;
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private TurnActionLimiter actionLimiter;

    private CharacterStatusEffects statusEffects;

    public CharacterHealth Health => health;
    public CharacterStats Stats => stats;
    public int Initiative => stats != null ? stats.Initiative : 0;
    public bool IsAlive => health != null && !health.IsDead;
    public bool IsTurnActive { get; private set; }

    private void Awake()
    {
        if (ap == null) ap = GetComponent<PlayerAP>();
        if (health == null) health = GetComponent<CharacterHealth>();
        if (stats == null) stats = GetComponent<CharacterStats>();
        if (mover == null) mover = GetComponent<PlayerNavMeshMover>();
        if (combatController == null) combatController = GetComponent<PlayerCombatController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (actionLimiter == null) actionLimiter = GetComponent<TurnActionLimiter>();

        statusEffects = GetComponent<CharacterStatusEffects>();
        if (statusEffects == null)
            statusEffects = gameObject.AddComponent<CharacterStatusEffects>();

        SetControlEnabled(false);
    }

    public bool BeginTurn()
    {
        if (!IsAlive)
            return false;

        IsTurnActive = true;

        bool survivedStartEffects = statusEffects == null || statusEffects.ProcessStartOfOwnerTurn();
        if (!survivedStartEffects || !IsAlive)
        {
            IsTurnActive = false;
            SetControlEnabled(false);
            StopMovement();
            return false;
        }

        if (statusEffects != null && statusEffects.IsCurrentTurnBlocked)
        {
            EndTurn();
            return false;
        }

        if (ap != null)
            ap.RestoreAllAP();

        actionLimiter?.ResetTurnUsage();
        SetControlEnabled(true);
        return true;
    }

    public void EndTurn()
    {
        if (IsTurnActive)
            statusEffects?.ProcessEndOfOwnerTurn();

        IsTurnActive = false;
        SetControlEnabled(false);
        StopMovement();
    }

    public void SetExplorationControl(bool enabled)
    {
        IsTurnActive = false;
        SetControlEnabled(enabled);

        if (!enabled)
            StopMovement();
    }

    private void SetControlEnabled(bool enabled)
    {
        if (mover != null)
            mover.SetTurnInputEnabled(enabled);

        if (combatController != null)
            combatController.SetTurnInputEnabled(enabled);

        if (!enabled && combatController != null)
            combatController.ClearSelectedSkill();
    }

    private void StopMovement()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }
}