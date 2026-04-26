using System;
using System.Collections.Generic;

public enum GameLogType
{
    Info,
    Combat,
    Success,
    Warning,
    Error
}

public readonly struct GameLogEntry
{
    public readonly string Message;
    public readonly GameLogType Type;
    public readonly DateTime Timestamp;

    public GameLogEntry(string message, GameLogType type)
    {
        Message = message;
        Type = type;
        Timestamp = DateTime.Now;
    }
}

public static class GameLog
{
    public static event Action<GameLogEntry> OnEntryAdded;

    private static readonly List<GameLogEntry> entries = new List<GameLogEntry>();
    public static IReadOnlyList<GameLogEntry> Entries => entries;

    public static void Clear()
    {
        entries.Clear();
    }

    public static void Write(string message, GameLogType type = GameLogType.Info)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        GameLogEntry entry = new GameLogEntry(message, type);
        entries.Add(entry);
        OnEntryAdded?.Invoke(entry);
    }

    public static void Info(string message) => Write(message, GameLogType.Info);
    public static void Combat(string message) => Write(message, GameLogType.Combat);
    public static void Success(string message) => Write(message, GameLogType.Success);
    public static void Warning(string message) => Write(message, GameLogType.Warning);
    public static void Error(string message) => Write(message, GameLogType.Error);
}