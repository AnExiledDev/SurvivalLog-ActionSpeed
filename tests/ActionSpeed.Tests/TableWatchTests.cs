using System.Linq;
using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class TableWatchTests
{
    private static readonly TableStamp Loaded = new(Actions: 1000, ActionCount: 700, Language: 5000, LanguageCount: 9000);

    /// <summary>The game's own durations: 20 actions, every one 60 seconds long.</summary>
    private static readonly Dictionary<int, float> GameDuring = Enumerable.Range(1, 20).ToDictionary(id => id, _ => 60f);

    /// <summary>The plan halves the even actions and leaves the odd ones at the game's length.</summary>
    private static readonly Dictionary<int, float> Planned = GameDuring.ToDictionary(pair => pair.Key, pair => pair.Key % 2 == 0 ? 30f : 60f);

    /// <summary>A watch that has just written the plan, and the table as that write left it.</summary>
    private static (TableWatch Watch, Dictionary<int, float> Table) AfterWrite()
    {
        var watch = new TableWatch();

        watch.Written(Loaded, Planned, GameDuring);

        return (watch, new Dictionary<int, float>(Planned));
    }

    private static Func<int, float> Reading(Dictionary<int, float> table, List<int>? asked = null) => id =>
    {
        asked?.Add(id);

        return table.TryGetValue(id, out var during) ? during : -1f;
    };

    [Fact]
    public void Nothing_written_yet_means_a_full_write()
    {
        Assert.True(new TableWatch().NeedsWrite(Loaded, Reading(new Dictionary<int, float>(Planned))));
    }

    [Fact]
    public void The_same_table_with_every_written_duration_in_place_is_not_read_again()
    {
        var (watch, table) = AfterWrite();

        Assert.False(watch.NeedsWrite(Loaded, Reading(table)));
    }

    [Fact]
    public void An_unchanged_table_is_checked_by_a_few_changed_actions_only()
    {
        var (watch, table) = AfterWrite();
        var asked = new List<int>();

        watch.NeedsWrite(Loaded, Reading(table, asked));

        Assert.NotEmpty(asked);
        Assert.True(asked.Count <= TableWatch.CanaryCount);
        Assert.All(asked, id => Assert.Equal(0, id % 2));
    }

    [Fact]
    public void A_replaced_action_table_is_written_again()
    {
        var (watch, table) = AfterWrite();

        Assert.True(watch.NeedsWrite(Loaded with { Actions = 3000 }, Reading(table)));
    }

    [Fact]
    public void Actions_added_to_the_table_are_written()
    {
        var (watch, table) = AfterWrite();

        Assert.True(watch.NeedsWrite(Loaded with { ActionCount = 701 }, Reading(table)));
    }

    [Fact]
    public void A_change_of_language_reads_the_table_again()
    {
        var (watch, table) = AfterWrite();

        Assert.True(watch.NeedsWrite(Loaded with { Language = 7000 }, Reading(table)));
    }

    [Fact]
    public void A_language_that_finishes_loading_reads_the_table_again()
    {
        var (watch, table) = AfterWrite();

        Assert.True(watch.NeedsWrite(Loaded with { LanguageCount = 9500 }, Reading(table)));
    }

    [Fact]
    public void Changed_settings_write_the_table_again_whatever_it_holds()
    {
        var (watch, table) = AfterWrite();

        watch.Forget();

        Assert.True(watch.NeedsWrite(Loaded, Reading(table)));
    }

    [Fact]
    public void A_reload_that_puts_back_the_games_durations_is_written_again()
    {
        var (watch, _) = AfterWrite();

        Assert.True(watch.NeedsWrite(Loaded, Reading(new Dictionary<int, float>(GameDuring))));
    }

    [Fact]
    public void A_watched_action_gone_from_the_table_is_written_again()
    {
        var (watch, _) = AfterWrite();

        Assert.True(watch.NeedsWrite(Loaded, Reading(new Dictionary<int, float>())));
    }

    [Fact]
    public void The_next_write_is_watched_in_place_of_the_last()
    {
        var (watch, table) = AfterWrite();
        var reloaded = Loaded with { Actions = 3000 };

        watch.Written(reloaded, Planned, GameDuring);

        Assert.False(watch.NeedsWrite(reloaded, Reading(table)));
        Assert.True(watch.NeedsWrite(Loaded, Reading(table)));
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
