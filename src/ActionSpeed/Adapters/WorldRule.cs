using BepInEx.Configuration;

namespace ActionSpeed.Adapters;

/// <summary>
/// With Exiled's Toolkit 0.17.0 or newer, a setting with this tag can be set per world (Customizable Difficulty picks it
/// for a new world). Every tagged setting must be watched, or the Toolkit leaves it out.
/// </summary>
internal static class WorldRule
{
    private const string Tag = "ModManager.WorldRule";

    public static ConfigDescription Rule(string description) => new(description, null, Tag);
}
