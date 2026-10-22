using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class LegacySettingsTests
{
    /// <summary>A settings file from 0.7, with comments and the other sections around [Actions].</summary>
    private const string OldFile = """
        ## Settings file was created by plugin Action Speed v0.7.1

        [Actions]

        ## Speed of furniture actions.
        # Setting type: Single
        # Default value: 1
        FurnitureUse = 2
        Food = 2
        Rest = 1
        Entertainment = 1
        Exercise = 1
        Social = 1
        Treatment = 2
        Move = 1
        Other = 2
        Free = 1
        RepairFurniture = 1
        FullBenefits = true
        Overrides =
        Reading = 10
        FreeSecondSearch = true

        [Speed]

        Crafting = 1
        Brewing = 1
        Cooking = 1
        """;

    private static IReadOnlyDictionary<Group, float> Resolved(Migration migration) => GroupSpeeds.Resolve(migration.Speeds);

    [Fact]
    public void An_old_file_is_found_and_its_action_values_are_read()
    {
        var found = LegacySettings.Find(OldFile);

        Assert.NotNull(found);
        Assert.Equal("2", found[LegacySettings.FurnitureUse]);
        Assert.Equal("10", found[LegacySettings.Reading]);
        Assert.False(found.ContainsKey("Crafting"));
    }

    [Fact]
    public void A_file_already_moved_is_never_moved_again_even_if_old_keys_were_left_behind()
    {
        Assert.Null(LegacySettings.Find(OldFile.Replace("FullBenefits = true", "Everything = 3\nFullBenefits = true")));
    }

    [Fact]
    public void A_new_or_missing_file_has_nothing_to_move()
    {
        Assert.Null(LegacySettings.Find(""));
        Assert.Null(LegacySettings.Find("[Actions]\nEverything = 1\nOther = 0\n[Speed]\nFood = 2\n"));
    }

    [Fact]
    public void Every_group_keeps_the_speed_its_actions_had_before()
    {
        var resolved = Resolved(LegacySettings.Migrate(LegacySettings.Find(OldFile)!));

        Assert.Equal(10f, resolved[Group.Reading]);
        Assert.Equal(2f, resolved[Group.Meals]);
        Assert.Equal(2f, resolved[Group.Medicine]);
        Assert.Equal(2f, resolved[Group.Unpacking]);
        Assert.Equal(1f, resolved[Group.Cleaning]);
        Assert.Equal(1f, resolved[Group.Sleep]);
        Assert.Equal(1f, resolved[Group.Repairs]);
        Assert.Equal(2f, resolved[Group.Projects]);
        Assert.Equal(2f, resolved[Group.Searching]);
        Assert.Equal(2f, resolved[Group.Other]);
    }

    [Fact]
    public void The_moved_file_shows_only_the_real_differences()
    {
        var speeds = LegacySettings.Migrate(LegacySettings.Find(OldFile)!).Speeds;
        var setSections = speeds.Sections.Where(pair => pair.Value > 0f).ToDictionary(pair => pair.Key, pair => pair.Value);
        var setGroups = speeds.Groups.Where(pair => pair.Value > 0f).ToDictionary(pair => pair.Key, pair => pair.Value);

        Assert.Equal(2f, speeds.Everything);
        Assert.Equal(new Dictionary<string, float> { ["Rest"] = 1f, ["Leisure"] = 1f, ["Building"] = 1f, ["Defense"] = 1f }, setSections);
        Assert.Equal(new Dictionary<Group, float> { [Group.Reading] = 10f, [Group.Cleaning] = 1f, [Group.Power] = 1f, [Group.Projects] = 2f }, setGroups);
    }

    [Theory]
    [InlineData("Food = 3", 3f)]
    [InlineData("Food = 0", ActionSpeeds.Slowest)]
    [InlineData("Food = 500", ActionSpeeds.Fastest)]
    [InlineData("Food = fast", 1f)]
    public void An_old_speed_moves_as_the_old_version_read_it(string line, float expected)
    {
        var resolved = Resolved(LegacySettings.Migrate(LegacySettings.Find($"[Actions]\n{line}\n")!));

        Assert.Equal(expected, resolved[Group.Snacks]);
        Assert.Equal(1f, resolved[Group.Sleep]);
    }

    [Fact]
    public void An_old_reading_setting_of_zero_left_reading_at_its_type_speed()
    {
        var resolved = Resolved(LegacySettings.Migrate(LegacySettings.Find("[Actions]\nEntertainment = 3\nReading = 0\n")!));

        Assert.Equal(3f, resolved[Group.Reading]);
    }

    [Fact]
    public void An_old_file_with_every_type_at_one_moves_to_an_untouched_new_file()
    {
        var speeds = LegacySettings.Migrate(LegacySettings.Find("[Actions]\nFood = 1\nRest = 1\nReading = 0\n")!).Speeds;

        Assert.Equal(1f, speeds.Everything);
        Assert.All(speeds.Sections.Values, speed => Assert.Equal(0f, speed));
        Assert.All(speeds.Groups.Values, speed => Assert.Equal(0f, speed));
    }

    [Fact]
    public void The_old_type_keys_are_retired_but_other_keeps_its_place()
    {
        var migration = LegacySettings.Migrate(LegacySettings.Find(OldFile)!);

        Assert.Contains(LegacySettings.Move, migration.RetiredKeys);
        Assert.Contains(LegacySettings.Reading, migration.RetiredKeys);
        Assert.DoesNotContain(LegacySettings.Other, migration.RetiredKeys);
        Assert.True(LegacySettings.IsRetiredKey(LegacySettings.Free));
        Assert.False(LegacySettings.IsRetiredKey(LegacySettings.Other));
        Assert.False(LegacySettings.IsRetiredKey("FullBenefits"));
    }

    [Fact]
    public void A_decimal_speed_reads_the_same_whatever_the_system_language()
    {
        var previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            Assert.Equal(1.5f, Resolved(LegacySettings.Migrate(LegacySettings.Find("[Actions]\nFood = 1.5\n")!))[Group.Meals]);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
