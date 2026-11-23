namespace EdgeOfTheChart.SimulationCore.Factions;

public sealed class FactionState
{
    public string Id { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public int RelationScore { get; set; }
    public bool IsAtWar { get; set; }
}
