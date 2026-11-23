using EdgeOfTheChart.SimulationCore.Clock;
using EdgeOfTheChart.SimulationCore.Data;
using EdgeOfTheChart.SimulationCore.Factions;
using EdgeOfTheChart.SimulationCore.Narrative;
using EdgeOfTheChart.SimulationCore.Officers;
using EdgeOfTheChart.SimulationCore.Routing;
using EdgeOfTheChart.SimulationCore.Sailing;

namespace EdgeOfTheChart.SimulationCore.World;

public sealed class WorldState
{
    public const int CurrentSaveVersion = 1;
    private const int MaxVoyageLogEntries = 20;

    public string ProtagonistId { get; set; } = "hero_diego";
    public GameSceneId CurrentScene { get; set; } = GameSceneId.Title;
    public string? CurrentPortId { get; set; }
    public double ShipX { get; set; } = 24.5;
    public double ShipY { get; set; } = 31.5;
    public double HeadingDegrees { get; set; } = 270.0;
    public SimulationClock Clock { get; set; } = new();
    public Fleet.Fleet PlayerFleet { get; set; } = new();
    public List<Officer> Officers { get; set; } = new();
    public List<PortState> Ports { get; set; } = new();
    public List<FactionState> Factions { get; set; } = new();
    public StoryFlagStore Flags { get; set; } = new();
    public List<int> ExploredTileIndices { get; set; } = new();
    public List<string> VoyageLog { get; set; } = new();
    public List<StormCell> StormCells { get; set; } = new();
    public int Gold { get; set; }
    public int Debt { get; set; }

    public void AddVoyageLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        VoyageLog.Add($"[{Clock.Month:00}/{Clock.Day:00}] {message}");
        if (VoyageLog.Count > MaxVoyageLogEntries)
        {
            VoyageLog.RemoveRange(0, VoyageLog.Count - MaxVoyageLogEntries);
        }
    }

    public static WorldState CreateNewGameSample(MasterDataCatalog catalog, string protagonistId = "hero_diego")
    {
        ArgumentNullException.ThrowIfNull(catalog);

        (string starterPortId, string starterShipTemplateId, string starterShipName, string starterOfficerId, int gold, int debt) =
            protagonistId switch
            {
                "hero_isabel" => ("port_seville", "ship_armed_caravel", "라 벤간사", "off_mateo_ruiz", 3200, 0),
                "hero_hendrik" => ("port_amsterdam", "ship_ice_barque", "노르드스테르", "off_ingrid_lind", 4200, 800),
                "hero_lorenzo" => ("port_venice", "ship_merchant_carrack", "레오네 도로", "off_pietro_bellini", 7500, 3000),
                "hero_arthur" => ("port_london", "ship_early_galleon", "블랙 호크", "off_william_drake", 5500, 0),
                "hero_seojin" => ("port_busanpo", "ship_joseon_haeryong_hybrid", "해룡", "off_choi_mugang", 4000, 0),
                _ => ("port_lisbon", "ship_caravel_latina", "에스페란사", "off_goncalo_alves", 5000, 1200)
            };

        ShipTemplateData shipTemplate = catalog.FindShip(starterShipTemplateId)
            ?? throw new InvalidDataException($"초기 선박 템플릿을 찾을 수 없습니다: {starterShipTemplateId}");

        OfficerData officerData = catalog.FindOfficer(starterOfficerId)
            ?? throw new InvalidDataException($"초기 동료 데이터를 찾을 수 없습니다: {starterOfficerId}");

        List<PortState> ports = catalog.Ports.Select(PortState.FromData).ToList();
        PortState? starterPort = ports.FirstOrDefault(p => string.Equals(p.Id, starterPortId, StringComparison.Ordinal));
        double startX = starterPort?.Phase1X is int px ? px + 0.5 : 24.5;
        double startY = starterPort?.Phase1Y is int py ? py + 0.5 : 31.5;

        var state = new WorldState
        {
            ProtagonistId = protagonistId,
            CurrentScene = GameSceneId.WorldMap,
            CurrentPortId = starterPortId,
            ShipX = startX,
            ShipY = startY,
            HeadingDegrees = 180.0,
            Clock = new SimulationClock { Year = 1522, Month = 5, Day = 1, Hour = 8, SpeedMultiplier = 1 },
            Gold = gold,
            Debt = debt,
            Ports = ports,
            Factions = CreateDefaultFactions(),
            StormCells = WindAndCurrentField.CreateDefaultStormCells()
        };

        var flagship = Fleet.Ship.FromTemplate(shipTemplate, "ship_flagship_01", starterShipName);
        flagship.CaptainOfficerId = officerData.Id;
        state.PlayerFleet.AddShip(flagship);
        state.PlayerFleet.Food = 200;
        state.PlayerFleet.Water = 200;
        state.PlayerFleet.RepairWood = 30;
        state.PlayerFleet.AmmoNormal = 60;
        state.PlayerFleet.AmmoSilver = 10;
        state.PlayerFleet.LimeJuice = 5;

        Officer starterOfficer = Officer.FromData(officerData);
        starterOfficer.AssignedShipId = flagship.Id;
        state.Officers.Add(starterOfficer);

        string portName = starterPort?.NameKo ?? "리스본";
        state.AddVoyageLog($"{portName} 외항에서 {flagship.Name} 출항 준비 완료.");

        return state;
    }

    private static List<FactionState> CreateDefaultFactions() =>
        new()
        {
            new FactionState { Id = "faction_portugal", NameKo = "포르투갈 왕국", RelationScore = 20 },
            new FactionState { Id = "faction_spain", NameKo = "스페인 제국", RelationScore = 0 },
            new FactionState { Id = "faction_netherlands", NameKo = "네덜란드 상인연방", RelationScore = 10 },
            new FactionState { Id = "faction_venice", NameKo = "베네치아 공화국", RelationScore = 10 },
            new FactionState { Id = "faction_england", NameKo = "잉글랜드 왕국", RelationScore = 5 },
            new FactionState { Id = "faction_joseon", NameKo = "조선", RelationScore = 0 },
            new FactionState { Id = "faction_ottoman", NameKo = "오스만 제국", RelationScore = -10 },
            new FactionState { Id = "faction_player_independent", NameKo = "독립 자유 선단", RelationScore = 100 }
        };
}
