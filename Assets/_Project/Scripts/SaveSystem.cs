using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    private const string BuildSaveFileName = "savegame.json";
    private const string EditorSaveFileName = "editor_savegame.json";

    public static string SavePath
    {
        get
        {
#if UNITY_EDITOR
            return Path.Combine(Application.persistentDataPath, EditorSaveFileName);
#else
            return Path.Combine(Application.persistentDataPath, BuildSaveFileName);
#endif
        }
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static bool SaveCurrentGame()
    {
        GameSession session = EnsureGameSessionExists();

        if (session == null)
        {
            Debug.LogWarning("SaveSystem: GameSession lipseste. Salvarea a fost anulata.");
            return false;
        }

        CaptureCurrentPlayerIntoSession(session);

        string currentSceneName = SceneManager.GetActiveScene().name;
        GameSaveData data = session.ExportSaveData(currentSceneName);

        if (RunLevelFlow.Instance != null)
        {
            data.runCurrentCombatLevel = RunLevelFlow.Instance.CurrentCombatLevel;
            data.runPendingLobbyLevel = RunLevelFlow.Instance.HasPendingLobbyCombat
                ? RunLevelFlow.Instance.PendingLobbyLevel
                : 0;

            data.runHasPendingLobbyCombat = RunLevelFlow.Instance.HasPendingLobbyCombat;
            data.runMaxCombatLevel = RunLevelFlow.Instance.MaxCombatLevel;
        }

        string json = JsonUtility.ToJson(data, true);

        string directory = Path.GetDirectoryName(SavePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(SavePath, json);

        Debug.Log("Joc salvat in: " + SavePath);
        return true;
    }

    public static bool TryLoadSaveIntoRuntime(out string sceneToLoad)
    {
        sceneToLoad = string.Empty;

        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("SaveSystem: nu exista niciun save.");
            return false;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        if (data == null)
        {
            Debug.LogWarning("SaveSystem: save-ul nu a putut fi citit.");
            return false;
        }

        GameSession session = EnsureGameSessionExists();

        if (session == null)
        {
            Debug.LogWarning("SaveSystem: GameSession lipseste la load.");
            return false;
        }

        session.ImportSaveData(data);

        RunLevelFlow flow = EnsureRunLevelFlowExists();

        if (flow != null)
        {
            int pending = data.runHasPendingLobbyCombat ? data.runPendingLobbyLevel : 0;
            flow.ApplyLoadedRunState(data.runCurrentCombatLevel, pending);
        }

        sceneToLoad = string.IsNullOrWhiteSpace(data.sceneName)
            ? "LobbyScene"
            : data.sceneName;

        Debug.Log("Save incarcat. Scena: " + sceneToLoad);
        return true;
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("SaveSystem: save sters din: " + SavePath);
        }
    }

#if UNITY_EDITOR
    public static string GetEditorSavePath()
    {
        return Path.Combine(Application.persistentDataPath, EditorSaveFileName);
    }

    public static string GetBuildSavePathPreview()
    {
        return Path.Combine(Application.persistentDataPath, BuildSaveFileName);
    }
#endif

    private static void CaptureCurrentPlayerIntoSession(GameSession session)
    {
        GameObject player = null;

        if (PlayerRuntimeRegistry.ResolvePlayerRoot() != null)
            player = PlayerRuntimeRegistry.ResolvePlayerRoot();

        if (player == null)
        {
            PlayerProgression progression = Object.FindFirstObjectByType<PlayerProgression>();
            if (progression != null)
                player = progression.gameObject;
        }

        if (player == null)
        {
            Debug.LogWarning("SaveSystem: playerul activ nu a fost gasit. Se salveaza doar starea GameSession.");
            return;
        }

        CharacterStats stats = player.GetComponent<CharacterStats>();
        PlayerProgression progressionRef = player.GetComponent<PlayerProgression>();
        PlayerWallet wallet = player.GetComponent<PlayerWallet>();
        CharacterInventory inventory = player.GetComponent<CharacterInventory>();
        CharacterEquipment equipment = player.GetComponent<CharacterEquipment>();
        PlayerSkillLoadout skillLoadout = player.GetComponent<PlayerSkillLoadout>();

        if (stats != null && stats.Class != CharacterClass.Unassigned)
            session.RememberAppliedPlayerClass(stats.Class);

        session.SaveFromPlayer(
            progressionRef,
            wallet,
            inventory,
            equipment,
            skillLoadout);

        session.ForceRestorableState();
    }

    private static GameSession EnsureGameSessionExists()
    {
        if (GameSession.Instance != null)
            return GameSession.Instance;

        GameSession existing = Object.FindFirstObjectByType<GameSession>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("GameSession");
        return go.AddComponent<GameSession>();
    }

    private static RunLevelFlow EnsureRunLevelFlowExists()
    {
        if (RunLevelFlow.Instance != null)
            return RunLevelFlow.Instance;

        RunLevelFlow existing = Object.FindFirstObjectByType<RunLevelFlow>();
        if (existing != null)
            return existing;

        GameObject sessionObject = GameSession.Instance != null
            ? GameSession.Instance.gameObject
            : new GameObject("GameSession");

        return sessionObject.AddComponent<RunLevelFlow>();
    }
}