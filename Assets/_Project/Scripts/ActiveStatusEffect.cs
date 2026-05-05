using System;

[Serializable]
public class ActiveStatusEffect
{
    public string SourceSkillId;
    public string SourceSkillName;
    public SkillEffectType EffectType;

    public int RemainingTurns;

    public int BonusStrength;
    public int BonusConstitution;
    public int BonusDexterity;
    public int BonusIntelligence;

    public float CritChanceBonusPercent;

    public float ElementalDamageBonusPercent;
    public bool AffectAllElements;
    public DamageType ElementalDamageType;

    public int DotMinDamage;
    public int DotMaxDamage;
    public float DotPowerScaling;
    public int DotPowerSnapshot;
    public DamageType DotDamageType;

    public float MovementCostMultiplier;

    public bool IsExpired => RemainingTurns <= 0;

    public ActiveStatusEffect(SkillDefinition sourceSkill, SkillEffectData effect, CharacterStats casterStats)
    {
        SourceSkillId = sourceSkill != null ? sourceSkill.SkillId : string.Empty;
        SourceSkillName = sourceSkill != null ? sourceSkill.DisplayName : "Effect";
        EffectType = effect != null ? effect.EffectType : SkillEffectType.None;

        RemainingTurns = effect != null ? Math.Max(1, effect.DurationTurns) : 0;

        BonusStrength = effect != null ? effect.BonusStrength : 0;
        BonusConstitution = effect != null ? effect.BonusConstitution : 0;
        BonusDexterity = effect != null ? effect.BonusDexterity : 0;
        BonusIntelligence = effect != null ? effect.BonusIntelligence : 0;

        CritChanceBonusPercent = effect != null ? effect.CritChanceBonusPercent : 0f;

        ElementalDamageBonusPercent = effect != null ? effect.ElementalDamageBonusPercent : 0f;
        AffectAllElements = effect != null && effect.AffectAllElements;
        ElementalDamageType = effect != null ? effect.ElementalDamageType : DamageType.Fire;

        DotMinDamage = effect != null ? effect.DotMinDamage : 0;
        DotMaxDamage = effect != null ? effect.DotMaxDamage : 0;
        DotPowerScaling = effect != null ? effect.DotPowerScaling : 0f;
        DotDamageType = effect != null ? effect.DotDamageType : DamageType.Fire;

        if (effect != null && casterStats != null)
        {
            DotPowerSnapshot = effect.DotUsesMagicPower
                ? casterStats.MagicPower
                : casterStats.PhysicalPower;
        }
        else
        {
            DotPowerSnapshot = 0;
        }

        MovementCostMultiplier = effect != null ? effect.MovementCostMultiplier : 1f;
    }

    public bool TickEndOfOwnerTurn()
    {
        if (RemainingTurns <= 0)
            return false;

        RemainingTurns--;
        return true;
    }

    public int RollDotDamage()
    {
        int rolled = DotMinDamage;

        if (DotMaxDamage >= DotMinDamage)
            rolled = UnityEngine.Random.Range(DotMinDamage, DotMaxDamage + 1);

        int scaled = UnityEngine.Mathf.RoundToInt(DotPowerSnapshot * DotPowerScaling);
        return UnityEngine.Mathf.Max(0, rolled + scaled);
    }
}