using EdgeOfTheChart.SimulationCore.Data;
using EdgeOfTheChart.SimulationCore.Officers;
using EdgeOfTheChart.SimulationCore.Routing;
using EdgeOfTheChart.SimulationCore.Serialization;
using EdgeOfTheChart.SimulationCore.World;
using Xunit;

namespace EdgeOfTheChart.SimulationCore.Tests;

public sealed class SaveSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_PreservesWorldStateRoundTrip()
    {
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(JsonDataLoaderTests.LocateDataDirectory());
        WorldState original = WorldState.CreateNewGameSample(catalog, "hero_diego");

        original.Clock.AdvanceHours(48);
        original.Gold += 750;
        original.Flags.SetFlag("isabel_helped_gibraltar");
        original.Flags.AdjustCameoAffinity("hero_isabel", 30);
        original.Officers[0].AdjustLoyalty(-15);
        original.CurrentScene = GameSceneId.WorldMap;

        string json = SaveSerializer.Serialize(original);
        WorldState restored = SaveSerializer.Deserialize(json);

        Assert.Equal(original.ProtagonistId, restored.ProtagonistId);
        Assert.Equal(GameSceneId.WorldMap, restored.CurrentScene);
        Assert.Equal(original.Clock.FormattedDate, restored.Clock.FormattedDate);
        Assert.Equal(5750, restored.Gold);
        Assert.Single(restored.PlayerFleet.Ships);
        Assert.Equal("에스페란사", restored.PlayerFleet.Flagship?.Name);
        Assert.Equal(75, restored.Officers[0].Loyalty);
        Assert.Equal(OfficerRole.FirstMate, restored.Officers[0].AssignedRole);
        Assert.True(restored.Flags.GetFlag("isabel_helped_gibraltar"));
        Assert.Equal(30, restored.Flags.GetCameoAffinity("hero_isabel"));
    }

    [Fact]
    public void Deserialize_ThrowsWhenSaveVersionUnsupported()
    {
        const string badJson = """
        {
          "SaveVersion": 999,
          "SavedAtUtc": "2026-11-23T00:00:00Z",
          "World": {}
        }
        """;

        Assert.Throws<InvalidDataException>(() => SaveSerializer.Deserialize(badJson));
    }
}
