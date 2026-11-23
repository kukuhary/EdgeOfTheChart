using EdgeOfTheChart.SimulationCore.Data;
using EdgeOfTheChart.SimulationCore.Fleet;
using EdgeOfTheChart.SimulationCore.Officers;
using EdgeOfTheChart.SimulationCore.Sailing;
using EdgeOfTheChart.SimulationCore.World;

namespace EdgeOfTheChart.SimulationCore.Survival;

public sealed class DailySurvivalReport
{
    public int FoodConsumed { get; set; }
    public int WaterConsumed { get; set; }
    public int CrewLost { get; set; }
    public int HullDamage { get; set; }
    public List<string> Events { get; } = new();
    public bool RequiresRescue { get; set; }
    public RescueReason? RescueReason { get; set; }
}

public static class SurvivalEngine
{
    public static DailySurvivalReport ProcessDay(
        WorldState state,
        SeaMapGrid map,
        WindAndCurrentField field,
        IRandomProvider rng,
        MasterDataCatalog? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(rng);

        var report = new DailySurvivalReport();
        Fleet.Fleet fleet = state.PlayerFleet;
        Ship? flagship = fleet.Flagship;

        if (flagship is null)
        {
            report.RequiresRescue = true;
            report.RescueReason = RescueReason.Shipwreck;
            return report;
        }

        int totalCrew = fleet.Ships.Sum(s => s.CrewCount);
        int dailyNeed = Math.Max(1, (int)Math.Ceiling(totalCrew * 0.25 * (fleet.RationPercent / 100.0)));

        int actualFood = Math.Min(fleet.Food, dailyNeed);
        fleet.Food -= actualFood;
        report.FoodConsumed = actualFood;
        bool foodDepleted = actualFood < dailyNeed;

        int actualWater = Math.Min(fleet.Water, dailyNeed);
        fleet.Water -= actualWater;
        report.WaterConsumed = actualWater;
        bool waterDepleted = actualWater < dailyNeed;

        int moraleDelta = 0;

        if (foodDepleted)
        {
            moraleDelta -= 10;
            int lost = ApplyCrewLossRatio(fleet, 0.05);
            report.CrewLost += lost;
            AddEvent(state, report, $"식량 고갈! 선원 {lost}명이 굶주림으로 쓰러졌습니다.");
        }

        if (waterDepleted)
        {
            moraleDelta -= 15;
            int lost = ApplyCrewLossRatio(fleet, 0.10);
            report.CrewLost += lost;
            AddEvent(state, report, $"식수 고갈! 선원 {lost}명이 탈수로 쓰러졌습니다.");
        }

        if (fleet.RationPercent == 75)
        {
            fleet.ConsecutiveLowRationDays++;
            if (fleet.ConsecutiveLowRationDays >= 7)
            {
                moraleDelta -= 2;
            }
        }
        else if (fleet.RationPercent == 50)
        {
            fleet.ConsecutiveLowRationDays++;
            moraleDelta -= 5;
        }
        else
        {
            fleet.ConsecutiveLowRationDays = 0;
        }

        fleet.DaysAtSea++;

        // 괴혈병 발병 및 진행
        if (!fleet.HasScurvy && (fleet.DaysAtSea >= 14 || waterDepleted || fleet.RationPercent == 50))
        {
            double baseProb = fleet.DaysAtSea >= 14 ? 0.15 : 0.04;
            if (fleet.RationPercent == 50)
            {
                baseProb *= 2.5;
            }

            if (catalog is not null)
            {
                baseProb *= GetScurvyChanceMultiplier(flagship, catalog);
            }

            if (rng.NextDouble() < baseProb)
            {
                fleet.HasScurvy = true;
                AddEvent(state, report, "장기 항해로 괴혈병이 발생했습니다! 라임 주스 또는 항구 휴식이 필요합니다.");
            }
        }

        if (fleet.HasScurvy)
        {
            int lost = ApplyCrewLossRatio(fleet, 0.03);
            report.CrewLost += lost;
            moraleDelta -= 3;
            AddEvent(state, report, $"괴혈병 악화로 선원 {lost}명이 사망했습니다.");
        }

        // 폭풍 셀 내부 피해 판정
        WindSample wind = field.SampleWind(state.ShipX, state.ShipY, state.Clock);
        if (wind.IsInStorm)
        {
            int rawDamage = rng.NextInt(12, 25);
            int finalDamage = fleet.SailsReefed
                ? Math.Max(1, (int)Math.Round(rawDamage * 0.30))
                : rawDamage;

            flagship.HullHp = Math.Max(0, flagship.HullHp - finalDamage);
            for (int i = 1; i < fleet.Ships.Count; i++)
            {
                int escortDmg = Math.Max(1, finalDamage / 2);
                fleet.Ships[i].HullHp = Math.Max(0, fleet.Ships[i].HullHp - escortDmg);
            }

            report.HullDamage += finalDamage;
            moraleDelta -= 4;
            string reefNote = fleet.SailsReefed ? "(돛 접기로 피해 70% 경감)" : "(폭풍 직격!)";
            AddEvent(state, report, $"폭풍우 강타 {reefNote}: 기함 내구 -{finalDamage}");
        }

        fleet.Morale = Math.Clamp(fleet.Morale + moraleDelta, 0, 100);

        // 사기 저하에 따른 반란 게이지 및 진압 판정
        if (fleet.Morale <= 15)
        {
            int riskGain = fleet.Morale == 0 ? 50 : 25;
            fleet.MutinyRisk = Math.Clamp(fleet.MutinyRisk + riskGain, 0, 100);
        }
        else
        {
            fleet.MutinyRisk = Math.Max(0, fleet.MutinyRisk - 10);
        }

        if (flagship.HullHp <= 0)
        {
            report.RequiresRescue = true;
            report.RescueReason = RescueReason.Shipwreck;
            AddEvent(state, report, $"{flagship.Name}의 선체가 파괴되어 침몰했습니다!");
            return report;
        }

        if (fleet.Ships.Sum(s => s.CrewCount) <= 0)
        {
            report.RequiresRescue = true;
            report.RescueReason = RescueReason.CrewLost;
            AddEvent(state, report, "모든 선원이 사망해 함대가 표류하고 있습니다!");
            return report;
        }

        if (fleet.MutinyRisk >= 100 || fleet.Morale == 0)
        {
            Officer? firstMate = state.Officers.FirstOrDefault(
                o => o.AssignedRole == OfficerRole.FirstMate && o.Loyalty >= 70);

            if (firstMate is not null && !state.Flags.GetFlag("mutiny_suppressed_this_voyage"))
            {
                state.Flags.SetFlag("mutiny_suppressed_this_voyage", true);
                fleet.Morale = 30;
                fleet.MutinyRisk = 40;
                firstMate.AdjustLoyalty(-10);
                AddEvent(state, report, $"부관 {firstMate.Name}이(가) 선상 반란을 가까스로 진압했습니다!");
            }
            else
            {
                report.RequiresRescue = true;
                report.RescueReason = RescueReason.Mutiny;
                AddEvent(state, report, "선원 반란이 폭발해 함대를 빼앗겼습니다!");
            }
        }

        return report;
    }

