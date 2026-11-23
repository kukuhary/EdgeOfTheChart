using System.Text.Json.Serialization;

namespace EdgeOfTheChart.SimulationCore.Data;

public sealed class MasterDataCatalog
{
    public IReadOnlyList<PortData> Ports { get; init; } = Array.Empty<PortData>();
    public IReadOnlyList<ShipTemplateData> Ships { get; init; } = Array.Empty<ShipTemplateData>();
    public IReadOnlyList<OfficerData> Officers { get; init; } = Array.Empty<OfficerData>();
    public IReadOnlyList<RelicOrFigureheadData> RelicsAndFigureheads { get; init; } = Array.Empty<RelicOrFigureheadData>();
    public IReadOnlyList<LeviathanData> Leviathans { get; init; } = Array.Empty<LeviathanData>();
    public IReadOnlyList<ScenarioData> Scenarios { get; init; } = Array.Empty<ScenarioData>();

    public PortData? FindPort(string id) =>
        Ports.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));

    public ShipTemplateData? FindShip(string id) =>
        Ships.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal));

    public OfficerData? FindOfficer(string id) =>
        Officers.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.Ordinal));
}

public sealed class PortsFileData
{
    [JsonPropertyName("ports")]
    public List<PortData> Ports { get; set; } = new();
}

public sealed class PortData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name_ko")]
    public string NameKo { get; set; } = string.Empty;

    [JsonPropertyName("culture")]
    public string Culture { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public string Size { get; set; } = "major";

    [JsonPropertyName("custom_scene")]
    public string? CustomScene { get; set; }

    [JsonPropertyName("world_tile")]
    public GridPointData WorldTile { get; set; } = new();

    [JsonPropertyName("owner_faction")]
    public string OwnerFaction { get; set; } = string.Empty;

    [JsonPropertyName("commerce_dev")]
    public int CommerceDev { get; set; }

    [JsonPropertyName("industry_dev")]
    public int IndustryDev { get; set; }

    [JsonPropertyName("fort_level")]
    public int FortLevel { get; set; }

    [JsonPropertyName("facilities")]
    public List<string> Facilities { get; set; } = new();

    [JsonPropertyName("specialties")]
    public List<string> Specialties { get; set; } = new();
}

public sealed class GridPointData
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}

public sealed class ShipsFileData
{
    [JsonPropertyName("ships")]
    public List<ShipTemplateData> Ships { get; set; } = new();
}

public sealed class ShipTemplateData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name_ko")]
    public string NameKo { get; set; } = string.Empty;

    [JsonPropertyName("culture_origin")]
    public string CultureOrigin { get; set; } = string.Empty;

    [JsonPropertyName("rig_type")]
    public string RigType { get; set; } = "lateen";

    [JsonPropertyName("base_speed")]
    public int BaseSpeed { get; set; }

    [JsonPropertyName("turn_ap")]
    public int TurnAp { get; set; }

    [JsonPropertyName("max_hull_hp")]
    public int MaxHullHp { get; set; }

    [JsonPropertyName("armor")]
    public int Armor { get; set; }

    [JsonPropertyName("total_capacity")]
    public int TotalCapacity { get; set; }

    [JsonPropertyName("min_crew")]
    public int MinCrew { get; set; }

    [JsonPropertyName("default_cabins")]
    public CabinAllocationData DefaultCabins { get; set; } = new();

    [JsonPropertyName("max_gun_slots")]
    public int MaxGunSlots { get; set; }

    [JsonPropertyName("ice_resistant")]
    public bool IceResistant { get; set; }

    [JsonPropertyName("oar_assisted")]
    public bool OarAssisted { get; set; }

    [JsonPropertyName("base_price")]
    public int BasePrice { get; set; }

    [JsonPropertyName("sprite_id")]
    public string SpriteId { get; set; } = string.Empty;
}

public sealed class CabinAllocationData
{
    [JsonPropertyName("crew")]
    public int Crew { get; set; }

    [JsonPropertyName("guns")]
    public int Guns { get; set; }

    [JsonPropertyName("cargo")]
    public int Cargo { get; set; }
}

public sealed class OfficersFileData
{
    [JsonPropertyName("officers")]
    public List<OfficerData> Officers { get; set; } = new();
}

