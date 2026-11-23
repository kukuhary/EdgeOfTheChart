using EdgeOfTheChart.SimulationCore.Clock;
using EdgeOfTheChart.SimulationCore.World;

namespace EdgeOfTheChart.SimulationCore.Sailing;

public sealed class StormCell
{
    public string Id { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Radius { get; set; } = 3.5;
    public double Dx { get; set; }
    public double Dy { get; set; }
    public int Intensity { get; set; } = 2;
}

public readonly record struct WindSample(double DirectionDegrees, int Strength, bool IsCalmZone, bool IsInStorm)
{
    public string DirectionLabel
    {
        get
        {
            if (IsCalmZone || Strength == 0)
            {
                return "무풍";
            }

            double deg = ((DirectionDegrees % 360.0) + 360.0) % 360.0;
            int octant = (int)Math.Round(deg / 45.0) % 8;
            return octant switch
            {
                0 => "동향풍(→)",
                1 => "남동풍(↘)",
                2 => "남향풍(↓)",
                3 => "남서풍(↙)",
                4 => "서향풍(←)",
                5 => "북서풍(↖)",
                6 => "북향풍(↑)",
                7 => "북동풍(↗)",
                _ => "동향풍(→)"
            };
        }
    }
}

public readonly record struct CurrentSample(double Dx, double Dy);

public sealed class WindAndCurrentField
{
    public List<StormCell> StormCells { get; }

    public WindAndCurrentField(List<StormCell>? stormCells = null)
    {
        StormCells = stormCells ?? CreateDefaultStormCells();
    }

    public static List<StormCell> CreateDefaultStormCells() =>
        new()
        {
            new StormCell { Id = "storm_atlantic", X = 12.0, Y = 22.0, Radius = 4.0, Dx = 1.2, Dy = 0.6, Intensity = 3 },
            new StormCell { Id = "storm_lion", X = 69.0, Y = 26.0, Radius = 3.5, Dx = 0.9, Dy = 0.5, Intensity = 2 },
            new StormCell { Id = "storm_tyrrhenian", X = 91.0, Y = 38.0, Radius = 3.5, Dx = -0.8, Dy = 0.4, Intensity = 2 }
        };

    public WindSample SampleWind(double x, double y, SimulationClock clock)
    {
        foreach (StormCell storm in StormCells)
        {
            double dx = x - storm.X;
            double dy = y - storm.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist <= storm.Radius)
            {
                double swirlAngle = Math.Atan2(dy, dx) * 180.0 / Math.PI + 105.0;
                return new WindSample(NormalizeAngle(swirlAngle), 3, IsCalmZone: false, IsInStorm: true);
            }
        }

        // 여름철(6~8월) 스페인 남동 해역 국지 무풍지대 또는 원양 남서쪽 정체대
        bool inMedCalm = clock.Month is >= 6 and <= 8 && x is >= 43 and <= 49 && y is >= 37 and <= 41;
        bool inAtlanticCalm = x is >= 14 and <= 19 && y is >= 57 and <= 62;
        if (inMedCalm || inAtlanticCalm)
        {
            return new WindSample(0.0, 0, IsCalmZone: true, IsInStorm: false);
        }

        double seasonalShift = Math.Sin((clock.Month - 1) / 12.0 * Math.PI * 2.0) * 18.0;

        if (x < 34.0)
        {
            // 대서양 해역: 북부 편서풍(동~남동), 남부 북동무역풍(남서)
            double dir = y < 35.0 ? 20.0 + seasonalShift : 215.0 + seasonalShift * 0.5;
            return new WindSample(NormalizeAngle(dir), 2, IsCalmZone: false, IsInStorm: false);
        }

        // 서지중해 해역: 리옹만 미스트랄(남동), 중부 지중해 계절풍
        double medDir = x is >= 56.0 and <= 78.0 && y < 30.0
            ? 125.0 + seasonalShift * 0.4
            : 75.0 + seasonalShift;

        int strength = y > 45.0 ? 1 : 2;
        return new WindSample(NormalizeAngle(medDir), strength, IsCalmZone: false, IsInStorm: false);
    }

    public CurrentSample SampleCurrent(double x, double y)
    {
        // 지브롤터 해협 대서양 → 지중해 유입 해류
        if (x is >= 32.0 and <= 44.0 && y is >= 41.0 and <= 47.0)
        {
            return new CurrentSample(0.55, 0.0);
        }

        // 카나리아 남향 해류 (리스본 외해 ~ 마데이라)
        if (x <= 22.0 && y >= 30.0)
        {
            return new CurrentSample(-0.15, 0.40);
        }

        return new CurrentSample(0.05, 0.0);
    }

    public void AdvanceDay(SeaMapGrid map)
    {
        ArgumentNullException.ThrowIfNull(map);

        foreach (StormCell storm in StormCells)
        {
            double nextX = storm.X + storm.Dx;
            double nextY = storm.Y + storm.Dy;

            if (nextX < 4.0 || nextX >= map.Width - 4.0)
            {
                storm.Dx = -storm.Dx;
                nextX = Math.Clamp(storm.X + storm.Dx, 4.0, map.Width - 4.0);
            }

            if (nextY < 4.0 || nextY >= map.Height - 4.0)
            {
                storm.Dy = -storm.Dy;
                nextY = Math.Clamp(storm.Y + storm.Dy, 4.0, map.Height - 4.0);
            }

            // 중심이 내륙 깊숙이 들어가지 않도록 방향 반전
            if (!map.IsSailable((int)Math.Floor(nextX), (int)Math.Floor(nextY)))
            {
                storm.Dx = -storm.Dx;
                storm.Dy = -storm.Dy;
                nextX = Math.Clamp(storm.X + storm.Dx, 4.0, map.Width - 4.0);
                nextY = Math.Clamp(storm.Y + storm.Dy, 4.0, map.Height - 4.0);
            }

            storm.X = nextX;
            storm.Y = nextY;
        }
    }

    public static double NormalizeAngle(double degrees)
    {
        double result = degrees % 360.0;
        return result < 0 ? result + 360.0 : result;
    }
}
