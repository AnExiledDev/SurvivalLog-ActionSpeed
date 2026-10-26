using System;

namespace ActionSpeed.Core;

/// <summary>
/// The names the action list shows. The game hands back the text key itself until its language has loaded, and the
/// list may first be written before that, so such a name shows as its key; the list is read again when the game's
/// text table loads or changes (<see cref="TableStamp"/>), and the name is asked for again then.
/// </summary>
public sealed class ActionNames
{
    private readonly Func<string, string?> _lookUp;

    /// <param name="lookUp">The game's text for a key: the key itself, blank or null while it has none.</param>
    public ActionNames(Func<string, string?> lookUp) => _lookUp = lookUp;

    /// <summary>The name to show for an action: the game's text, or the raw key while that has not loaded.</summary>
    public string Name(string key)
    {
        if (key.Length == 0) return key;

        var shown = _lookUp(key);

        return string.IsNullOrWhiteSpace(shown) || shown == key ? key : shown;
    }
}
