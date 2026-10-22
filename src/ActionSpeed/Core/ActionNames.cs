using System;
using System.Collections.Generic;
using System.Linq;

namespace ActionSpeed.Core;

/// <summary>
/// The names the action list shows. The game hands back the text key itself until its language has loaded, and the
/// list is first written before that, so a name that did not resolve is remembered and asked for again later; once
/// any of them resolves, the list is worth writing again.
/// </summary>
public sealed class ActionNames
{
    private readonly Func<string, string?> _lookUp;

    /// <summary>The text key of every action whose name has not resolved yet, by action id.</summary>
    private readonly Dictionary<int, string> _waiting = new();

    /// <param name="lookUp">The game's text for a key: the key itself, blank or null while it has none.</param>
    public ActionNames(Func<string, string?> lookUp) => _lookUp = lookUp;

    /// <summary>The name to show for an action: the game's text, or the raw key while that has not loaded.</summary>
    public string Name(int id, string key)
    {
        if (key.Length == 0) return key;

        var shown = Resolve(key);

        if (shown == null)
        {
            _waiting[id] = key;

            return key;
        }

        _waiting.Remove(id);

        return shown;
    }

    /// <summary>Whether a name shown as its raw key would now show the game's text.</summary>
    public bool HasNewlyResolved() => _waiting.Values.Any(key => Resolve(key) != null);

    private string? Resolve(string key)
    {
        var shown = _lookUp(key);

        return string.IsNullOrWhiteSpace(shown) || shown == key ? null : shown;
    }
}
