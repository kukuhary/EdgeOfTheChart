using EdgeOfTheChart.SimulationCore.Data;

namespace EdgeOfTheChart.SimulationCore.Officers;

public enum OfficerRole
{
    None,
    FirstMate,
    Navigator,
    Purser,
    MasterGunner,
    EscortCaptain,
    PortGovernor
}

public sealed class Officer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public int NavLevel { get; set; } = 1;
    public int CombatLevel { get; set; } = 1;
    public OfficerRole AssignedRole { get; set; } = OfficerRole.None;
    public int Loyalty { get; set; } = 70;
    public List<string> Personalities { get; set; } = new();
    public string? AssignedShipId { get; set; }
    public string? GovernedPortId { get; set; }
    public string? PersonalQuestId { get; set; }
    public string? PortraitId { get; set; }

    public void AdjustLoyalty(int delta)
    {
        Loyalty = Math.Clamp(Loyalty + delta, 0, 100);
    }

    public static Officer FromData(OfficerData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new Officer
        {
            Id = data.Id,
            Name = data.NameKo,
            Nationality = data.Nationality,
            NavLevel = data.NavLevel,
            CombatLevel = data.CombatLevel,
            AssignedRole = ParseRole(data.RecommendedRole),
            Loyalty = Math.Clamp(data.InitialLoyalty, 0, 100),
            Personalities = new List<string>(data.Personalities),
            PersonalQuestId = data.PersonalQuestId,
            PortraitId = data.PortraitId
        };
    }

    public static OfficerRole ParseRole(string? roleText)
    {
        return roleText?.ToLowerInvariant() switch
        {
            "first_mate" => OfficerRole.FirstMate,
            "navigator" => OfficerRole.Navigator,
            "purser" => OfficerRole.Purser,
            "master_gunner" => OfficerRole.MasterGunner,
            "escort_captain" => OfficerRole.EscortCaptain,
            "port_governor" => OfficerRole.PortGovernor,
            _ => OfficerRole.None
        };
    }
}
