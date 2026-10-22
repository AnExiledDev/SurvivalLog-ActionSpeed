using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ActionSpeed.Core;

/// <summary>One action as the game's table defines it, before this mod wrote over it.</summary>
/// <param name="Type">The game's ActionType, 0 for furniture use (the game leaves that bucket unnamed).</param>
/// <param name="GameDuring">Game seconds the action takes; 0 or less marks an action timed by something else.</param>
/// <param name="IsReading">The game plays its reading animation or sound for it: books, magazines, newspapers, notes and letters.</param>
/// <param name="Effect">The config name of the action's effect, such as Action/Install_Trap; empty when it has none.</param>
/// <param name="ItemCategory">The game's ItemCategory of the item the action uses, 0 for an action that uses no item.</param>
/// <param name="ItemSubCategory">The game's ItemSubCategory of that item, 0 when it has none.</param>
public readonly record struct ActionRow(int Id, string Name, int Type, float GameDuring, bool IsReading = false, string Effect = "", int ItemCategory = 0, int ItemSubCategory = 0);

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

/// <param name="SpeedByGroup">Speed per group, already resolved through its section and the everything speed; a
/// group with no entry keeps the game's duration.</param>
/// <param name="Overrides">Speed per action id, which beats the group's speed.</param>
/// <param name="FullBenefits">A sped-up action gives everything it gives at the game's pace, by running its effect
/// faster; off, it is cut short and pays out only for the shorter time.</param>
public sealed record SpeedSettings(IReadOnlyDictionary<Group, float> SpeedByGroup, IReadOnlyDictionary<int, float> Overrides, bool FullBenefits);

public sealed record ParsedOverrides(IReadOnlyDictionary<int, float> Speeds, IReadOnlyList<string> Rejected);

public static class ActionSpeeds
{
    public const float Slowest = 0.25f;
    public const float Fastest = 20f;

    private const float SecondsPerMinute = 60f;
    private const int ReadBookAnimation = 21;

    private static readonly string[] ReadingSounds = { "/kanshu", "/kanbaozhi" };

    /// <summary>A speed that is not a number means nothing was meant, so the action keeps the game's speed.</summary>
    public static float Clamp(float speed) => float.IsFinite(speed) ? Math.Clamp(speed, Slowest, Fastest) : 1f;

    /// <summary>
    /// How an action runs at its speed. An override is the player naming one action, so it beats the group's speed;
    /// an action timed by something else has no duration here to scale. With full benefits the table keeps the
    /// game's duration and the effect's clock runs faster, so every payout and the end come sooner together; the
    /// table value is never divided as well, or the action would be cut short a second time.
    /// </summary>
    public static ActionDecision Decide(ActionRow row, SpeedSettings settings)
    {
        if (row.GameDuring <= 0f) return new ActionDecision(row.GameDuring, 1f, Reason.TimedElsewhere);

        if (settings.Overrides.TryGetValue(row.Id, out var overridden)) return AtSpeed(row, overridden, settings.FullBenefits, Reason.Overridden);

        if (!settings.SpeedByGroup.TryGetValue(ActionGroups.Of(row), out var speed) || speed == 1f) return new ActionDecision(row.GameDuring, 1f, Reason.GameSpeed);

        return AtSpeed(row, speed, settings.FullBenefits, Reason.Changed);
    }

    /// <summary>Whether any action could run its effect on a faster or slower clock, which is when the clock needs hooking.</summary>
    public static bool NeedsClock(SpeedSettings settings) =>
        settings.FullBenefits && settings.SpeedByGroup.Values.Concat(settings.Overrides.Values).Any(speed => speed != 1f);

    /// <summary>A section or group speed: 0 or less, or not a number, follows the speed above it.</summary>
    public static float? Follow(float configured) => float.IsFinite(configured) && configured > 0f ? Clamp(configured) : null;

    /// <summary>
    /// Whether an action is reading, by what the game shows for it: its reading animation (Config_Show
    /// Armature_ReadBook) or its page-turning or newspaper sound. Action names are text keys, so they cannot say.
    /// </summary>
    public static bool IsReading(int showConfigId, string? fmodPath) =>
        showConfigId == ReadBookAnimation || (fmodPath != null && ReadingSounds.Any(fmodPath.EndsWith));

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

    public static string Describe(ActionRow row, ActionDecision decision)
    {
        var benefits = decision.Clock == 1f ? "" : ", full benefits";
        var cost = row.GameDuring <= 0f ? "not timed by this table" : $"{Minutes(row.GameDuring)} -> {Minutes(decision.Lasts)}{benefits}";

        return $"{row.Id} | {row.Name} | {ActionGroups.Label(ActionGroups.Of(row))} | {cost} | {decision.Reason}";
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
