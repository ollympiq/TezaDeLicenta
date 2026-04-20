using System;
using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [SerializeField] private int currentGold = 0;

    public event Action OnGoldChanged;

    public int CurrentGold => Mathf.Max(0, currentGold);

    public void SetGold(int value)
    {
        currentGold = Mathf.Max(0, value);
        OnGoldChanged?.Invoke();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        currentGold += amount;
        OnGoldChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (currentGold < amount)
            return false;

        currentGold -= amount;
        OnGoldChanged?.Invoke();
        return true;
    }
}