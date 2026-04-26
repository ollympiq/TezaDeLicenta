using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(TurnActionLimiter))]
public class CharacterSkillCaster : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private LayerMask skillTargetMask;

    private CharacterStats casterStats;
    private PlayerAP playerAP;
    private PlayerAnimationController animationController;
    private NavMeshAgent agent;
    private CharacterHealth selfHealth;
    private TurnActionLimiter turnActionLimiter;

    private void Awake()
    {
        casterStats = GetComponent<CharacterStats>();
        playerAP = GetComponent<PlayerAP>();
        animationController = GetComponent<PlayerAnimationController>();
        agent = GetComponent<NavMeshAgent>();
        selfHealth = GetComponent<CharacterHealth>();
        turnActionLimiter = GetComponent<TurnActionLimiter>();
    }

    public bool TryUseSkillOnTarget(SkillDefinition skill, CharacterStats primaryTarget)
    {
        if (selfHealth != null && selfHealth.IsDead)
            return false;

        if (skill == null || skill.SkillType != SkillType.Active)
            return false;

        if (turnActionLimiter != null && !turnActionLimiter.CanUseSkill(skill))
        {
            GameLog.Warning($"Skill-ul {skill.DisplayName} a fost deja folosit in acest tur.");
            return false;
        }

        if (primaryTarget == null)
            return false;

        CharacterHealth primaryHealth = primaryTarget.GetComponent<CharacterHealth>();
        if (primaryHealth == null || primaryHealth.IsDead)
            return false;

        if (!IsPointInRange(primaryTarget.transform.position, skill.Range))
        {
            GameLog.Warning($"Tinta este prea departe pentru skill-ul {skill.DisplayName}.");
            return false;
        }

        List<CharacterStats> targets = CollectTargetsFromTarget(skill, primaryTarget);
        if (targets.Count == 0)
        {
            GameLog.Warning($"Nu exista tinte valide pentru skill-ul {skill.DisplayName}.");
            return false;
        }

        if (!TrySpendAP(skill.ApCost, skill.DisplayName))
            return false;

        turnActionLimiter?.MarkSkillUsed(skill);

        StopMovement();

        if (animationController != null)
            animationController.PlayAttackAnimation(primaryTarget.transform);

        ApplySkillToTargets(skill, targets);
        return true;
    }

    public bool TryUseSkillAtPoint(SkillDefinition skill, Vector3 point)
    {
        if (selfHealth != null && selfHealth.IsDead)
            return false;

        if (skill == null || skill.SkillType != SkillType.Active)
            return false;

        if (turnActionLimiter != null && !turnActionLimiter.CanUseSkill(skill))
        {
            GameLog.Warning($"Skill-ul {skill.DisplayName} a fost deja folosit in acest tur.");
            return false;
        }

        if (!IsPointInRange(point, skill.Range))
        {
            GameLog.Warning($"Punctul ales este prea departe pentru skill-ul {skill.DisplayName}.");
            return false;
        }

        List<CharacterStats> targets = CollectTargetsFromPoint(skill, point);
        if (targets.Count == 0)
        {
            GameLog.Warning($"Nu exista tinte valide in aria skill-ului {skill.DisplayName}.");
            return false;
        }

        if (!TrySpendAP(skill.ApCost, skill.DisplayName))
            return false;

        turnActionLimiter?.MarkSkillUsed(skill);

        StopMovement();
        ApplySkillToTargets(skill, targets);
        return true;
    }

    private List<CharacterStats> CollectTargetsFromTarget(SkillDefinition skill, CharacterStats primaryTarget)
    {
        List<CharacterStats> result = new List<CharacterStats>();

        if (skill.AreaMode == SkillAreaMode.SingleTarget)
        {
            if (IsValidTarget(primaryTarget))
                result.Add(primaryTarget);

            return result;
        }

        return CollectTargetsInCircle(primaryTarget.transform.position, skill.AreaRadius);
    }

    private List<CharacterStats> CollectTargetsFromPoint(SkillDefinition skill, Vector3 point)
    {
        float radius = skill.AreaMode == SkillAreaMode.Circle
            ? skill.AreaRadius
            : Mathf.Max(0.75f, skill.AreaRadius);

        return CollectTargetsInCircle(point, radius);
    }

    private List<CharacterStats> CollectTargetsInCircle(Vector3 center, float radius)
    {
        List<CharacterStats> result = new List<CharacterStats>();
        HashSet<CharacterStats> uniqueTargets = new HashSet<CharacterStats>();

        Collider[] hits = Physics.OverlapSphere(center, radius, skillTargetMask);

        for (int i = 0; i < hits.Length; i++)
        {
            CharacterStats targetStats = hits[i].GetComponentInParent<CharacterStats>();
            if (targetStats == null)
                continue;

            if (targetStats == casterStats)
                continue;

            if (!IsValidTarget(targetStats))
                continue;

            if (uniqueTargets.Add(targetStats))
                result.Add(targetStats);
        }

        return result;
    }

    private bool IsValidTarget(CharacterStats targetStats)
    {
        CharacterHealth health = targetStats.GetComponent<CharacterHealth>();
        return health != null && !health.IsDead;
    }

    private bool TrySpendAP(int apCost, string skillName)
    {
        if (playerAP == null)
            return true;

        if (!playerAP.HasEnoughAP(apCost))
        {
            GameLog.Warning($"Nu ai destul AP pentru skill-ul {skillName}.");
            return false;
        }

        return playerAP.SpendAP(apCost);
    }

    private void StopMovement()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private bool IsPointInRange(Vector3 worldPoint, float maxRange)
    {
        Vector3 a = transform.position;
        Vector3 b = worldPoint;

        a.y = 0f;
        b.y = 0f;

        float distance = Vector3.Distance(a, b);
        return distance <= maxRange;
    }

    private void ApplySkillToTargets(SkillDefinition skill, List<CharacterStats> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            CharacterStats targetStats = targets[i];
            CharacterHealth targetHealth = targetStats.GetComponent<CharacterHealth>();

            if (targetHealth == null || targetHealth.IsDead)
                continue;

            DamageResult result = DamageCalculator.ResolveSkill(casterStats, targetStats, skill);

            if (result.Hit)
            {
                targetHealth.TakeDamage(result.FinalDamage);

                if (DamageNumberManager.Instance != null)
                {
                    DamageNumberManager.Instance.ShowDamage(
                        result.FinalDamage,
                        targetStats.transform,
                        result.DamageType,
                        result.WasCritical
                    );
                }
            }
            else
            {
                if (DamageNumberManager.Instance != null)
                    DamageNumberManager.Instance.ShowMiss(targetStats.transform);
            }

            GameLog.Combat(BuildCombatLog(skill, targetStats.name, result, targetHealth));
        }
    }

    private string BuildCombatLog(SkillDefinition skill, string targetName, DamageResult result, CharacterHealth targetHealth)
    {
        if (!result.Hit)
        {
            return
                $"{name} used {skill.DisplayName} on {targetName} but missed. " +
                $"Hit chance: {result.HitChance:F1}% | " +
                $"Target HP: {targetHealth.CurrentHP}/{targetHealth.MaxHP}";
        }

        string critText = result.WasCritical ? " CRITICAL!" : "";

        return
            $"{name} used {skill.DisplayName} on {targetName} | " +
            $"Type: {result.DamageType} | " +
            $"Base: {result.BaseDamage} | " +
            $"Armor Reduction: {result.ArmorReductionPercent:F1}% | " +
            $"Resistance: {result.ResistancePercent:F1}% | " +
            $"Final Damage: {result.FinalDamage}{critText} | " +
            $"Target HP: {targetHealth.CurrentHP}/{targetHealth.MaxHP}";
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
#endif
}