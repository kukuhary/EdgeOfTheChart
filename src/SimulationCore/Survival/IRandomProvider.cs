namespace EdgeOfTheChart.SimulationCore.Survival;

public interface IRandomProvider
{
    double NextDouble();
    int NextInt(int minInclusive, int maxExclusive);
}

public sealed class SystemRandomProvider : IRandomProvider
{
    private readonly Random _random;

    public SystemRandomProvider(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
    }

    public double NextDouble() => _random.NextDouble();

    public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
