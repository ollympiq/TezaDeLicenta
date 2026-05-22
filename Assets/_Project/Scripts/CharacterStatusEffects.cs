using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class CharacterStatusEffects : MonoBehaviour
{
    [SerializeField] private List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();

    private CharacterStats ownerStats;
    private CharacterHealth ownerHealth;
    private bool currentTurnBlocked;

    public IReadOnlyList<ActiveStatusEffect> ActiveEffects => activeEffects;

    public int TotalStrengthBonus => SumStrengthBonus();
    public int TotalConstitutionBonus => SumConstitutionBonus();
    public int TotalDexterityBonus => SumDexterityBonus();
    public int TotalIntelligenceBonus => SumIntelligenceBonus();

    public float TotalCritChanceBonusPercent => SumCritBonus();
    public float TotalElementalDamageAllBonusPercent => SumAllElementalBonus();

    public bool IsCurrentTurnBlocked => currentTurnBlocked;

    public float MovementCostMultiplier => GetHighestMovementCostMultiplier();

    private void Awake()
    {
        ownerStats = GetComponent<CharacterStats>();
        ownerHealth = GetComponent<CharacterHealth>();
    }

    public bool ApplySkillEffects(SkillDefinition skill, CharacterStats casterStats)
    {
        if (skill == null || skill.Effects == null || skill.Effects.Count == 0)
            return false;

        bool anyApplied = false;

        for (int i = 0; i < skill.Effects.Count; i++)
        {
            SkillEffectData effect = skill.Effects[i];
            if (effect == null)
                continue;

            if (ApplySingleEffect(skill, effect, casterStats))
                anyApplied = true;
        }

        if (anyApplied)
            NotifyEffectsChanged();

        return anyApplied;
    }

    public bool ApplyAttackEffects(AttackDefinition attack, CharacterStats casterStats)
    {
        if (attack == null || attack.Effects == null || attack.Effects.Count == 0)
            return false;

        bool anyApplied = false;

        for (int i = 0; i < attack.Effects.Count; i++)
        {
            SkillEffectData effect = attack.Effects[i];

            if (effect == null)
                continue;

            if (ApplySingleAttackEffect(attack, effect, casterStats))
                anyApplied = true;
        }

        if (anyApplied)
            NotifyEffectsChanged();

        return anyApplied;
    }

    private bool ApplySingleAttackEffect(AttackDefinition attack, SkillEffectData effect, CharacterStats casterStats)
    {
        switch (effect.EffectType)
        {
            case SkillEffectType.HealInstant:
                return ApplyInstantHeal(attack.AttackName, effect, casterStats);

            case SkillEffectType.BuffPrimaryAttributes:
            case SkillEffectType.BuffCritChance:
            case SkillEffectType.BuffElementalDamage:
            case SkillEffectType.DamageOverTime:
            case SkillEffectType.SlowMovement:
            case SkillEffectType.SkipTurn:
                return ApplyTimedAttackEffect(attack, effect, casterStats);

            default:
                return false;
        }
    }

    private bool ApplyTimedAttackEffect(AttackDefinition attack, SkillEffectData effect, CharacterStats casterStats)
    {
        if (effect.DurationTurns <= 0)
        {
            GameLog.Warning($"{attack.AttackName}: efectul are durata 0 si a fost ignorat.");
            return false;
        }

        ActiveStatusEffect instance = new ActiveStatusEffect(attack, effect, casterStats);
        activeEffects.Add(instance);

        ShowAppliedEffectText(effect, attack.AttackName);

        GameLog.Info($"{gameObject.name} primeste efectul {attack.AttackName} pentru {instance.RemainingTurns} ture.");
        return true;
    }

    public bool ProcessStartOfOwnerTurn()
    {
        currentTurnBlocked = false;

        if (ownerHealth == null || ownerHealth.IsDead)
            return false;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null)
                continue;

            if (effect.EffectType != SkillEffectType.DamageOverTime)
                continue;

            int dotDamage = effect.RollDotDamage();
            if (dotDamage <= 0)
                continue;

            ownerHealth.TakeDamage(dotDamage);

            if (DamageNumberManager.Instance != null)
            {
                DamageNumberManager.Instance.ShowDamage(
                    dotDamage,
                    transform,
                    effect.DotDamageType,
                    false);
            }

            GameLog.Info(
                $"{gameObject.name} primeste {dotDamage} damage over time de tip {effect.DotDamageType} " +
                $"de la efectul {effect.SourceSkillName}. HP ramas: {ownerHealth.CurrentHP}/{ownerHealth.MaxHP}");

            if (ownerHealth.IsDead)
            {
                GameLog.Info($"{gameObject.name} a murit din efectul {effect.SourceSkillName} inainte sa actioneze.");
                return false;
            }
        }

        if (HasActiveSkipTurnEffect())
        {
            currentTurnBlocked = true;
            GameLog.Info($"{gameObject.name} este incapacitat si isi pierde tura.");
        }

        return !ownerHealth.IsDead;
    }

    public void ProcessEndOfOwnerTurn()
    {
        currentTurnBlocked = false;

        if (activeEffects.Count == 0)
            return;

        bool changed = false;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null)
            {
                activeEffects.RemoveAt(i);
                changed = true;
                continue;
            }

            effect.TickEndOfOwnerTurn();

            if (effect.IsExpired)
            {
                GameLog.Info($"{gameObject.name}: efectul {effect.SourceSkillName} a expirat.");
                activeEffects.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
            NotifyEffectsChanged();
    }

    public float GetSpecificElementDamageBonusPercent(DamageType damageType)
    {
        float total = 0f;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null)
                continue;

            if (effect.EffectType != SkillEffectType.BuffElementalDamage)
                continue;

            if (effect.AffectAllElements)
                continue;

            if (effect.ElementalDamageType == damageType)
                total += effect.ElementalDamageBonusPercent;
        }

        return total;
    }

    public void ClearAllEffects()
    {
        currentTurnBlocked = false;

        if (activeEffects.Count == 0)
            return;

        activeEffects.Clear();
        NotifyEffectsChanged();
    }

    private bool ApplySingleEffect(SkillDefinition skill, SkillEffectData effect, CharacterStats casterStats)
    {
        switch (effect.EffectType)
        {
            case SkillEffectType.HealInstant:
                return ApplyInstantHeal(skill.DisplayName, effect, casterStats);

            case SkillEffectType.BuffPrimaryAttributes:
            case SkillEffectType.BuffCritChance:
            case SkillEffectType.BuffElementalDamage:
            case SkillEffectType.DamageOverTime:
            case SkillEffectType.SlowMovement:
            case SkillEffectType.SkipTurn:
                return ApplyTimedEffect(skill, effect, casterStats);

            default:
                return false;
        }
    }

    private bool ApplyInstantHeal(string sourceName, SkillEffectData effect, CharacterStats casterStats)
    {
        if (ownerHealth == null || ownerHealth.IsDead)
            return false;

        int rolled = effect.RollFlatValue();

        int sourcePower = 0;

        if (casterStats != null)
            sourcePower = effect.UseMagicPower ? casterStats.MagicPower : casterStats.PhysicalPower;

        int finalHeal = rolled + Mathf.RoundToInt(sourcePower * effect.PowerScaling);
        finalHeal = Mathf.Max(0, finalHeal);

        if (finalHeal <= 0)
            return false;

        ownerHealth.Heal(finalHeal);

        GameLog.Info($"{sourceName}: {gameObject.name} se vindeca cu {finalHeal} HP.");
        return true;
    }

    private bool ApplyTimedEffect(SkillDefinition skill, SkillEffectData effect, CharacterStats casterStats)
    {
        if (effect.DurationTurns <= 0)
        {
            GameLog.Warning($"{skill.DisplayName}: efectul are durata 0 si a fost ignorat.");
            return false;
        }

        ActiveStatusEffect instance = new ActiveStatusEffect(skill, effect, casterStats);
        activeEffects.Add(instance);

        ShowAppliedEffectText(effect, skill.DisplayName);

        GameLog.Info($"{gameObject.name} primeste efectul {skill.DisplayName} pentru {instance.RemainingTurns} ture.");
        return true;
    }

    private void ShowAppliedEffectText(SkillEffectData effect, string sourceName)
    {
        if (DamageNumberManager.Instance == null || effect == null)
            return;

        string text = BuildAppliedEffectText(effect, sourceName);

        if (string.IsNullOrWhiteSpace(text))
            return;

        if (IsPositiveEffect(effect))
            DamageNumberManager.Instance.ShowBuffText(text, transform);
        else
            DamageNumberManager.Instance.ShowDebuffText(text, transform);
    }

    private string BuildAppliedEffectText(SkillEffectData effect, string sourceName)
    {
        switch (effect.EffectType)
        {
            case SkillEffectType.BuffPrimaryAttributes:
                return BuildPrimaryBuffText(effect, sourceName);

            case SkillEffectType.BuffCritChance:
                return effect.CritChanceBonusPercent >= 0f
                    ? $"+Crit {effect.CritChanceBonusPercent:0.#}%"
                    : $"Crit {effect.CritChanceBonusPercent:0.#}%";

            case SkillEffectType.BuffElementalDamage:
                if (effect.AffectAllElements)
                    return $"+Element {effect.ElementalDamageBonusPercent:0.#}%";

                return $"+{effect.ElementalDamageType} {effect.ElementalDamageBonusPercent:0.#}%";

            case SkillEffectType.DamageOverTime:
                return $"DOT({effect.DotDamageType})";

            case SkillEffectType.SlowMovement:
                return "Slow";

            case SkillEffectType.SkipTurn:
                return "Knocked";

            default:
                return sourceName;
        }
    }

    private string BuildPrimaryBuffText(SkillEffectData effect, string sourceName)
    {
        if (effect.BonusStrength != 0)
            return $"STR {FormatSigned(effect.BonusStrength)}";

        if (effect.BonusConstitution != 0)
            return $"CON {FormatSigned(effect.BonusConstitution)}";

        if (effect.BonusDexterity != 0)
            return $"DEX {FormatSigned(effect.BonusDexterity)}";

        if (effect.BonusIntelligence != 0)
            return $"INT {FormatSigned(effect.BonusIntelligence)}";

        return string.IsNullOrWhiteSpace(sourceName) ? "Buff" : sourceName;
    }

    private string FormatSigned(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private bool IsPositiveEffect(SkillEffectData effect)
    {
        switch (effect.EffectType)
        {
            case SkillEffectType.BuffPrimaryAttributes:
            case SkillEffectType.BuffCritChance:
            case SkillEffectType.BuffElementalDamage:
                return true;

            default:
                return false;
        }
    }

    private bool HasActiveSkipTurnEffect()
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null)
                continue;

            if (effect.EffectType == SkillEffectType.SkipTurn)
                return true;
        }

        return false;
    }

    private float GetHighestMovementCostMultiplier()
    {
        float multiplier = 1f;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null)
                continue;

            if (effect.EffectType != SkillEffectType.SlowMovement)
                continue;

            if (effect.MovementCostMultiplier > multiplier)
                multiplier = effect.MovementCostMultiplier;
        }

        return Mathf.Max(1f, multiplier);
    }

    private int SumStrengthBonus()
    {
        int total = 0;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null || effect.EffectType != SkillEffectType.BuffPrimaryAttributes)
                continue;

            total += effect.BonusStrength;
        }

        return total;
    }

    private int SumConstitutionBonus()
    {
        int total = 0;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null || effect.EffectType != SkillEffectType.BuffPrimaryAttributes)
                continue;

            total += effect.BonusConstitution;
        }

        return total;
    }

    private int SumDexterityBonus()
    {
        int total = 0;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null || effect.EffectType != SkillEffectType.BuffPrimaryAttributes)
                continue;

            total += effect.BonusDexterity;
        }

        return total;
    }

    private int SumIntelligenceBonus()
    {
        int total = 0;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null || effect.EffectType != SkillEffectType.BuffPrimaryAttributes)
                continue;

            total += effect.BonusIntelligence;
        }

        return total;
    }

    private float SumCritBonus()
    {
        float total = 0f;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null || effect.EffectType != SkillEffectType.BuffCritChance)
                continue;

            total += effect.CritChanceBonusPercent;
        }

        return total;
    }

    private float SumAllElementalBonus()
    {
        float total = 0f;

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect == null || effect.EffectType != SkillEffectType.BuffElementalDamage)
                continue;

            if (effect.AffectAllElements)
                total += effect.ElementalDamageBonusPercent;
        }

        return total;
    }

    private void NotifyEffectsChanged()
    {
        if (ownerStats == null)
            ownerStats = GetComponent<CharacterStats>();

        ownerStats?.NotifyStatsChanged();
    }
}