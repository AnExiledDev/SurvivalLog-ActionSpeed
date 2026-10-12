using System;
using ActionSpeed.Core;

namespace ActionSpeed.Adapters;

/// <summary>Logs each distinct failure inside one game hook once; a hook that fails on every call stays one line.</summary>
internal sealed class HookFailures
{
    private readonly DistinctFailures _seen = new();
    private readonly Action<string> _log;
    private readonly string _what;

    public HookFailures(Action<string> log, string what)
    {
        _log = log;
        _what = what;
    }

    public void Report(Exception ex)
    {
        if (_seen.IsNew($"{ex.GetType().FullName}: {ex.Message}")) _log($"{_what} failed, it keeps the game's duration: {ex}");
    }
}
