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

    private readonly record struct FakeEffect(bool IsActionEffect, float FullLife, float LifeLeft, long EffectId, int? OwnersAction, bool IsTargetSearched = false) : IRunningEffect
    {
        public int? OwnersActionId() => OwnersAction;

        public bool IsOwnersTargetSearched() => IsTargetSearched;
    }

    private const int Search = 1919;

    private static EffectClock WithSearches()
    {
        var clock = new EffectClock();

        clock.Publish(new Dictionary<int, float> { [Search] = 2f, [UseToilet] = 4f });
        clock.PublishSearches(new HashSet<int> { Search });

        return clock;
    }

    [Fact]
    public void Searching_a_container_searched_before_ends_at_once()
    {
        Assert.Equal(EffectClock.Instant, WithSearches().ClockOf(Started(7, Search) with { IsTargetSearched = true }));
    }

    [Fact]
    public void The_first_search_of_a_container_takes_the_searchs_own_time()
    {
        Assert.Equal(2f, WithSearches().ClockOf(Started(7, Search)));
    }

    [Fact]
    public void An_action_that_is_not_a_search_keeps_its_clock_on_a_searched_container()
    {
        Assert.Equal(4f, WithSearches().ClockOf(Started(7, UseToilet) with { IsTargetSearched = true }));
    }

    [Fact]
    public void A_second_search_is_free_even_when_no_action_is_sped_up()
    {
        var clock = new EffectClock();

        clock.PublishSearches(new HashSet<int> { Search });

        Assert.Equal(EffectClock.Instant, clock.ClockOf(Started(7, Search) with { IsTargetSearched = true }));
    }

    [Fact]
    public void The_bar_of_a_second_search_is_shortened_with_it()
    {
        Assert.True(EffectClock.BarSeconds(60f, WithSearches().ClockOfAction(Search, () => true)) < 0.1f);
    }

    [Theory]
    [InlineData(EffectClock.OpenBagEffect, 60f, true)]
    [InlineData(EffectClock.OpenBagEffect, -1f, false)]
    [InlineData(8, 60f, false)]
    public void Only_a_timed_action_that_opens_a_bag_is_a_search(int effectId, float gameDuring, bool isSearch)
    {
        Assert.Equal(isSearch, EffectClock.IsSearch(effectId, gameDuring));
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

        Assert.Equal(3f, clock.ClockOfAction(7, () => false));
        Assert.Equal(1f, clock.ClockOfAction(8, () => false));
        Assert.Equal(1f, clock.ClockOfAction(null, () => false));
    }
}
