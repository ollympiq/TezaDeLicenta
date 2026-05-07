using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttackEffectProfile", menuName = "Game/AI/Enemy Attack Effect Profile")]
public class EnemyAttackEffectProfile : ScriptableObject
{
    [SerializeField] private List<SkillEffectData> effects = new List<SkillEffectData>();

    public IReadOnlyList<SkillEffectData> Effects => effects;
}