using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttackEffectProfile", menuName = "Game/AI/Enemy Attack Effect Profile")]
public class EnemyAttackEffectProfile : ScriptableObject
{
    [SerializeField] private string profileName = "New Effect Profile";
    [SerializeField] private List<SkillEffectData> effects = new List<SkillEffectData>();

    public string ProfileName => profileName;
    public IReadOnlyList<SkillEffectData> Effects => effects;
}