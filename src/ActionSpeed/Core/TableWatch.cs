using System;
using System.Collections.Generic;
using System.Linq;

namespace ActionSpeed.Core;

/// <summary>
/// Which action table the last full write went to, and which language its names were read in: the game replaces the
/// table object when it loads it again, and swaps or fills its text table when the language loads or changes.
/// </summary>
public readonly record struct TableStamp(long Actions, int ActionCount, long Language, int LanguageCount);

/// <summary>
/// Decides when the action table needs writing again. A full write reads every action through the game's runtime and
/// costs several milliseconds, so it runs only when the table was replaced, the language changed, the settings
/// changed, or one of a few watched actions lost the duration this mod gave it. The check between full writes reads
/// only the watched actions and allocates nothing.
/// </summary>
public sealed class TableWatch
{
    public const int CanaryCount = 8;

    private TableStamp? _written;
    private int[] _canaryIds = Array.Empty<int>();
    private float[] _canaryDurations = Array.Empty<float>();

    /// <summary>The settings changed, so the table is written again whatever it holds.</summary>
    public void Forget() => _written = null;

    /// <param name="durationOf">An action's duration in the table now; a missing action reads as -1, so it counts as changed.</param>
    public bool NeedsWrite(TableStamp now, Func<int, float> durationOf)
    {
        if (_written != now) return true;

        for (var i = 0; i < _canaryIds.Length; i++)
        {
            if (durationOf(_canaryIds[i]) != _canaryDurations[i]) return true;
        }

        return false;
    }

    /// <summary>A full write went to the table, so its stamp and a few of the actions it changed are watched from now on.</summary>
    public void Written(TableStamp stamp, IReadOnlyDictionary<int, float> planned, IReadOnlyDictionary<int, float> gameDuring)
    {
        var changedIds = planned.Where(pair => gameDuring.TryGetValue(pair.Key, out var game) && game != pair.Value).Select(pair => pair.Key);

        _canaryIds = PickCanaries(changedIds).ToArray();
        _canaryDurations = _canaryIds.Select(id => planned[id]).ToArray();
        _written = stamp;
    }

    /// <summary>
    /// Up to <see cref="CanaryCount"/> changed actions, spread across the id range, to check between full writes.
    /// Only changed actions are watched: a reload puts the game's own duration back, which only shows on an action this mod changed.
    /// </summary>
    public static IReadOnlyList<int> PickCanaries(IEnumerable<int> changedIds)
    {
        var ids = changedIds.OrderBy(id => id).ToList();

        if (ids.Count <= CanaryCount) return ids;

        return Enumerable.Range(0, CanaryCount).Select(i => ids[i * (ids.Count - 1) / (CanaryCount - 1)]).ToList();
    }
}
