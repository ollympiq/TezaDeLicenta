using System;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
[DisallowMultipleComponent]
public class PlayerProgression : MonoBehaviour
{
    [Header("Progression")]
    [SerializeField, Min(1)] private int currentLevel = 1;
    [SerializeField, Min(0)] private int unspentStatPoints = 0;
    [SerializeField, Min(1)] private int statPointsPerLevel = 5;

    [Header("References")]
    [SerializeField] private CharacterStats stats;
    [SerializeField] private CharacterHealth health;

    public event Action OnProgressionChanged;

    public int CurrentLevel => Mathf.Max(1, currentLevel);
    public int UnspentStatPoints => Mathf.Max(0, unspentStatPoints);
    public int StatPointsPerLevel => Mathf.Max(1, statPointsPerLevel);

    private void Awake()
    {
        if (stats == null)
            stats = GetComponent<CharacterStats>();

        if (health == null)
            health = GetComponent<CharacterHealth>();

        currentLevel = Mathf.Max(1, currentLevel);
        unspentStatPoints = Mathf.Max(0, unspentStatPoints);
    }

    [ContextMenu("Level Up Once")]
    public void LevelUpOnce()
    {
        currentLevel++;
        unspentStatPoints += StatPointsPerLevel;

        if (stats != null)
            stats.AddBaseLevel(1);

        if (health != null)
            health.ResetToFull();

        NotifyChanged();
    }

    public bool SpendPoint(PlayerStatType statType, int amount = 1)
    {
        amount = Mathf.Max(1, amount);

        if (unspentStatPoints < amount || stats == null)
            return false;

        int str = 0;
        int con = 0;
        int dex = 0;
        int intel = 0;

        switch (statType)
        {
            case PlayerStatType.Strength:
                str = amount;
                break;

            case PlayerStatType.Constitution:
                con = amount;
                break;

            case PlayerStatType.Dexterity:
                dex = amount;
                break;

            case PlayerStatType.Intelligence:
                intel = amount;
                break;

            default:
                return false;
        }

        stats.AddBasePrimaryAttributes(str, con, dex, intel);
        unspentStatPoints -= amount;

        if (health != null)
            health.ResetToFull();

        NotifyChanged();
        return true;
    }

    public void GiveStatPoints(int amount)
    {
        if (amount <= 0)
            return;

        unspentStatPoints += amount;
        NotifyChanged();
    }

    public void SetProgressionState(int level, int points)
    {
        currentLevel = Mathf.Max(1, level);
        unspentStatPoints = Mathf.Max(0, points);

        NotifyChanged();
    }

    private void NotifyChanged()
    {
        OnProgressionChanged?.Invoke();
    }
}