using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public string saveVersion = "1.0";
    public string sceneName = "LobbyScene";
    public string savedAt;

    public CharacterClass selectedPlayerClass = CharacterClass.Unassigned;
    public bool hasRestorablePlayerState;

    public int runCurrentCombatLevel = 1;
    public int runPendingLobbyLevel = 0;
    public int runMaxCombatLevel = 10;
    public bool runHasPendingLobbyCombat;

    public int gameSessionCurrentCombatLevel = 1;
    public bool pendingLobbyLevelUp;

    public int savedPlayerLevel = 1;
    public int savedUnspentStatPoints = 0;
    public int savedGold = 0;

    public int savedStrength = 10;
    public int savedConstitution = 10;
    public int savedDexterity = 10;
    public int savedIntelligence = 10;

    public List<GameSession.SavedItemInstance> savedInventory = new List<GameSession.SavedItemInstance>();
    public List<GameSession.SavedEquippedItem> savedEquipment = new List<GameSession.SavedEquippedItem>();
    public GameSession.SavedSkillLoadout savedSkills = new GameSession.SavedSkillLoadout();
}