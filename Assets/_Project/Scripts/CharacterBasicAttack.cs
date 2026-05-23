using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(CharacterEquipment))]
[RequireComponent(typeof(TurnActionLimiter))]
public class CharacterBasicAttack : MonoBehaviour
{
    private CharacterStats attackerStats;
    private CharacterEquipment equipment;
    private PlayerAP playerAP;
    private TurnActionLimiter turnActionLimiter;

    private PlayerAnimationController playerAnimationController;
    private EnemyAnimationController enemyAnimationController;
    private CharacterCombatAudio combatAudio;

    private NavMeshAgent agent;
    private CharacterHealth selfHealth;

    private void Awake()
    {
        attackerStats = GetComponent<CharacterStats>();
        equipment = GetComponent<CharacterEquipment>();
        playerAP = GetComponent<PlayerAP>();
        turnActionLimiter = GetComponent<TurnActionLimiter>();

        playerAnimationController = GetComponent<PlayerAnimationController>();
        enemyAnimationController = GetComponent<EnemyAnimationController>();
        combatAudio = GetComponent<CharacterCombatAudio>();

        agent = GetComponent<NavMeshAgent>();
        selfHealth = GetComponent<CharacterHealth>();
    }

    public int GetAttackAPCost()
    {
        WeaponDefinition weapon = equipment != null ? equipment.EquippedWeaponDefinition : null;
        return GetAttackAPCost(weapon, null);
    }

    public int GetAttackAPCost(WeaponDefinition weapon, SkillDefinition basicAttackSkill)
    {
        if (basicAttackSkill != null)
            return Mathf.Max(0, basicAttackSkill.ApCost);

        return weapon != null ? Mathf.Max(0, weapon.ApCost) : 999;
    }

    public float GetAttackRange()
    {
        WeaponDefinition weapon = equipment != null ? equipment.EquippedWeaponDefinition : null;
        return GetAttackRange(weapon, null);
    }

    public float GetAttackRange(WeaponDefinition weapon, SkillDefinition basicAttackSkill)
    {
        if (basicAttackSkill != null && basicAttackSkill.Range > 0f)
            return basicAttackSkill.Range;

        return weapon != null ? Mathf.Max(0f, weapon.Range) : 0f;
    }

    public bool IsTargetInAttackRange(Transform target)
    {
        WeaponDefinition weapon = equipment != null ? equipment.EquippedWeaponDefinition : null;
        return IsTargetInAttackRange(target, weapon, null);
    }

    public bool IsTargetInAttackRange(Transform target, WeaponDefinition weapon, SkillDefinition basicAttackSkill)
    {
        if (target == null)
            return false;

        float range = GetAttackRange(weapon, basicAttackSkill);
        return IsTargetInRange(target, range);
    }

    public bool TryAttackTarget(CharacterStats targetStats)
    {
        return TryAttackTarget(targetStats, null);
    }

