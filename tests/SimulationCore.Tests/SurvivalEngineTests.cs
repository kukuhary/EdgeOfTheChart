using EdgeOfTheChart.SimulationCore.Data;
using EdgeOfTheChart.SimulationCore.Sailing;
using EdgeOfTheChart.SimulationCore.Survival;
using EdgeOfTheChart.SimulationCore.World;
using Xunit;

namespace EdgeOfTheChart.SimulationCore.Tests;

public sealed class SurvivalEngineTests
{
    [Fact]
    public void ProcessDay_ConsumesSuppliesByRationAndKillsCrewOnWaterDepletion()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        WorldState state = WorldState.CreateNewGameSample(catalog, "hero_diego");
        SeaMapGrid map = IberiaMedMapBuilder.Build(catalog.Ports);
        var field = new WindAndCurrentField(new List<StormCell>());
        var rng = new StubRandomProvider(nextDouble: 0.99);

        int startFood = state.PlayerFleet.Food;
        DailySurvivalReport normalDay = SurvivalEngine.ProcessDay(state, map, field, rng, catalog);
        Assert.True(normalDay.FoodConsumed > 0);
        Assert.Equal(startFood - normalDay.FoodConsumed, state.PlayerFleet.Food);
        Assert.Equal(0, normalDay.CrewLost);

        // 50% 비상 배급 시 소모량 절반 가까이 감소 & 사기 하락
        state.PlayerFleet.SetRationPercent(50);
        int moraleBefore = state.PlayerFleet.Morale;
        DailySurvivalReport lowRationDay = SurvivalEngine.ProcessDay(state, map, field, rng, catalog);
        Assert.True(lowRationDay.FoodConsumed < normalDay.FoodConsumed);
        Assert.Equal(moraleBefore - 5, state.PlayerFleet.Morale);

        // 식수 고갈 시 탈수 사망 및 사기 대폭 하락
        state.PlayerFleet.Water = 0;
        int crewBefore = state.PlayerFleet.Flagship!.CrewCount;
        DailySurvivalReport thirstDay = SurvivalEngine.ProcessDay(state, map, field, rng, catalog);
        Assert.True(thirstDay.CrewLost > 0);
        Assert.True(state.PlayerFleet.Flagship.CrewCount < crewBefore);
    }

    [Fact]
    public void ProcessDay_TriggersScurvyAndLimeJuiceCuresIt()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        WorldState state = WorldState.CreateNewGameSample(catalog, "hero_diego");
        SeaMapGrid map = IberiaMedMapBuilder.Build(catalog.Ports);
        var field = new WindAndCurrentField(new List<StormCell>());
        var rng = new StubRandomProvider(nextDouble: 0.01);

        state.PlayerFleet.DaysAtSea = 14;
        DailySurvivalReport report = SurvivalEngine.ProcessDay(state, map, field, rng, catalog);

        Assert.True(state.PlayerFleet.HasScurvy);
        Assert.True(report.CrewLost >= 1);

        int limeBefore = state.PlayerFleet.LimeJuice;
        (bool cured, _) = SurvivalEngine.UseLimeJuice(state);
        Assert.True(cured);
        Assert.Equal(limeBefore - 1, state.PlayerFleet.LimeJuice);
        Assert.False(state.PlayerFleet.HasScurvy);
        Assert.Equal(0, state.PlayerFleet.DaysAtSea);
    }

    [Fact]
    public void ProcessDay_ReducesStormDamageWhenSailsReefed()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        SeaMapGrid map = IberiaMedMapBuilder.Build(catalog.Ports);
        var stormCells = new List<StormCell>
        {
            new StormCell { Id = "test_storm", X = 10.0, Y = 30.0, Radius = 4.0, Intensity = 3 }
        };
        var field = new WindAndCurrentField(stormCells);
        var rng = new StubRandomProvider(nextDouble: 0.99, nextInt: 20);

        WorldState unreefed = WorldState.CreateNewGameSample(catalog, "hero_diego");
        unreefed.ShipX = 10.0;
        unreefed.ShipY = 30.0;
        unreefed.PlayerFleet.SailsReefed = false;
        DailySurvivalReport rawStorm = SurvivalEngine.ProcessDay(unreefed, map, field, rng, catalog);

        WorldState reefed = WorldState.CreateNewGameSample(catalog, "hero_diego");
        reefed.ShipX = 10.0;
        reefed.ShipY = 30.0;
        reefed.PlayerFleet.SailsReefed = true;
        DailySurvivalReport reducedStorm = SurvivalEngine.ProcessDay(reefed, map, field, rng, catalog);

        Assert.Equal(20, rawStorm.HullDamage);
        Assert.Equal(6, reducedStorm.HullDamage);
    }

    [Fact]
    public void EmergencyForageAndRepair_WorkWithExpectedConstraints()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        WorldState state = WorldState.CreateNewGameSample(catalog, "hero_diego");
        var rng = new StubRandomProvider(nextDouble: 0.5, nextInt: 6);

        int foodBefore = state.PlayerFleet.Food;
        (bool ok1, _) = SurvivalEngine.PerformEmergencyForage(state, rng);
        Assert.True(ok1);
        Assert.Equal(foodBefore + 6, state.PlayerFleet.Food);

        // 같은 날 쿨다운으로 재시도 불가
        (bool ok2, _) = SurvivalEngine.PerformEmergencyForage(state, rng);
        Assert.False(ok2);

        state.PlayerFleet.Flagship!.HullHp = 100;
        int woodBefore = state.PlayerFleet.RepairWood;
        (bool repaired, _) = SurvivalEngine.PerformEmergencyRepair(state);
        Assert.True(repaired);
        Assert.Equal(125, state.PlayerFleet.Flagship.HullHp);
        Assert.Equal(woodBefore - 5, state.PlayerFleet.RepairWood);
    }

    [Fact]
    public void ProcessDay_FirstMateSuppressesFirstMutinyThenSecondTriggersRescue()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        WorldState state = WorldState.CreateNewGameSample(catalog, "hero_diego");
        SeaMapGrid map = IberiaMedMapBuilder.Build(catalog.Ports);
        var field = new WindAndCurrentField(new List<StormCell>());
        var rng = new StubRandomProvider(nextDouble: 0.99);

        state.PlayerFleet.Morale = 0;
        state.PlayerFleet.MutinyRisk = 100;

        DailySurvivalReport firstMutiny = SurvivalEngine.ProcessDay(state, map, field, rng, catalog);
        Assert.False(firstMutiny.RequiresRescue);
        Assert.Equal(30, state.PlayerFleet.Morale);

        state.PlayerFleet.Morale = 0;
        state.PlayerFleet.MutinyRisk = 100;
        DailySurvivalReport secondMutiny = SurvivalEngine.ProcessDay(state, map, field, rng, catalog);
        Assert.True(secondMutiny.RequiresRescue);
        Assert.Equal(RescueReason.Mutiny, secondMutiny.RescueReason);
    }
}