    public static (bool Success, string Message) PerformEmergencyForage(WorldState state, IRandomProvider rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(rng);

        Fleet.Fleet fleet = state.PlayerFleet;
        int currentStamp = state.Clock.DayStamp;
        if (currentStamp - fleet.LastForageDayStamp < 2)
        {
            return (false, "낚시·빗물 채집은 2일마다 한 번씩만 시도할 수 있습니다.");
        }

        state.Clock.AdvanceHours(12);
        int gainedFood = rng.NextInt(4, 11);
        int gainedWater = rng.NextInt(3, 9);
        fleet.Food += gainedFood;
        fleet.Water += gainedWater;
        fleet.LastForageDayStamp = state.Clock.DayStamp;

        string msg = $"비상 낚시·빗물 채집(12시간 경과): 식량 +{gainedFood}, 식수 +{gainedWater}";
        state.AddVoyageLog(msg);
        return (true, msg);
    }

    public static (bool Success, string Message) PerformEmergencyRepair(WorldState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        Fleet.Fleet fleet = state.PlayerFleet;
        Ship? flagship = fleet.Flagship;
        if (flagship is null)
        {
            return (false, "수리할 기함이 없습니다.");
        }

        if (flagship.HullHp >= flagship.MaxHullHp)
        {
            return (false, "기함 선체가 이미 최대 내구도입니다.");
        }

        if (fleet.RepairWood < 5)
        {
            return (false, "수리용 목재가 부족합니다(필요: 5).");
        }

        fleet.RepairWood -= 5;
        int before = flagship.HullHp;
        flagship.HullHp = Math.Min(flagship.MaxHullHp, flagship.HullHp + 25);
        int healed = flagship.HullHp - before;

        string msg = $"해상 응급 수리: 목재 5 소모 → 기함 내구도 +{healed} ({flagship.HullHp}/{flagship.MaxHullHp})";
        state.AddVoyageLog(msg);
        return (true, msg);
    }

