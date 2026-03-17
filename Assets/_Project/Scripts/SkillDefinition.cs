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
    [SerializeField] private bool keepSelectedAfterUse = true;

    [Header("Visuals")]
    [SerializeField] private Sprite icon;
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public SkillType SkillType => skillType;
    public SkillTargetingMode TargetingMode => targetingMode;
    public bool KeepSelectedAfterUse => keepSelectedAfterUse;
    public Sprite Icon => icon;
    public Texture2D CursorTexture => cursorTexture;
    public Vector2 CursorHotspot => cursorHotspot;
}