public sealed class OfficerData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name_ko")]
    public string NameKo { get; set; } = string.Empty;

    [JsonPropertyName("nationality")]
    public string Nationality { get; set; } = string.Empty;

    [JsonPropertyName("nav_level")]
    public int NavLevel { get; set; }

    [JsonPropertyName("combat_level")]
    public int CombatLevel { get; set; }

    [JsonPropertyName("stats")]
    public OfficerStatsData Stats { get; set; } = new();

    [JsonPropertyName("personalities")]
    public List<string> Personalities { get; set; } = new();

    [JsonPropertyName("initial_loyalty")]
    public int InitialLoyalty { get; set; }

    [JsonPropertyName("recruit_cost")]
    public int RecruitCost { get; set; }

    [JsonPropertyName("recommended_role")]
    public string RecommendedRole { get; set; } = "none";

    [JsonPropertyName("personal_quest_id")]
    public string? PersonalQuestId { get; set; }

    [JsonPropertyName("portrait_id")]
    public string PortraitId { get; set; } = string.Empty;
}

public sealed class OfficerStatsData
{
    [JsonPropertyName("helm")]
    public int Helm { get; set; }

    [JsonPropertyName("survey")]
    public int Survey { get; set; }

    [JsonPropertyName("negotiate")]
    public int Negotiate { get; set; }

    [JsonPropertyName("gunnery")]
    public int Gunnery { get; set; }

    [JsonPropertyName("leadership")]
    public int Leadership { get; set; }
}

public sealed class RelicsFileData
{
    [JsonPropertyName("items")]
    public List<RelicOrFigureheadData> Items { get; set; } = new();
}

public sealed class RelicOrFigureheadData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("slot_type")]
    public string SlotType { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("name_ko")]
    public string NameKo { get; set; } = string.Empty;

    [JsonPropertyName("modifiers")]
    public List<StatModifierData> Modifiers { get; set; } = new();
}

public sealed class StatModifierData
{
    [JsonPropertyName("stat")]
    public string Stat { get; set; } = string.Empty;

    [JsonPropertyName("op")]
    public string Op { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

public sealed class LeviathansFileData
{
    [JsonPropertyName("leviathans")]
    public List<LeviathanData> Leviathans { get; set; } = new();
}

public sealed class LeviathanData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name_ko")]
    public string NameKo { get; set; } = string.Empty;

    [JsonPropertyName("grid_footprint")]
    public GridFootprintData GridFootprint { get; set; } = new();

    [JsonPropertyName("weak_ammo")]
    public List<string> WeakAmmo { get; set; } = new();

    [JsonPropertyName("parts")]
    public List<LeviathanPartData> Parts { get; set; } = new();

    [JsonPropertyName("telegraph_patterns")]
    public List<TelegraphPatternData> TelegraphPatterns { get; set; } = new();
}

public sealed class GridFootprintData
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public sealed class LeviathanPartData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name_ko")]
    public string NameKo { get; set; } = string.Empty;

    [JsonPropertyName("hp")]
    public int Hp { get; set; }

    [JsonPropertyName("armor")]
    public int Armor { get; set; }

    [JsonPropertyName("is_core")]
    public bool IsCore { get; set; }

    [JsonPropertyName("on_destroy")]
    public string OnDestroy { get; set; } = string.Empty;
}

public sealed class TelegraphPatternData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name_ko")]
    public string NameKo { get; set; } = string.Empty;

    [JsonPropertyName("telegraph_turns")]
    public int TelegraphTurns { get; set; }

    [JsonPropertyName("damage")]
    public int Damage { get; set; }

    [JsonPropertyName("aoe_shape")]
    public string AoeShape { get; set; } = string.Empty;
}

public sealed class ScenarioData
{
    [JsonPropertyName("scenario_id")]
    public string ScenarioId { get; set; } = string.Empty;

    [JsonPropertyName("protagonist_id")]
    public string ProtagonistId { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<ScenarioEventData> Events { get; set; } = new();
}

public sealed class ScenarioEventData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("conditions")]
    public ScenarioConditionsData Conditions { get; set; } = new();

    [JsonPropertyName("cast")]
    public List<string> Cast { get; set; } = new();

    [JsonPropertyName("choices")]
    public List<ScenarioChoiceData> Choices { get; set; } = new();
}

public sealed class ScenarioConditionsData
{
    [JsonPropertyName("min_year")]
    public int MinYear { get; set; }

    [JsonPropertyName("max_year")]
    public int MaxYear { get; set; }

    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    [JsonPropertyName("required_flag_false")]
    public string? RequiredFlagFalse { get; set; }
}

public sealed class ScenarioChoiceData
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("effects")]
    public List<ScenarioEffectData> Effects { get; set; } = new();
}

public sealed class ScenarioEffectData
{
    [JsonPropertyName("set_flag")]
    public string? SetFlag { get; set; }

    [JsonPropertyName("relation")]
    public TargetDeltaData? Relation { get; set; }

    [JsonPropertyName("cameo_affinity")]
    public TargetDeltaData? CameoAffinity { get; set; }
}

public sealed class TargetDeltaData
{
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("delta")]
    public int Delta { get; set; }
}
