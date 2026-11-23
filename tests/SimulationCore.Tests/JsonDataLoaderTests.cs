using EdgeOfTheChart.SimulationCore.Data;
using Xunit;

namespace EdgeOfTheChart.SimulationCore.Tests;

public sealed class JsonDataLoaderTests
{
    internal static string LocateDataDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "ports.json")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("테스트 실행 경로 상위에서 data 디렉터리를 찾을 수 없습니다.");
    }

    [Fact]
    public void LoadFromDirectory_LoadsAllPhase0MasterData()
    {
        string dataDir = LocateDataDirectory();
        MasterDataCatalog catalog = JsonDataLoader.LoadFromDirectory(dataDir);

        Assert.Equal(8, catalog.Ports.Count);
        Assert.NotNull(catalog.FindPort("port_lisbon"));
        Assert.NotNull(catalog.FindPort("port_busanpo"));

        Assert.Equal(6, catalog.Ships.Count);
        Assert.NotNull(catalog.FindShip("ship_caravel_latina"));
        Assert.NotNull(catalog.FindShip("ship_joseon_haeryong_hybrid"));

        Assert.Equal(6, catalog.Officers.Count);
        Assert.NotNull(catalog.FindOfficer("off_goncalo_alves"));
        Assert.NotNull(catalog.FindOfficer("off_choi_mugang"));

        Assert.Equal(4, catalog.RelicsAndFigureheads.Count);
        Assert.Single(catalog.Leviathans);
        Assert.Equal("levi_atlantic_kraken", catalog.Leviathans[0].Id);

        ScenarioData scenario = Assert.Single(catalog.Scenarios);
        Assert.Equal("diego_ch1", scenario.ScenarioId);
        Assert.Equal(2, scenario.Events.Count);
        Assert.Contains(scenario.Events, e => e.Id == "evt_diego_intro_lisbon");
        Assert.Contains(scenario.Events, e => e.Id == "evt_cameo_gibraltar_1522");
    }

    [Fact]
    public void LoadFromDirectory_ThrowsWhenDirectoryMissing()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Assert.Throws<DirectoryNotFoundException>(() => JsonDataLoader.LoadFromDirectory(missingPath));
    }
}
