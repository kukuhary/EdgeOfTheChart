namespace EdgeOfTheChart.SimulationCore.Fleet;

public sealed class Fleet
{
    public const int MaxShips = 3;

    public List<Ship> Ships { get; set; } = new();
    public int Food { get; set; }
    public int Water { get; set; }
    public int RepairWood { get; set; }
    public int AmmoNormal { get; set; }
    public int AmmoSilver { get; set; }
    public int AmmoFireArrow { get; set; }
    public int AmmoHarpoon { get; set; }
    public int RationPercent { get; set; } = 100;
    public int Morale { get; set; } = 80;
    public int CurseGauge { get; set; } = 0;

    public Ship? Flagship => Ships.Count > 0 ? Ships[0] : null;

    public bool TryAddShip(Ship ship)
    {
        ArgumentNullException.ThrowIfNull(ship);

        if (Ships.Count >= MaxShips)
        {
            return false;
        }

        if (Ships.Any(s => string.Equals(s.Id, ship.Id, StringComparison.Ordinal)))
        {
            return false;
        }

        ship.ValidateCabins();
        Ships.Add(ship);
        return true;
    }

    public void AddShip(Ship ship)
    {
        if (!TryAddShip(ship))
        {
            if (Ships.Count >= MaxShips)
            {
                throw new InvalidOperationException($"함대는 최대 {MaxShips}척까지만 편성할 수 있습니다.");
            }

            throw new InvalidOperationException($"중복된 선박 ID '{ship.Id}'는 추가할 수 없습니다.");
        }
    }

    public void AssignFlagship(int index)
    {
        if (index < 0 || index >= Ships.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "유효하지 않은 선박 인덱스입니다.");
        }

        if (index == 0)
        {
            return;
        }

        Ship target = Ships[index];
        Ships.RemoveAt(index);
        Ships.Insert(0, target);
    }

    public bool RemoveShip(string shipId)
    {
        int idx = Ships.FindIndex(s => string.Equals(s.Id, shipId, StringComparison.Ordinal));
        if (idx < 0)
        {
            return false;
        }

        Ships.RemoveAt(idx);
        return true;
    }

    public void SetRationPercent(int percent)
    {
        if (percent is not (50 or 75 or 100))
        {
            throw new ArgumentOutOfRangeException(
                nameof(percent),
                percent,
                "배급률은 50, 75, 100(%)만 허용됩니다.");
        }

        RationPercent = percent;
    }
}
