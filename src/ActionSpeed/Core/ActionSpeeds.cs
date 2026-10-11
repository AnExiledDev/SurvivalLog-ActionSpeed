using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ActionSpeed.Core;

/// <summary>One action as the game's table defines it, before this mod wrote over it.</summary>
/// <param name="Type">The game's ActionType, 0 for furniture use (the game leaves that bucket unnamed).</param>
/// <param name="GameDuring">Game seconds the action takes; 0 or less marks an action timed by something else.</param>
/// <param name="PaysOverTime">Its effect drains or restores stats for as long as it runs.</param>
public readonly record struct ActionRow(int Id, string Name, int Type, float GameDuring, bool PaysOverTime);

public enum Reason
{
    Changed,
    Overridden,
    GameSpeed,
    TimedElsewhere,
    PaysOverTime,
}

public readonly record struct ActionDecision(float During, Reason Reason);

/// <param name="SpeedByType">Speed per ActionType; a type with no entry keeps the game's duration.</param>
/// <param name="Overrides">Speed per action id, which beats the type and the pays-over-time rule.</param>
/// <param name="KeepPayingActions">Leave actions that pay out over their duration at the game's duration.</param>
public sealed record SpeedSettings(IReadOnlyDictionary<int, float> SpeedByType, IReadOnlyDictionary<int, float> Overrides, bool KeepPayingActions);

public sealed record ParsedOverrides(IReadOnlyDictionary<int, float> Speeds, IReadOnlyList<string> Rejected);

public static class ActionSpeeds
{
    public const float Slowest = 0.25f;
    public const float Fastest = 20f;

    private const float SecondsPerMinute = 60f;

    /// <summary>A speed that is not a number means nothing was meant, so the action keeps the game's speed.</summary>
    public static float Clamp(float speed) => float.IsFinite(speed) ? Math.Clamp(speed, Slowest, Fastest) : 1f;

    /// <summary>
    /// The clock an action costs at its speed. An override is the player naming one action, so it beats the type's
    /// speed and the pay-out rule; an action timed by something else has no duration here to scale.
    /// </summary>
    public static ActionDecision Decide(ActionRow row, SpeedSettings settings)
    {
        if (row.GameDuring <= 0f) return new ActionDecision(row.GameDuring, Reason.TimedElsewhere);

        if (settings.Overrides.TryGetValue(row.Id, out var overridden)) return new ActionDecision(row.GameDuring / overridden, Reason.Overridden);

        if (!settings.SpeedByType.TryGetValue(row.Type, out var speed) || speed == 1f) return new ActionDecision(row.GameDuring, Reason.GameSpeed);

        if (row.PaysOverTime && settings.KeepPayingActions) return new ActionDecision(row.GameDuring, Reason.PaysOverTime);

        return new ActionDecision(row.GameDuring / speed, Reason.Changed);
    }

    /// <summary>Reads "id:speed" pairs; an entry that is not one is handed back so the player can be told.</summary>
    public static ParsedOverrides ParseOverrides(string text)
    {
        var speeds = new Dictionary<int, float>();
        var rejected = new List<string>();

        foreach (var entry in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TryParsePair(entry, out var id, out var speed)) speeds[id] = Clamp(speed);
            else rejected.Add(entry);
        }

        return new ParsedOverrides(speeds, rejected);
    }

    /// <summary>
    /// Whether an effect drains or restores stats on a timer while its action runs. Shortening such an action
    /// shrinks what it gives or takes, so it counts whichever way the values point. An effect the table does not
    /// hold cannot be checked, so it is treated as paying.
    /// </summary>
    public static bool PaysOverTime(int effectId, IReadOnlyDictionary<int, float[]> intervalsByEffect)
    {
        if (effectId <= 0) return false;

        return !intervalsByEffect.TryGetValue(effectId, out var intervals) || intervals.Any(value => value != 0f);
    }

    public static string Describe(ActionRow row, ActionDecision decision, string typeName)
    {
        var cost = row.GameDuring <= 0f ? "not timed by this table" : $"{Minutes(row.GameDuring)} -> {Minutes(decision.During)}";

        return $"{row.Id} | {row.Name} | {typeName} | {cost} | {decision.Reason}";
    }

    private static bool TryParsePair(string entry, out int id, out float speed)
    {
        var parts = entry.Split(':', StringSplitOptions.TrimEntries);
        speed = 0f;
        id = 0;

        return parts.Length == 2
               && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
               && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out speed)
               && float.IsFinite(speed)
               && speed > 0f;
    }

    private static string Minutes(float gameSeconds) => (gameSeconds / SecondsPerMinute).ToString("0.0", CultureInfo.InvariantCulture) + " min";
}

public static class Walking
{
    public const float Slowest = 0.25f;
    public const float Fastest = 5f;

    public static float Clamp(float multiplier) => float.IsFinite(multiplier) ? Math.Clamp(multiplier, Slowest, Fastest) : 1f;
}
