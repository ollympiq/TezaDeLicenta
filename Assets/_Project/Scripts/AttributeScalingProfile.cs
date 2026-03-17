using System;
using UnityEngine;

[Serializable]
public class AttributeScalingProfile
{
    [SerializeField, Range(0f, 3f)] private float strengthScale = 1f;
    [SerializeField, Range(0f, 3f)] private float constitutionScale = 0f;
    [SerializeField, Range(0f, 3f)] private float dexterityScale = 0f;
    [SerializeField, Range(0f, 3f)] private float intelligenceScale = 0f;

    public float StrengthScale => strengthScale;
    public float ConstitutionScale => constitutionScale;
    public float DexterityScale => dexterityScale;
    public float IntelligenceScale => intelligenceScale;

    public float GetScalingBonus(CharacterStats stats)
    {
        if (stats == null)
            return 0f;

        return
            stats.Strength * strengthScale +
            stats.Constitution * constitutionScale +
            stats.Dexterity * dexterityScale +
            stats.Intelligence * intelligenceScale;
    }
}