using System;
using UnityEngine;

[Serializable]
public class TraderShopStockEntry
{
    [SerializeField] private ItemInstance item;
    [SerializeField] private int buyPrice;

    public TraderShopStockEntry(ItemInstance item, int buyPrice)
    {
        this.item = item;
        this.buyPrice = Mathf.Max(0, buyPrice);
    }

    public ItemInstance Item => item;
    public int BuyPrice => Mathf.Max(0, buyPrice);
    public bool IsValid => item != null && item.IsValid;
}