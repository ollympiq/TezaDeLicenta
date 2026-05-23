using System;
using UnityEngine;

[Serializable]
public class CombatTelemetryData
{
    [Header("Level")]
    public int completedLevel = 1;
    public float clearTimeSeconds;
    public float targetClearTimeSeconds = 120f;

    [Header("Damage Dealt")]
    public int physicalDamageDealt;
    public int fireDamageDealt;
    public int earthDamageDealt;
    public int windDamageDealt;
    public int lightningDamageDealt;
    public int iceDamageDealt;

    [Header("Player State")]
    [Range(0f, 1f)] public float playerHpPercentAtEnd = 1f;
    public int damageTaken;
    public int potionsUsed;

    [Header("Play Style")]
    public int skillsUsed;
    public int basicAttacksUsed;
    public int movementActions;
    public float averageDistanceToEnemies;

    [Header("Effects Used")]
    public int dotEffectsApplied;
    public int slowEffectsApplied;
    public int knockEffectsApplied;

    public int TotalDamageDealt =>
        physicalDamageDealt +
        fireDamageDealt +
        earthDamageDealt +
        windDamageDealt +
        lightningDamageDealt +
        iceDamageDealt;

    public float GetDamageRatio(DamageType damageType)
    {
        int total = Mathf.Max(1, TotalDamageDealt);

        switch (damageType)
        {
            case DamageType.Physical:
                return physicalDamageDealt / (float)total;

            case DamageType.Fire:
                return fireDamageDealt / (float)total;

            case DamageType.Earth:
                return earthDamageDealt / (float)total;

            case DamageType.Wind:
                return windDamageDealt / (float)total;

            case DamageType.Lightning:
                return lightningDamageDealt / (float)total;

            case DamageType.Ice:
                return iceDamageDealt / (float)total;

            default:
                return 0f;
        }
    }

    public DamageType GetDominantDamageType()
    {
        DamageType bestType = DamageType.Physical;
        int bestValue = physicalDamageDealt;

        Check(ref bestType, ref bestValue, DamageType.Fire, fireDamageDealt);
        Check(ref bestType, ref bestValue, DamageType.Earth, earthDamageDealt);
        Check(ref bestType, ref bestValue, DamageType.Wind, windDamageDealt);
        Check(ref bestType, ref bestValue, DamageType.Lightning, lightningDamageDealt);
        Check(ref bestType, ref bestValue, DamageType.Ice, iceDamageDealt);

        return bestType;
    }

    private void Check(ref DamageType bestType, ref int bestValue, DamageType candidateType, int candidateValue)
    {
        if (candidateValue <= bestValue)
            return;

        bestType = candidateType;
        bestValue = candidateValue;
    }

    public float GetClearSpeedScore()
    {
        if (targetClearTimeSeconds <= 0.01f)
            return 0f;

        float ratio = clearTimeSeconds / targetClearTimeSeconds;

        // Sub 1 inseamna ca jucatorul a terminat mai repede decat tinta.
        return Mathf.Clamp01(1f - ratio);
    }

    public bool LooksTooEasy()
    {
        bool fastClear = clearTimeSeconds > 0f && clearTimeSeconds <= targetClearTimeSeconds * 0.75f;
        bool highHp = playerHpPercentAtEnd >= 0.65f;
        bool lowPotions = potionsUsed <= 1;

        return fastClear && highHp && lowPotions;
    }

    public bool LooksTooHard()
    {
        bool lowHp = playerHpPercentAtEnd <= 0.25f;
        bool manyPotions = potionsUsed >= 3;
        bool slowClear = clearTimeSeconds >= targetClearTimeSeconds * 1.35f;

        return lowHp || manyPotions || slowClear;
    }
}