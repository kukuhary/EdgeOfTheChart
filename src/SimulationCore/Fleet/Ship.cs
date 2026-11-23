using EdgeOfTheChart.SimulationCore.Data;

namespace EdgeOfTheChart.SimulationCore.Fleet;

public sealed class Ship
{
    public string Id { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RigType Rig { get; set; } = RigType.Lateen;
    public int TotalCapacity { get; set; }
    public int MaxHullHp { get; set; }
    public int HullHp { get; set; }
    public int MinCrew { get; set; }
    public int CrewCount { get; set; }
    public int CabinCrewSlots { get; set; }
    public int CabinGunSlots { get; set; }
    public int CabinCargoSlots { get; set; }
    public string CannonTypeId { get; set; } = "cannon_pedrero";
    public string? FigureheadId { get; set; }
    public string? CaptainOfficerId { get; set; }
    public List<string> RelicIds { get; set; } = new();

    public void SetCabins(int crewSlots, int gunSlots, int cargoSlots)
    {
        if (crewSlots < 0 || gunSlots < 0 || cargoSlots < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crewSlots), "선실 배분 값은 음수일 수 없습니다.");
        }

        if (crewSlots + gunSlots + cargoSlots > TotalCapacity)
        {
            throw new InvalidOperationException(
                $"선실 합계({crewSlots + gunSlots + cargoSlots})가 선박 총 용적({TotalCapacity})을 초과했습니다.");
        }

        CabinCrewSlots = crewSlots;
        CabinGunSlots = gunSlots;
        CabinCargoSlots = cargoSlots;
    }

    public void ValidateCabins()
    {
        if (CabinCrewSlots < 0 || CabinGunSlots < 0 || CabinCargoSlots < 0)
        {
            throw new InvalidDataException($"선박 '{Id}'의 선실 배분에 음수가 포함되어 있습니다.");
        }

        if (CabinCrewSlots + CabinGunSlots + CabinCargoSlots > TotalCapacity)
        {
            throw new InvalidDataException($"선박 '{Id}'의 선실 합계가 총 용적({TotalCapacity})을 초과했습니다.");
        }
    }

    public static Ship FromTemplate(ShipTemplateData template, string instanceId, string customName)
    {
        ArgumentNullException.ThrowIfNull(template);

        var rig = template.RigType.ToLowerInvariant() switch
        {
            "lateen" => RigType.Lateen,
            "square" => RigType.Square,
            "hybrid" => RigType.Hybrid,
            _ => throw new InvalidDataException($"지원하지 않는 범장 타입입니다: {template.RigType}")
        };

        var ship = new Ship
        {
            Id = instanceId,
            TemplateId = template.Id,
            Name = string.IsNullOrWhiteSpace(customName) ? template.NameKo : customName,
            Rig = rig,
            TotalCapacity = template.TotalCapacity,
            MaxHullHp = template.MaxHullHp,
            HullHp = template.MaxHullHp,
            MinCrew = template.MinCrew,
            CrewCount = template.DefaultCabins.Crew
        };

        ship.SetCabins(
            template.DefaultCabins.Crew,
            template.DefaultCabins.Guns,
            template.DefaultCabins.Cargo);

        return ship;
    }
}
