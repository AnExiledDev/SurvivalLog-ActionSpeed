using System.Collections.Generic;

namespace ActionSpeed.Core;

/// <summary>
/// Keeps the workbench's progress bar in step with a shortened craft. When a craft starts, the game tells the bench
/// window the craft's length so its bar fills at the right rate, but it does so with the game's length, before the
/// craft is shortened. The window is told either while the start is still running or after it, so both are covered:
/// while it runs, the length is scaled; after it, the next length that bench's window is told is shortened by as much as the craft was.
/// </summary>
public sealed class CraftBar
{
    private readonly KindSpeed _speed;
    private readonly Dictionary<long, (float Game, float Shortened)> _started = new();
    private long? _starting;
    private bool _isShownWhileStarting;

    public CraftBar(KindSpeed speed)
    {
        _speed = speed;
    }

    /// <summary>A start request for this bench is running.</summary>
    public void Starting(long benchId)
    {
        _starting = benchId;
        _isShownWhileStarting = false;
    }

    /// <summary>The start request finished; <paramref name="shortenedSeconds"/> is null when no craft was shortened.</summary>
    public void Started(long benchId, float gameSeconds, float? shortenedSeconds)
    {
        // A new start replaces whatever an earlier craft at this bench left unshown.
        _started.Remove(benchId);

        if (_starting == benchId && !_isShownWhileStarting && shortenedSeconds is { } shortened) _started[benchId] = (gameSeconds, shortened);

        _starting = null;
    }

    /// <summary>The craft length the bench window should be told, given the length the game told it.</summary>
    public float Shown(long benchId, float seconds)
    {
        if (seconds <= 0f) return seconds;

        if (_starting == benchId)
        {
            _isShownWhileStarting = true;

            return KindSpeeds.ScaleSeconds(seconds, _speed);
        }

        if (!_started.Remove(benchId, out var craft) || craft.Game <= 0f) return seconds;

        return seconds * craft.Shortened / craft.Game;
    }
}
