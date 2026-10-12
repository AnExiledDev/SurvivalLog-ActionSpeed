using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class KindSpeedTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(-2f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void A_speed_that_is_not_above_zero_falls_back_to_the_game_pace(float requested)
    {
        var speed = KindSpeed.From(requested);

        Assert.Equal(1f, speed.Speed);
        Assert.Equal(SpeedVerdict.Rejected, speed.Verdict);
        Assert.True(speed.IsGamePace);
    }

    [Theory]
    [InlineData(500f, 20f)]
    [InlineData(0.01f, 0.25f)]
    public void A_speed_out_of_range_is_pulled_to_the_nearest_end(float requested, float expected)
    {
        var speed = KindSpeed.From(requested);

        Assert.Equal(expected, speed.Speed);
        Assert.Equal(SpeedVerdict.Clamped, speed.Verdict);
    }

    [Fact]
    public void A_speed_in_range_is_taken_as_written()
    {
        var speed = KindSpeed.From(3f);

        Assert.Equal(3f, speed.Speed);
        Assert.Equal(SpeedVerdict.AsWritten, speed.Verdict);
    }

    [Fact]
    public void A_speed_taken_as_written_is_not_complained_about()
    {
        Assert.Null(KindSpeeds.Complaint("Speed/Crafting", KindSpeed.From(2f)));
    }

    [Fact]
    public void A_clamped_speed_is_reported_with_its_key_and_what_is_used_instead()
    {
        var complaint = KindSpeeds.Complaint("Speed/Brewing", KindSpeed.From(500f));

        Assert.NotNull(complaint);
        Assert.Contains("Speed/Brewing", complaint);
        Assert.Contains("using 20", complaint);
    }

    [Fact]
    public void A_rejected_speed_is_reported_as_running_at_the_game_pace()
    {
        var complaint = KindSpeeds.Complaint("Speed/Cooking", KindSpeed.From(0f));

        Assert.NotNull(complaint);
        Assert.Contains("Speed/Cooking", complaint);
        Assert.Contains("game's pace", complaint);
    }
}

/// <summary>
/// The default timing is guarded by each patch's Apply, which hooks nothing at the game's pace, not by these tests:
/// dividing by 1 changes nothing, so the speed-1 cases below only pin the arithmetic.
/// </summary>
public class ScaleTests
{
    [Theory]
    [InlineData(5f)]
    [InlineData(3.7f)]
    [InlineData(0.333f)]
    [InlineData(1234.5678f)]
    public void A_duration_in_seconds_at_speed_one_is_unchanged_by_the_arithmetic(float gameSeconds)
    {
        Assert.Equal(gameSeconds, KindSpeeds.ScaleSeconds(gameSeconds, KindSpeed.From(1f)));
    }

    [Theory]
    [InlineData(24)]
    [InlineData(37)]
    [InlineData(168)]
    public void A_duration_in_hours_at_speed_one_is_unchanged_by_the_arithmetic(int gameHours)
    {
        Assert.Equal(gameHours, KindSpeeds.ScaleHours(gameHours, KindSpeed.From(1f)));
    }

    [Fact]
    public void A_rejected_speed_leaves_durations_at_the_game_pace()
    {
        Assert.Equal(5f, KindSpeeds.ScaleSeconds(5f, KindSpeed.From(-1f)));
        Assert.Equal(24, KindSpeeds.ScaleHours(24, KindSpeed.From(0f)));
    }

    [Fact]
    public void Doubling_the_speed_halves_a_craft_or_a_cook()
    {
        Assert.Equal(2.5f, KindSpeeds.ScaleSeconds(5f, KindSpeed.From(2f)));
    }

    [Fact]
    public void A_speed_below_one_makes_a_cook_take_longer()
    {
        Assert.Equal(10f, KindSpeeds.ScaleSeconds(5f, KindSpeed.From(0.5f)));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void A_duration_the_game_does_not_time_is_left_alone(float gameSeconds)
    {
        Assert.Equal(gameSeconds, KindSpeeds.ScaleSeconds(gameSeconds, KindSpeed.From(4f)));
    }

    [Fact]
    public void Doubling_the_speed_halves_a_brew()
    {
        Assert.Equal(12, KindSpeeds.ScaleHours(24, KindSpeed.From(2f)));
    }

    [Fact]
    public void A_brew_that_does_not_divide_evenly_rounds_to_the_nearest_hour()
    {
        Assert.Equal(13, KindSpeeds.ScaleHours(25, KindSpeed.From(2f)));
        Assert.Equal(3, KindSpeeds.ScaleHours(10, KindSpeed.From(3f)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    public void Even_the_fastest_speed_never_shortens_a_brew_to_nothing(int gameHours)
    {
        Assert.Equal(1, KindSpeeds.ScaleHours(gameHours, KindSpeed.From(20f)));
    }
}

public class FreshCraftTests
{
    private const int Idle = 0;
    private const int WaitingReceive = 2;

    [Fact]
    public void An_idle_bench_that_starts_making_is_a_new_craft()
    {
        Assert.True(KindSpeeds.IsFreshCraft(new CraftSnapshot(Idle, 0f), new CraftSnapshot(KindSpeeds.Making, 0f)));
    }

    [Fact]
    public void A_bench_the_game_had_no_record_of_that_starts_making_is_a_new_craft()
    {
        Assert.True(KindSpeeds.IsFreshCraft(null, new CraftSnapshot(KindSpeeds.Making, 0f)));
    }

    [Fact]
    public void A_start_request_on_a_bench_already_making_never_shortens_the_running_craft_again()
    {
        Assert.False(KindSpeeds.IsFreshCraft(new CraftSnapshot(KindSpeeds.Making, 0f), new CraftSnapshot(KindSpeeds.Making, 0f)));
        Assert.False(KindSpeeds.IsFreshCraft(new CraftSnapshot(KindSpeeds.Making, 1.5f), new CraftSnapshot(KindSpeeds.Making, 1.5f)));
    }

    [Fact]
    public void A_start_request_the_game_refused_is_not_a_craft()
    {
        Assert.False(KindSpeeds.IsFreshCraft(new CraftSnapshot(WaitingReceive, 0f), new CraftSnapshot(WaitingReceive, 0f)));
    }

    [Fact]
    public void A_bench_whose_clock_is_already_running_is_not_a_new_craft()
    {
        Assert.False(KindSpeeds.IsFreshCraft(new CraftSnapshot(Idle, 0f), new CraftSnapshot(KindSpeeds.Making, 0.2f)));
    }
}

public class DistinctFailuresTests
{
    [Fact]
    public void A_failure_that_repeats_is_only_new_the_first_time()
    {
        var failures = new DistinctFailures();

        Assert.True(failures.IsNew("NullReferenceException: bench"));
        Assert.False(failures.IsNew("NullReferenceException: bench"));
    }

    [Fact]
    public void A_different_failure_is_still_reported()
    {
        var failures = new DistinctFailures();

        failures.IsNew("NullReferenceException: bench");

        Assert.True(failures.IsNew("InvalidCastException: state"));
    }
}
