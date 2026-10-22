using System;
using System.Collections.Generic;
using System.Linq;

namespace ActionSpeed.Core;

/// <summary>The speeds as written in the settings, where 0 or less means "follow the speed above".</summary>
/// <param name="Everything">The speed every action follows unless its section or group says otherwise.</param>
/// <param name="Sections">Each section's umbrella speed, by section name.</param>
/// <param name="Groups">Each group's own speed.</param>
public sealed record ConfiguredSpeeds(float Everything, IReadOnlyDictionary<string, float> Sections, IReadOnlyDictionary<Group, float> Groups);

public static class GroupSpeeds
{
    /// <summary>
    /// The speed each group runs at: its own speed when set, else its section's, else the everything speed. A
    /// player who wants one kind of work faster sets one line, and a whole section or the whole game is one line too.
    /// </summary>
    public static IReadOnlyDictionary<Group, float> Resolve(ConfiguredSpeeds configured)
    {
        var everything = ActionSpeeds.Follow(configured.Everything) ?? 1f;

        return Enum.GetValues<Group>().ToDictionary(group => group, group => Own(configured, group) ?? OfSection(configured, group) ?? everything);
    }

    private static float? Own(ConfiguredSpeeds configured, Group group) =>
        configured.Groups.TryGetValue(group, out var speed) ? ActionSpeeds.Follow(speed) : null;

    private static float? OfSection(ConfiguredSpeeds configured, Group group) =>
        configured.Sections.TryGetValue(ActionGroups.SectionOf(group), out var speed) ? ActionSpeeds.Follow(speed) : null;
}
