using System;

namespace ActionSpeed.Core;

/// <summary>Who is walking or working, as far as their speed goes.</summary>
public enum Character
{
    /// <summary>The character the player controls.</summary>
    Player,
    Robot,

    /// <summary>The other people living at the base.</summary>
    Survivor,

    /// <summary>Anyone else, raiders and creatures included; they always keep the game's pace.</summary>
    Other,
}

public readonly record struct CharacterSpeeds(float Player, float Robot, float Survivor)
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
    public static Character Classify(bool isLeadingRole, bool isBaseResident, bool isRobot)
    {
        if (isLeadingRole) return Character.Player;

        if (!isBaseResident) return Character.Other;

        return isRobot ? Character.Robot : Character.Survivor;
    }

    public static float SpeedFor(Character walker, CharacterSpeeds speeds) => walker switch
    {
        Character.Player => speeds.Player,
        Character.Robot => speeds.Robot,
        Character.Survivor => speeds.Survivor,
        Character.Other => 1f,
        _ => throw new ArgumentOutOfRangeException(nameof(walker), walker, null),
    };
}
