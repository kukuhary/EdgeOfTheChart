using EdgeOfTheChart.SimulationCore.World;

namespace EdgeOfTheChart.SimulationCore.Serialization;

public sealed class SaveSnapshot
{
    public int SaveVersion { get; set; } = WorldState.CurrentSaveVersion;
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    public WorldState World { get; set; } = new();
}
