using System;
using UnityEngine;

[Serializable]
public class ItemStatModifier
{
    [SerializeField] private ItemBonusType bonusType;
    [SerializeField] private float value;

    public ItemBonusType BonusType => bonusType;
    public float Value => value;
}