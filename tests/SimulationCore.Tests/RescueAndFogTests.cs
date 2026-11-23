using EdgeOfTheChart.SimulationCore.Data;
using EdgeOfTheChart.SimulationCore.Fleet;
using EdgeOfTheChart.SimulationCore.Sailing;
using EdgeOfTheChart.SimulationCore.Serialization;
using EdgeOfTheChart.SimulationCore.Survival;
using EdgeOfTheChart.SimulationCore.World;
using Xunit;

namespace EdgeOfTheChart.SimulationCore.Tests;

public sealed class RescueAndFogTests
{
    [Fact]
    public void ExecuteRescue_ReturnsToHomeOrIndependentPortWithSmallShipAndDebt()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        WorldState state = WorldState.CreateNewGameSample(catalog, "hero_diego");

        state.ShipX = 95.0;
        state.ShipY = 35.0;
        state.Flags.SetFlag("important_story_clue");
        int initialDebt = state.Debt;

        RescueReport report = RescueService.ExecuteRescue(state, catalog, RescueReason.Shipwreck);

        Assert.Equal("port_lisbon", report.RescuePortId);
        Assert.Equal(1500, report.AddedDebt);
        Assert.Equal(initialDebt + 1500, state.Debt);
        Assert.Single(state.PlayerFleet.Ships);
        Assert.True(state.PlayerFleet.Ships.Count <= Fleet.Fleet.MaxShips);
        Assert.Equal("재기의 카라벨", state.PlayerFleet.Flagship?.Name);
        Assert.True(state.Flags.GetFlag("important_story_clue"));
        Assert.Single(state.Officers);
        Assert.InRange(state.ShipX, 24.0, 25.0);
        Assert.InRange(state.ShipY, 31.0, 32.0);
    }

    [Fact]
    public void FogOfWar_RevealsTilesAndPersistsThroughSaveRoundTrip()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        WorldState state = WorldState.CreateNewGameSample(catalog, "hero_diego");
        SeaMapGrid map = IberiaMedMapBuilder.Build(catalog.Ports);
        var fog = new FogOfWarGrid(map.Width, map.Height);
        var field = new WindAndCurrentField(new List<StormCell>());
        var rng = new StubRandomProvider();

        Assert.Equal(0.0, fog.CalculateExplorationPercent(map));

        SailingPhysics.RevealFogAndSync(state, map, fog, catalog);
        double pctAfterSpawn = fog.CalculateExplorationPercent(map);
        Assert.True(pctAfterSpawn > 0.0);
        Assert.NotEmpty(state.ExploredTileIndices);

        // 서쪽으로 조금 항해하여 탐험률 상승 확인
        state.PlayerFleet.LimeJuice = 7;
        SailingPhysics.StepMovement(state, catalog, map, field, fog, -1.0, 0.0, 24.0, rng);
        double pctAfterMove = fog.CalculateExplorationPercent(map);
        Assert.True(pctAfterMove > pctAfterSpawn);

        string json = SaveSerializer.Serialize(state);
        WorldState restored = SaveSerializer.Deserialize(json);

        Assert.Equal(state.ShipX, restored.ShipX);
        Assert.Equal(state.ShipY, restored.ShipY);
        Assert.Equal(7, restored.PlayerFleet.LimeJuice);
        Assert.Equal(state.ExploredTileIndices.Count, restored.ExploredTileIndices.Count);

        var restoredFog = new FogOfWarGrid(map.Width, map.Height);
        restoredFog.LoadFromIndices(restored.ExploredTileIndices);
        Assert.Equal(pctAfterMove, restoredFog.CalculateExplorationPercent(map), 4);
    }
}
