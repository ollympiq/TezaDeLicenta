using UnityEngine;

public class WorldHealthBarManager : MonoBehaviour
{
    public static WorldHealthBarManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform spawnRoot;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private WorldHealthBarUI worldHealthBarPrefab;

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

    public void CreateBar(CharacterHealth health, Transform followTarget)
    {
        if (health == null || followTarget == null || worldHealthBarPrefab == null || spawnRoot == null)
            return;

        WorldHealthBarUI bar = Instantiate(worldHealthBarPrefab, spawnRoot);
        bar.transform.SetAsLastSibling();
        bar.Initialize(health, followTarget, rootCanvas, worldCamera);
    }
}