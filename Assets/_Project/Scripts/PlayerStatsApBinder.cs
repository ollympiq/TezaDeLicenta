using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(PlayerAP))]
public class PlayerStatsApBinder : MonoBehaviour
{
    private CharacterStats stats;
    private PlayerAP playerAP;
    private bool initialized;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        playerAP = GetComponent<PlayerAP>();
    }

    private void OnEnable()
    {
        if (stats != null)
            stats.OnStatsChanged += ApplyStatsToAP;
    }

    private void OnDisable()
    {
        if (stats != null)
            stats.OnStatsChanged -= ApplyStatsToAP;
    }

    private void Start()
    {
        ApplyStatsToAP();
    }

    public void ApplyStatsToAP()
    {
        if (stats == null || playerAP == null)
            return;

        if (!initialized)
        {
            playerAP.SetMaxAP(stats.MaxAP, true);
            initialized = true;
        }
        else
        {
            playerAP.SetMaxAP(stats.MaxAP, false);
        }
    }
}