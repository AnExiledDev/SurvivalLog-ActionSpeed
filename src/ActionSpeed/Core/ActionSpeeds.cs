using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ActionSpeed.Core;

/// <summary>One action as the game's table defines it, before this mod wrote over it.</summary>
/// <param name="Type">The game's ActionType, 0 for furniture use (the game leaves that bucket unnamed).</param>
/// <param name="GameDuring">Game seconds the action takes; 0 or less marks an action timed by something else.</param>
public readonly record struct ActionRow(int Id, string Name, int Type, float GameDuring);

public enum Reason
{
    Changed,
    Overridden,
    GameSpeed,
    TimedElsewhere,
}

/// <param name="During">Game seconds written to the action table.</param>
/// <param name="Clock">How fast the action's effect runs: 1 is the game's own clock, 2 runs its whole effect, payouts
/// and end alike, in half the time. Above 1 only with full benefits.</param>
public readonly record struct ActionDecision(float During, float Clock, Reason Reason)
{
    /// <summary>Game seconds the action really lasts.</summary>
    public float Lasts => During / Clock;
}

/// <param name="SpeedByType">Speed per ActionType; a type with no entry keeps the game's duration.</param>
/// <param name="Overrides">Speed per action id, which beats the type's speed.</param>
/// <param name="FullBenefits">A sped-up action gives everything it gives at the game's pace, by running its effect
/// faster; off, it is cut short and pays out only for the shorter time.</param>
public sealed record SpeedSettings(IReadOnlyDictionary<int, float> SpeedByType, IReadOnlyDictionary<int, float> Overrides, bool FullBenefits);

public sealed record ParsedOverrides(IReadOnlyDictionary<int, float> Speeds, IReadOnlyList<string> Rejected);

public static class ActionSpeeds
{
    public const float Slowest = 0.25f;
    public const float Fastest = 20f;

    private const float SecondsPerMinute = 60f;

    /// <summary>A speed that is not a number means nothing was meant, so the action keeps the game's speed.</summary>
    public static float Clamp(float speed) => float.IsFinite(speed) ? Math.Clamp(speed, Slowest, Fastest) : 1f;

    /// <summary>
    /// How an action runs at its speed. An override is the player naming one action, so it beats the type's speed;
    /// an action timed by something else has no duration here to scale. With full benefits the table keeps the
    /// game's duration and the effect's clock runs faster, so every payout and the end come sooner together; the
    /// table value is never divided as well, or the action would be cut short a second time.
    /// </summary>
    public static ActionDecision Decide(ActionRow row, SpeedSettings settings)
    {
        if (row.GameDuring <= 0f) return new ActionDecision(row.GameDuring, 1f, Reason.TimedElsewhere);

        if (settings.Overrides.TryGetValue(row.Id, out var overridden)) return AtSpeed(row, overridden, settings.FullBenefits, Reason.Overridden);

        if (!settings.SpeedByType.TryGetValue(row.Type, out var speed) || speed == 1f) return new ActionDecision(row.GameDuring, 1f, Reason.GameSpeed);

        return AtSpeed(row, speed, settings.FullBenefits, Reason.Changed);
    }

    /// <summary>Whether any action could run its effect on a faster or slower clock, which is when the clock needs hooking.</summary>
    public static bool NeedsClock(SpeedSettings settings) =>
        settings.FullBenefits && settings.SpeedByType.Values.Concat(settings.Overrides.Values).Any(speed => speed != 1f);

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

    public static string Describe(ActionRow row, ActionDecision decision, string typeName)
    {
        var benefits = decision.Clock == 1f ? "" : ", full benefits";
        var cost = row.GameDuring <= 0f ? "not timed by this table" : $"{Minutes(row.GameDuring)} -> {Minutes(decision.Lasts)}{benefits}";

        return $"{row.Id} | {row.Name} | {typeName} | {cost} | {decision.Reason}";
    }

    private static ActionDecision AtSpeed(ActionRow row, float speed, bool isFullBenefits, Reason reason) =>
        isFullBenefits && speed != 1f
            ? new ActionDecision(row.GameDuring, speed, reason)
            : new ActionDecision(row.GameDuring / speed, 1f, reason);

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
