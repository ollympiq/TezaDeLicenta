using UnityEngine;

public class CurrentLevelContext : MonoBehaviour
{
    public static CurrentLevelContext Instance { get; private set; }

    [SerializeField, Min(1)] private int currentLevel = 1;

    public int CurrentLevel => Mathf.Max(1, currentLevel);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Exista deja un CurrentLevelContext in scena. Il pastrez pe primul.");
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}