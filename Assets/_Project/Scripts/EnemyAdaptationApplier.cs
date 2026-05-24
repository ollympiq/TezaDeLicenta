using System.Text;
using UnityEngine;

public static class EnemyAdaptationApplier
{
    public static EnemyAdaptationApplyReport Apply(
        GameObject enemy,
        EnemyAdaptationRuntimeConfig config,
        EnemyAdaptationEffectLibrary effectLibrary = null)
    {
        EnemyAdaptationApplyReport report = new EnemyAdaptationApplyReport();

        if (enemy == null || config == null || !config.enabled)
            return report;

        config.Clamp();

        ApplyRuntimeStats(enemy, config);
        ApplyAttackDamageTypes(enemy, config);

        ApplyAttackEffects(enemy, config, effectLibrary, report);

        CharacterHealth health = enemy.GetComponent<CharacterHealth>();
        if (health != null)
            health.ResetToFull();

        report.Applied = true;

        return report;
    }

    public static EnemyAdaptationApplyReport Apply(GameObject enemy, EnemyAdaptationConfig config)
    {
        if (enemy == null || config == null || !config.Enabled)
            return new EnemyAdaptationApplyReport();

        EnemyAdaptationRuntimeConfig runtimeConfig = ConvertPrototypeConfig(config);
        return Apply(enemy, runtimeConfig, null);
    }

    private static EnemyAdaptationRuntimeConfig ConvertPrototypeConfig(EnemyAdaptationConfig config)
    {
        EnemyAdaptationRuntimeConfig runtime = new EnemyAdaptationRuntimeConfig();

        runtime.enabled = config.Enabled;

        runtime.overrideMediumAttackDamageType = config.OverrideMediumAttackDamageType;
        runtime.mediumAttackDamageType = config.MediumAttackDamageType;

        runtime.overrideHeavyAttackDamageType = config.OverrideHeavyAttackDamageType;
        runtime.heavyAttackDamageType = config.HeavyAttackDamageType;

        runtime.strengthBonus = config.StrengthBonus;
        runtime.constitutionBonus = config.ConstitutionBonus;
        runtime.dexterityBonus = config.DexterityBonus;
        runtime.intelligenceBonus = config.IntelligenceBonus;

        runtime.maxHpBonus = config.MaxHpBonus;
        runtime.armorBonus = config.ArmorBonus;

        runtime.physicalResistanceBonus = config.PhysicalResistanceBonus;
        runtime.fireResistanceBonus = config.FireResistanceBonus;
        runtime.earthResistanceBonus = config.EarthResistanceBonus;
        runtime.windResistanceBonus = config.WindResistanceBonus;
        runtime.lightningResistanceBonus = config.LightningResistanceBonus;
        runtime.iceResistanceBonus = config.IceResistanceBonus;

        runtime.Clamp();
        return runtime;
    }

    private static void ApplyRuntimeStats(GameObject enemy, EnemyAdaptationRuntimeConfig config)
    {
        CharacterStats stats = enemy.GetComponent<CharacterStats>();
        if (stats == null)
            return;

        stats.AddRuntimePrimaryAttributeBonuses(
            config.strengthBonus,
            config.constitutionBonus,
            config.dexterityBonus,
            config.intelligenceBonus,
            false
        );

        stats.AddRuntimeBaseValueBonuses(
            config.maxHpBonus,
            config.armorBonus,
            false
        );

        stats.AddRuntimeResistanceBonuses(
            config.physicalResistanceBonus,
            config.fireResistanceBonus,
            config.earthResistanceBonus,
            config.windResistanceBonus,
            config.lightningResistanceBonus,
            config.iceResistanceBonus,
            false
        );

        stats.NotifyStatsChanged();
    }

    private static void ApplyAttackDamageTypes(GameObject enemy, EnemyAdaptationRuntimeConfig config)
    {
        EnemyTurnController enemyTurn = enemy.GetComponent<EnemyTurnController>();
        if (enemyTurn == null)
            return;

        enemyTurn.ApplyAdaptiveAttackDamageTypes(
            config.overrideMediumAttackDamageType,
            config.mediumAttackDamageType,
            config.overrideHeavyAttackDamageType,
            config.heavyAttackDamageType
        );
    }

