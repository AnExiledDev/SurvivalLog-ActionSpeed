using System.Collections.Generic;
using System.Linq;

namespace ActionSpeed.Core;

/// <summary>Which action table the last full write went to: the game replaces the table object when it loads it again.</summary>
public readonly record struct TableStamp(long Actions, int ActionCount);

/// <summary>A watched action's duration as this mod wrote it, and the duration it carries now.</summary>
public readonly record struct Canary(float Written, float Now);

/// <summary>
/// Decides when the action table needs writing again. A full write reads every action through the game's runtime and
/// costs several milliseconds, so it runs only when the table was replaced, the settings changed, or one of a few
/// watched actions lost the duration this mod gave it.
/// </summary>
public static class TableWatch
{
    public const int CanaryCount = 8;

    /// <summary>True when nothing about the table has changed since the last full write.</summary>
    public static bool IsUnchanged(TableStamp? last, TableStamp now, IEnumerable<Canary> canaries) =>
        last == now && canaries.All(canary => canary.Written == canary.Now);

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
