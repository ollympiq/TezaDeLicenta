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

        agent = GetComponent<NavMeshAgent>();
        selfHealth = GetComponent<CharacterHealth>();
    }

    public int GetAttackAPCost()
    {
        WeaponDefinition weapon = equipment != null ? equipment.EquippedWeaponDefinition : null;
        return weapon != null ? weapon.ApCost : 999;
    }

    public float GetAttackRange()
    {
        WeaponDefinition weapon = equipment != null ? equipment.EquippedWeaponDefinition : null;
        return weapon != null ? weapon.Range : 0f;
    }

    public bool IsTargetInAttackRange(Transform target)
    {
        if (target == null)
            return false;

        WeaponDefinition weapon = equipment != null ? equipment.EquippedWeaponDefinition : null;
        if (weapon == null)
            return false;

        return IsTargetInRange(target, weapon.Range);
    }

    public bool TryAttackTarget(CharacterStats targetStats)
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

        if (turnActionLimiter != null && !turnActionLimiter.CanUseBasicAttack())
        {
            GameLog.Warning("Basic Attack a fost deja folosit in acest tur.");
            return false;
        }

        CharacterHealth targetHealth = targetStats.GetComponent<CharacterHealth>();
        if (targetHealth == null || targetHealth.IsDead)
            return false;

        if (!IsTargetInRange(targetStats.transform, weapon.Range))
        {
            GameLog.Warning("Tinta este prea departe pentru Basic Attack.");
            return false;
        }

        if (playerAP != null)
        {
            if (!playerAP.HasEnoughAP(weapon.ApCost))
            {
                GameLog.Warning("Nu ai destul AP pentru Basic Attack.");
                return false;
            }

            if (!playerAP.SpendAP(weapon.ApCost))
                return false;
        }

        turnActionLimiter?.MarkBasicAttackUsed();

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (playerAnimationController != null)
            playerAnimationController.PlayAttackAnimation(targetStats.transform);
        else if (enemyAnimationController != null)
            enemyAnimationController.PlayAttackAnimation(targetStats.transform);

        DamageResult result = DamageCalculator.ResolveWeaponAttack(attackerStats, targetStats, weapon);

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

        LogAttackResult(weapon, targetStats, targetHealth, result);
        return true;
    }

    private void LogAttackResult(WeaponDefinition weapon, CharacterStats targetStats, CharacterHealth targetHealth, DamageResult result)
    {
        string attackerName = CompareTag("Player") ? "Player" : gameObject.name;
        string targetName = targetStats != null ? targetStats.gameObject.name : "Target";

        string line = result.BuildLogLine(attackerName, weapon.DisplayName, targetName);

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
}