    private static void ApplyAttackEffects(
        GameObject enemy,
        EnemyAdaptationRuntimeConfig config,
        EnemyAdaptationEffectLibrary effectLibrary,
        EnemyAdaptationApplyReport report)
    {
        if (report == null)
            return;

        report.MediumEffectText = "None";
        report.HeavyEffectText = "None";

        if (effectLibrary == null)
            return;

        EnemyTurnController enemyTurn = enemy.GetComponent<EnemyTurnController>();
        if (enemyTurn == null)
            return;

        EnemyAttackEffectProfile mediumProfile = RollMediumProfile(config, effectLibrary);
        EnemyAttackEffectProfile heavyProfile = RollHeavyProfile(config, effectLibrary);

        if (mediumProfile != null)
        {
            enemyTurn.SetMediumAttackEffects(mediumProfile);
            report.MediumEffectText = BuildEffectProfileText(mediumProfile);
        }
        else
        {
            enemyTurn.ClearMediumAttackEffects();
            report.MediumEffectText = "None";
        }

        if (heavyProfile != null)
        {
            enemyTurn.SetHeavyAttackEffects(heavyProfile);
            report.HeavyEffectText = BuildEffectProfileText(heavyProfile);
        }
        else
        {
            enemyTurn.ClearHeavyAttackEffects();
            report.HeavyEffectText = "None";
        }
    }

    private static EnemyAttackEffectProfile RollMediumProfile(
        EnemyAdaptationRuntimeConfig config,
        EnemyAdaptationEffectLibrary effectLibrary)
    {
        if (Random.value <= config.mediumKnockChance && effectLibrary.MediumKnockProfile != null)
            return effectLibrary.MediumKnockProfile;

        if (Random.value <= config.mediumDotChance && effectLibrary.MediumDotProfile != null)
            return effectLibrary.MediumDotProfile;

        if (Random.value <= config.mediumSlowChance && effectLibrary.MediumSlowProfile != null)
            return effectLibrary.MediumSlowProfile;

        return null;
    }

    private static EnemyAttackEffectProfile RollHeavyProfile(
        EnemyAdaptationRuntimeConfig config,
        EnemyAdaptationEffectLibrary effectLibrary)
    {
        if (Random.value <= config.heavyKnockChance && effectLibrary.HeavyKnockProfile != null)
            return effectLibrary.HeavyKnockProfile;

        if (Random.value <= config.heavyDotChance && effectLibrary.HeavyDotProfile != null)
            return effectLibrary.HeavyDotProfile;

        if (Random.value <= config.heavySlowChance && effectLibrary.HeavySlowProfile != null)
            return effectLibrary.HeavySlowProfile;

        return null;
    }

    private static string BuildEffectProfileText(EnemyAttackEffectProfile profile)
    {
        if (profile == null || profile.Effects == null || profile.Effects.Count == 0)
            return "None";

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < profile.Effects.Count; i++)
        {
            SkillEffectData effect = profile.Effects[i];

            if (effect == null)
                continue;

            if (sb.Length > 0)
                sb.Append(" + ");

            sb.Append(BuildSingleEffectText(effect));
        }

        if (sb.Length <= 0)
            return "None";

        return sb.ToString();
    }

    private static string BuildSingleEffectText(SkillEffectData effect)
    {
        if (effect == null)
            return "None";

        switch (effect.EffectType)
        {
            case SkillEffectType.DamageOverTime:
                return $"DOT({effect.DotDamageType})";

            case SkillEffectType.SlowMovement:
                return "Slow";

            case SkillEffectType.SkipTurn:
                return "Knock";

            case SkillEffectType.HealInstant:
                return "Heal";

            case SkillEffectType.BuffPrimaryAttributes:
                return "Attribute Buff";

            case SkillEffectType.BuffCritChance:
                return "Crit Buff";

            case SkillEffectType.BuffElementalDamage:
                return "Elemental Buff";

            default:
                return effect.EffectType.ToString();
        }
    }
}