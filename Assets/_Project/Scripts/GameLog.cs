using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

    public static bool EnableRichTextColors = true;

    private const string White = "#FFFFFF";
    private const string Muted = "#CFCFCF";

    private const string TelemetryColor = "#66D9EF";
    private const string NeuralColor = "#C586F7";
    private const string AdaptationColor = "#FFD166";
    private const string SpawnColor = "#F4B860";

    private const string SuccessColor = "#6EE7B7";
    private const string WarningColor = "#FBBF24";
    private const string ErrorColor = "#FF6B6B";
    private const string CombatColor = "#FFCC66";

    private const string PhysicalColor = "#D8D8D8";
    private const string FireColor = "#FF8A3D";
    private const string EarthColor = "#9CDC65";
    private const string WindColor = "#7FFFD4";
    private const string LightningColor = "#FFD84D";
    private const string IceColor = "#6EC6FF";

    private const string HpColor = "#FF5555";
    private const string ArmorColor = "#9CDCFE";
    private const string EffectColor = "#FF9F43";
    private const string KnockColor = "#FF4D6D";
    private const string SlowColor = "#7DD3FC";
    private const string DotColor = "#FF9F43";

    public static void Clear()
    {
        entries.Clear();
    }

    public static void Write(string message, GameLogType type = GameLogType.Info)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        string formattedMessage = EnableRichTextColors
            ? FormatMessage(message, type)
            : message;

        GameLogEntry entry = new GameLogEntry(formattedMessage, type);
        entries.Add(entry);
        OnEntryAdded?.Invoke(entry);
    }

    public static void Info(string message) => Write(message, GameLogType.Info);
    public static void Combat(string message) => Write(message, GameLogType.Combat);
    public static void Success(string message) => Write(message, GameLogType.Success);
    public static void Warning(string message) => Write(message, GameLogType.Warning);
    public static void Error(string message) => Write(message, GameLogType.Error);

    public static void Telemetry(string message) => Write(message, GameLogType.Info);
    public static void Neural(string message) => Write(message, GameLogType.Info);
    public static void Adaptation(string message) => Write(message, GameLogType.Info);

    private static string FormatMessage(string message, GameLogType type)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        // Daca mesajul e deja colorat manual, nu il dublam cu taguri noi.
        if (message.Contains("<color=", StringComparison.OrdinalIgnoreCase))
            return message;

        string result = message;

        result = FormatHeaders(result);
        result = FormatSystemPrefixes(result);
        result = FormatLabels(result);
        result = FormatDamageTypes(result);
        result = FormatEffects(result);
        result = FormatStats(result);

        if (result == message)
            result = FormatByType(result, type);

        return result;
    }

    private static string FormatHeaders(string text)
    {
        text = text.Replace(
            "=== Combat Telemetry Final ===",
            Bold(Color("=== Combat Telemetry Final ===", TelemetryColor))
        );

        text = text.Replace(
            "=== Neural Enemy Adaptation Config ===",
            Bold(Color("=== Neural Enemy Adaptation Config ===", NeuralColor))
        );

        text = text.Replace(
            "=== RuleBased Enemy Adaptation Config ===",
            Bold(Color("=== Rule-Based Enemy Adaptation Config ===", AdaptationColor))
        );

        text = text.Replace(
            "=== RuleBased Fallback Enemy Adaptation Config ===",
            Bold(Color("=== Rule-Based Fallback Enemy Adaptation Config ===", WarningColor))
        );

        text = text.Replace(
            "=== Generated Enemy Adaptation Config ===",
            Bold(Color("=== Generated Enemy Adaptation Config ===", AdaptationColor))
        );

        return text;
    }

    private static string FormatSystemPrefixes(string text)
    {
        text = text.Replace("CombatTelemetryTracker:", Bold(Color("CombatTelemetryTracker:", TelemetryColor)));
        text = text.Replace("EnemySpawner:", Bold(Color("EnemySpawner:", SpawnColor)));
        text = text.Replace("NeuralAdaptationGenerator:", Bold(Color("NeuralAdaptationGenerator:", NeuralColor)));
        text = text.Replace("GameSession", Color("GameSession", SuccessColor));

        return text;
    }

    private static string FormatLabels(string text)
    {
        string[] telemetryLabels =
        {
            "Level:",
            "Clear Time:",
            "Target:",
            "Total Damage:",
            "Damage Taken:",
            "Potions:",
            "Skills:",
            "Basic Attacks:",
            "Moves:",
            "Avg Distance:",
            "HP End:"
        };

        for (int i = 0; i < telemetryLabels.Length; i++)
            text = text.Replace(telemetryLabels[i], Color(telemetryLabels[i], TelemetryColor));

        string[] adaptationLabels =
        {
            "Enabled:",
            "Source Completed Level:",
            "Target Level:",
            "Medium Damage Type:",
            "Heavy Damage Type:",
            "Resistances",
            "Effects",
            "Spawn Weights",
            "MediumWeights",
            "HeavyWeights",
            "CurrentLevel=",
            "SourceCompletedLevel=",
            "TargetLevel=",
            "Category=",
            "Intensity=",
            "ResistanceMode=",
            "MediumDamage=",
            "HeavyDamage=",
            "MediumEffect=",
            "HeavyEffect="
        };

        for (int i = 0; i < adaptationLabels.Length; i++)
            text = text.Replace(adaptationLabels[i], Color(adaptationLabels[i], AdaptationColor));

        return text;
    }

    private static string FormatDamageTypes(string text)
    {
        text = ReplaceWord(text, "Physical", PhysicalColor);
        text = ReplaceWord(text, "Fire", FireColor);
        text = ReplaceWord(text, "Earth", EarthColor);
        text = ReplaceWord(text, "Wind", WindColor);
        text = ReplaceWord(text, "Lightning", LightningColor);
        text = ReplaceWord(text, "Ice", IceColor);

        return text;
    }

    private static string FormatEffects(string text)
    {
        text = ReplaceWord(text, "DOT", DotColor);
        text = ReplaceWord(text, "Slow", SlowColor);
        text = ReplaceWord(text, "Slowed", SlowColor);
        text = ReplaceWord(text, "Knock", KnockColor);
        text = ReplaceWord(text, "Knocked", KnockColor);
        text = ReplaceWord(text, "None", Muted);

        return text;
    }

    private static string FormatStats(string text)
    {
        text = ReplaceWord(text, "HP", HpColor);
        text = ReplaceWord(text, "Armor", ArmorColor);
        text = ReplaceWord(text, "STR", "#FFB86C");
        text = ReplaceWord(text, "CON", "#FF6B6B");
        text = ReplaceWord(text, "DEX", "#50FA7B");
        text = ReplaceWord(text, "INT", "#BD93F9");

        return text;
    }

    private static string FormatByType(string text, GameLogType type)
    {
        switch (type)
        {
            case GameLogType.Combat:
                return Color(text, CombatColor);

            case GameLogType.Success:
                return Color(text, SuccessColor);

            case GameLogType.Warning:
                return Bold(Color(text, WarningColor));

            case GameLogType.Error:
                return Bold(Color(text, ErrorColor));

            case GameLogType.Info:
            default:
                return Color(text, Muted);
        }
    }

    private static string ReplaceWord(string text, string word, string color)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(word))
            return text;

        string pattern = $@"\b{Regex.Escape(word)}\b";

        return Regex.Replace(
            text,
            pattern,
            match => Color(match.Value, color)
        );
    }

    private static string Color(string text, string color)
    {
        return $"<color={color}>{text}</color>";
    }

    private static string Bold(string text)
    {
        return $"<b>{text}</b>";
    }
}