using System;
using UnityEngine;

public class PlayerAP : MonoBehaviour
{
    [Header("AP")]
    [SerializeField] private int maxAP = 6;
    [SerializeField] private int currentAP;

    public int MaxAP => maxAP;
    public int CurrentAP => currentAP;

    public event Action<int, int> OnAPChanged;

    private void Awake()
    {
        currentAP = maxAP;
        NotifyChanged();
    }

    public bool HasEnoughAP(int amount)
    {
        return currentAP >= amount;
    }

    public bool SpendAP(int amount)
    {
        if (amount <= 0)
            return true;

        if (currentAP < amount)
            return false;

        currentAP -= amount;
        NotifyChanged();
        return true;
    }

    public void RestoreAllAP()
    {
        currentAP = maxAP;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnAPChanged?.Invoke(currentAP, maxAP);
    }
    public void SetMaxAP(int newMaxAP, bool refillCurrent = true)
    {
        maxAP = Mathf.Max(1, newMaxAP);

        if (refillCurrent)
            currentAP = maxAP;
        else
            currentAP = Mathf.Min(currentAP, maxAP);

        NotifyChanged();
    }
}