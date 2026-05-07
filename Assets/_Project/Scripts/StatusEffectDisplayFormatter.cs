using System.Collections.Generic;
using UnityEngine;

public static class StatusEffectDisplayFormatter
{
    public static string BuildInlineList(CharacterStatusEffects statusEffects)
    {
        List<string> labels = BuildLabels(statusEffects);
        return labels.Count > 0 ? string.Join(", ", labels) : "-";
    }

    public static List<string> BuildLabels(CharacterStatusEffects statusEffects)
    {
        List<string> orderedKeys = new List<string>();
        Dictionary<string, int> maxTurnsByLabel = new Dictionary<string, int>();

        IReadOnlyList<ActiveStatusEffect> effects = statusEffects != null ? statusEffects.ActiveEffects : null;
        if (effects == null || effects.Count == 0)
            return new List<string>();

        for (int i = 0; i < effects.Count; i++)
        {
            ActiveStatusEffect effect = effects[i];
            if (effect == null || effect.IsExpired)
                continue;

            int turns = Mathf.Max(1, effect.RemainingTurns);
            AddLabelsForEffect(effect, turns, orderedKeys, maxTurnsByLabel);
        }

        List<string> labels = new List<string>();

        for (int i = 0; i < orderedKeys.Count; i++)
        {
            string key = orderedKeys[i];
            labels.Add($"{key}[{maxTurnsByLabel[key]}]");
        }

        return labels;
    }

    private static void AddLabelsForEffect(
        ActiveStatusEffect effect,
        int turns,
        List<string> orderedKeys,
        Dictionary<string, int> maxTurnsByLabel)
    {
        switch (effect.EffectType)
        {
            case SkillEffectType.DamageOverTime:
                AddLabel($"DOT({GetDamageTypeShortName(effect.DotDamageType)})", turns, orderedKeys, maxTurnsByLabel);
                break;

            case SkillEffectType.SlowMovement:
                AddLabel($"Slow x{FormatNumber(effect.MovementCostMultiplier)}", turns, orderedKeys, maxTurnsByLabel);
                break;

            case SkillEffectType.SkipTurn:
                AddLabel("Knocked", turns, orderedKeys, maxTurnsByLabel);
                break;

            case SkillEffectType.BuffPrimaryAttributes:
                AddPrimaryAttributeLabels(effect, turns, orderedKeys, maxTurnsByLabel);
                break;

            case SkillEffectType.BuffCritChance:
                if (!Mathf.Approximately(effect.CritChanceBonusPercent, 0f))
                    AddLabel($"Crit Chance {FormatSignedPercent(effect.CritChanceBonusPercent)}", turns, orderedKeys, maxTurnsByLabel);
                break;

            case SkillEffectType.BuffElementalDamage:
                if (!Mathf.Approximately(effect.ElementalDamageBonusPercent, 0f))
                {
                    string damageLabel = effect.AffectAllElements
                        ? "All Dmg"
                        : $"{GetDamageTypeShortName(effect.ElementalDamageType)} Dmg";

                    AddLabel($"{damageLabel} {FormatSignedPercent(effect.ElementalDamageBonusPercent)}", turns, orderedKeys, maxTurnsByLabel);
                }
                break;
        }
    }

    private static void AddPrimaryAttributeLabels(
        ActiveStatusEffect effect,
        int turns,
        List<string> orderedKeys,
        Dictionary<string, int> maxTurnsByLabel)
    {
        if (effect.BonusStrength != 0)
            AddLabel($"STR {FormatSignedInt(effect.BonusStrength)}", turns, orderedKeys, maxTurnsByLabel);

        if (effect.BonusConstitution != 0)
            AddLabel($"CON {FormatSignedInt(effect.BonusConstitution)}", turns, orderedKeys, maxTurnsByLabel);

        if (effect.BonusDexterity != 0)
            AddLabel($"DEX {FormatSignedInt(effect.BonusDexterity)}", turns, orderedKeys, maxTurnsByLabel);

        if (effect.BonusIntelligence != 0)
            AddLabel($"INT {FormatSignedInt(effect.BonusIntelligence)}", turns, orderedKeys, maxTurnsByLabel);
    }

    private static void AddLabel(
        string label,
        int turns,
        List<string> orderedKeys,
        Dictionary<string, int> maxTurnsByLabel)
    {
        if (string.IsNullOrWhiteSpace(label))
            return;

        if (!maxTurnsByLabel.ContainsKey(label))
        {
            maxTurnsByLabel[label] = turns;
            orderedKeys.Add(label);
        }
        else
        {
            maxTurnsByLabel[label] = Mathf.Max(maxTurnsByLabel[label], turns);
        }
    }

    private static string FormatSignedPercent(float value)
    {
        string sign = value > 0f ? "+" : string.Empty;
        return $"{sign}{FormatNumber(value)}%";
    }

    private static string FormatSignedInt(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    private static string FormatNumber(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
            return Mathf.RoundToInt(value).ToString();

        return value.ToString("0.##");
    }

    private static string GetDamageTypeShortName(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Fire:
                return "Fire";

            case DamageType.Ice:
                return "Ice";

            case DamageType.Earth:
                return "Earth";

            case DamageType.Wind:
                return "Wind";

            case DamageType.Lightning:
                return "Lightning";

            case DamageType.Physical:
                return "Physical";

            default:
                return damageType.ToString();
        }
    }
}