using EdgeOfTheChart.SimulationCore.Fleet;
using Xunit;

namespace EdgeOfTheChart.SimulationCore.Tests;

public sealed class FleetTests
{
    private static Ship CreateDummyShip(string id, string name)
    {
        var ship = new Ship
        {
            Id = id,
            TemplateId = "ship_caravel_latina",
            Name = name,
            Rig = RigType.Lateen,
            TotalCapacity = 160,
            MaxHullHp = 180,
            HullHp = 180,
            MinCrew = 12,
            CrewCount = 45
        };
        ship.SetCabins(45, 15, 100);
        return ship;
    }

    [Fact]
    public void Fleet_EnforcesMaximumThreeShips()
    {
        Assert.Equal(3, Fleet.Fleet.MaxShips);

        var fleet = new Fleet.Fleet();
        Assert.True(fleet.TryAddShip(CreateDummyShip("s1", "1호함")));
        Assert.True(fleet.TryAddShip(CreateDummyShip("s2", "2호함")));
        Assert.True(fleet.TryAddShip(CreateDummyShip("s3", "3호함")));

        Assert.Equal(3, fleet.Ships.Count);
        Assert.False(fleet.TryAddShip(CreateDummyShip("s4", "4호함")));
        Assert.Throws<InvalidOperationException>(() => fleet.AddShip(CreateDummyShip("s4", "4호함")));
    }

    [Fact]
    public void AssignFlagship_MovesSelectedShipToFirstIndex()
    {
        var fleet = new Fleet.Fleet();
        fleet.AddShip(CreateDummyShip("s1", "에스페란사"));
        fleet.AddShip(CreateDummyShip("s2", "산타마리아"));
        fleet.AddShip(CreateDummyShip("s3", "핀타"));

        fleet.AssignFlagship(2);

        Assert.Equal("s3", fleet.Flagship?.Id);
        Assert.Equal(3, fleet.Ships.Count);
    }

    [Fact]
    public void SetRationPercent_ValidatesAllowedSteps()
    {
        var fleet = new Fleet.Fleet();
        fleet.SetRationPercent(50);
        Assert.Equal(50, fleet.RationPercent);

        fleet.SetRationPercent(75);
        Assert.Equal(75, fleet.RationPercent);

        Assert.Throws<ArgumentOutOfRangeException>(() => fleet.SetRationPercent(80));
    }

    [Fact]
    public void Ship_SetCabins_ThrowsWhenExceedingTotalCapacity()
    {
        Ship ship = CreateDummyShip("s1", "에스페란사");
        Assert.Throws<InvalidOperationException>(() => ship.SetCabins(100, 40, 30));
    }
}
