using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class HookSwitchTests
{
    [Fact]
    public void At_the_games_pace_from_the_start_nothing_is_hooked()
    {
        var hook = new HookSwitch();

        Assert.False(hook.Use(isNeeded: false));
        Assert.False(hook.IsOn);
        Assert.False(hook.IsIn);
    }

    [Fact]
    public void The_first_setting_off_the_games_pace_puts_the_hook_in()
    {
        var hook = new HookSwitch();

        Assert.True(hook.Use(isNeeded: true));
        Assert.True(hook.IsOn);
    }

    [Fact]
    public void Back_at_the_games_pace_the_hook_stays_in_and_does_nothing()
    {
        var hook = new HookSwitch();

        hook.Use(isNeeded: true);
        hook.Placed();

        Assert.False(hook.Use(isNeeded: false));
        Assert.False(hook.IsOn);
        Assert.True(hook.IsIn);
    }

    [Fact]
    public void A_hook_already_in_is_never_put_in_twice()
    {
        var hook = new HookSwitch();

        hook.Use(isNeeded: true);
        hook.Placed();
        hook.Use(isNeeded: false);

        Assert.False(hook.Use(isNeeded: true));
        Assert.True(hook.IsOn);
    }

    [Fact]
    public void A_hook_that_failed_to_go_in_is_tried_again_at_the_next_change()
    {
        var hook = new HookSwitch();

        hook.Use(isNeeded: true);

        Assert.True(hook.Use(isNeeded: true));
    }
}
