using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class ClockMemoTests
{
    [Fact]
    public void A_running_effect_keeps_the_clock_it_was_given()
    {
        var memo = new ClockMemo();

        memo.Remember(7, 2f);

        Assert.True(memo.TryRecall(7, isFresh: false, out var clock));
        Assert.Equal(2f, clock);
    }

    [Fact]
    public void A_new_effect_reusing_an_old_id_is_looked_up_again()
    {
        var memo = new ClockMemo();

        memo.Remember(7, 2f);

        Assert.False(memo.TryRecall(7, isFresh: true, out _));
    }

    [Fact]
    public void An_effect_never_seen_runs_on_the_game_clock()
    {
        Assert.False(new ClockMemo().TryRecall(7, isFresh: false, out var clock));
        Assert.Equal(1f, clock);
    }

    [Fact]
    public void An_effect_still_running_is_kept_however_often_the_memo_ages()
    {
        var memo = new ClockMemo();

        memo.Remember(7, 3f);

        for (var age = 0; age < 5; age++)
        {
            memo.Age();

            Assert.True(memo.TryRecall(7, isFresh: false, out var clock));
            Assert.Equal(3f, clock);
        }
    }

    [Fact]
    public void A_newly_loaded_game_forgets_every_effect_of_the_last()
    {
        var memo = new ClockMemo();

        memo.Remember(7, 3f);
        memo.Age();
        memo.Remember(8, 3f);
        memo.Clear();

        Assert.False(memo.TryRecall(7, isFresh: false, out _));
        Assert.False(memo.TryRecall(8, isFresh: false, out _));
    }

    [Fact]
    public void An_effect_that_ended_is_kept_through_one_age_and_forgotten_after_two()
    {
        var memo = new ClockMemo();

        memo.Remember(7, 3f);
        memo.Remember(8, 3f);
        memo.Age();

        Assert.True(memo.TryRecall(8, isFresh: false, out _));

        memo.Age();

        Assert.False(memo.TryRecall(7, isFresh: false, out var clock));
        Assert.Equal(1f, clock);
    }
}
