using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDefinition", menuName = "Game/Skills/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string skillId = "new_skill";
    [SerializeField] private string displayName = "New Skill";

    [Header("Behavior")]
    [SerializeField] private SkillType skillType = SkillType.Active;
    [SerializeField] private SkillTargetingMode targetingMode = SkillTargetingMode.None;
    [SerializeField] private SkillAreaMode areaMode = SkillAreaMode.SingleTarget;
    [SerializeField] private bool keepSelectedAfterUse = true;

    [Header("Animation")]
    [SerializeField] private SkillAnimationType animationType = SkillAnimationType.MeleeAttack;

    [Header("Weapon Requirement")]
    [SerializeField] private SkillWeaponRequirement requiredWeapon = SkillWeaponRequirement.AnyWeapon;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

    [Header("Combat Damage")]
    [SerializeField] private bool dealsDamage = true;
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [SerializeField] private int minDamage = 10;
    [SerializeField] private int maxDamage = 16;
    [SerializeField, Range(0f, 3f)] private float powerScaling = 0.35f;
    [SerializeField, Range(0f, 100f)] private float bonusAccuracy = 0f;
    [SerializeField] private bool canCrit = true;

    [Header("Usage")]
    [SerializeField] private int apCost = 2;
    [SerializeField] private float range = 6f;
    [SerializeField] private float areaRadius = 2.5f;

    [Header("Effects")]
    [SerializeField] private List<SkillEffectData> effects = new List<SkillEffectData>();

    public string SkillId => skillId;
    public string DisplayName => displayName;

    public SkillType SkillType => skillType;
    public SkillTargetingMode TargetingMode => targetingMode;
    public SkillAreaMode AreaMode => areaMode;
    public bool KeepSelectedAfterUse => keepSelectedAfterUse;

    public SkillAnimationType AnimationType => animationType;
    public SkillWeaponRequirement RequiredWeapon => requiredWeapon;

    public Sprite Icon => icon;
    public Texture2D CursorTexture => cursorTexture;
    public Vector2 CursorHotspot => cursorHotspot;

    public bool DealsDamage => dealsDamage;
    public DamageType DamageType => damageType;
    public int MinDamage => minDamage;
    public int MaxDamage => maxDamage;
    public float PowerScaling => powerScaling;
    public float BonusAccuracy => bonusAccuracy;
    public bool CanCrit => canCrit;

    public int ApCost => apCost;
    public float Range => range;
    public float AreaRadius => areaRadius;

    public IReadOnlyList<SkillEffectData> Effects => effects;
    public bool HasAnyPayload => dealsDamage || (effects != null && effects.Count > 0);

    private void OnValidate()
    {
        if (maxDamage < minDamage)
            maxDamage = minDamage;

        if (apCost < 0)
            apCost = 0;

        if (range < 0f)
            range = 0f;

        if (areaRadius < 0f)
            areaRadius = 0f;

        if (targetingMode == SkillTargetingMode.Self)
        {
            areaMode = SkillAreaMode.SingleTarget;
            range = 0f;
        }

        if (effects != null)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] != null)
                    effects[i].ClampValues();
            }
        }
    }
}