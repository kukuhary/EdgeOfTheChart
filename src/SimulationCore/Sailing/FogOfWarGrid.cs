using EdgeOfTheChart.SimulationCore.World;

namespace EdgeOfTheChart.SimulationCore.Sailing;

public sealed class FogOfWarGrid
{
    public int Width { get; }
    public int Height { get; }
    public bool[] Explored { get; }

    public FogOfWarGrid(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "안개 격자 크기는 양수여야 합니다.");
        }

        Width = width;
        Height = height;
        Explored = new bool[width * height];
    }

    public bool IsExplored(int x, int y)
    {
        if (y < 0 || y >= Height || x < 0 || x >= Width)
        {
            return false;
        }

        return Explored[y * Width + x];
    }

    public int RevealAround(int centerX, int centerY, int radius, bool wrapHorizontal = false)
    {
        int newlyRevealed = 0;
        int r2 = radius * radius;

        for (int dy = -radius; dy <= radius; dy++)
        {
            int y = centerY + dy;
            if (y < 0 || y >= Height)
            {
                continue;
            }

            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy > r2)
                {
                    continue;
                }

                int x = centerX + dx;
                if (wrapHorizontal)
                {
                    x %= Width;
                    if (x < 0)
                    {
                        x += Width;
                    }
                }
                else if (x < 0 || x >= Width)
                {
                    continue;
                }

                int index = y * Width + x;
                if (!Explored[index])
                {
                    Explored[index] = true;
                    newlyRevealed++;
                }
            }
        }

        return newlyRevealed;
    }

    public double CalculateExplorationPercent(SeaMapGrid map)
    {
        ArgumentNullException.ThrowIfNull(map);

        int totalSailable = 0;
        int exploredSailable = 0;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!map.IsSailable(x, y))
                {
                    continue;
                }

                totalSailable++;
                if (Explored[y * Width + x])
                {
                    exploredSailable++;
                }
            }
        }

        return totalSailable == 0 ? 0.0 : (exploredSailable * 100.0) / totalSailable;
    }

    public List<int> ToIndices()
    {
        var indices = new List<int>();
        for (int i = 0; i < Explored.Length; i++)
        {
            if (Explored[i])
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    public void LoadFromIndices(IEnumerable<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);

        Array.Clear(Explored);
        foreach (int index in indices)
        {
            if (index >= 0 && index < Explored.Length)
            {
                Explored[index] = true;
            }
        }
    }
}
