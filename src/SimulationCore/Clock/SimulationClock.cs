namespace EdgeOfTheChart.SimulationCore.Clock;

public sealed class SimulationClock
{
    private static readonly int[] AllowedSpeeds = { 0, 1, 2, 4 };

    public int Year { get; set; } = 1522;
    public int Month { get; set; } = 5;
    public int Day { get; set; } = 1;
    public int Hour { get; set; } = 8;
    public int SpeedMultiplier { get; set; } = 1;

    public string FormattedDate => $"{Year:0000}-{Month:00}-{Day:00} {Hour:00}:00";
    public int DayStamp => (new DateTime(Year, Month, Day) - new DateTime(1522, 1, 1)).Days;

    public void SetSpeed(int speedMultiplier)
    {
        if (Array.IndexOf(AllowedSpeeds, speedMultiplier) < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speedMultiplier),
                speedMultiplier,
                "배속은 0(일시정지), 1, 2, 4만 허용됩니다.");
        }

        SpeedMultiplier = speedMultiplier;
    }

    public void AdvanceHours(int hours)
    {
        if (hours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hours), hours, "경과 시간은 음수일 수 없습니다.");
        }

        if (hours == 0)
        {
            return;
        }

        Hour += hours;
        while (Hour >= 24)
        {
            Hour -= 24;
            Day += 1;

            int daysInMonth = DateTime.DaysInMonth(Year, Month);
            if (Day > daysInMonth)
            {
                Day = 1;
                Month += 1;
                if (Month > 12)
                {
                    Month = 1;
                    Year += 1;
                }
            }
        }
    }
}
