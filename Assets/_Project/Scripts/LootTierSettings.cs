using System;
using UnityEngine;

[Serializable]
public class LootTierSettings
{
    [SerializeField] private EnemyLootTier tier = EnemyLootTier.Normal;

    [Header("Gold")]
    [SerializeField] private int minGold = 0;
    [SerializeField] private int maxGold = 10;

    [Header("Item Count")]
    [SerializeField] private int minItems = 0;
    [SerializeField] private int maxItems = 1;

    [Header("Category Weights")]
    [SerializeField] private float weaponWeight = 25f;
    [SerializeField] private float armorWeight = 35f;
    [SerializeField] private float potionWeight = 35f;
    [SerializeField] private float skillBookWeight = 5f;

    [Header("Rarity Weights")]
    [SerializeField] private float commonWeight = 60f;
    [SerializeField] private float uncommonWeight = 25f;
    [SerializeField] private float rareWeight = 10f;
    [SerializeField] private float epicWeight = 4f;
    [SerializeField] private float legendaryWeight = 1f;
    [SerializeField] private float uniqueWeight = 0f;

    public EnemyLootTier Tier => tier;

    public int MinGold => Mathf.Max(0, minGold);
    public int MaxGold => Mathf.Max(MinGold, maxGold);

    public int MinItems => Mathf.Max(0, minItems);
    public int MaxItems => Mathf.Max(MinItems, maxItems);

    public float WeaponWeight => Mathf.Max(0f, weaponWeight);
    public float ArmorWeight => Mathf.Max(0f, armorWeight);
    public float PotionWeight => Mathf.Max(0f, potionWeight);
    public float SkillBookWeight => Mathf.Max(0f, skillBookWeight);

    public float CommonWeight => Mathf.Max(0f, commonWeight);
    public float UncommonWeight => Mathf.Max(0f, uncommonWeight);
    public float RareWeight => Mathf.Max(0f, rareWeight);
    public float EpicWeight => Mathf.Max(0f, epicWeight);
    public float LegendaryWeight => Mathf.Max(0f, legendaryWeight);
    public float UniqueWeight => Mathf.Max(0f, uniqueWeight);
}