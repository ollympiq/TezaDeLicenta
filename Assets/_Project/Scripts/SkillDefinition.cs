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

    [Header("Visuals")]
    [SerializeField] private Sprite icon;
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

    [Header("Combat")]
    [SerializeField] private DamageType damageType = DamageType.Physical;
    [SerializeField] private int minDamage = 10;
    [SerializeField] private int maxDamage = 16;
    [SerializeField, Range(0f, 3f)] private float powerScaling = 0.35f;
    [SerializeField, Range(0f, 100f)] private float bonusAccuracy = 0f;
    [SerializeField] private bool canCrit = true;
    [SerializeField] private int apCost = 2;
    [SerializeField] private float range = 6f;
    [SerializeField] private float areaRadius = 2.5f;

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public SkillType SkillType => skillType;
    public SkillTargetingMode TargetingMode => targetingMode;
    public SkillAreaMode AreaMode => areaMode;
    public bool KeepSelectedAfterUse => keepSelectedAfterUse;

    public Sprite Icon => icon;
    public Texture2D CursorTexture => cursorTexture;
    public Vector2 CursorHotspot => cursorHotspot;

    public DamageType DamageType => damageType;
    public int MinDamage => minDamage;
    public int MaxDamage => maxDamage;
    public float PowerScaling => powerScaling;
    public float BonusAccuracy => bonusAccuracy;
    public bool CanCrit => canCrit;
    public int ApCost => apCost;
    public float Range => range;
    public float AreaRadius => areaRadius;

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
    }
}