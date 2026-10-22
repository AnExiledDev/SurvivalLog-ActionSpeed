using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class WalkingTests
{
    private static readonly CharacterSpeeds Speeds = new(Player: 2f, Robot: 3f, Survivor: 4f);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void The_character_you_control_is_the_player_whatever_else_it_is(bool isBaseResident, bool isRobot)
    {
        Assert.Equal(Character.Player, Walking.Classify(isLeadingRole: true, isBaseResident, isRobot));
    }

    [Fact]
    public void A_base_resident_flagged_as_a_robot_is_the_robot()
    {
        Assert.Equal(Character.Robot, Walking.Classify(isLeadingRole: false, isBaseResident: true, isRobot: true));
    }

    [Fact]
    public void Any_other_base_resident_is_a_survivor()
    {
        Assert.Equal(Character.Survivor, Walking.Classify(isLeadingRole: false, isBaseResident: true, isRobot: false));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_character_who_does_not_live_at_the_base_is_never_sped_up(bool isRobot)
    {
        var walker = Walking.Classify(isLeadingRole: false, isBaseResident: false, isRobot);

        Assert.Equal(Character.Other, walker);
        Assert.Equal(1f, Walking.SpeedFor(walker, Speeds));
    }

    [Theory]
    [InlineData(Character.Player, 2f)]
    [InlineData(Character.Robot, 3f)]
    [InlineData(Character.Survivor, 4f)]
    public void Each_kind_of_walker_gets_its_own_speed(Character walker, float expected)
    {
        Assert.Equal(expected, Walking.SpeedFor(walker, Speeds));
    }

    [Fact]
    public void Walking_is_left_alone_only_when_every_speed_is_the_game_speed()
    {
        Assert.True(new CharacterSpeeds(1f, 1f, 1f).IsGamePace);
        Assert.False(new CharacterSpeeds(1f, 1.5f, 1f).IsGamePace);
        Assert.False(new CharacterSpeeds(1f, 1f, 1.5f).IsGamePace);
    }
}