    public bool TryAttackTarget(CharacterStats targetStats, SkillDefinition basicAttackSkill)
    {
        if (selfHealth != null && selfHealth.IsDead)
            return false;

        if (targetStats == null || targetStats == attackerStats)
            return false;

        WeaponDefinition weapon = equipment != null ? equipment.EquippedWeaponDefinition : null;
        if (weapon == null)
        {
            GameLog.Warning("Nu ai nicio arma echipata.");
            return false;
        }

        if (basicAttackSkill != null && !SkillWeaponRequirementValidator.CanUseSkill(basicAttackSkill, weapon))
        {
            GameLog.Warning(SkillWeaponRequirementValidator.BuildRequirementMessage(basicAttackSkill, weapon));
            return false;
        }

        if (turnActionLimiter != null && !turnActionLimiter.CanUseBasicAttack())
        {
            GameLog.Warning("Basic Attack a fost deja folosit in acest tur.");
            return false;
        }

        CharacterHealth targetHealth = targetStats.GetComponent<CharacterHealth>();
        if (targetHealth == null || targetHealth.IsDead)
            return false;

        float attackRange = GetAttackRange(weapon, basicAttackSkill);
        int apCost = GetAttackAPCost(weapon, basicAttackSkill);

        if (!IsTargetInRange(targetStats.transform, attackRange))
        {
            GameLog.Warning("Tinta este prea departe pentru Basic Attack.");
            return false;
        }

        if (playerAP != null)
        {
            if (!playerAP.HasEnoughAP(apCost))
            {
                GameLog.Warning("Nu ai destul AP pentru Basic Attack.");
                return false;
            }

            if (!playerAP.SpendAP(apCost))
                return false;
        }

        if (IsPlayerAttacker() && CombatTelemetryTracker.Instance != null)
            CombatTelemetryTracker.Instance.RecordBasicAttackUsed();

        turnActionLimiter?.MarkBasicAttackUsed();

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        PlayBasicAttackAnimationAndSound(weapon, basicAttackSkill, targetStats.transform);

        DamageResult result = DamageCalculator.ResolveWeaponAttack(attackerStats, targetStats, weapon);

        if (result.Hit)
        {
            targetHealth.TakeDamage(result.FinalDamage);

            if (IsPlayerAttacker() && CombatTelemetryTracker.Instance != null)
            {
                CombatTelemetryTracker.Instance.RecordPlayerDamageDealt(
                    result.DamageType,
                    result.FinalDamage
                );
            }

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

        LogAttackResult(weapon, targetStats, targetHealth, result);
        return true;
    }

    private void PlayBasicAttackAnimationAndSound(WeaponDefinition weapon, SkillDefinition basicAttackSkill, Transform target)
    {
        SkillAnimationType animationType = basicAttackSkill != null
            ? basicAttackSkill.AnimationType
            : ResolveAnimationTypeFromWeapon(weapon);

        combatAudio?.PlaySkillAttackSound(animationType);

        if (playerAnimationController != null)
        {
            playerAnimationController.PlaySkillAnimation(animationType, target);
        }
        else if (enemyAnimationController != null)
        {
            enemyAnimationController.PlayBasicAttackAnimation(target);
        }
    }

    private SkillAnimationType ResolveAnimationTypeFromWeapon(WeaponDefinition weapon)
    {
        if (weapon == null)
            return SkillAnimationType.MeleeAttack;

        switch (weapon.WeaponFamily)
        {
            case WeaponFamily.Bow:
            case WeaponFamily.Crossbow:
                return SkillAnimationType.BowShot;

            case WeaponFamily.Staff:
            case WeaponFamily.Wand:
            case WeaponFamily.Spellblade:
                return SkillAnimationType.SpellCast;

            default:
                return SkillAnimationType.MeleeAttack;
        }
    }

    private void LogAttackResult(WeaponDefinition weapon, CharacterStats targetStats, CharacterHealth targetHealth, DamageResult result)
    {
        string attackerName = CompareTag("Player") ? "Player" : gameObject.name;
        string targetName = targetStats != null ? targetStats.gameObject.name : "Target";

        string weaponName = weapon != null ? weapon.DisplayName : "weapon";
        string line = result.BuildLogLine(attackerName, weaponName, targetName);

        if (targetHealth != null)
            line += $" | Target HP: {targetHealth.CurrentHP}/{targetHealth.MaxHP}";

        GameLog.Combat(line);
    }

    private bool IsTargetInRange(Transform target, float range)
    {
        float surfaceDistance = GetSurfaceDistanceToTarget(target);
        return surfaceDistance <= range;
    }

    public float GetSurfaceDistanceToTarget(Transform target)
    {
        if (target == null)
            return float.MaxValue;

        Vector3 a = transform.position;
        Vector3 b = target.position;

        a.y = 0f;
        b.y = 0f;

        float centerDistance = Vector3.Distance(a, b);
        float combinedRadii = GetBodyRadius(transform) + GetBodyRadius(target);

        return Mathf.Max(0f, centerDistance - combinedRadii);
    }

    private float GetBodyRadius(Transform t)
    {
        if (t == null)
            return 0.5f;

        if (t.TryGetComponent<NavMeshAgent>(out var navAgent))
            return Mathf.Max(0.1f, navAgent.radius);

        if (t.TryGetComponent<CapsuleCollider>(out var capsule))
        {
            float scale = Mathf.Max(t.lossyScale.x, t.lossyScale.z);
            return capsule.radius * scale;
        }

        if (t.TryGetComponent<SphereCollider>(out var sphere))
        {
            float scale = Mathf.Max(t.lossyScale.x, t.lossyScale.z);
            return sphere.radius * scale;
        }

        if (t.TryGetComponent<Collider>(out var col))
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z);

        return 0.5f;
    }

    private bool IsPlayerAttacker()
    {
        return GetComponent<PlayerTurnController>() != null ||
               GetComponent<PlayerCombatController>() != null ||
               CompareTag("Player");
    }
}