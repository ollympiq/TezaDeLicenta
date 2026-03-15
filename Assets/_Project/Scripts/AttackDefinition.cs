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

    public string AttackName => attackName;
    public DamageType DamageType => damageType;
    public int MinDamage => minDamage;
    public int MaxDamage => maxDamage;
    public float PowerScaling => powerScaling;
    public float BonusAccuracy => bonusAccuracy;
    public bool CanCrit => canCrit;
    public int ApCost => apCost;
    public float Range => range;
}