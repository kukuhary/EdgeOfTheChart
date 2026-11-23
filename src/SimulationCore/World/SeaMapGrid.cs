namespace EdgeOfTheChart.SimulationCore.World;

public enum SeaTileType
{
    DeepSea,
    CoastalSea,
    Reef,
    Land,
    PortWater
}

public sealed class SeaMapGrid
{
    private readonly Dictionary<(int X, int Y), string> _portLookup = new();

    public int Width { get; }
    public int Height { get; }
    public bool WrapHorizontal { get; }
    public SeaTileType[] Tiles { get; }
    public IReadOnlyDictionary<(int X, int Y), string> PortLookup => _portLookup;

    public SeaMapGrid(int width, int height, bool wrapHorizontal = false)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "맵 크기는 양수여야 합니다.");
        }

        Width = width;
        Height = height;
        WrapHorizontal = wrapHorizontal;
        Tiles = new SeaTileType[width * height];
    }

    public int NormalizeX(int x)
    {
        if (!WrapHorizontal)
        {
            return x;
        }

        int m = x % Width;
        return m < 0 ? m + Width : m;
    }

    public bool IsInBounds(int x, int y)
    {
        if (y < 0 || y >= Height)
        {
            return false;
        }

        return WrapHorizontal || (x >= 0 && x < Width);
    }

    public SeaTileType GetTile(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            return SeaTileType.Land;
        }

        int nx = NormalizeX(x);
        return Tiles[y * Width + nx];
    }

    public void SetTile(int x, int y, SeaTileType tileType)
    {
        if (!IsInBounds(x, y))
        {
            return;
        }

        int nx = NormalizeX(x);
        Tiles[y * Width + nx] = tileType;
    }

    public bool IsSailable(int x, int y)
    {
        return IsInBounds(x, y) && GetTile(x, y) != SeaTileType.Land;
    }

    public void RegisterPort(int x, int y, string portId)
    {
        if (!IsInBounds(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"항구 '{portId}' 좌표({x},{y})가 맵 범위를 벗어났습니다.");
        }

        int nx = NormalizeX(x);
        SetTile(nx, y, SeaTileType.PortWater);
        _portLookup[(nx, y)] = portId;
    }

    public string? FindAdjacentPortId(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            return null;
        }

        int nx = NormalizeX(x);
        if (_portLookup.TryGetValue((nx, y), out string? exactPort))
        {
            return exactPort;
        }

        for (int dy = -1; dy <= 1; dy++)
        {
            int cy = y + dy;
            if (cy < 0 || cy >= Height)
            {
                continue;
            }

            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                if (!IsInBounds(x + dx, cy))
                {
                    continue;
                }

                int cx = NormalizeX(x + dx);
                if (_portLookup.TryGetValue((cx, cy), out string? nearPort))
                {
                    return nearPort;
                }
            }
        }

        return null;
    }

    public int CountSailableTiles()
    {
        int count = 0;
        for (int i = 0; i < Tiles.Length; i++)
        {
            if (Tiles[i] != SeaTileType.Land)
            {
                count++;
            }
        }

        return count;
    }
}