    public static (bool Success, string Message) UseLimeJuice(WorldState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        Fleet.Fleet fleet = state.PlayerFleet;
        if (fleet.LimeJuice <= 0)
        {
            return (false, "라임 주스가 남아 있지 않습니다.");
        }

        fleet.LimeJuice -= 1;
        fleet.HasScurvy = false;
        fleet.DaysAtSea = 0;
        fleet.Morale = Math.Clamp(fleet.Morale + 5, 0, 100);

        const string msg = "라임 주스를 배급해 괴혈병을 치유하고 항해 피로를 씻었습니다 (사기 +5).";
        state.AddVoyageLog(msg);
        return (true, msg);
    }

    public static (bool Success, string Message) ResupplyAtPort(
        WorldState state,
        int addFood,
        int addWater,
        int addWood,
        int addLime)
    {
        int cost = addFood * 2 + addWater * 1 + addWood * 12 + addLime * 35;
        if (cost > state.Gold)
        {
            return (false, $"자금이 부족합니다 (필요: {cost:N0} G / 보유: {state.Gold:N0} G).");
        }

        state.Gold -= cost;
        state.PlayerFleet.Food += addFood;
        state.PlayerFleet.Water += addWater;
        state.PlayerFleet.RepairWood += addWood;
        state.PlayerFleet.LimeJuice += addLime;
        state.PlayerFleet.DaysAtSea = 0;
        state.PlayerFleet.HasScurvy = false;
        state.Flags.SetFlag("mutiny_suppressed_this_voyage", false);

        string msg = $"항구 보급 완료 (-{cost:N0} G): 식량 +{addFood}, 식수 +{addWater}, 목재 +{addWood}, 라임 +{addLime}";
        state.AddVoyageLog(msg);
        return (true, msg);
    }

    public static (bool Success, string Message) RepairAtPort(WorldState state)
    {
        int missingHp = state.PlayerFleet.Ships.Sum(s => Math.Max(0, s.MaxHullHp - s.HullHp));
        if (missingHp == 0)
        {
            return (false, "수리가 필요한 함선이 없습니다.");
        }

        int cost = missingHp * 2;
        if (cost > state.Gold)
        {
            return (false, $"수리 자금이 부족합니다 (필요: {cost:N0} G).");
        }

        state.Gold -= cost;
        foreach (Ship ship in state.PlayerFleet.Ships)
        {
            ship.HullHp = ship.MaxHullHp;
        }

        string msg = $"조선소 완전 수리 완료 (-{cost:N0} G, 내구 +{missingHp}).";
        state.AddVoyageLog(msg);
        return (true, msg);
    }

