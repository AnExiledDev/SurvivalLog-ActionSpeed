using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

/// <summary>
/// What a sped-up action pays out, run through a model of the game's effect update. The step sizes are powers of
/// two so every countdown is exact in float arithmetic and the totals can be compared for equality.
/// </summary>
public class FullBenefitsTests
{
    private const int Food = 1;

    /// <summary>Game seconds per frame; at the fastest speed one frame still passes less than the shortest payout period.</summary>
    private const float FrameStep = 0.25f;

    /// <summary>A long frame, such as a hitch: at high speed one frame passes more than the shortest payout period.</summary>
    private const float LongFrameStep = 1f;

    /// <summary>The shortest payout period among the shapes below, which is as long as a sped-up step may be.</summary>
    private const float ShortestPeriod = 7f;

    /// <summary>Like the game's sleep: 5 every half hour from just before the first half hour, 10 at the end, a wake-up trigger near the end.</summary>
    private static readonly EffectShape Sleep = new(Life: 21600f, FirstPayout: 1799f, Period: 1800f, Payout: 5f, EndPayout: 10f, DelayAt: 21500f);

    /// <summary>Effects with odd payout periods and lives that are no multiple of them, with what each pays at the game's pace.</summary>
    private static readonly Dictionary<string, (EffectShape Shape, float GameTotal)> Shapes = new()
    {
        ["every 7s"] = (new EffectShape(Life: 1801f, FirstPayout: 7f, Period: 7f, Payout: 1f, EndPayout: 0f, DelayAt: 0f), 257f),
        ["every 333s"] = (new EffectShape(Life: 1801f, FirstPayout: 333f, Period: 333f, Payout: 1f, EndPayout: 0f, DelayAt: 0f), 5f),
        ["once at 1799s"] = (new EffectShape(Life: 1801f, FirstPayout: 1799f, Period: 1799f, Payout: 1f, EndPayout: 0f, DelayAt: 0f), 1f),
        ["first at 100s then every 333s"] = (new EffectShape(Life: 1801f, FirstPayout: 100f, Period: 333f, Payout: 1f, EndPayout: 0f, DelayAt: 0f), 6f),
        ["sleep"] = (Sleep, 70f),
    };

    public static TheoryData<float, string> SpeedsAndShapes()
    {
        var data = new TheoryData<float, string>();

        foreach (var speed in new[] { 0.25f, 1f, 2f, 20f })
        {
            foreach (var name in Shapes.Keys) data.Add(speed, name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SpeedsAndShapes))]
    public void A_sped_up_action_with_full_benefits_pays_out_exactly_what_it_pays_at_the_game_pace(float speed, string shapeName)
    {
        var (shape, gameTotal) = Shapes[shapeName];
        var run = Run(shape, Decide(shape, speed, fullBenefits: true), FrameStep);

        Assert.Equal(gameTotal, run.Total);
        Assert.Equal(shape.DelayAt > 0f, run.HasDelayFired);
    }

    [Theory]
    [MemberData(nameof(SpeedsAndShapes))]
    public void A_sped_up_action_with_full_benefits_still_ends_sooner(float speed, string shapeName)
    {
        var shape = Shapes[shapeName].Shape;
        var expected = shape.Life / speed;

        Assert.InRange(Run(shape, Decide(shape, speed, fullBenefits: true), FrameStep).GameSeconds, expected, expected + FrameStep);
    }

    public static TheoryData<float, string> FastSpeedsAndShapes()
    {
        var data = new TheoryData<float, string>();

        foreach (var speed in new[] { 7f, 8f, 20f })
        {
            foreach (var name in Shapes.Keys) data.Add(speed, name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(FastSpeedsAndShapes))]
    public void A_frame_that_passes_more_than_a_payout_period_loses_no_payout_and_adds_none(float speed, string shapeName)
    {
        var (shape, gameTotal) = Shapes[shapeName];
        var run = Run(shape, Decide(shape, speed, fullBenefits: true), LongFrameStep);

        Assert.Equal(gameTotal, run.Total);
        Assert.Equal(shape.DelayAt > 0f, run.HasDelayFired);
    }

    [Fact]
    public void Uncut_steps_longer_than_the_payout_period_would_lose_payouts()
    {
        var (shape, gameTotal) = Shapes["every 7s"];
        var uncut = Run(shape, Decide(shape, 20f, fullBenefits: true), LongFrameStep, longestStep: float.PositiveInfinity);

        Assert.True(uncut.Total < gameTotal, $"uncut steps paid {uncut.Total}, the game pays {gameTotal}");
    }

    [Fact]
    public void Without_full_benefits_a_sped_up_sleep_ends_as_soon_but_pays_out_less()
    {
        var full = Run(Sleep, Decide(Sleep, 2f, fullBenefits: true), FrameStep);
        var cut = Run(Sleep, Decide(Sleep, 2f, fullBenefits: false), FrameStep);

        Assert.Equal(full.GameSeconds, cut.GameSeconds);
        Assert.True(cut.Total < full.Total, $"cut short paid {cut.Total}, full benefits paid {full.Total}");
    }

    [Fact]
    public void Without_full_benefits_a_wake_up_set_near_the_end_never_fires()
    {
        Assert.False(Run(Sleep, Decide(Sleep, 2f, fullBenefits: false), FrameStep).HasDelayFired);
    }

    private static ActionDecision Decide(EffectShape shape, float speed, bool fullBenefits)
    {
        var row = new ActionRow(5, "action", Food, shape.Life);

        return ActionSpeeds.Decide(row, new SpeedSettings(new Dictionary<Group, float> { [ActionGroups.Of(row)] = speed }, new Dictionary<int, float>(), fullBenefits));
    }

    /// <summary>
    /// One action's effect, frame by frame, the way the game's effect manager runs it. The table duration becomes
    /// the effect's life. Each frame the hook hands the effect a step on its clock, cut to the shortest payout
    /// period and to the life left, then the game counts down the delayed triggers, runs the payout timer (one payout per frame at most, the overshoot carried into the next
    /// period), and last counts down the life, ending the effect with its end payout once the life reaches 0.
    /// </summary>
    private static EffectRun Run(EffectShape shape, ActionDecision decision, float frameStep, float longestStep = ShortestPeriod)
    {
        var life = decision.During;
        var untilPayout = shape.FirstPayout;
        var untilDelay = shape.DelayAt;
        var hasDelayFired = false;
        var total = 0f;
        var frames = 0;

        while (true)
        {
            frames++;

            var step = decision.Clock == 1f ? frameStep : EffectClock.Step(frameStep, decision.Clock, longestStep, life);

            if (shape.DelayAt > 0f && !hasDelayFired)
            {
                untilDelay -= step;
                hasDelayFired = untilDelay <= 0f;
            }

            untilPayout -= step;

            if (untilPayout <= 0f)
            {
                total += shape.Payout;
                untilPayout += shape.Period;
            }

            life -= step;

            if (life <= 0f) return new EffectRun(total + shape.EndPayout, frames * frameStep, hasDelayFired);
        }
    }

    private readonly record struct EffectRun(float Total, float GameSeconds, bool HasDelayFired);
}

/// <param name="Life">The action's game duration in game seconds.</param>
/// <param name="DelayAt">When a one-off trigger fires, such as waking up; 0 for none.</param>
public sealed record EffectShape(float Life, float FirstPayout, float Period, float Payout, float EndPayout, float DelayAt);
