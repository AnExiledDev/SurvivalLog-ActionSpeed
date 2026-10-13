using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class DecideTests
{
    private const int Food = 1;
    private const int Rest = 2;
    private const int Other = 8;

    private static ActionRow Eat(float during = 1800f) => new(500, "eat", Food, during);

    private static SpeedSettings Settings(float foodSpeed = 2f, bool fullBenefits = true, Dictionary<int, float>? overrides = null) =>
        new(new Dictionary<int, float> { [Food] = foodSpeed, [Other] = 3f }, overrides ?? new Dictionary<int, float>(), fullBenefits);

    [Fact]
    public void With_full_benefits_a_doubled_speed_keeps_the_table_duration_and_runs_the_effect_twice_as_fast()
    {
        var decision = ActionSpeeds.Decide(Eat(), Settings());

        Assert.Equal(new ActionDecision(1800f, 2f, Reason.Changed), decision);
        Assert.Equal(900f, decision.Lasts);
    }

    [Fact]
    public void Without_full_benefits_a_doubled_speed_halves_the_table_duration_on_the_game_clock()
    {
        Assert.Equal(new ActionDecision(900f, 1f, Reason.Changed), ActionSpeeds.Decide(Eat(), Settings(fullBenefits: false)));
    }

    [Fact]
    public void A_speed_below_one_makes_an_action_take_longer()
    {
        Assert.Equal(3600f, ActionSpeeds.Decide(Eat(), Settings(foodSpeed: 0.5f)).Lasts);
        Assert.Equal(3600f, ActionSpeeds.Decide(Eat(), Settings(foodSpeed: 0.5f, fullBenefits: false)).Lasts);
    }

    [Fact]
    public void A_speed_of_one_leaves_the_game_duration_and_clock()
    {
        Assert.Equal(new ActionDecision(1800f, 1f, Reason.GameSpeed), ActionSpeeds.Decide(Eat(), Settings(foodSpeed: 1f)));
    }

    [Fact]
    public void An_action_type_with_no_speed_set_keeps_the_game_duration()
    {
        Assert.Equal(new ActionDecision(600f, 1f, Reason.GameSpeed), ActionSpeeds.Decide(new ActionRow(7, "sleep", Rest, 600f), Settings()));
    }

    [Fact]
    public void An_action_timed_by_something_else_is_never_touched_even_when_overridden()
    {
        var crafting = new ActionRow(39, "craft", Other, -1f);
        var overrides = new Dictionary<int, float> { [39] = 5f };

        Assert.Equal(new ActionDecision(-1f, 1f, Reason.TimedElsewhere), ActionSpeeds.Decide(crafting, Settings(overrides: overrides)));
    }

    [Theory]
    [InlineData(true, 1800f, 4f)]
    [InlineData(false, 450f, 1f)]
    public void An_override_beats_the_type_speed(bool fullBenefits, float during, float clock)
    {
        var decision = ActionSpeeds.Decide(Eat(), Settings(fullBenefits: fullBenefits, overrides: new Dictionary<int, float> { [500] = 4f }));

        Assert.Equal(new ActionDecision(during, clock, Reason.Overridden), decision);
    }

    [Fact]
    public void An_override_of_one_puts_back_the_game_pace_for_a_single_action()
    {
        var decision = ActionSpeeds.Decide(Eat(), Settings(overrides: new Dictionary<int, float> { [500] = 1f }));

        Assert.Equal(1800f, decision.During);
        Assert.Equal(1f, decision.Clock);
    }
}

public class NeedsClockTests
{
    private static SpeedSettings Settings(float typeSpeed, bool fullBenefits, float? overrideSpeed = null) =>
        new(new Dictionary<int, float> { [1] = typeSpeed },
            overrideSpeed is { } speed ? new Dictionary<int, float> { [112] = speed } : new Dictionary<int, float>(),
            fullBenefits);

    [Fact]
    public void Effects_need_a_faster_clock_when_full_benefits_is_on_and_a_type_is_sped_up()
    {
        Assert.True(ActionSpeeds.NeedsClock(Settings(2f, fullBenefits: true)));
    }

    [Fact]
    public void An_override_alone_is_enough_to_need_the_clock()
    {
        Assert.True(ActionSpeeds.NeedsClock(Settings(1f, fullBenefits: true, overrideSpeed: 3f)));
    }

    [Fact]
    public void Without_full_benefits_or_with_every_speed_at_one_no_effect_clock_changes()
    {
        Assert.False(ActionSpeeds.NeedsClock(Settings(2f, fullBenefits: false, overrideSpeed: 3f)));
        Assert.False(ActionSpeeds.NeedsClock(Settings(1f, fullBenefits: true, overrideSpeed: 1f)));
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

public class DescribeTests
{
    [Fact]
    public void The_action_list_shows_how_long_an_action_lasts_and_that_it_keeps_its_full_benefits()
    {
        var row = new ActionRow(112, "sit down", 8, 1800f);

        Assert.Equal("112 | sit down | Other | 30.0 min -> 15.0 min, full benefits | Changed", ActionSpeeds.Describe(row, new ActionDecision(1800f, 2f, Reason.Changed), "Other"));
    }

    [Fact]
    public void An_action_cut_short_is_listed_without_full_benefits()
    {
        var row = new ActionRow(112, "sit down", 8, 1800f);

        Assert.Equal("112 | sit down | Other | 30.0 min -> 15.0 min | Changed", ActionSpeeds.Describe(row, new ActionDecision(900f, 1f, Reason.Changed), "Other"));
    }

    [Fact]
    public void An_action_timed_elsewhere_is_listed_without_minutes()
    {
        var row = new ActionRow(39, "craft", 8, -1f);

        Assert.Equal("39 | craft | Other | not timed by this table | TimedElsewhere", ActionSpeeds.Describe(row, new ActionDecision(-1f, 1f, Reason.TimedElsewhere), "Other"));
    }
}
