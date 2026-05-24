public class EnemyAdaptationApplyReport
{
    public bool Applied { get; set; }

    public string MediumEffectText { get; set; } = "None";
    public string HeavyEffectText { get; set; } = "None";

    public bool HasMediumEffect => !string.IsNullOrWhiteSpace(MediumEffectText) && MediumEffectText != "None";
    public bool HasHeavyEffect => !string.IsNullOrWhiteSpace(HeavyEffectText) && HeavyEffectText != "None";
}