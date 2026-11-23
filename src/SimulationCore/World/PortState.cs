using EdgeOfTheChart.SimulationCore.Data;

namespace EdgeOfTheChart.SimulationCore.World;

public sealed class PortState
{
    public string Id { get; set; } = string.Empty;
    public string NameKo { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public string Size { get; set; } = "major";
    public int WorldX { get; set; }
    public int WorldY { get; set; }
    public int? Phase1X { get; set; }
    public int? Phase1Y { get; set; }
    public string OwnerFactionId { get; set; } = string.Empty;
    public string? GovernorOfficerId { get; set; }
    public int CommerceDev { get; set; }
    public int IndustryDev { get; set; }
    public int FortLevel { get; set; }
    public int TaxRate { get; set; } = 10;
    public int PublicOrder { get; set; } = 75;
    public List<string> Facilities { get; set; } = new();
    public List<string> Specialties { get; set; } = new();

    public static PortState FromData(PortData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new PortState
        {
            Id = data.Id,
            NameKo = data.NameKo,
            Culture = data.Culture,
            Size = data.Size,
            WorldX = data.WorldTile.X,
            WorldY = data.WorldTile.Y,
            Phase1X = data.Phase1Tile?.X,
            Phase1Y = data.Phase1Tile?.Y,
            OwnerFactionId = data.OwnerFaction,
            CommerceDev = data.CommerceDev,
            IndustryDev = data.IndustryDev,
            FortLevel = data.FortLevel,
            Facilities = new List<string>(data.Facilities),
            Specialties = new List<string>(data.Specialties)
        };
    }
}
