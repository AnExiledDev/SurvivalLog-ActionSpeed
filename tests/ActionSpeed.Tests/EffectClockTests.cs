using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class EffectClockTests
{
    private const int UseToilet = 112;
    private const int Sleep = 113;

    private static EffectClock Published()
    {
        var clock = new EffectClock();

        clock.Publish(new Dictionary<int, float> { [UseToilet] = 4f, [Sleep] = 2f });

        return clock;
    }

    private static FakeEffect Started(long id, int? ownersAction) => new(IsActionEffect: true, FullLife: 100f, LifeLeft: 100f, EffectId: id, OwnersAction: ownersAction);

    [Fact]
    public void A_sped_up_actions_effect_runs_on_the_actions_clock()
    {
        Assert.Equal(4f, Published().ClockOf(Started(7, UseToilet)));
    }

    [Fact]
    public void An_effect_not_started_by_an_action_keeps_the_game_clock()
    {
        Assert.Equal(1f, Published().ClockOf(Started(7, UseToilet) with { IsActionEffect = false }));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(0f)]
    public void An_effect_with_no_length_to_finish_sooner_keeps_the_game_clock(float fullLife)
    {
        Assert.Equal(1f, Published().ClockOf(Started(7, UseToilet) with { FullLife = fullLife, LifeLeft = fullLife }));
    }

    [Fact]
    public void A_running_effect_keeps_its_clock_after_its_owner_starts_another_action()
    {
        var clock = Published();

        clock.ClockOf(Started(7, UseToilet));

        Assert.Equal(4f, clock.ClockOf(Started(7, ownersAction: null) with { LifeLeft = 50f }));
    }

    [Fact]
    public void A_new_effect_reusing_an_ended_effects_id_takes_its_own_actions_clock()
    {
        var clock = Published();

        clock.ClockOf(Started(7, UseToilet));

        Assert.Equal(2f, clock.ClockOf(Started(7, Sleep)));
    }

    [Fact]
    public void An_effect_whose_owner_cannot_be_found_keeps_the_game_clock()
    {
        Assert.Equal(1f, Published().ClockOf(Started(7, ownersAction: null)));
    }

    [Fact]
    public void A_step_longer_than_the_shortest_payout_period_is_cut_to_it()
    {
        Assert.Equal(7f, EffectClock.Step(gameStep: 1f, clock: 20f, longestStep: 7f, lifeLeft: 1000f));
    }

    [Fact]
    public void The_last_step_stops_at_the_end_of_the_effect()
    {
        Assert.Equal(3f, EffectClock.Step(gameStep: 1f, clock: 20f, longestStep: 7f, lifeLeft: 3f));
    }

    [Fact]
    public void A_sped_up_step_is_never_shorter_than_the_games_own()
    {
        Assert.Equal(10f, EffectClock.Step(gameStep: 10f, clock: 20f, longestStep: 7f, lifeLeft: 1000f));
    }

    [Fact]
    public void A_slowed_effect_keeps_its_slower_step()
    {
        Assert.Equal(0.25f, EffectClock.Step(gameStep: 1f, clock: 0.25f, longestStep: 7f, lifeLeft: 1000f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(float.NaN)]
    public void With_no_known_payout_period_a_step_is_only_cut_at_the_end(float longestStep)
    {
        Assert.Equal(20f, EffectClock.Step(gameStep: 1f, clock: 20f, longestStep: longestStep, lifeLeft: 1000f));
    }

    [Fact]
    public void The_shortest_payout_period_is_counted_in_the_games_scaled_time()
    {
        Assert.Equal(6f, EffectClock.LongestStep(timeScale: 60));
        Assert.Equal(float.PositiveInfinity, EffectClock.LongestStep(timeScale: 0));
    }

    private readonly record struct FakeEffect(bool IsActionEffect, float FullLife, float LifeLeft, long EffectId, int? OwnersAction) : IRunningEffect
    {
        public int? OwnersActionId() => OwnersAction;
    }

    [Fact]
    public void A_bar_for_an_action_four_times_as_fast_runs_a_quarter_as_long()
    {
        Assert.Equal(10f, EffectClock.BarSeconds(40f, 4f));
    }

    [Fact]
    public void A_bar_for_an_action_on_the_game_clock_keeps_its_length()
    {
        Assert.Equal(40f, EffectClock.BarSeconds(40f, 1f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-2f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void A_clock_that_makes_no_sense_leaves_the_bar_alone(float clock)
    {
        Assert.Equal(40f, EffectClock.BarSeconds(40f, clock));
    }

    [Fact]
    public void The_clock_published_for_an_action_is_the_one_its_bar_uses()
    {
        var clock = new EffectClock();

        clock.Publish(new Dictionary<int, float> { [7] = 3f });

        Assert.Equal(3f, clock.ClockOfAction(7));
        Assert.Equal(1f, clock.ClockOfAction(8));
        Assert.Equal(1f, clock.ClockOfAction(null));
    }
}
