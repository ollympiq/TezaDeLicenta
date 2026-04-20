using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInventory : MonoBehaviour
{
    [SerializeField] private int capacity = 24;
    [SerializeField] private List<StartingInventoryEntry> startingItems = new List<StartingInventoryEntry>();
    [SerializeField] private List<ItemInstance> items = new List<ItemInstance>();

    public event Action OnInventoryChanged;

    public IReadOnlyList<ItemInstance> Items => items;
    public int Capacity => capacity;
    public int ItemCount => items.Count;

    private void Awake()
    {
        AddStartingItems();
    }

    public ItemInstance GetItemAt(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;

        return items[index];
    }

    public bool AddItem(ItemDefinition definition, int amount = 1)
    {
        if (definition == null || amount <= 0)
            return false;

        bool changed = false;

        while (amount > 0)
        {
            if (definition.Stackable)
            {
                ItemInstance existingStack = FindStack(definition);
                if (existingStack != null && existingStack.StackCount < existingStack.MaxStack)
                {
                    int freeSpace = existingStack.MaxStack - existingStack.StackCount;
                    int toAdd = Mathf.Min(freeSpace, amount);

                    existingStack.AddToStack(toAdd);
                    amount -= toAdd;
                    changed = true;
                    continue;
                }
            }

            if (items.Count >= capacity)
                break;

            int stackAmount = definition.Stackable ? Mathf.Min(definition.MaxStack, amount) : 1;
            items.Add(new ItemInstance(definition, stackAmount));
            amount -= stackAmount;
            changed = true;
        }

        if (changed)
            OnInventoryChanged?.Invoke();

        return amount == 0;
    }

    public bool AddItemInstance(ItemInstance instance)
    {
        if (instance == null || instance.Definition == null)
            return false;

        if (instance.CanStack)
        {
            ItemInstance existingStack = FindCompatibleStack(instance);
            if (existingStack != null && existingStack.StackCount < existingStack.MaxStack)
            {
                int freeSpace = existingStack.MaxStack - existingStack.StackCount;
                int toAdd = Mathf.Min(freeSpace, instance.StackCount);
                existingStack.AddToStack(toAdd);
                instance.RemoveFromStack(toAdd);

                if (instance.StackCount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        if (items.Count >= capacity)
            return false;

        items.Add(instance);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool CanAddItemInstance(ItemInstance instance)
    {
        if (instance == null || !instance.IsValid)
            return false;

        int remaining = instance.StackCount;
        int freeSlots = capacity - items.Count;

        if (instance.CanStack)
        {
            for (int i = 0; i < items.Count; i++)
            {
                ItemInstance existing = items[i];
                if (existing == null || !existing.IsValid)
                    continue;

                if (!existing.CanStackWith(instance))
                    continue;

                int freeSpace = existing.MaxStack - existing.StackCount;
                if (freeSpace <= 0)
                    continue;

                int toFit = Mathf.Min(freeSpace, remaining);
                remaining -= toFit;

                if (remaining <= 0)
                    return true;
            }
        }

        while (remaining > 0)
        {
            if (freeSlots <= 0)
                return false;

            int stackSize = instance.CanStack ? instance.MaxStack : 1;
            remaining -= Mathf.Min(stackSize, remaining);
            freeSlots--;
        }

        return true;
    }

    public bool CanAddDefinitionAmount(ItemDefinition definition, int amount)
    {
        if (definition == null || amount <= 0)
            return false;

        ItemInstance probe = new ItemInstance(definition, amount);
        return CanAddItemInstance(probe);
    }

    public ItemInstance TakeAt(int index)
    {
        if (index < 0 || index >= items.Count)
            return null;

        ItemInstance item = items[index];
        items.RemoveAt(index);
        OnInventoryChanged?.Invoke();
        return item;
    }

    public bool RemoveAt(int index, int amount = 1)
    {
        if (index < 0 || index >= items.Count || amount <= 0)
            return false;

        ItemInstance item = items[index];

        if (item.CanStack && item.StackCount > amount)
        {
            item.RemoveFromStack(amount);
        }
        else
        {
            items.RemoveAt(index);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool UseAt(int index, GameObject user)
    {
        if (index < 0 || index >= items.Count || user == null)
            return false;

        ItemInstance item = items[index];
        if (item == null || !item.IsValid)
            return false;

        if (TryUsePotion(item, user))
        {
            RemoveAt(index, 1);
            return true;
        }

        if (TryUseSkillBook(item, user))
        {
            RemoveAt(index, 1);
            return true;
        }

        return false;
    }
    private bool TryUsePotion(ItemInstance item, GameObject user)
    {
        PotionDefinition potion = item.PotionDefinition;
        if (potion == null)
            return false;

        CharacterHealth health = user.GetComponent<CharacterHealth>();
        PlayerAP playerAP = user.GetComponent<PlayerAP>();

        bool used = false;

        if (health != null && potion.HealAmount > 0)
        {
            health.Heal(potion.HealAmount);
            used = true;
        }

        if (playerAP != null && potion.RestoreAP > 0)
        {
            playerAP.RestoreAP(potion.RestoreAP);
            used = true;
        }

        return used;
    }

    private bool TryUseSkillBook(ItemInstance item, GameObject user)
    {
        SkillBookDefinition skillBook = item.SkillBookDefinition;
        if (skillBook == null || skillBook.TaughtSkill == null)
            return false;

        PlayerSkillLoadout loadout = user.GetComponent<PlayerSkillLoadout>();
        if (loadout == null)
            return false;

        if (loadout.HasSkill(skillBook.TaughtSkill))
        {
            Debug.Log("Player already knows this skill.");
            return false;
        }

        return loadout.LearnSkill(skillBook.TaughtSkill, true);
    }
    private void AddStartingItems()
    {
        if (startingItems == null || startingItems.Count == 0)
            return;

        for (int i = 0; i < startingItems.Count; i++)
        {
            StartingInventoryEntry entry = startingItems[i];
            if (entry == null || entry.ItemDefinition == null)
                continue;

            AddItem(entry.ItemDefinition, entry.Amount);
        }
    }

    private ItemInstance FindStack(ItemDefinition definition)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Definition == definition && items[i].CanStack && items[i].StackCount < items[i].MaxStack)
                return items[i];
        }

        return null;
    }

    private ItemInstance FindCompatibleStack(ItemInstance instance)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].CanStackWith(instance) && items[i].StackCount < items[i].MaxStack)
                return items[i];
        }

        return null;
    }
}