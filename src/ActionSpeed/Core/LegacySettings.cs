using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ActionSpeed.Core;

/// <param name="Speeds">The new settings that give every group the speed it ran at before.</param>
/// <param name="RetiredKeys">The [Actions] keys of the old file that no longer exist, to be removed from it.</param>
public sealed record Migration(ConfiguredSpeeds Speeds, IReadOnlyList<string> RetiredKeys);

/// <summary>
/// Moves a settings file from one speed per game ActionType (and one reading speed) to the group settings. Each group
/// takes the old speed of the type most of its actions had, so a player's file keeps doing what it did. The values
/// are then lifted as high as they can go: the speed most groups share becomes the everything speed, and the speed
/// most groups of a section share becomes that section's, so the file shows only the real differences.
/// </summary>
public static class LegacySettings
{
    public const string FurnitureUse = "FurnitureUse";
    public const string Food = "Food";
    public const string Rest = "Rest";
    public const string Entertainment = "Entertainment";
    public const string Exercise = "Exercise";
    public const string Social = "Social";
    public const string Treatment = "Treatment";
    public const string Move = "Move";
    public const string Other = "Other";
    public const string Free = "Free";
    public const string RepairFurniture = "RepairFurniture";
    public const string Reading = "Reading";

    /// <summary>The key only the new settings have; a file that has it was already moved.</summary>
    public const string EverythingKey = "Everything";

    /// <summary>Old [Actions] keys with no place in the new settings. Other is not among them: the new [Actions] keeps it.</summary>
    private static readonly string[] RetiredActionKeys = { FurnitureUse, Food, Rest, Entertainment, Exercise, Social, Treatment, Move, Free, RepairFurniture, Reading };

    private static readonly Dictionary<Group, string> TypeOfGroup = new()
    {
        [Group.Meals] = Food,
        [Group.Snacks] = Food,
        [Group.Drinks] = Food,
        [Group.Medicine] = Treatment,
        [Group.Sickness] = Other,
        [Group.Moods] = Other,
        [Group.Toilet] = Other,
        [Group.Washing] = Other,
        [Group.Sleep] = Rest,
        [Group.Nap] = Rest,
        [Group.Reading] = Entertainment,
        [Group.Games] = Entertainment,
        [Group.Music] = Entertainment,
        [Group.Dancing] = Entertainment,
        [Group.QuietTime] = Social,
        [Group.Relaxing] = Entertainment,
        [Group.Workouts] = Exercise,
        [Group.Cleaning] = Exercise,
        [Group.Organizing] = Other,
        [Group.BoilingWater] = Other,
        [Group.Power] = Exercise,
        [Group.Unpacking] = FurnitureUse,
        [Group.Salvaging] = FurnitureUse,
        [Group.Planting] = Other,
        [Group.Watering] = Other,
        [Group.Harvesting] = Other,
        [Group.Tending] = Other,
        [Group.Repairs] = RepairFurniture,
        [Group.Reinforcing] = RepairFurniture,
        [Group.Furnishing] = RepairFurniture,
        [Group.Demolishing] = RepairFurniture,
        [Group.Projects] = FurnitureUse,
        [Group.Searching] = Other,
        [Group.BreakingIn] = Other,
        [Group.Inspecting] = Other,
        [Group.Drones] = Other,
        [Group.Traps] = RepairFurniture,
        [Group.Throwing] = RepairFurniture,
        [Group.Other] = Other,
    };

    /// <summary>Whether an [Actions] key belonged to the old settings only, so a file still carrying it can drop it.</summary>
    public static bool IsRetiredKey(string key) => RetiredActionKeys.Contains(key);

    /// <summary>The old ActionType whose speed a group takes over.</summary>
    public static string TypeOf(Group group) => TypeOfGroup[group];

    /// <summary>
    /// The [Actions] values of a settings file that still has the old keys, or null when there is nothing to move:
    /// no old keys, or the file already has the new ones (so a move that could not tidy the file never runs twice).
    /// </summary>
    public static IReadOnlyDictionary<string, string>? Find(string settingsText)
    {
        var actions = SectionValues(settingsText, ActionGroups.ActionsSection);
        var hasOldKeys = RetiredActionKeys.Any(actions.ContainsKey);

        return hasOldKeys && !actions.ContainsKey(EverythingKey) ? actions : null;
    }

    public static Migration Migrate(IReadOnlyDictionary<string, string> oldActions)
    {
        var wanted = Enum.GetValues<Group>().ToDictionary(group => group, group => OldSpeed(oldActions, group));
        var everything = MostShared(wanted.Values, preferred: 1f);
        var sections = new Dictionary<string, float>();
        var groups = new Dictionary<Group, float>();

        foreach (var section in ActionGroups.Sections)
        {
            var shared = MostShared(section.Groups.Select(group => wanted[group]), preferred: everything);

            sections[section.Name] = shared == everything ? 0f : shared;
        }

        foreach (var (group, speed) in wanted)
        {
            var inherited = sections.TryGetValue(ActionGroups.SectionOf(group), out var sectionSpeed) && sectionSpeed > 0f ? sectionSpeed : everything;

            groups[group] = speed == inherited ? 0f : speed;
        }

        var retired = RetiredActionKeys.Where(oldActions.ContainsKey).ToList();

        return new Migration(new ConfiguredSpeeds(everything, sections, groups), retired);
    }

    /// <summary>What a group ran at before. Reading had its own speed, and 0 there meant "the book's type".</summary>
    private static float OldSpeed(IReadOnlyDictionary<string, string> oldActions, Group group)
    {
        if (group == Group.Reading && ActionSpeeds.Follow(Parse(oldActions, Reading, 0f)) is { } reading) return reading;

        return ActionSpeeds.Clamp(Parse(oldActions, TypeOf(group), 1f));
    }

    /// <summary>The speed most values share; a tie goes to the preferred speed, then to the game's pace, then to the slowest.</summary>
    private static float MostShared(IEnumerable<float> speeds, float preferred)
    {
        var counts = speeds.GroupBy(speed => speed).ToDictionary(group => group.Key, group => group.Count());
        var most = counts.Values.Max();
        var tied = counts.Where(pair => pair.Value == most).Select(pair => pair.Key).ToList();

        if (tied.Contains(preferred)) return preferred;

        return tied.Contains(1f) ? 1f : tied.Min();
    }

    private static float Parse(IReadOnlyDictionary<string, string> values, string key, float missing) =>
        values.TryGetValue(key, out var text) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : missing;

    /// <summary>The key = value lines of one [section] of a BepInEx settings file.</summary>
    private static Dictionary<string, string> SectionValues(string settingsText, string section)
    {
        var values = new Dictionary<string, string>();
        var current = "";

        foreach (var rawLine in settingsText.Split('\n'))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#')) continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = line[1..^1].Trim();

                continue;
            }

            var equals = line.IndexOf('=');

            if (current == section && equals > 0) values[line[..equals].Trim()] = line[(equals + 1)..].Trim();
        }

        return values;
    }
}
