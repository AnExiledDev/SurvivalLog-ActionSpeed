namespace ActionSpeed.Core;

/// <summary>
/// When a game hook goes in and when it does its work. A hook goes in the first time a setting needs it and then
/// stays: taking a hook back out of a game method leaves the game's call running through the same wrapper, so it would
/// save nothing. While no setting needs it, the hook returns at once.
/// </summary>
public sealed class HookSwitch
{
    public bool IsIn { get; private set; }

    /// <summary>A setting needs the hook; while this is false the hook returns before reading anything.</summary>
    public bool IsOn { get; private set; }

    /// <summary>Takes whether the settings need the hook; true when it should go in now.</summary>
    public bool Use(bool isNeeded)
    {
        IsOn = isNeeded;

        return isNeeded && !IsIn;
    }

    /// <summary>The hook went in. A hook that failed to go in is tried again the next time a setting needs it.</summary>
    public void Placed() => IsIn = true;
}
