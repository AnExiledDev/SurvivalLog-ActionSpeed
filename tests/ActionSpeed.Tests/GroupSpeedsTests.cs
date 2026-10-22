using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class GroupSpeedsTests
{
    private static IReadOnlyDictionary<Group, float> Resolve(float everything, Dictionary<string, float>? sections = null, Dictionary<Group, float>? groups = null) =>
        GroupSpeeds.Resolve(new ConfiguredSpeeds(everything, sections ?? new Dictionary<string, float>(), groups ?? new Dictionary<Group, float>()));

    [Fact]
    public void A_group_set_on_its_own_beats_its_section_and_the_everything_speed()
    {
        var speeds = Resolve(2f, new Dictionary<string, float> { ["Leisure"] = 3f }, new Dictionary<Group, float> { [Group.Reading] = 10f });

        Assert.Equal(10f, speeds[Group.Reading]);
        Assert.Equal(3f, speeds[Group.Music]);
        Assert.Equal(2f, speeds[Group.Sleep]);
    }

    [Fact]
    public void A_group_left_at_zero_follows_its_section_and_a_section_left_at_zero_follows_everything()
    {
        var speeds = Resolve(4f, new Dictionary<string, float> { ["Rest"] = 0f, ["Eating"] = 2f }, new Dictionary<Group, float> { [Group.Sleep] = 0f, [Group.Snacks] = 0f });

        Assert.Equal(4f, speeds[Group.Sleep]);
        Assert.Equal(2f, speeds[Group.Snacks]);
    }

    [Fact]
    public void Other_follows_everything_directly_because_it_belongs_to_no_section()
    {
        var speeds = Resolve(3f, new Dictionary<string, float> { ["Chores"] = 5f });

        Assert.Equal(3f, speeds[Group.Other]);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void An_everything_speed_of_zero_or_less_or_nonsense_is_the_game_pace(float everything)
    {
        Assert.All(Resolve(everything).Values, speed => Assert.Equal(1f, speed));
    }

    [Fact]
    public void Every_group_gets_a_speed_held_to_the_range()
    {
        var speeds = Resolve(500f, groups: new Dictionary<Group, float> { [Group.Nap] = 0.01f });

        Assert.Equal(Enum.GetValues<Group>().Length, speeds.Count);
        Assert.Equal(ActionSpeeds.Slowest, speeds[Group.Nap]);
        Assert.Equal(ActionSpeeds.Fastest, speeds[Group.Meals]);
    }
}
