using System;

namespace ActionSpeed.Core;

/// <summary>Who is walking, as far as walking speed goes.</summary>
public enum Walker
{
    /// <summary>The character the player controls.</summary>
    Player,
    Robot,

    /// <summary>The other people living at the base.</summary>
    Survivor,

    /// <summary>Anyone else, raiders and creatures included; they always walk at the game's speed.</summary>
    Other,
}

public readonly record struct WalkSpeeds(float Player, float Robot, float Survivor)
{
    public bool IsGamePace => Player == 1f && Robot == 1f && Survivor == 1f;
}

public static class Walking
{
    public const float Slowest = 0.25f;
    public const float Fastest = 5f;

    public static float Clamp(float multiplier) => float.IsFinite(multiplier) ? Math.Clamp(multiplier, Slowest, Fastest) : 1f;

    /// <summary>
    /// The game keeps the base's survivors and its robot as one kind of character and every hostile as another, so
    /// only that first kind, plus the player, is ever sped up.
    /// </summary>
    public static Walker Classify(bool isLeadingRole, bool isBaseResident, bool isRobot)
    {
        if (isLeadingRole) return Walker.Player;

        if (!isBaseResident) return Walker.Other;

        return isRobot ? Walker.Robot : Walker.Survivor;
    }

    public static float SpeedFor(Walker walker, WalkSpeeds speeds) => walker switch
    {
        Walker.Player => speeds.Player,
        Walker.Robot => speeds.Robot,
        Walker.Survivor => speeds.Survivor,
        Walker.Other => 1f,
        _ => throw new ArgumentOutOfRangeException(nameof(walker), walker, null),
    };
}
