using System;
using UnityEngine;

[Serializable]
public class StartingInventoryEntry
{
    [SerializeField] private ItemDefinition itemDefinition;
    [SerializeField] private int amount = 1;

    public ItemDefinition ItemDefinition => itemDefinition;
    public int Amount => Mathf.Max(1, amount);
}