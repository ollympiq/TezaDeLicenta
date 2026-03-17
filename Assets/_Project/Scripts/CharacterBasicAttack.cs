using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterStats))]
public class CharacterBasicAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private AttackDefinition basicAttack = new AttackDefinition();

    private CharacterStats attackerStats;
    private PlayerAP playerAP;
    private PlayerAnimationController animationController;
    private NavMeshAgent agent;

    private void Awake()
    {
        attackerStats = GetComponent<CharacterStats>();
        playerAP = GetComponent<PlayerAP>();
        animationController = GetComponent<PlayerAnimationController>();
        agent = GetComponent<NavMeshAgent>();
    }

    public bool TryAttackTarget(CharacterStats targetStats)
    {
        if (targetStats == null || targetStats == attackerStats)
            return false;

        CharacterHealth targetHealth = targetStats.GetComponent<CharacterHealth>();
        if (targetHealth == null || targetHealth.IsDead)
            return false;

        if (!IsTargetInRange(targetStats.transform))
        {
            Debug.Log("Tinta este prea departe.");
            return false;
        }

        if (playerAP != null)
        {
            if (!playerAP.HasEnoughAP(basicAttack.ApCost))
            {
                Debug.Log("Nu ai destul AP pentru atac.");
                return false;
            }

            if (!playerAP.SpendAP(basicAttack.ApCost))
                return false;
        }

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (animationController != null)
            animationController.PlayAttackAnimation(targetStats.transform);

        DamageResult result = DamageCalculator.ResolveAttack(attackerStats, targetStats, basicAttack);

        if (result.Hit)
        {
            targetHealth.TakeDamage(result.FinalDamage);

            if (DamageNumberManager.Instance != null)
                DamageNumberManager.Instance.ShowDamage(
                    result.FinalDamage,
                    targetStats.transform,
                    result.DamageType,
                    result.WasCritical
                );
        }
        else
        {
            if (DamageNumberManager.Instance != null)
                DamageNumberManager.Instance.ShowMiss(targetStats.transform);
        }

        Debug.Log(BuildCombatLog(targetStats.name, result, targetHealth));
        return true;
    }

    private bool IsTargetInRange(Transform target)
    {
        Vector3 a = transform.position;
        Vector3 b = target.position;

        a.y = 0f;
        b.y = 0f;

        float distance = Vector3.Distance(a, b);
        return distance <= basicAttack.Range;
    }

    private string BuildCombatLog(string targetName, DamageResult result, CharacterHealth targetHealth)
    {
        if (!result.Hit)
            return $"{name} used {basicAttack.AttackName} on {targetName} but missed. Hit chance: {result.HitChance:F1}% | Target HP: {targetHealth.CurrentHP}/{targetHealth.MaxHP}";

        string critText = result.WasCritical ? " CRITICAL!" : "";

        return
            $"{name} used {basicAttack.AttackName} on {targetName} | " +
            $"Type: {result.DamageType} | " +
            $"Base: {result.BaseDamage} | " +
            $"Armor Reduction: {result.ArmorReductionPercent:F1}% | " +
            $"Resistance: {result.ResistancePercent:F1}% | " +
            $"Final Damage: {result.FinalDamage}{critText} | " +
            $"Target HP: {targetHealth.CurrentHP}/{targetHealth.MaxHP}";
    }
}