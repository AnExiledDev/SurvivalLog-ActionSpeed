using System;
using System.Collections.Generic;

namespace ActionSpeed.Core;

/// <summary>How a configured speed was taken.</summary>
public enum SpeedVerdict
{
    AsWritten,
    Clamped,
    Rejected,
}

/// <summary>
/// A speed for one kind of timed work that has been checked, so it is never zero, negative or out of range. Only
/// <see cref="From"/> makes one, which is what lets the scaling below divide by it without a guard.
/// </summary>
public sealed class KindSpeed
{
    public static readonly KindSpeed GamePace = new(1f, 1f, SpeedVerdict.AsWritten);

    private KindSpeed(float speed, float requested, SpeedVerdict verdict)
    {
        Speed = speed;
        Requested = requested;
        Verdict = verdict;
    }

    public float Speed { get; }

    public float Requested { get; }

    public SpeedVerdict Verdict { get; }

    public bool IsGamePace => Speed == 1f;

    /// <summary>Zero, negatives and non-numbers fall back to the game's pace; anything else is pulled into range.</summary>
    public static KindSpeed From(float requested)
    {
        if (!float.IsFinite(requested) || requested <= 0f) return new KindSpeed(1f, requested, SpeedVerdict.Rejected);

        var speed = Math.Clamp(requested, ActionSpeeds.Slowest, ActionSpeeds.Fastest);

        return new KindSpeed(speed, requested, speed == requested ? SpeedVerdict.AsWritten : SpeedVerdict.Clamped);
    }
}

/// <summary>A craft at a workbench as the game holds it; <paramref name="State"/> is the game's HandMadeState.</summary>
public readonly record struct CraftSnapshot(int State, float MakingTime);

/// <summary>
/// Shortens work the game times outside its action table: crafting at a workbench, in game seconds. Durations only
/// ever get shorter or longer, never zero, so nothing is skipped.
/// </summary>
public static class KindSpeeds
{
    /// <summary>The game's HandMadeState.Making.</summary>
    public const int Making = 1;

    /// <summary>One line for the log when a setting was not taken as written; null when it was.</summary>
    public static string? Complaint(string key, KindSpeed speed) => speed.Verdict switch
    {
        SpeedVerdict.AsWritten => null,
        SpeedVerdict.Clamped => $"{key} = {speed.Requested} is outside {ActionSpeeds.Slowest} to {ActionSpeeds.Fastest}, using {speed.Speed}",
        SpeedVerdict.Rejected => $"{key} = {speed.Requested} is not a speed above 0, using the game's pace (1)",
        _ => throw new ArgumentOutOfRangeException(nameof(speed), speed.Verdict, "unknown verdict"),
    };

    /// <summary>A duration the game keeps as a float; one it does not time (0 or less) is left alone.</summary>
    public static float ScaleSeconds(float gameSeconds, KindSpeed speed) =>
        speed.IsGamePace || !float.IsFinite(gameSeconds) || gameSeconds <= 0f ? gameSeconds : gameSeconds / speed.Speed;

    /// <summary>
    /// Whether a start request just began a new craft. The game sets a fresh craft's clock to 0 and its state to
    /// Making; a request it refused leaves both alone, so a craft already running is never shortened twice.
    /// </summary>
    /// <param name="before">The bench before the request, null when the game had no record of it yet.</param>
    public static bool IsFreshCraft(CraftSnapshot? before, CraftSnapshot after) =>
        after.State == Making && after.MakingTime == 0f && before?.State != Making;
}

/// <summary>Lets a hook that can fail on every call log each distinct failure once instead of flooding the log.</summary>
public sealed class DistinctFailures
{
    private readonly HashSet<string> _seen = new();

    /// <summary>True the first time this failure is seen.</summary>
    public bool IsNew(string failure) => _seen.Add(failure);
}
