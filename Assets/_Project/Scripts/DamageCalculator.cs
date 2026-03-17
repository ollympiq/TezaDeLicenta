using UnityEngine;

public static class DamageCalculator
{
    public static DamageResult ResolveWeaponAttack(
        CharacterStats attacker,
        CharacterStats defender,
        WeaponDefinition weapon)
    {
        if (weapon == null)
            return default;

        float scalingBonus = weapon.Scaling.GetScalingBonus(attacker);
        float classBonusPercent = attacker.GetWeaponMasteryBonusPercent(weapon.WeaponFamily);

        return ResolveInternal(
            attacker,
            defender,
            weapon.DamageType,
            weapon.MinDamage,
            weapon.MaxDamage,
            scalingBonus,
            weapon.BonusAccuracy,
            weapon.CanCrit,
            classBonusPercent
        );
    }

    public static DamageResult ResolveAttack(
        CharacterStats attacker,
        CharacterStats defender,
        AttackDefinition attack)
    {
        float offensivePower = attack.DamageType == DamageType.Physical
            ? attacker.PhysicalPower
            : attacker.MagicPower;

        float scalingBonus = offensivePower * attack.PowerScaling;

        return ResolveInternal(
            attacker,
            defender,
            attack.DamageType,
            attack.MinDamage,
            attack.MaxDamage,
            scalingBonus,
            attack.BonusAccuracy,
            attack.CanCrit,
            0f
        );
    }

    public static DamageResult ResolveSkill(
        CharacterStats attacker,
        CharacterStats defender,
        SkillDefinition skill)
    {
        float offensivePower = skill.DamageType == DamageType.Physical
            ? attacker.PhysicalPower
            : attacker.MagicPower;

        float scalingBonus = offensivePower * skill.PowerScaling;

        return ResolveInternal(
            attacker,
            defender,
            skill.DamageType,
            skill.MinDamage,
            skill.MaxDamage,
            scalingBonus,
            skill.BonusAccuracy,
            skill.CanCrit,
            0f
        );
    }

    private static DamageResult ResolveInternal(
        CharacterStats attacker,
        CharacterStats defender,
        DamageType damageType,
        int minDamage,
        int maxDamage,
        float scalingBonus,
        float bonusAccuracy,
        bool canCrit,
        float classBonusPercent)
    {
        DamageResult result = new DamageResult();
        result.DamageType = damageType;
        result.ScalingBonus = scalingBonus;
        result.ClassBonusPercent = classBonusPercent;

        float hitChance = Mathf.Clamp(attacker.Accuracy + bonusAccuracy - defender.Evasion, 5f, 95f);
        result.HitChance = hitChance;

        bool hit = Random.Range(0f, 100f) <= hitChance;
        result.Hit = hit;

        if (!hit)
        {
            result.FinalDamage = 0;
            return result;
        }

        int rolledDamage = Random.Range(minDamage, maxDamage + 1);

        float rawDamage = rolledDamage + scalingBonus;

        if (classBonusPercent > 0f)
            rawDamage *= 1f + classBonusPercent / 100f;

        float elementalBonusPercent = 0f;

        if (damageType != DamageType.Physical)
        {
            elementalBonusPercent = attacker.ElementalDamageBonusPercent;
            rawDamage *= 1f + elementalBonusPercent / 100f;
        }

        result.ElementalBonusPercent = elementalBonusPercent;
        result.BaseDamage = Mathf.RoundToInt(rawDamage);

        bool wasCrit = canCrit && Random.Range(0f, 100f) <= attacker.CritChance;
        result.WasCritical = wasCrit;

        if (wasCrit)
            rawDamage *= 2f;

        float armorReduction = 0f;

        if (damageType == DamageType.Physical)
        {
            armorReduction = defender.ArmorPhysicalReductionPercent;
            rawDamage *= 1f - armorReduction / 100f;
        }

        result.ArmorReductionPercent = armorReduction;

        float resistance = GetResistance(defender, damageType);
        result.ResistancePercent = resistance;

        rawDamage *= 1f - resistance / 100f;

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(rawDamage));
        result.FinalDamage = finalDamage;

        return result;
    }

    private static float GetResistance(CharacterStats stats, DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                return stats.PhysicalResistance;
            case DamageType.Fire:
                return stats.FireResistance;
            case DamageType.Earth:
                return stats.EarthResistance;
            case DamageType.Wind:
                return stats.WindResistance;
            case DamageType.Lightning:
                return stats.LightningResistance;
            case DamageType.Ice:
                return stats.IceResistance;
            default:
                return 0f;
        }
    }
}