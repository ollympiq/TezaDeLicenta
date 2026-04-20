using System;
using UnityEngine;

[Serializable]
public class ItemRolledModifier
{
    [SerializeField] private ItemBonusType bonusType;
    [SerializeField] private float value;

    public ItemRolledModifier(ItemBonusType bonusType, float value)
    {
        this.bonusType = bonusType;
        this.value = value;
    }

    public ItemBonusType BonusType => bonusType;
    public float Value => value;
}