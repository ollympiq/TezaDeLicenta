using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class AttackDefinition
{
    [SerializeField] private string attackName = "Basic Attack";
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [SerializeField] private int minDamage = 12;
    [SerializeField] private int maxDamage = 18;
    [SerializeField, Range(0f, 3f)] private float powerScaling = 0.35f;
    [SerializeField, Range(0f, 100f)] private float bonusAccuracy = 0f;
    [SerializeField] private bool canCrit = true;
    [SerializeField] private int apCost = 2;
    [SerializeField] private float range = 2.2f;

    [Header("Effects")]
    [SerializeField] private bool applyEffectsOnlyOnHit = true;
    [SerializeField] private List<SkillEffectData> effects = new List<SkillEffectData>();

    public string AttackName => attackName;
    public DamageType DamageType => damageType;
    public int MinDamage => minDamage;
    public int MaxDamage => maxDamage;
    public float PowerScaling => powerScaling;
    public float BonusAccuracy => bonusAccuracy;
    public bool CanCrit => canCrit;
    public int ApCost => apCost;
    public float Range => range;

    public bool ApplyEffectsOnlyOnHit => applyEffectsOnlyOnHit;
    public IReadOnlyList<SkillEffectData> Effects => effects;
    public bool HasEffects => effects != null && effects.Count > 0;

    public void ClearEffects()
    {
        if (effects == null)
            effects = new List<SkillEffectData>();

        effects.Clear();
    }

    public void SetEffects(IEnumerable<SkillEffectData> newEffects)
    {
        if (effects == null)
            effects = new List<SkillEffectData>();

        effects.Clear();

        if (newEffects == null)
            return;

        foreach (SkillEffectData effect in newEffects)
        {
            if (effect == null)
                continue;

            effects.Add(effect.Clone());
        }
    }

    public void AddEffect(SkillEffectData effect)
    {
        if (effect == null)
            return;

        if (effects == null)
            effects = new List<SkillEffectData>();

        effects.Add(effect.Clone());
    }

    private void OnValidate()
    {
        if (maxDamage < minDamage)
            maxDamage = minDamage;

        if (apCost < 0)
            apCost = 0;

        if (range < 0f)
            range = 0f;

        if (effects == null)
            effects = new List<SkillEffectData>();

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] != null)
                effects[i].ClampValues();
        }
    }
}