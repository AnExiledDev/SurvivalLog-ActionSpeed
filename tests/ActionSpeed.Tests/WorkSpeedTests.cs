using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class WorkSpeedTests
{
    private const int UseToilet = 112;
    private const int Sleep = 113;
    private const int Search = 1919;

    private static readonly CharacterSpeeds Workers = new(Player: 2f, Robot: 3f, Survivor: 4f);

    private static EffectClock WithWorkers(CharacterSpeeds workers)
    {
        var clock = new EffectClock();

        clock.Publish(new Dictionary<int, float> { [UseToilet] = 5f });
        clock.PublishSearches(new HashSet<int> { Search });
        clock.PublishWorkSpeeds(workers);

        return clock;
    }

    private static Effect Started(int action, Character who) => new(action, who);

    [Theory]
    [InlineData(Character.Player, 2f)]
    [InlineData(Character.Robot, 3f)]
    [InlineData(Character.Survivor, 4f)]
    public void Each_kind_of_character_does_an_action_at_its_own_work_speed(Character who, float expected)
    {
        Assert.Equal(expected, WithWorkers(Workers).ClockOf(Started(Sleep, who)));
    }

    [Fact]
    public void A_work_speed_adds_to_the_actions_own_speed()
    {
        Assert.Equal(10f, WithWorkers(Workers).ClockOf(Started(UseToilet, Character.Player)));
    }

    [Fact]
    public void Raiders_and_creatures_keep_the_game_pace_whatever_the_work_speeds()
    {
        Assert.Equal(1f, WithWorkers(Workers).ClockOf(Started(Sleep, Character.Other)));
        Assert.Equal(5f, WithWorkers(Workers).ClockOf(Started(UseToilet, Character.Other)));
    }

    [Fact]
    public void A_work_speed_alone_speeds_up_actions_when_no_action_speed_is_set()
    {
        var clock = new EffectClock();

        clock.PublishWorkSpeeds(Workers);

        Assert.Equal(4f, clock.ClockOf(Started(Sleep, Character.Survivor)));
    }

    [Fact]
    public void A_second_search_stays_instant_whoever_does_it()
    {
        var clock = WithWorkers(Workers);

        Assert.Equal(EffectClock.Instant, clock.ClockOf(Started(Search, Character.Robot) with { IsTargetSearched = true }));
    }

    [Fact]
    public void The_progress_bar_follows_the_work_speed()
    {
        Assert.Equal(3f, WithWorkers(Workers).ClockOfAction(Sleep, new Worker(Character.Robot)));
    }

    [Fact]
    public void At_the_game_pace_the_owner_is_never_looked_up()
    {
        var clock = WithWorkers(new CharacterSpeeds(1f, 1f, 1f));

        Assert.Equal(5f, clock.ClockOfAction(UseToilet, new Worker(Who: null)));
    }

    private readonly record struct Effect(int Action, Character Who, bool IsTargetSearched = false) : IRunningEffect
    {
        public bool IsActionEffect => true;

        public float FullLife => 100f;

        public float LifeLeft => 100f;

        public long EffectId => 7;

        public long WorldId => 1;

        public int? OwnersActionId() => Action;

        public int TimeScale() => 60;

        public bool IsOwnersTargetSearched() => IsTargetSearched;

        public Character Owner() => Who;
    }

    /// <summary>Whose progress bar it is; a null worker fails when asked, for what must not look the owner up.</summary>
    private readonly record struct Worker(Character? Who) : IEffectOwner
    {
        public bool IsOwnersTargetSearched() => false;

        public Character Owner() => Who ?? throw new InvalidOperationException("owner looked up");
    }
}
