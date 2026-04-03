using System;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class CharacterHealth : MonoBehaviour
{
    [SerializeField] private int currentHP;
    private CharacterStats stats;
    private bool initialized;

    public int CurrentHP => currentHP;
    public int MaxHP => stats != null ? stats.MaxHP : 0;
    public bool IsDead => currentHP <= 0;

    public event Action<int, int> OnHealthChanged;
    public event Action<CharacterHealth> OnDied;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.OnStatsChanged += HandleStatsChanged;
    }

    private void OnDisable()
    {
        if (stats != null)
            stats.OnStatsChanged -= HandleStatsChanged;
    }

    public void ResetToFull()
    {
        InitializeIfNeeded();
        currentHP = MaxHP;
        NotifyChanged();
    }

    public void Heal(int amount)
    {
        InitializeIfNeeded();

        if (amount <= 0 || IsDead)
            return;

        currentHP = Mathf.Clamp(currentHP + amount, 0, MaxHP);
        NotifyChanged();
    }

    public void TakeDamage(int amount)
    {
        InitializeIfNeeded();

        if (amount <= 0 || IsDead)
            return;

        bool wasAlive = !IsDead;
        currentHP = Mathf.Clamp(currentHP - amount, 0, MaxHP);
        NotifyChanged();

        if (wasAlive && currentHP <= 0)
            OnDied?.Invoke(this);
    }

    public void SetCurrentHP(int value)
    {
        InitializeIfNeeded();

        bool wasAlive = !IsDead;
        currentHP = Mathf.Clamp(value, 0, MaxHP);
        NotifyChanged();

        if (wasAlive && currentHP <= 0)
            OnDied?.Invoke(this);
    }

    private void HandleStatsChanged()
    {
        if (!initialized)
        {
            InitializeIfNeeded();
            return;
        }

        currentHP = Mathf.Clamp(currentHP, 0, MaxHP);
        NotifyChanged();
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        currentHP = MaxHP;
        initialized = true;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnHealthChanged?.Invoke(currentHP, MaxHP);
    }
}