    public static (bool Success, string Message) RestCrewAtPort(WorldState state)
    {
        const int restCost = 100;
        int actualCost = state.Gold >= restCost ? restCost : 0;
        state.Gold -= actualCost;

        state.Clock.AdvanceHours(24);
        Fleet.Fleet fleet = state.PlayerFleet;
        fleet.Morale = Math.Max(fleet.Morale, 85);
        fleet.MutinyRisk = 0;
        fleet.HasScurvy = false;
        fleet.DaysAtSea = 0;
        fleet.ConsecutiveLowRationDays = 0;
        state.Flags.SetFlag("mutiny_suppressed_this_voyage", false);

        string msg = $"항구 여관·주점 휴식 (-{actualCost:N0} G, 1일 경과): 사기 {fleet.Morale}, 피로 초기화.";
        state.AddVoyageLog(msg);
        return (true, msg);
    }

    public static (bool Success, string Message) RecruitCrewAtPort(WorldState state, int count)
    {
        Ship? flagship = state.PlayerFleet.Flagship;
        if (flagship is null)
        {
            return (false, "기함이 없습니다.");
        }

        int space = flagship.CabinCrewSlots - flagship.CrewCount;
        if (space <= 0)
        {
            return (false, "기함 선원실이 가득 찼습니다.");
        }

        int recruitCount = Math.Min(count, space);
        int cost = recruitCount * 10;
        if (cost > state.Gold)
        {
            return (false, $"선원 모집 자금이 부족합니다 (필요: {cost:N0} G).");
        }

        state.Gold -= cost;
        flagship.CrewCount += recruitCount;
        string msg = $"선원 {recruitCount}명 모집 완료 (-{cost:N0} G, 현재 {flagship.CrewCount}/{flagship.CabinCrewSlots}명).";
        state.AddVoyageLog(msg);
        return (true, msg);
    }

    public static (bool Success, string Message) RepayDebtAtPort(WorldState state, int maxAmount)
    {
        if (state.Debt <= 0)
        {
            return (false, "상환할 채무가 없습니다.");
        }

        int pay = Math.Min(state.Gold, Math.Min(state.Debt, maxAmount));
        if (pay <= 0)
        {
            return (false, "상환할 보유 금화가 부족합니다.");
        }

        state.Gold -= pay;
        state.Debt -= pay;
        string msg = $"은행 채무 상환: -{pay:N0} G (남은 채무: {state.Debt:N0} G).";
        state.AddVoyageLog(msg);
        return (true, msg);
    }

    private static int ApplyCrewLossRatio(Fleet.Fleet fleet, double ratio)
    {
        int totalLost = 0;
        foreach (Ship ship in fleet.Ships)
        {
            if (ship.CrewCount <= 0)
            {
                continue;
            }

            int loss = Math.Min(ship.CrewCount, Math.Max(1, (int)Math.Ceiling(ship.CrewCount * ratio)));
            ship.CrewCount -= loss;
            totalLost += loss;
        }

        return totalLost;
    }

    private static double GetScurvyChanceMultiplier(Ship flagship, MasterDataCatalog catalog)
    {
        double mult = 1.0;
        foreach (string relicId in flagship.RelicIds)
        {
            RelicOrFigureheadData? relic = catalog.RelicsAndFigureheads
                .FirstOrDefault(r => string.Equals(r.Id, relicId, StringComparison.Ordinal));
            if (relic is null)
            {
                continue;
            }

            foreach (StatModifierData mod in relic.Modifiers)
            {
                if (string.Equals(mod.Stat, "scurvy_chance_mult", StringComparison.Ordinal))
                {
                    mult *= mod.Value;
                }
            }
        }

        return mult;
    }

    private static void AddEvent(WorldState state, DailySurvivalReport report, string message)
    {
        report.Events.Add(message);
        state.AddVoyageLog(message);
    }
}
