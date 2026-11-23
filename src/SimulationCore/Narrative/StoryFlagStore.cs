namespace EdgeOfTheChart.SimulationCore.Narrative;

public sealed class StoryFlagStore
{
    public Dictionary<string, bool> Flags { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> CameoAffinities { get; set; } = new(StringComparer.Ordinal);

    public void SetFlag(string key, bool value = true)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("스토리 플래그 키는 비어 있을 수 없습니다.", nameof(key));
        }

        Flags[key] = value;
    }

    public bool GetFlag(string key)
    {
        return Flags.TryGetValue(key, out bool value) && value;
    }

    public void AdjustCameoAffinity(string heroId, int delta)
    {
        if (string.IsNullOrWhiteSpace(heroId))
        {
            throw new ArgumentException("주인공 ID는 비어 있을 수 없습니다.", nameof(heroId));
        }

        int current = GetCameoAffinity(heroId);
        CameoAffinities[heroId] = Math.Clamp(current + delta, -100, 100);
    }

    public int GetCameoAffinity(string heroId)
    {
        return CameoAffinities.TryGetValue(heroId, out int value) ? value : 0;
    }
}
