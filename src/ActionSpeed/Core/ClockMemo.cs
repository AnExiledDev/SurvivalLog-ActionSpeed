using System.Collections.Generic;

namespace ActionSpeed.Core;

/// <summary>
/// Remembers which clock each running action effect was given, so the effect is looked up once and not every frame.
/// The game reuses effect ids for later effects, so an effect that has just started is always looked up again.
/// Entries live in two generations: each <see cref="Age"/> moves the recent ones to the older set and drops what was
/// already older, so an effect that ended is forgotten after two ages without anyone tracking its end.
/// </summary>
public sealed class ClockMemo
{
    private Dictionary<long, float> _recent = new();
    private Dictionary<long, float> _older = new();

    /// <param name="isFresh">The effect has not used any of its time yet, so the id may belong to an earlier effect.</param>
    public bool TryRecall(long effectId, bool isFresh, out float clock)
    {
        clock = 1f;

        if (isFresh) return false;

        if (_recent.TryGetValue(effectId, out clock)) return true;

        if (!_older.TryGetValue(effectId, out clock))
        {
            clock = 1f;

            return false;
        }

        _recent[effectId] = clock;

        return true;
    }

    public void Remember(long effectId, float clock) => _recent[effectId] = clock;

    public void Age()
    {
        (_older, _recent) = (_recent, _older);
        _recent.Clear();
    }

    /// <summary>Forgets every effect, for a newly loaded game whose effect ids start over.</summary>
    public void Clear()
    {
        _recent.Clear();
        _older.Clear();
    }
}
