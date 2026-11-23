using EdgeOfTheChart.SimulationCore.Data;
using EdgeOfTheChart.SimulationCore.Fleet;
using EdgeOfTheChart.SimulationCore.Sailing;
using EdgeOfTheChart.SimulationCore.Serialization;
using EdgeOfTheChart.SimulationCore.Survival;
using EdgeOfTheChart.SimulationCore.World;
using Godot;

namespace EdgeOfTheChart.Godot;

public partial class GameSession : Node
{
    public MasterDataCatalog? Catalog { get; private set; }
    public WorldState? CurrentWorld { get; private set; }
    public SeaMapGrid? SeaMap { get; private set; }
    public WindAndCurrentField? WindField { get; private set; }
    public FogOfWarGrid? FogGrid { get; private set; }
    public IRandomProvider Rng { get; } = new SystemRandomProvider();
    public string DataDirectoryPath { get; private set; } = string.Empty;

    public void InitializeNewSession(string protagonistId = "hero_diego")
    {
        DataDirectoryPath = ResolveDataDirectory();
        Catalog = JsonDataLoader.LoadFromDirectory(DataDirectoryPath);
        CurrentWorld = WorldState.CreateNewGameSample(Catalog, protagonistId);
        RebuildPhase1Runtime();
    }

    public void RebuildPhase1Runtime()
    {
        if (Catalog is null || CurrentWorld is null)
        {
            return;
        }

        SeaMap = IberiaMedMapBuilder.Build(Catalog.Ports);
        if (CurrentWorld.StormCells.Count == 0)
        {
            CurrentWorld.StormCells = WindAndCurrentField.CreateDefaultStormCells();
        }

        WindField = new WindAndCurrentField(CurrentWorld.StormCells);
        FogGrid = new FogOfWarGrid(SeaMap.Width, SeaMap.Height);

        if (CurrentWorld.ExploredTileIndices.Count > 0)
        {
            FogGrid.LoadFromIndices(CurrentWorld.ExploredTileIndices);
        }

        SailingPhysics.RevealFogAndSync(CurrentWorld, SeaMap, FogGrid, Catalog);
    }

    public string GetCatalogSummaryText()
    {
        if (Catalog is null || CurrentWorld is null)
        {
            return "데이터가 아직 로드되지 않았습니다.";
        }

        Ship? flagship = CurrentWorld.PlayerFleet.Flagship;
        string flagshipName = flagship?.Name ?? "없음";

        return
            $"마스터 데이터 로드 완료 | 항구 {Catalog.Ports.Count} · 선박 {Catalog.Ships.Count} · 항해사 {Catalog.Officers.Count} · 유물 {Catalog.RelicsAndFigureheads.Count} · 괴수 {Catalog.Leviathans.Count} · 시나리오 {Catalog.Scenarios.Count}\n" +
            $"현재 날짜: {CurrentWorld.Clock.FormattedDate} | 기함: {flagshipName} | 함대: {CurrentWorld.PlayerFleet.Ships.Count}/{Fleet.MaxShips}척 | 소지금: {CurrentWorld.Gold:N0} G";
    }

    public string SaveToUserSlot(string fileName = "phase1_quicksave.json")
    {
        if (CurrentWorld is null)
        {
            return "실패: WorldState가 초기화되지 않았습니다.";
        }

        if (FogGrid is not null)
        {
            CurrentWorld.ExploredTileIndices = FogGrid.ToIndices();
        }

        string savePath = ProjectSettings.GlobalizePath($"user://{fileName}");
        SaveSerializer.SaveToFile(CurrentWorld, savePath);
        return $"저장 완료 ({fileName}, 탐험 타일 {CurrentWorld.ExploredTileIndices.Count}개)";
    }

    public string RunSaveLoadSelfTest()
    {
        if (CurrentWorld is null)
        {
            return "실패: WorldState가 초기화되지 않았습니다.";
        }

        if (FogGrid is not null)
        {
            CurrentWorld.ExploredTileIndices = FogGrid.ToIndices();
        }

        string savePath = ProjectSettings.GlobalizePath("user://phase0_save_test.json");
        CurrentWorld.Flags.SetFlag("phase0_selftest_verified");
        SaveSerializer.SaveToFile(CurrentWorld, savePath);
        WorldState restored = SaveSerializer.LoadFromFile(savePath);

        bool matched =
            restored.ProtagonistId == CurrentWorld.ProtagonistId &&
            restored.Gold == CurrentWorld.Gold &&
            restored.PlayerFleet.Ships.Count == CurrentWorld.PlayerFleet.Ships.Count &&
            restored.Flags.GetFlag("phase0_selftest_verified");

        if (!matched)
        {
            return "실패: 세이브/로드 왕복 검증 값이 일치하지 않습니다.";
        }

        CurrentWorld = restored;
        RebuildPhase1Runtime();
        return $"성공: 세이브/로드 왕복 검증 완료 ({Path.GetFileName(savePath)}, 함대 {restored.PlayerFleet.Ships.Count}/{Fleet.MaxShips}척)";
    }

    private static string ResolveDataDirectory()
    {
        string projectDir = ProjectSettings.GlobalizePath("res://");
        string repoData = Path.GetFullPath(Path.Combine(projectDir, "..", "data"));
        if (Directory.Exists(repoData))
        {
            return repoData;
        }

        string bundledData = ProjectSettings.GlobalizePath("res://data");
        if (Directory.Exists(bundledData))
        {
            return bundledData;
        }

        throw new DirectoryNotFoundException($"data 폴더를 찾을 수 없습니다. 확인한 경로: {repoData}, {bundledData}");
    }
}
