using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class SkillTooltipTextBuilder
{
    public static string Build(SkillDefinition skill, CharacterStats previewCasterStats = null)
    {
        if (skill == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(skill.DisplayName);
        builder.Append(BuildDetails(skill, previewCasterStats));
        return builder.ToString().TrimEnd();
    }

    public static string BuildDetails(SkillDefinition skill, CharacterStats previewCasterStats = null)
    {
        if (skill == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        builder.AppendLine(UIRichTextColors.Line("AP Cost", skill.ApCost.ToString(), UIRichTextColors.AP));

        if (skill.DealsDamage)
            AppendDamageLines(builder, skill, previewCasterStats);

        AppendEffectLines(builder, skill.Effects);

        builder.AppendLine(UIRichTextColors.Line("Range", BuildRangeText(skill), UIRichTextColors.White));
        builder.AppendLine(UIRichTextColors.Line("Area Radius", BuildAreaText(skill), UIRichTextColors.White));

        return builder.ToString().TrimEnd();
    }

    private static void AppendDamageLines(StringBuilder builder, SkillDefinition skill, CharacterStats previewCasterStats)
    {
        string dmgColor = UIRichTextColors.DamageTypeColor(skill.DamageType);
        string damageText = $"{skill.MinDamage}-{skill.MaxDamage}";

        if (previewCasterStats != null)
        {
            DamagePreviewUtility.TryBuildSkillPreview(previewCasterStats, skill, out DamagePreviewInfo preview);
            damageText = $"{preview.MinPreview}-{preview.MaxPreview}";
        }

        builder.AppendLine(UIRichTextColors.Line("Damage Type", skill.DamageType.ToString(), dmgColor));
        builder.AppendLine(UIRichTextColors.Line("Damage", damageText, dmgColor));

        if (skill.PowerScaling > 0f)
            builder.AppendLine(UIRichTextColors.Line("Power Scaling", FormatPercent(skill.PowerScaling), UIRichTextColors.White));

        builder.AppendLine(UIRichTextColors.Line("Can Crit", skill.CanCrit ? "Yes" : "No", UIRichTextColors.White));
    }

    private static void AppendEffectLines(StringBuilder builder, IReadOnlyList<SkillEffectData> effects)
    {
        if (effects == null || effects.Count == 0)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            SkillEffectData effect = effects[i];

            if (effect == null || effect.EffectType == SkillEffectType.None)
                continue;

            string line = BuildEffectLine(effect);

            if (!string.IsNullOrWhiteSpace(line))
                builder.AppendLine(line);
        }
    }

    private static string BuildEffectLine(SkillEffectData effect)
    {
        switch (effect.EffectType)
        {
            case SkillEffectType.HealInstant:
                return BuildHealLine(effect);

            case SkillEffectType.BuffPrimaryAttributes:
                return BuildPrimaryAttributeBuffLine(effect);

            case SkillEffectType.BuffCritChance:
                return BuildCritBuffLine(effect);

            case SkillEffectType.BuffElementalDamage:
                return BuildElementalDamageBuffLine(effect);

            case SkillEffectType.DamageOverTime:
                return BuildDotLine(effect);

            case SkillEffectType.SlowMovement:
                return BuildSlowLine(effect);

            case SkillEffectType.SkipTurn:
                return BuildSkipTurnLine(effect);

            default:
                return string.Empty;
        }
    }

    private static string BuildHealLine(SkillEffectData effect)
    {
        string powerType = effect.UseMagicPower ? "Magic Power" : "Physical Power";
        string value = $"{effect.FlatMinValue}-{effect.FlatMaxValue}";

        if (effect.PowerScaling > 0f)
            value += $" + {FormatPercent(effect.PowerScaling)} {powerType}";

        return UIRichTextColors.Line("Heal", value, UIRichTextColors.MagicPower);
    }

    private static string BuildPrimaryAttributeBuffLine(SkillEffectData effect)
    {
        List<string> parts = new List<string>();

        if (effect.BonusStrength != 0)
            parts.Add($"Strength +{effect.BonusStrength}");

        if (effect.BonusConstitution != 0)
            parts.Add($"Constitution +{effect.BonusConstitution}");

        if (effect.BonusDexterity != 0)
            parts.Add($"Dexterity +{effect.BonusDexterity}");

        if (effect.BonusIntelligence != 0)
            parts.Add($"Intelligence +{effect.BonusIntelligence}");

        if (parts.Count == 0)
            return string.Empty;

        string value = $"{string.Join(", ", parts)} ({FormatTurns(effect.DurationTurns)})";
        return UIRichTextColors.Line("Buff", value, UIRichTextColors.White);
    }

    private static string BuildCritBuffLine(SkillEffectData effect)
    {
        if (Mathf.Approximately(effect.CritChanceBonusPercent, 0f))
            return string.Empty;

        string value = $"+{FormatNumber(effect.CritChanceBonusPercent)}% ({FormatTurns(effect.DurationTurns)})";
        return UIRichTextColors.Line("Crit Chance", value, UIRichTextColors.White);
    }

    private static string BuildElementalDamageBuffLine(SkillEffectData effect)
    {
        if (Mathf.Approximately(effect.ElementalDamageBonusPercent, 0f))
            return string.Empty;

        string target = effect.AffectAllElements ? "All Elements" : effect.ElementalDamageType.ToString();
        string color = effect.AffectAllElements ? UIRichTextColors.White : UIRichTextColors.DamageTypeColor(effect.ElementalDamageType);
        string value = $"+{FormatNumber(effect.ElementalDamageBonusPercent)}% ({FormatTurns(effect.DurationTurns)})";

        return UIRichTextColors.Line($"{target} Damage", value, color);
    }

    private static string BuildDotLine(SkillEffectData effect)
    {
        string powerType = effect.DotUsesMagicPower ? "Magic Power" : "Physical Power";
        string color = UIRichTextColors.DamageTypeColor(effect.DotDamageType);
        string value = $"{effect.DotMinDamage}-{effect.DotMaxDamage}";

        if (effect.DotPowerScaling > 0f)
            value += $" + {FormatPercent(effect.DotPowerScaling)} {powerType}";

        value += $" ({FormatTurns(effect.DurationTurns)})";

        return UIRichTextColors.Line($"DOT ({effect.DotDamageType})", value, color);
    }

    private static string BuildSlowLine(SkillEffectData effect)
    {
        string value = $"x{FormatNumber(effect.MovementCostMultiplier)} movement cost ({FormatTurns(effect.DurationTurns)})";
        return UIRichTextColors.Line("Slow Movement", value, UIRichTextColors.White);
    }

    private static string BuildSkipTurnLine(SkillEffectData effect)
    {
        return UIRichTextColors.Line("Knock", $"skips {FormatTurns(effect.DurationTurns)}", UIRichTextColors.White);
    }

    private static string BuildRangeText(SkillDefinition skill)
    {
        if (skill.TargetingMode == SkillTargetingMode.Self || skill.Range <= 0f)
            return "Self";

        return FormatNumber(skill.Range);
    }

    private static string BuildAreaText(SkillDefinition skill)
    {
        if (skill.AreaMode != SkillAreaMode.Circle || skill.AreaRadius <= 0f)
            return "-";

        return FormatNumber(skill.AreaRadius);
    }

    private static string FormatTurns(int turns)
    {
        int safeTurns = Mathf.Max(1, turns);
        return safeTurns == 1 ? "1 turn" : $"{safeTurns} turns";
    }

    private static string FormatPercent(float value)
    {
        return $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private static string FormatNumber(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
            return Mathf.RoundToInt(value).ToString();

        return value.ToString("0.##");
    }
}
