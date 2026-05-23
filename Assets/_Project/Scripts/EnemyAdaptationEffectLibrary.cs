using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAdaptationEffectLibrary", menuName = "Game/AI/Enemy Adaptation Effect Library")]
public class EnemyAdaptationEffectLibrary : ScriptableObject
{
    [Header("Medium Attack Profiles")]
    [SerializeField] private EnemyAttackEffectProfile mediumSlowProfile;
    [SerializeField] private EnemyAttackEffectProfile mediumDotProfile;
    [SerializeField] private EnemyAttackEffectProfile mediumKnockProfile;

    [Header("Heavy Attack Profiles")]
    [SerializeField] private EnemyAttackEffectProfile heavySlowProfile;
    [SerializeField] private EnemyAttackEffectProfile heavyDotProfile;
    [SerializeField] private EnemyAttackEffectProfile heavyKnockProfile;

    public EnemyAttackEffectProfile MediumSlowProfile => mediumSlowProfile;
    public EnemyAttackEffectProfile MediumDotProfile => mediumDotProfile;
    public EnemyAttackEffectProfile MediumKnockProfile => mediumKnockProfile;

    public EnemyAttackEffectProfile HeavySlowProfile => heavySlowProfile;
    public EnemyAttackEffectProfile HeavyDotProfile => heavyDotProfile;
    public EnemyAttackEffectProfile HeavyKnockProfile => heavyKnockProfile;
}