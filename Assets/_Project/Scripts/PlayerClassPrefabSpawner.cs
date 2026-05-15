using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class PlayerClassPrefabSpawner : MonoBehaviour
{
    [Serializable]
    public class PlayerClassPrefabEntry
    {
        public CharacterClass classType;
        public GameObject prefab;

        [Tooltip("Optional. Foloseste asta daca ai deja playerul pus in scena si vrei doar sa activezi varianta corecta.")]
        public GameObject sceneInstance;
    }

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private CharacterClass fallbackClass = CharacterClass.Melee;
    [SerializeField] private PlayerClassPrefabEntry[] playerPrefabs;

    [Header("Behaviour")]
    [SerializeField] private bool useSceneInstancesIfAssigned = true;
    [SerializeField] private bool deactivateUnselectedSceneInstances = true;
    [SerializeField] private bool destroyPreviousRuntimePlayer = true;
    [SerializeField] private bool bindSceneReferences = true;
    [SerializeField] private bool restartCombatAfterBinding = true;
    [SerializeField] private string activePlayerName = "Player";

    private GameObject activePlayer;

    public GameObject ActivePlayer => activePlayer;

    private void Awake()
    {
        SpawnOrActivateSelectedPlayer();
    }

    private IEnumerator Start()
    {
        yield return null;

        if (activePlayer == null)
            yield break;

        PlayerRuntimeRegistry.Register(activePlayer);

        if (bindSceneReferences)
            PlayerSceneReferenceBinder.BindAllToPlayer(activePlayer);

        RestorePlayerAP();

        if (restartCombatAfterBinding && TurnManager.Instance != null)
        {
            TurnManager.Instance.ResetCombatState();
            TurnManager.Instance.StartCombat();
        }
    }

    private void OnDestroy()
    {
        PlayerRuntimeRegistry.Clear(activePlayer);
    }

    public void SpawnOrActivateSelectedPlayer()
    {
        CharacterClass selectedClass = ResolveSelectedClass();
        PlayerClassPrefabEntry entry = FindEntry(selectedClass);

        if (entry == null)
        {
            Debug.LogWarning($"PlayerClassPrefabSpawner: nu exista prefab pentru clasa {selectedClass}. Se foloseste fallback {fallbackClass}.");
            selectedClass = fallbackClass;
            entry = FindEntry(selectedClass);
        }

        if (entry == null)
        {
            Debug.LogError("PlayerClassPrefabSpawner: nu exista niciun prefab valid pentru player.");
            return;
        }

        if (deactivateUnselectedSceneInstances)
            DeactivateUnselectedSceneInstances(entry);

        if (useSceneInstancesIfAssigned && entry.sceneInstance != null)
        {
            activePlayer = entry.sceneInstance;
            activePlayer.SetActive(true);
            MovePlayerToSpawnPoint(activePlayer);
        }
        else
        {
            if (destroyPreviousRuntimePlayer && activePlayer != null)
                Destroy(activePlayer);

            if (entry.prefab == null)
            {
                Debug.LogError($"PlayerClassPrefabSpawner: prefab lipsa pentru clasa {entry.classType}.");
                return;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            activePlayer = Instantiate(entry.prefab, position, rotation);
        }

        activePlayer.name = activePlayerName;

        if (activePlayer.CompareTag("Untagged"))
            activePlayer.tag = "Player";

        PlayerRuntimeRegistry.Register(activePlayer);

        if (bindSceneReferences)
            PlayerSceneReferenceBinder.BindAllToPlayer(activePlayer);
    }

    private CharacterClass ResolveSelectedClass()
    {
        if (GameSession.Instance != null && GameSession.Instance.SelectedPlayerClass != CharacterClass.Unassigned)
            return GameSession.Instance.SelectedPlayerClass;

        return fallbackClass;
    }

    private PlayerClassPrefabEntry FindEntry(CharacterClass classType)
    {
        if (playerPrefabs == null)
            return null;

        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            PlayerClassPrefabEntry entry = playerPrefabs[i];

            if (entry != null && entry.classType == classType)
                return entry;
        }

        return null;
    }

    private void DeactivateUnselectedSceneInstances(PlayerClassPrefabEntry selectedEntry)
    {
        if (playerPrefabs == null)
            return;

        for (int i = 0; i < playerPrefabs.Length; i++)
        {
            PlayerClassPrefabEntry entry = playerPrefabs[i];

            if (entry == null || entry.sceneInstance == null)
                continue;

            if (entry == selectedEntry)
                continue;

            entry.sceneInstance.SetActive(false);
        }
    }

    private void MovePlayerToSpawnPoint(GameObject playerObject)
    {
        if (playerObject == null || spawnPoint == null)
            return;

        playerObject.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
    }

    private void RestorePlayerAP()
    {
        if (activePlayer == null)
            return;

        PlayerAP ap = activePlayer.GetComponent<PlayerAP>();

        if (ap != null)
            ap.RestoreAllAP();
    }
}