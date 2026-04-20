using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GeneratedLootResult
{
    [SerializeField] private int goldAmount;
    [SerializeField] private List<ItemInstance> items = new List<ItemInstance>();

    public int GoldAmount => Mathf.Max(0, goldAmount);
    public IReadOnlyList<ItemInstance> Items => items;

    public void SetGold(int amount)
    {
        goldAmount = Mathf.Max(0, amount);
    }

    public void AddItem(ItemInstance item)
    {
        if (item == null || !item.IsValid)
            return;

        items.Add(item);
    }

    public void Clear()
    {
        goldAmount = 0;
        items.Clear();
    }
}