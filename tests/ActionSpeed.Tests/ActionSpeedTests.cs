using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class DecideTests
{
    private const int Food = 1;
    private const int Rest = 2;
    private const int Other = 8;

    private static ActionRow Eat(float during = 1800f, bool paysOverTime = false) => new(500, "eat", Food, during, paysOverTime);

    private static SpeedSettings Settings(float foodSpeed = 2f, bool keepPaying = true, Dictionary<int, float>? overrides = null) =>
        new(new Dictionary<int, float> { [Food] = foodSpeed, [Other] = 3f }, overrides ?? new Dictionary<int, float>(), keepPaying);

    [Fact]
    public void Doubling_the_speed_halves_the_clock_an_action_costs()
    {
        var decision = ActionSpeeds.Decide(Eat(), Settings());

        Assert.Equal(900f, decision.During);
        Assert.Equal(Reason.Changed, decision.Reason);
    }

    [Fact]
    public void A_speed_below_one_makes_an_action_take_longer()
    {
        Assert.Equal(3600f, ActionSpeeds.Decide(Eat(), Settings(foodSpeed: 0.5f)).During);
    }

    [Fact]
    public void A_speed_of_one_leaves_the_game_duration()
    {
        Assert.Equal(new ActionDecision(1800f, Reason.GameSpeed), ActionSpeeds.Decide(Eat(), Settings(foodSpeed: 1f)));
    }

    [Fact]
    public void An_action_type_with_no_speed_set_keeps_the_game_duration()
    {
        Assert.Equal(new ActionDecision(600f, Reason.GameSpeed), ActionSpeeds.Decide(new ActionRow(7, "sleep", Rest, 600f, false), Settings()));
    }

    [Fact]
    public void An_action_timed_by_something_else_is_never_touched_even_when_overridden()
    {
        var crafting = new ActionRow(39, "craft", Other, -1f, false);
        var overrides = new Dictionary<int, float> { [39] = 5f };

        Assert.Equal(new ActionDecision(-1f, Reason.TimedElsewhere), ActionSpeeds.Decide(crafting, Settings(overrides: overrides)));
    }

    [Fact]
    public void An_action_that_pays_out_while_it_runs_keeps_its_duration_so_it_still_gives_what_it_gave()
    {
        Assert.Equal(new ActionDecision(1800f, Reason.PaysOverTime), ActionSpeeds.Decide(Eat(paysOverTime: true), Settings()));
    }

    [Fact]
    public void Turning_off_the_pay_out_rule_speeds_those_actions_up_too()
    {
        Assert.Equal(900f, ActionSpeeds.Decide(Eat(paysOverTime: true), Settings(keepPaying: false)).During);
    }

    [Fact]
    public void An_override_beats_both_the_type_speed_and_the_pay_out_rule()
    {
        var decision = ActionSpeeds.Decide(Eat(paysOverTime: true), Settings(overrides: new Dictionary<int, float> { [500] = 4f }));

        Assert.Equal(new ActionDecision(450f, Reason.Overridden), decision);
    }

    [Fact]
    public void An_override_of_one_puts_back_the_game_duration_for_a_single_action()
    {
        var decision = ActionSpeeds.Decide(Eat(), Settings(overrides: new Dictionary<int, float> { [500] = 1f }));

        Assert.Equal(1800f, decision.During);
    }
}

public class ClampTests
{
    [Theory]
    [InlineData(0f, ActionSpeeds.Slowest)]
    [InlineData(-3f, ActionSpeeds.Slowest)]
    [InlineData(500f, ActionSpeeds.Fastest)]
    [InlineData(2.5f, 2.5f)]
    [InlineData(float.NaN, 1f)]
    public void Action_speeds_stay_in_range_and_nonsense_means_the_game_speed(float wanted, float expected)
    {
        Assert.Equal(expected, ActionSpeeds.Clamp(wanted));
    }

