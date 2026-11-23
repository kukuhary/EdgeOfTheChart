using EdgeOfTheChart.SimulationCore.Clock;
using Xunit;

namespace EdgeOfTheChart.SimulationCore.Tests;

public sealed class SimulationClockTests
{
    [Fact]
    public void AdvanceHours_RollsOverDaysMonthsAndYears()
    {
        var clock = new SimulationClock { Year = 1522, Month = 5, Day = 31, Hour = 20 };

        clock.AdvanceHours(8);
        Assert.Equal(1522, clock.Year);
        Assert.Equal(6, clock.Month);
        Assert.Equal(1, clock.Day);
        Assert.Equal(4, clock.Hour);

        var yearEndClock = new SimulationClock { Year = 1522, Month = 12, Day = 31, Hour = 22 };
        yearEndClock.AdvanceHours(26);
        Assert.Equal(1523, yearEndClock.Year);
        Assert.Equal(1, yearEndClock.Month);
        Assert.Equal(2, yearEndClock.Day);
        Assert.Equal(0, yearEndClock.Hour);
    }

    [Fact]
    public void SetSpeed_AllowsOnlyZeroOneTwoFour()
    {
        var clock = new SimulationClock();
        foreach (int speed in new[] { 0, 1, 2, 4 })
        {
            clock.SetSpeed(speed);
            Assert.Equal(speed, clock.SpeedMultiplier);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetSpeed(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => clock.SetSpeed(-1));
    }
}
