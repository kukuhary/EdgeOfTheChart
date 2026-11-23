using EdgeOfTheChart.SimulationCore.Data;
using EdgeOfTheChart.SimulationCore.Fleet;
using EdgeOfTheChart.SimulationCore.World;

namespace EdgeOfTheChart.SimulationCore.Survival;

public enum RescueReason
{
    Shipwreck,
    CrewLost,
    Mutiny
}

public sealed record RescueReport(
    RescueReason Reason,
    string RescuePortId,
    string RescuePortName,
    int AddedDebt,
    int NewTotalDebt,
    string SummaryMessage);

public static class RescueService
{
    public static RescueReport ExecuteRescue(
        WorldState state,
        MasterDataCatalog catalog,
        RescueReason reason)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(catalog);

        PortState rescuePort = SelectRescuePort(state);
        bool isIndependent = string.Equals(
            rescuePort.OwnerFactionId,
            "faction_player_independent",
            StringComparison.Ordinal);

        int addedDebt = isIndependent ? 500 : 1500;
        state.Debt += addedDebt;

        ShipTemplateData caravelTemplate = catalog.FindShip("ship_caravel_latina")
            ?? catalog.Ships.FirstOrDefault()
            ?? throw new InvalidDataException("구조용 기본 선박 템플릿을 찾을 수 없습니다.");

        state.PlayerFleet.Ships.Clear();
        var recoveryShip = Ship.FromTemplate(caravelTemplate, $"ship_rescue_{state.Clock.DayStamp}", "재기의 카라벨");
        recoveryShip.HullHp = Math.Max(1, (int)Math.Round(recoveryShip.MaxHullHp * 0.60));
        recoveryShip.CrewCount = Math.Clamp(20, recoveryShip.MinCrew, recoveryShip.CabinCrewSlots);
        if (state.Officers.Count > 0)
        {
            recoveryShip.CaptainOfficerId = state.Officers[0].Id;
            state.Officers[0].AssignedShipId = recoveryShip.Id;
        }

        state.PlayerFleet.AddShip(recoveryShip);
        state.PlayerFleet.Food = 80;
        state.PlayerFleet.Water = 80;
        state.PlayerFleet.RepairWood = 10;
        state.PlayerFleet.LimeJuice = 2;
        state.PlayerFleet.Morale = 55;
        state.PlayerFleet.HasScurvy = false;
        state.PlayerFleet.DaysAtSea = 0;
        state.PlayerFleet.ConsecutiveLowRationDays = 0;
        state.PlayerFleet.MutinyRisk = 0;
        state.PlayerFleet.SailsReefed = false;
        state.Flags.SetFlag("mutiny_suppressed_this_voyage", false);

        state.ShipX = (rescuePort.Phase1X ?? 24) + 0.5;
        state.ShipY = (rescuePort.Phase1Y ?? 31) + 0.5;
        state.CurrentPortId = rescuePort.Id;

        string reasonLabel = reason switch
        {
            RescueReason.Shipwreck => "선체 파손으로 난파",
            RescueReason.CrewLost => "선원 전멸로 표류",
            RescueReason.Mutiny => "선원 반란으로 함선 상실",
            _ => "해상 조난"
        };

        string summary =
            $"{reasonLabel} 후 {rescuePort.NameKo} 항구로 구조되었습니다. " +
            $"소형선 '{recoveryShip.Name}' 1척과 구조 채무 {addedDebt:N0} G를 안고 재기합니다.";
        state.AddVoyageLog(summary);

        return new RescueReport(
            reason,
            rescuePort.Id,
            rescuePort.NameKo,
            addedDebt,
            state.Debt,
            summary);
    }

    private static PortState SelectRescuePort(WorldState state)
    {
        List<PortState> phase1Ports = state.Ports
            .Where(p => p.Phase1X.HasValue && p.Phase1Y.HasValue)
            .ToList();

        List<PortState> independentPorts = phase1Ports
            .Where(p => string.Equals(p.OwnerFactionId, "faction_player_independent", StringComparison.Ordinal))
            .ToList();

        if (independentPorts.Count > 0)
        {
            return independentPorts
                .OrderBy(p => DistanceSq(state.ShipX, state.ShipY, p.Phase1X!.Value + 0.5, p.Phase1Y!.Value + 0.5))
                .First();
        }

        string homePortId = state.ProtagonistId switch
        {
            "hero_isabel" => "port_seville",
            "hero_lorenzo" => "port_venice",
            _ => "port_lisbon"
        };

        return phase1Ports.FirstOrDefault(p => string.Equals(p.Id, homePortId, StringComparison.Ordinal))
            ?? phase1Ports.FirstOrDefault(p => string.Equals(p.Id, "port_lisbon", StringComparison.Ordinal))
            ?? phase1Ports.First();
    }

    private static double DistanceSq(double ax, double ay, double bx, double by)
    {
        double dx = ax - bx;
        double dy = ay - by;
        return dx * dx + dy * dy;
    }
}