    [Theory]
    [InlineData(0f, Walking.Slowest)]
    [InlineData(50f, Walking.Fastest)]
    [InlineData(1.5f, 1.5f)]
    [InlineData(float.PositiveInfinity, 1f)]
    public void Walking_speed_stays_in_range_and_nonsense_means_the_game_speed(float wanted, float expected)
    {
        Assert.Equal(expected, Walking.Clamp(wanted));
    }
}

public class OverrideTests
{
    [Fact]
    public void Overrides_are_id_and_speed_pairs_separated_by_commas()
    {
        var parsed = ActionSpeeds.ParseOverrides(" 112:3, 9075 : 2.5 ");

        Assert.Equal(new Dictionary<int, float> { [112] = 3f, [9075] = 2.5f }, parsed.Speeds);
        Assert.Empty(parsed.Rejected);
    }

    [Fact]
    public void An_empty_setting_overrides_nothing()
    {
        var parsed = ActionSpeeds.ParseOverrides("");

        Assert.Empty(parsed.Speeds);
        Assert.Empty(parsed.Rejected);
    }

    [Fact]
    public void A_malformed_entry_is_reported_and_the_rest_still_apply()
    {
        var parsed = ActionSpeeds.ParseOverrides("112:3, toilet:2, 40, 41:-1, 42:x");

        Assert.Equal(new Dictionary<int, float> { [112] = 3f }, parsed.Speeds);
        Assert.Equal(new[] { "toilet:2", "40", "41:-1", "42:x" }, parsed.Rejected);
    }

    [Fact]
    public void Override_speeds_are_held_to_the_same_range_as_type_speeds()
    {
        Assert.Equal(ActionSpeeds.Fastest, ActionSpeeds.ParseOverrides("7:1000").Speeds[7]);
    }

    [Fact]
    public void A_decimal_point_reads_the_same_whatever_the_system_language()
    {
        var previous = Thread.CurrentThread.CurrentCulture;

        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            Assert.Equal(1.5f, ActionSpeeds.ParseOverrides("7:1.5").Speeds[7]);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}

public class PaysOverTimeTests
{
    private static readonly Dictionary<int, float[]> Effects = new()
    {
        [10] = new[] { 0f, 0f, 0f },
        [11] = new[] { 0f, -5f, 0f },
        [12] = new[] { 2f, 0f, 0f },
    };

    [Fact]
    public void An_action_with_no_effect_pays_nothing_over_time()
    {
        Assert.False(ActionSpeeds.PaysOverTime(0, Effects));
    }

    [Fact]
    public void An_effect_with_only_start_and_end_values_does_not_pay_over_time()
    {
        Assert.False(ActionSpeeds.PaysOverTime(10, Effects));
    }

    [Fact]
    public void A_drain_while_running_counts_as_paying_over_time_as_much_as_a_gain()
    {
        Assert.True(ActionSpeeds.PaysOverTime(11, Effects));
        Assert.True(ActionSpeeds.PaysOverTime(12, Effects));
    }

    [Fact]
    public void An_effect_missing_from_the_table_is_assumed_to_pay_over_time_so_it_is_left_alone()
    {
        Assert.True(ActionSpeeds.PaysOverTime(99, Effects));
    }
}

public class DescribeTests
{
    [Fact]
    public void The_action_list_shows_the_game_and_new_clock_cost_in_minutes()
    {
        var row = new ActionRow(112, "sit down", 8, 1800f, false);

        Assert.Equal("112 | sit down | Other | 30.0 min -> 15.0 min | Changed", ActionSpeeds.Describe(row, new ActionDecision(900f, Reason.Changed), "Other"));
    }

    [Fact]
    public void An_action_timed_elsewhere_is_listed_without_minutes()
    {
        var row = new ActionRow(39, "craft", 8, -1f, false);

        Assert.Equal("39 | craft | Other | not timed by this table | TimedElsewhere", ActionSpeeds.Describe(row, new ActionDecision(-1f, Reason.TimedElsewhere), "Other"));
    }
}
