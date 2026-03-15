using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(PlayerAP))]
public class PlayerStatsApBinder : MonoBehaviour
{
    private CharacterStats stats;
    private PlayerAP playerAP;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        playerAP = GetComponent<PlayerAP>();
    }

    private void Start()
    {
        ApplyStatsToAP();
    }

    public void ApplyStatsToAP()
    {
        if (stats == null || playerAP == null)
            return;

        playerAP.SetMaxAP(stats.MaxAP, true);
    }
}