using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

/// <summary>The bench's progress bar is told the shortened length, so it fills right as the craft finishes.</summary>
public class CraftBarTests
{
    private const long Bench = 7;
    private const long OtherBench = 8;

    [Fact]
    public void A_length_told_while_the_craft_starts_is_scaled_and_not_swapped_again_later()
    {
        var bar = new CraftBar(KindSpeed.From(4f));

        bar.Starting(Bench);
        var shown = bar.Shown(Bench, 20f);
        bar.Started(Bench, 20f, 5f);

        Assert.Equal(5f, shown);
        Assert.Equal(20f, bar.Shown(Bench, 20f));
    }

    [Fact]
    public void A_length_told_after_the_craft_started_is_swapped_for_the_shortened_one_once()
    {
        var bar = new CraftBar(KindSpeed.From(4f));

        bar.Starting(Bench);
        bar.Started(Bench, 20f, 5f);

        Assert.Equal(5f, bar.Shown(Bench, 20f));
        Assert.Equal(20f, bar.Shown(Bench, 20f));
    }

    [Fact]
    public void A_length_told_after_the_craft_started_in_other_units_is_shortened_by_as_much()
    {
        var bar = new CraftBar(KindSpeed.From(4f));

        bar.Starting(Bench);
        bar.Started(Bench, 20f, 5f);

        Assert.Equal(3f, bar.Shown(Bench, 12f));
    }

    [Fact]
    public void Other_benches_and_crafts_left_alone_are_shown_as_the_game_says()
    {
        var bar = new CraftBar(KindSpeed.From(4f));

        bar.Starting(Bench);
        Assert.Equal(20f, bar.Shown(OtherBench, 20f));
        bar.Started(Bench, 20f, 5f);
        Assert.Equal(20f, bar.Shown(OtherBench, 20f));

        bar.Starting(Bench);
        bar.Started(Bench, 20f, null);
        Assert.Equal(20f, bar.Shown(Bench, 20f));
    }

    [Fact]
    public void A_start_that_ended_without_finishing_leaves_later_lengths_as_the_game_says()
    {
        var bar = new CraftBar(KindSpeed.From(4f));

        bar.Starting(Bench);
        bar.StartEnded();

        Assert.Equal(20f, bar.Shown(Bench, 20f));
    }
}
