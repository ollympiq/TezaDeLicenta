using UnityEngine;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform spawnRoot;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private DamageNumberUI damageNumberPrefab;

    [Header("Damage Colors")]
    [SerializeField] private Color physicalColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color fireColor = new Color(1f, 0.45f, 0.1f, 1f);
    [SerializeField] private Color earthColor = new Color(0.68f, 0.50f, 0.22f, 1f);
    [SerializeField] private Color windColor = new Color(0.60f, 1f, 0.75f, 1f);
    [SerializeField] private Color lightningColor = new Color(1f, 0.95f, 0.25f, 1f);
    [SerializeField] private Color iceColor = new Color(0.55f, 0.85f, 1f, 1f);

    [Header("Other Colors")]
    [SerializeField] private Color missColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color healColor = new Color(0.35f, 1f, 0.45f, 1f);
    [SerializeField] private Color buffColor = new Color(0.45f, 0.85f, 1f, 1f);
    [SerializeField] private Color debuffColor = new Color(1f, 0.55f, 0.2f, 1f);
    [SerializeField] private Color statusColor = new Color(1f, 0.9f, 0.35f, 1f);

    [Header("Scale")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float criticalScale = 1.2f;
    [SerializeField] private float statusScale = 0.95f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (spawnRoot == null)
            spawnRoot = transform as RectTransform;

        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    public void ShowDamage(int amount, Transform target, DamageType damageType, bool isCritical = false)
    {
        Color color = GetColorForDamageType(damageType);
        float scale = isCritical ? criticalScale : normalScale;

        SpawnText(amount.ToString(), target, color, scale);
    }

    public void ShowMiss(Transform target)
    {
        SpawnText("Miss", target, missColor, normalScale);
    }

    public void ShowHeal(int amount, Transform target)
    {
        if (amount <= 0)
            return;

        SpawnText($"+{amount} HP", target, healColor, normalScale);
    }

    public void ShowBuffText(string text, Transform target)
    {
        ShowStatusText(text, target, buffColor);
    }

    public void ShowDebuffText(string text, Transform target)
    {
        ShowStatusText(text, target, debuffColor);
    }

    public void ShowStatusText(string text, Transform target)
    {
        ShowStatusText(text, target, statusColor);
    }

    public void ShowStatusText(string text, Transform target, Color color)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        SpawnText(text, target, color, statusScale);
    }

    private void SpawnText(string displayText, Transform target, Color color, float scale)
    {
        if (damageNumberPrefab == null || spawnRoot == null || rootCanvas == null)
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return;

        DamageNumberUI instance = Instantiate(damageNumberPrefab, spawnRoot);
        instance.transform.SetAsLastSibling();
        instance.Initialize(rootCanvas, worldCamera, target, displayText, color, scale);
    }

    private Color GetColorForDamageType(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                return physicalColor;
            case DamageType.Fire:
                return fireColor;
            case DamageType.Earth:
                return earthColor;
            case DamageType.Wind:
                return windColor;
            case DamageType.Lightning:
                return lightningColor;
            case DamageType.Ice:
                return iceColor;
            default:
                return physicalColor;
        }
    }
}