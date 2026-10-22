using ActionSpeed.Core;
using Xunit;

namespace ActionSpeed.Tests;

public class TravelTests
{
    [Fact]
    public void A_faster_trip_takes_the_game_time_divided_by_the_speed()
    {
        Assert.Equal(900, Travel.Seconds(3600, KindSpeed.From(4f)));
    }

    [Fact]
    public void A_slower_trip_takes_longer()
    {
        Assert.Equal(7200, Travel.Seconds(3600, KindSpeed.From(0.5f)));
    }

    [Fact]
    public void At_the_game_pace_a_trip_keeps_the_game_time()
    {
        Assert.Equal(5400, Travel.Seconds(5400, KindSpeed.GamePace));
    }

    [Fact]
    public void A_place_reached_without_travel_stays_without_travel()
    {
        Assert.Equal(0, Travel.Seconds(0, KindSpeed.From(4f)));
    }

    [Fact]
    public void A_trip_never_shrinks_to_no_travel_at_all()
    {
        Assert.Equal(1, Travel.Seconds(1, KindSpeed.From(20f)));
    }

    [Fact]
    public void A_speed_that_is_not_a_speed_leaves_the_trip_at_the_game_time()
    {
        Assert.Equal(3600, Travel.Seconds(3600, KindSpeed.From(0f)));
    }
}
