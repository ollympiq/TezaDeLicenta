using UnityEngine;

public struct DamagePreviewInfo
{
    public DamageType DamageType;
    public int MinBase;
    public int MaxBase;
    public int MinPreview;
    public int MaxPreview;
    public int MinCritPreview;
    public int MaxCritPreview;
    public bool CanCrit;
}

public static class DamagePreviewUtility
{
    public static bool TryBuildWeaponPreview(
        CharacterStats attacker,
        WeaponDefinition weapon,
        out DamagePreviewInfo preview)
    {
        preview = default;

        if (attacker == null || weapon == null)
            return false;

        float scalingBonus = weapon.Scaling.GetScalingBonus(attacker);
        float classBonusPercent = attacker.GetWeaponMasteryBonusPercent(weapon.WeaponFamily);

        preview = BuildPreview(
            attacker,
            weapon.DamageType,
            weapon.MinDamage,
            weapon.MaxDamage,
            scalingBonus,
            weapon.CanCrit,
            classBonusPercent
        );

        return true;
    }

    public static bool TryBuildSkillPreview(
        CharacterStats attacker,
        SkillDefinition skill,
        out DamagePreviewInfo preview)
    {
        preview = default;

        if (attacker == null || skill == null)
            return false;

        float offensivePower = skill.DamageType == DamageType.Physical
            ? attacker.PhysicalPower
            : attacker.MagicPower;

        float scalingBonus = offensivePower * skill.PowerScaling;

        preview = BuildPreview(
            attacker,
            skill.DamageType,
            skill.MinDamage,
            skill.MaxDamage,
            scalingBonus,
            skill.CanCrit,
            0f
        );

        return true;
    }

    private static DamagePreviewInfo BuildPreview(
        CharacterStats attacker,
        DamageType damageType,
        int minDamage,
        int maxDamage,
        float scalingBonus,
        bool canCrit,
        float classBonusPercent)
    {
        float minRaw = minDamage + scalingBonus;
        float maxRaw = maxDamage + scalingBonus;

        if (classBonusPercent > 0f)
        {
            float multiplier = 1f + classBonusPercent / 100f;
            minRaw *= multiplier;
            maxRaw *= multiplier;
        }

        if (damageType != DamageType.Physical)
        {
            float elementalMultiplier = 1f + attacker.GetDamageBonusPercent(damageType) / 100f;
            minRaw *= elementalMultiplier;
            maxRaw *= elementalMultiplier;
        }

        int minPreview = Mathf.RoundToInt(minRaw);
        int maxPreview = Mathf.RoundToInt(maxRaw);

        int minCrit = canCrit ? Mathf.RoundToInt(minRaw * 2f) : minPreview;
        int maxCrit = canCrit ? Mathf.RoundToInt(maxRaw * 2f) : maxPreview;

        return new DamagePreviewInfo
        {
            DamageType = damageType,
            MinBase = minDamage,
            MaxBase = maxDamage,
            MinPreview = minPreview,
            MaxPreview = maxPreview,
            MinCritPreview = minCrit,
            MaxCritPreview = maxCrit,
            CanCrit = canCrit
        };
    }
}