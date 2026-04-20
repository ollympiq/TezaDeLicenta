using UnityEngine;

[CreateAssetMenu(fileName = "SkillBookDefinition", menuName = "Game/Items/Skill Book Definition")]
public class SkillBookDefinition : ItemDefinition
{
    [Header("Skill Book")]
    [SerializeField] private SkillDefinition taughtSkill;

    public override ItemCategory Category => ItemCategory.SkillBook;

    public SkillDefinition TaughtSkill => taughtSkill;
}