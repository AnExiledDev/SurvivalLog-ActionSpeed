using System.Linq;
using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class TableWatchTests
{
    private static readonly TableStamp Loaded = new(Actions: 1000, ActionCount: 700);

    [Fact]
    public void Nothing_written_yet_means_a_full_write()
    {
        Assert.False(TableWatch.IsUnchanged(null, Loaded, []));
    }

    [Fact]
    public void The_same_table_with_every_watched_duration_in_place_skips_the_write()
    {
        Assert.True(TableWatch.IsUnchanged(Loaded, Loaded, [new Canary(1.5f, 1.5f), new Canary(0.25f, 0.25f)]));
    }

    [Fact]
    public void A_replaced_action_table_is_written_again()
    {
        Assert.False(TableWatch.IsUnchanged(Loaded, Loaded with { Actions = 3000 }, [new Canary(1.5f, 1.5f)]));
    }

    [Fact]
    public void Actions_added_to_the_table_are_written()
    {
        Assert.False(TableWatch.IsUnchanged(Loaded, Loaded with { ActionCount = 701 }, []));
    }

    [Fact]
    public void A_watched_action_back_at_the_game_duration_is_written_again()
    {
        Assert.False(TableWatch.IsUnchanged(Loaded, Loaded, [new Canary(1.5f, 1.5f), new Canary(1.5f, 6f)]));
    }

    [Fact]
    public void Few_changed_actions_are_all_watched()
    {
        Assert.Equal([3, 5, 9], TableWatch.PickCanaries([9, 3, 5]));
    }

    [Fact]
    public void Many_changed_actions_are_watched_from_first_to_last()
    {
        var picked = TableWatch.PickCanaries(Enumerable.Range(1, 100));

        Assert.Equal(TableWatch.CanaryCount, picked.Count);
        Assert.Equal(1, picked[0]);
        Assert.Equal(100, picked[^1]);
        Assert.Equal(picked.Count, picked.Distinct().Count());
    }
}
