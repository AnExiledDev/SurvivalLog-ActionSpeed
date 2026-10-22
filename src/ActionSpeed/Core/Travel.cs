using System;

namespace ActionSpeed.Core;

/// <summary>How long a trip between home and an expedition place takes at the travel speed.</summary>
public static class Travel
{
    /// <summary>
    /// A trip that takes time still takes at least a second, so the game never mistakes a fast trip for a place with
    /// no travel at all.
    /// </summary>
    /// <param name="gameSeconds">The game's own trip time; 0 for a place it reaches without travel.</param>
    public static int Seconds(int gameSeconds, KindSpeed speed)
    {
        if (gameSeconds <= 0 || speed.IsGamePace) return gameSeconds;

        return Math.Max(1, (int)MathF.Round(gameSeconds / speed.Speed));
    }
}
